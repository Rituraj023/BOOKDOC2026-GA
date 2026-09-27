namespace BookDoc2026.Contracts.Clinical;

public sealed record RecordVitalSignsRequest(
    string? BookingId,
    string? ClinicalEncounterId,
    int? SystolicBp,
    int? DiastolicBp,
    int? PulseBpm,
    decimal? TemperatureF,
    int? SpO2Percent,
    int? RespiratoryRate,
    decimal? WeightKg,
    decimal? HeightCm,
    decimal? BloodGlucoseMgDl,
    string? ClinicalNotes);

public sealed record VitalSignsResponse(
    string Id,
    string PatientId,
    string? BookingId,
    string? ClinicalEncounterId,
    int? SystolicBp,
    int? DiastolicBp,
    int? PulseBpm,
    decimal? TemperatureF,
    int? SpO2Percent,
    int? RespiratoryRate,
    decimal? WeightKg,
    decimal? HeightCm,
    decimal? Bmi,
    decimal? BloodGlucoseMgDl,
    string? RecordedByActorId,
    DateTimeOffset RecordedUtc,
    string? ClinicalNotes,
    long Version);

public sealed record PrescriptionItemDto(
    string DrugName,
    string DosageForm,
    string Strength,
    string Frequency,
    int DurationValue,
    string DurationUnit,
    string Timing,
    string? SpecialInstructions);

public sealed record FormattedPrescriptionSlip(
    string ClinicName,
    string ClinicAddress,
    string ClinicPhone,
    string PatientFullName,
    string? PatientGender,
    int? PatientAgeYears,
    string PatientPhone,
    string DoctorFullName,
    string? DoctorRegistrationNumber,
    string? DoctorQualifications,
    string? DoctorSpecialty,
    DateTimeOffset ConsultationDate,
    string ChiefComplaint,
    string? Diagnosis,
    VitalSignsResponse? Vitals,
    IReadOnlyList<PrescriptionItemDto> Medications,
    string? GeneralAdvice,
    DateTimeOffset? FollowUpDate);
