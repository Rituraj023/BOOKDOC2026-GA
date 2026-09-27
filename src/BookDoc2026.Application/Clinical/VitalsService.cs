using System.Text.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Clinical;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;

namespace BookDoc2026.Application.Clinical;

public sealed class VitalsService(
    IEncounterRepository repository,
    ICurrentActor actor,
    IPublicIdCodec publicIds,
    IClock clock,
    ICorrelationContext correlation) : IVitalsService
{
    public async Task<VitalSignsResponse> RecordVitalsAsync(
        long branchId,
        string patientId,
        RecordVitalSignsRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.EncounterDraftsManage, branchId, cancellationToken);
        var decodedPatientId = publicIds.Decode(PublicIdKind.Patient, patientId, branch.TenantId);
        var decodedBookingId = publicIds.DecodeOptional(PublicIdKind.Booking, request.BookingId, branch.TenantId);
        var decodedEncounterId = publicIds.DecodeOptional(PublicIdKind.Encounter, request.ClinicalEncounterId, branch.TenantId);

        var now = clock.UtcNow;
        var vitals = PatientVitalSigns.Record(
            branch.TenantId,
            branchId,
            decodedPatientId,
            decodedBookingId,
            decodedEncounterId,
            request.SystolicBp,
            request.DiastolicBp,
            request.PulseBpm,
            request.TemperatureF,
            request.SpO2Percent,
            request.RespiratoryRate,
            request.WeightKg,
            request.HeightCm,
            request.BloodGlucoseMgDl,
            actor.ActorId > 0 ? actor.ActorId.ToString() : null,
            request.ClinicalNotes,
            now);

        var audit = Audit(branch, vitals, "Vitals.Recorded", now);
        await repository.AddVitalSignsAsync(vitals, audit, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Map(vitals);
    }

    public async Task<VitalSignsResponse?> GetLatestVitalsAsync(long branchId, string patientId, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.EncountersView, branchId, cancellationToken);
        var decodedPatientId = publicIds.Decode(PublicIdKind.Patient, patientId, branch.TenantId);
        var vitals = await repository.GetLatestVitalSignsAsync(branchId, decodedPatientId, cancellationToken);
        return vitals is null ? null : Map(vitals);
    }

    public async Task<IReadOnlyCollection<VitalSignsResponse>> ListVitalsHistoryAsync(long branchId, string patientId, int take, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.EncountersView, branchId, cancellationToken);
        var decodedPatientId = publicIds.Decode(PublicIdKind.Patient, patientId, branch.TenantId);
        var list = await repository.ListVitalSignsAsync(branchId, decodedPatientId, Math.Clamp(take, 1, 100), cancellationToken);
        return list.Select(Map).ToArray();
    }

    public async Task<VitalSignsResponse?> GetVitalsByBookingAsync(long branchId, string bookingId, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.EncountersView, branchId, cancellationToken);
        var decodedBookingId = publicIds.Decode(PublicIdKind.Booking, bookingId, branch.TenantId);
        var vitals = await repository.GetVitalSignsByBookingAsync(branchId, decodedBookingId, cancellationToken);
        return vitals is null ? null : Map(vitals);
    }

    public async Task<VitalSignsResponse?> GetVitalsByEncounterAsync(long branchId, string encounterId, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.EncountersView, branchId, cancellationToken);
        var decodedEncounterId = publicIds.Decode(PublicIdKind.Encounter, encounterId, branch.TenantId);
        var vitals = await repository.GetVitalSignsByEncounterAsync(branchId, decodedEncounterId, cancellationToken);
        return vitals is null ? null : Map(vitals);
    }

    private async Task<Branch> RequireBranchAsync(string permission, long branchId, CancellationToken cancellationToken)
    {
        if (actor.TenantId is null || !actor.BranchIds.Contains(branchId) || !actor.HasPermission(permission))
            throw new ForbiddenException("The actor is not authorized for this clinical operation.");
        return await repository.GetBranchAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch was not found in the current tenant scope.");
    }

    private AuditEvent Audit(Branch branch, PatientVitalSigns vitals, string action, DateTimeOffset now) =>
        AuditEvent.Record(
            branch.TenantId,
            branch.Id,
            actor.ActorId,
            action,
            nameof(PatientVitalSigns),
            vitals.Id,
            JsonSerializer.Serialize(new
            {
                vitals.PatientId,
                vitals.SystolicBp,
                vitals.DiastolicBp,
                vitals.PulseBpm,
                vitals.TemperatureF,
                vitals.SpO2Percent,
                vitals.WeightKg,
                vitals.HeightCm,
                vitals.Bmi,
                vitals.BloodGlucoseMgDl
            }),
            correlation.CorrelationId,
            now);

    private VitalSignsResponse Map(PatientVitalSigns item) => new(
        publicIds.Encode(PublicIdKind.PatientVitalSigns, item.Id, item.TenantId),
        publicIds.Encode(PublicIdKind.Patient, item.PatientId, item.TenantId),
        publicIds.EncodeOptional(PublicIdKind.Booking, item.BookingId, item.TenantId),
        publicIds.EncodeOptional(PublicIdKind.Encounter, item.ClinicalEncounterId, item.TenantId),
        item.SystolicBp,
        item.DiastolicBp,
        item.PulseBpm,
        item.TemperatureF,
        item.SpO2Percent,
        item.RespiratoryRate,
        item.WeightKg,
        item.HeightCm,
        item.Bmi,
        item.BloodGlucoseMgDl,
        item.RecordedByActorId,
        item.RecordedUtc,
        item.ClinicalNotes,
        item.Version);
}
