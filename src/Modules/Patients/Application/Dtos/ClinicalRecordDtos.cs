using VCare.Modules.Patients.Domain.Entities;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.Patients.Application.Dtos;

/// <summary>Severity is Unknown, Mild, Moderate or Severe.</summary>
public sealed record RecordAllergyRequest(string Substance, string? Reaction, string? Severity);

public sealed record RemoveAllergyRequest(string Reason);

public sealed record AllergyResponse(Guid Id, string Substance, string? Reaction, AllergySeverity Severity,
    DateTime RecordedAt, Guid RecordedByStaffId, DateTime? RemovedAt, string? RemovalReason);

public sealed record AllergiesResponse(AllergyStatus Status, IReadOnlyList<AllergyResponse> Allergies);

/// <summary>
/// Kind is Hospital or Leave. StartedAt is the time in the home; it defaults to
/// now, for a resident leaving as it is recorded.
/// </summary>
public sealed record StartAbsenceRequest(string Kind, DateTime? StartedAt, string? Note);

public sealed record EndAbsenceRequest(DateTime? EndedAt);

public sealed record AbsenceResponse(Guid Id, AbsenceKind Kind, DateTime StartedAt, DateTime? EndedAt, string? Note,
    Guid RecordedByStaffId, Guid? EndedByStaffId);
