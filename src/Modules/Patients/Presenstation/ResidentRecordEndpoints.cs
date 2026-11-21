using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using VCare.Modules.Patients.Application.Dtos;
using VCare.Modules.Patients.Application.Services;
using VCare.SharedKernel.Results;

namespace Patients.Presenstation
{
    public static class ResidentRecordEndpoints
    {
        public static void AddResidentRecordEndpoints(this IEndpointRouteBuilder app)
        {
            var patient = app.MapGroup("/api/patient/{patientId:Guid}").WithTags("Patients").RequireAuthorization();

            // Any carer can record an allergy they are told about: a missing allergy
            // is the dangerous mistake. Removing one is for an admin.
            patient.MapGet("/allergies", GetAllergies).RequireAuthorization("Admin_Or_Carer");
            patient.MapPost("/allergies", RecordAllergy).RequireAuthorization("Admin_Or_Carer");
            patient.MapPost("/allergies/none", RecordNoKnownAllergies).RequireAuthorization("Admin_Or_Carer");
            patient.MapPost("/allergies/{allergyId:Guid}/remove", RemoveAllergy).RequireAuthorization("Admin_Role");

            // In hospital or on leave: doses due meanwhile are not counted as missed.
            patient.MapGet("/absences", GetAbsences).RequireAuthorization("Admin_Or_Carer");
            patient.MapPost("/absences", StartAbsence).RequireAuthorization("Admin_Or_Carer");
            patient.MapPost("/absences/end", EndAbsence).RequireAuthorization("Admin_Or_Carer");
        }

        private static async Task<IResult> GetAllergies(Guid patientId, ResidentRecordService records, CancellationToken token)
        {
            var result = await records.GetAllergiesAsync(patientId, token);
            return result.IsFailure ? result.ToProblem() : TypedResults.Ok(result.Value);
        }

        private static async Task<IResult> RecordAllergy(Guid patientId, RecordAllergyRequest request,
            ResidentRecordService records, CancellationToken token)
        {
            var result = await records.RecordAllergyAsync(patientId, request, token);
            return result.IsFailure ? result.ToProblem() : TypedResults.Ok(new { AllergyId = result.Value });
        }

        private static async Task<IResult> RecordNoKnownAllergies(Guid patientId, ResidentRecordService records, CancellationToken token)
        {
            var result = await records.RecordNoKnownAllergiesAsync(patientId, token);
            return result.IsFailure ? result.ToProblem() : TypedResults.NoContent();
        }

        private static async Task<IResult> RemoveAllergy(Guid patientId, Guid allergyId, RemoveAllergyRequest request,
            ResidentRecordService records, CancellationToken token)
        {
            var result = await records.RemoveAllergyAsync(patientId, allergyId, request, token);
            return result.IsFailure ? result.ToProblem() : TypedResults.NoContent();
        }

        private static async Task<IResult> GetAbsences(Guid patientId, ResidentRecordService records, CancellationToken token)
        {
            var result = await records.GetAbsencesAsync(patientId, token);
            return result.IsFailure ? result.ToProblem() : TypedResults.Ok(result.Value);
        }

        private static async Task<IResult> StartAbsence(Guid patientId, StartAbsenceRequest request,
            ResidentRecordService records, CancellationToken token)
        {
            var result = await records.StartAbsenceAsync(patientId, request, token);
            return result.IsFailure ? result.ToProblem() : TypedResults.Ok(new { AbsenceId = result.Value });
        }

        private static async Task<IResult> EndAbsence(Guid patientId, EndAbsenceRequest request,
            ResidentRecordService records, CancellationToken token)
        {
            var result = await records.EndAbsenceAsync(patientId, request, token);
            return result.IsFailure ? result.ToProblem() : TypedResults.NoContent();
        }
    }
}
