using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Clinical;

public sealed class PatientVitalSigns : TenantScopedEntity
{
    private PatientVitalSigns() { }

    public long BranchId { get; private set; }
    public long PatientId { get; private set; }
    public long? BookingId { get; private set; }
    public long? ClinicalEncounterId { get; private set; }

    public int? SystolicBp { get; private set; }
    public int? DiastolicBp { get; private set; }
    public int? PulseBpm { get; private set; }
    public decimal? TemperatureF { get; private set; }
    public int? SpO2Percent { get; private set; }
    public int? RespiratoryRate { get; private set; }
    public decimal? WeightKg { get; private set; }
    public decimal? HeightCm { get; private set; }
    public decimal? Bmi { get; private set; }
    public decimal? BloodGlucoseMgDl { get; private set; }

    public string? RecordedByActorId { get; private set; }
    public DateTimeOffset RecordedUtc { get; private set; }
    public string? ClinicalNotes { get; private set; }
    public long Version { get; private set; } = 1;

    public static PatientVitalSigns Record(
        long tenantId,
        long branchId,
        long patientId,
        long? bookingId,
        long? clinicalEncounterId,
        int? systolicBp,
        int? diastolicBp,
        int? pulseBpm,
        decimal? temperatureF,
        int? spO2Percent,
        int? respiratoryRate,
        decimal? weightKg,
        decimal? heightCm,
        decimal? bloodGlucoseMgDl,
        string? recordedByActorId,
        string? clinicalNotes,
        DateTimeOffset now)
    {
        if (tenantId <= 0) throw new DomainRuleException("Tenant ID must be greater than zero.");
        if (branchId <= 0) throw new DomainRuleException("Branch ID must be greater than zero.");
        if (patientId <= 0) throw new DomainRuleException("Patient ID must be greater than zero.");

        if (systolicBp.HasValue && (systolicBp.Value < 50 || systolicBp.Value > 300))
            throw new DomainRuleException("Systolic BP must be between 50 and 300 mmHg.");

        if (diastolicBp.HasValue && (diastolicBp.Value < 30 || diastolicBp.Value > 200))
            throw new DomainRuleException("Diastolic BP must be between 30 and 200 mmHg.");

        if (pulseBpm.HasValue && (pulseBpm.Value < 30 || pulseBpm.Value > 250))
            throw new DomainRuleException("Pulse must be between 30 and 250 bpm.");

        if (temperatureF.HasValue && (temperatureF.Value < 80m || temperatureF.Value > 115m))
            throw new DomainRuleException("Temperature must be between 80.0°F and 115.0°F.");

        if (spO2Percent.HasValue && (spO2Percent.Value < 50 || spO2Percent.Value > 100))
            throw new DomainRuleException("SpO2 must be between 50% and 100%.");

        if (respiratoryRate.HasValue && (respiratoryRate.Value < 5 || respiratoryRate.Value > 80))
            throw new DomainRuleException("Respiratory rate must be between 5 and 80 breaths/min.");

        if (weightKg.HasValue && (weightKg.Value <= 0m || weightKg.Value > 500m))
            throw new DomainRuleException("Weight must be between 0.1 and 500.0 kg.");

        if (heightCm.HasValue && (heightCm.Value <= 0m || heightCm.Value > 300m))
            throw new DomainRuleException("Height must be between 1.0 and 300.0 cm.");

        if (bloodGlucoseMgDl.HasValue && (bloodGlucoseMgDl.Value < 10m || bloodGlucoseMgDl.Value > 1000m))
            throw new DomainRuleException("Blood glucose must be between 10 and 1000 mg/dL.");

        decimal? calculatedBmi = null;
        if (weightKg.HasValue && weightKg.Value > 0m && heightCm.HasValue && heightCm.Value > 0m)
        {
            var heightMeters = heightCm.Value / 100m;
            calculatedBmi = Math.Round(weightKg.Value / (heightMeters * heightMeters), 2, MidpointRounding.AwayFromZero);
        }

        var vitals = new PatientVitalSigns
        {
            TenantId = tenantId,
            BranchId = branchId,
            PatientId = patientId,
            BookingId = bookingId,
            ClinicalEncounterId = clinicalEncounterId,
            SystolicBp = systolicBp,
            DiastolicBp = diastolicBp,
            PulseBpm = pulseBpm,
            TemperatureF = temperatureF,
            SpO2Percent = spO2Percent,
            RespiratoryRate = respiratoryRate,
            WeightKg = weightKg,
            HeightCm = heightCm,
            Bmi = calculatedBmi,
            BloodGlucoseMgDl = bloodGlucoseMgDl,
            RecordedByActorId = string.IsNullOrWhiteSpace(recordedByActorId) ? null : recordedByActorId.Trim(),
            RecordedUtc = now,
            ClinicalNotes = string.IsNullOrWhiteSpace(clinicalNotes) ? null : clinicalNotes.Trim(),
            Version = 1
        };

        vitals.StampCreated(now);
        return vitals;
    }

    public void LinkEncounter(long encounterId, DateTimeOffset now)
    {
        if (encounterId <= 0) throw new DomainRuleException("Encounter ID must be greater than zero.");
        ClinicalEncounterId = encounterId;
        Version++;
        StampModified(now);
    }
}
