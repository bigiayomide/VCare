using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using Patients.Application.Services.Interfaces;
using Patients.Domain.Entities;
using VCare.Modules.Patients.Application.Services;
using VCare.Modules.Patients.Domain.Entities;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.Patients.Infrastructure.Persistence.Configurations;

internal sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients", PatientsDbContext.Schema);
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Firstname).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Lastname).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Gender).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Address).HasMaxLength(200).IsRequired();
        builder.Property(p => p.DateOfBirth).IsRequired();
        builder.HasMany(p => p.Documents)
            .WithOne()
            .HasForeignKey(d => d.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.CareHomeId)
            .HasConversion(id => id.Value, value => new CareHomeId(value))
            .IsRequired();
        builder.HasIndex(p => p.CareHomeId);

        builder.Property(p => p.AllergyStatus).HasConversion<string>().HasMaxLength(30);
        builder.HasMany(p => p.Allergies).WithOne().HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Absences).WithOne().HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Cascade);

        // Domain events are behaviour, not persisted state.
        builder.Ignore(p => p.DomainEvents);
    }
}

internal sealed class PatientAllergyConfiguration : IEntityTypeConfiguration<PatientAllergy>
{
    public void Configure(EntityTypeBuilder<PatientAllergy> builder)
    {
        builder.ToTable("PatientAllergies", PatientsDbContext.Schema);
        builder.HasKey(a => a.Id);
        // The id is set in code when the row is added to its parent; without this EF
        // takes a set key for an existing row and updates nothing instead of inserting.
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Substance).HasMaxLength(PatientAllergy.SubstanceMaxLength).IsRequired();
        builder.Property(a => a.Reaction).HasMaxLength(PatientAllergy.TextMaxLength);
        builder.Property(a => a.RemovalReason).HasMaxLength(PatientAllergy.TextMaxLength);
        builder.Property(a => a.Severity).HasConversion<string>().HasMaxLength(30);
        builder.Ignore(a => a.IsActive);
        builder.HasIndex(a => a.PatientId);
    }
}

internal sealed class PatientAbsenceConfiguration : IEntityTypeConfiguration<PatientAbsence>
{
    public void Configure(EntityTypeBuilder<PatientAbsence> builder)
    {
        builder.ToTable("PatientAbsences", PatientsDbContext.Schema);
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Kind).HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.Note).HasMaxLength(PatientAbsence.NoteMaxLength);
        builder.Ignore(a => a.IsOpen);
        // The missed-dose recorder asks "who was away between these times".
        builder.HasIndex(a => new { a.PatientId, a.StartedAt });
    }
}

internal sealed class PatientDocumentConfiguration : IEntityTypeConfiguration<PatientDocument>
{
    public void Configure(EntityTypeBuilder<PatientDocument> builder)
    {
        builder.ToTable("PatientDocuments", PatientsDbContext.Schema);
        builder.HasKey(d => d.Id);
        // Set in code (Guid.NewGuid()) and added to an already-loaded patient: EF read
        // the set key as an existing row and every upload failed with 0 rows updated.
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.FileName).HasMaxLength(200).IsRequired();
        builder.Property(d => d.StorageKeyName).HasMaxLength(200).IsRequired();
        builder.Property(d => d.PatientId).IsRequired();
        builder.HasIndex(d => d.PatientId);
        builder.ToTable("PatientDocuments", PatientsDbContext.Schema);
    }
}
