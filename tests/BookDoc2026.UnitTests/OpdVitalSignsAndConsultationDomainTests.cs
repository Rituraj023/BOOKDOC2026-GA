using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Common;

namespace BookDoc2026.UnitTests;

public sealed class OpdVitalSignsAndConsultationDomainTests
{
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void RecordVitals_WithWeightAndHeight_CalculatesBmiCorrectly()
    {
        // 70 kg, 175 cm -> 70 / (1.75 * 1.75) = 22.86
        var vitals = PatientVitalSigns.Record(
            tenantId: 1,
            branchId: 10,
            patientId: 100,
            bookingId: null,
            clinicalEncounterId: null,
            systolicBp: 120,
            diastolicBp: 80,
            pulseBpm: 72,
            temperatureF: 98.6m,
            spO2Percent: 99,
            respiratoryRate: 16,
            weightKg: 70m,
            heightCm: 175m,
            bloodGlucoseMgDl: 95m,
            recordedByActorId: "nurse-1",
            clinicalNotes: "Normal healthy vitals",
            now: _now);

        Assert.NotNull(vitals);
        Assert.Equal(22.86m, vitals.Bmi);
        Assert.Equal(120, vitals.SystolicBp);
        Assert.Equal(80, vitals.DiastolicBp);
        Assert.Equal(72, vitals.PulseBpm);
        Assert.Equal(98.6m, vitals.TemperatureF);
        Assert.Equal(99, vitals.SpO2Percent);
        Assert.Equal("nurse-1", vitals.RecordedByActorId);
    }

    [Theory]
    [InlineData(40)]
    [InlineData(350)]
    public void RecordVitals_InvalidSystolicBp_ThrowsDomainRuleException(int invalidSystolic)
    {
        Assert.Throws<DomainRuleException>(() =>
            PatientVitalSigns.Record(
                tenantId: 1,
                branchId: 10,
                patientId: 100,
                bookingId: null,
                clinicalEncounterId: null,
                systolicBp: invalidSystolic,
                diastolicBp: 80,
                pulseBpm: 72,
                temperatureF: 98.6m,
                spO2Percent: 99,
                respiratoryRate: 16,
                weightKg: 70m,
                heightCm: 175m,
                bloodGlucoseMgDl: null,
                recordedByActorId: null,
                clinicalNotes: null,
                now: _now));
    }

    [Theory]
    [InlineData(20)]
    [InlineData(300)]
    public void RecordVitals_InvalidPulse_ThrowsDomainRuleException(int invalidPulse)
    {
        Assert.Throws<DomainRuleException>(() =>
            PatientVitalSigns.Record(
                tenantId: 1,
                branchId: 10,
                patientId: 100,
                bookingId: null,
                clinicalEncounterId: null,
                systolicBp: 120,
                diastolicBp: 80,
                pulseBpm: invalidPulse,
                temperatureF: 98.6m,
                spO2Percent: 99,
                respiratoryRate: 16,
                weightKg: 70m,
                heightCm: 175m,
                bloodGlucoseMgDl: null,
                recordedByActorId: null,
                clinicalNotes: null,
                now: _now));
    }

    [Theory]
    [InlineData(40)]
    [InlineData(110)]
    public void RecordVitals_InvalidSpO2_ThrowsDomainRuleException(int invalidSpO2)
    {
        Assert.Throws<DomainRuleException>(() =>
            PatientVitalSigns.Record(
                tenantId: 1,
                branchId: 10,
                patientId: 100,
                bookingId: null,
                clinicalEncounterId: null,
                systolicBp: 120,
                diastolicBp: 80,
                pulseBpm: 75,
                temperatureF: 98.6m,
                spO2Percent: invalidSpO2,
                respiratoryRate: 16,
                weightKg: 70m,
                heightCm: 175m,
                bloodGlucoseMgDl: null,
                recordedByActorId: null,
                clinicalNotes: null,
                now: _now));
    }

    [Fact]
    public void LinkEncounter_UpdatesEncounterIdAndIncrementsVersion()
    {
        var vitals = PatientVitalSigns.Record(
            tenantId: 1,
            branchId: 10,
            patientId: 100,
            bookingId: null,
            clinicalEncounterId: null,
            systolicBp: 120,
            diastolicBp: 80,
            pulseBpm: 72,
            temperatureF: 98.6m,
            spO2Percent: 99,
            respiratoryRate: 16,
            weightKg: 70m,
            heightCm: 175m,
            bloodGlucoseMgDl: null,
            recordedByActorId: "nurse",
            clinicalNotes: null,
            now: _now);

        var initialVersion = vitals.Version;
        vitals.LinkEncounter(555, _now.AddMinutes(15));

        Assert.Equal(555, vitals.ClinicalEncounterId);
        Assert.Equal(initialVersion + 1, vitals.Version);
    }
}
