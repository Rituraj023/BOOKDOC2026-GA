using BookDoc2026.Contracts.Clinical;

namespace BookDoc2026.Application.Clinical;

public interface IVitalsService
{
    Task<VitalSignsResponse> RecordVitalsAsync(long branchId, string patientId, RecordVitalSignsRequest request, CancellationToken cancellationToken);
    Task<VitalSignsResponse?> GetLatestVitalsAsync(long branchId, string patientId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<VitalSignsResponse>> ListVitalsHistoryAsync(long branchId, string patientId, int take, CancellationToken cancellationToken);
    Task<VitalSignsResponse?> GetVitalsByBookingAsync(long branchId, string bookingId, CancellationToken cancellationToken);
    Task<VitalSignsResponse?> GetVitalsByEncounterAsync(long branchId, string encounterId, CancellationToken cancellationToken);
}
