namespace VCare.Modules.Patients.Application.Dtos;

// AllergyStatus is on the summary so whoever is looking at the resident sees
// it without asking: Unknown is not the same as no known allergies.
public sealed record PatientResponse(Guid Id, string FullName, DateOnly DateOfBirth,
    VCare.Modules.Patients.Domain.Entities.AllergyStatus AllergyStatus = default);
