using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace VCare.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public class AuditTrailTests(SqlServerFixture sql)
{
    private static async Task<List<JsonElement>> TrailAsync(HttpClient api, Guid resident) =>
        [.. (await api.GetFromJsonAsync<JsonElement>($"/api/patient/{resident}/audit")).EnumerateArray()];

    [Fact]
    public async Task A_change_to_a_resident_record_is_in_their_trail_with_who_made_it_and_what_changed()
    {
        var home = await sql.Homes.GetAsync("Oak House");
        var resident = await home.RegisterResidentAsync("Audrey");

        var recorded = await home.Admin.PostAsJsonAsync($"/api/patient/{resident}/allergies",
            new { substance = "Penicillin", reaction = "Rash", severity = "Severe" });
        Assert.Equal(HttpStatusCode.OK, recorded.StatusCode);

        var trail = await TrailAsync(home.Admin, resident);
        Assert.Contains(trail, e => e.GetProperty("action").GetString() == "Created" && e.GetProperty("entityType").GetString() == "Patient");
        var allergy = Assert.Single(trail, e => e.GetProperty("entityType").GetString() == "PatientAllergy");
        Assert.Equal("Created", allergy.GetProperty("action").GetString());
        Assert.Equal(home.AdminStaffId, allergy.GetProperty("staffId").GetGuid());
        Assert.Equal("Penicillin", allergy.GetProperty("changes").GetProperty("Substance").GetString());
        // The status moving from Unknown to HasAllergies is on the resident's own entry.
        var status = Assert.Single(trail, e => e.GetProperty("action").GetString() == "Updated" && e.GetProperty("entityType").GetString() == "Patient");
        var change = status.GetProperty("changes").GetProperty("AllergyStatus");
        Assert.Equal("Unknown", change.GetProperty("from").GetString());
        Assert.Equal("HasAllergies", change.GetProperty("to").GetString());
    }

    [Fact]
    public async Task Reading_part_of_a_resident_record_is_in_their_trail()
    {
        var home = await sql.Homes.GetAsync("Oak House");
        var resident = await home.RegisterResidentAsync("Vera");

        (await home.Admin.GetAsync($"/api/patient/{resident}/allergies")).EnsureSuccessStatusCode();

        var trail = await TrailAsync(home.Admin, resident);
        var view = Assert.Single(trail, e => e.GetProperty("action").GetString() == "Viewed"
            && e.GetProperty("detail").GetString() == "GET /api/patient/{patientId:Guid}/allergies");
        Assert.Equal(home.AdminStaffId, view.GetProperty("staffId").GetGuid());
    }

    [Fact]
    public async Task Another_home_sees_nothing_of_a_residents_trail()
    {
        var home = await sql.Homes.GetAsync("Oak House");
        var elsewhere = await sql.Homes.GetAsync("Elm House");
        var resident = await home.RegisterResidentAsync("Iris");

        var trail = await TrailAsync(elsewhere.Admin, resident);

        Assert.Empty(trail);
    }
}

[Collection(SqlServerCollection.Name)]
public class CarePlanVersionTests(SqlServerFixture sql)
{
    [Fact]
    public async Task A_second_edit_to_the_same_version_is_refused_and_both_versions_are_kept()
    {
        var home = await sql.Homes.GetAsync("Oak House");
        var resident = await home.RegisterResidentAsync("Edna");
        var api = home.Admin;
        Assert.Equal(HttpStatusCode.Created, (await api.PostAsJsonAsync($"/api/patient/{resident}/careplan", new
        {
            goal = "Stay mobile", diagnoses = new[] { new { name = "Falls risk", goalDescription = "No falls", intervention = "Frame at all times" } },
        })).StatusCode);
        var plan = await api.GetFromJsonAsync<JsonElement>($"/api/patient/{resident}/careplan");
        var planId = plan.GetProperty("id").GetGuid();
        Assert.Equal(1, plan.GetProperty("version").GetInt32());

        // Two nurses opened version 1; the first saves, then the second.
        var first = await api.PutAsJsonAsync($"/api/patient/{resident}/careplan/{planId}", new { goal = "Walk to the dining room daily", version = 1 });
        var second = await api.PutAsJsonAsync($"/api/patient/{resident}/careplan/{planId}", new { goal = "Use a wheelchair outdoors", version = 1 });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("CarePlans.EditConflict", (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        plan = await api.GetFromJsonAsync<JsonElement>($"/api/patient/{resident}/careplan");
        Assert.Equal("Walk to the dining room daily", plan.GetProperty("goal").GetString());

        var versions = (await api.GetFromJsonAsync<JsonElement>($"/api/patient/{resident}/careplan/versions")).EnumerateArray()
            .Select(v => v.GetProperty("number").GetInt32()).ToList();
        Assert.Equal([1, 2], versions);
        var original = await api.GetFromJsonAsync<JsonElement>($"/api/patient/{resident}/careplan/versions/1");
        Assert.Equal("Stay mobile", original.GetProperty("goal").GetString());
        Assert.Equal("Falls risk", original.GetProperty("diagnoses")[0].GetProperty("name").GetString());
    }
}
