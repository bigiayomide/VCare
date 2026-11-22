using VCare.Modules.Patients.Domain.Entities;

namespace VCare.Modules.Patients.Application.Dtos;

// AllergyStatus is on the summary so whoever is looking at the resident sees
// it without asking: Unknown is not the same as no known allergies. Residency
// says whether they still live here; LeftAt is when they were discharged or died.
public sealed record PatientResponse(Guid Id, string FullName, DateOnly DateOfBirth,
    AllergyStatus AllergyStatus = default,
    string? NhsNumber = null, string? Room = null,
    string? GpName = null, string? GpPractice = null, string? GpPhone = null,
    DateOnly? AdmittedOn = null, ResidencyStatus Residency = default, DateTime? LeftAt = null,
    decimal? LatestWeightKg = null, DateTime? LatestWeightMeasuredAt = null)
{
    internal static PatientResponse From(Patient p) => new(p.Id.Value, p.FullName, p.DateOfBirth, p.AllergyStatus,
        p.NhsNumber, p.Room, p.GpName, p.GpPractice, p.GpPhone, p.AdmittedOn, p.Residency, p.LeftAt,
        p.LatestWeight?.Kilograms, p.LatestWeight?.MeasuredAt);
}
