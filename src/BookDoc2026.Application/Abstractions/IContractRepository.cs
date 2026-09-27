using BookDoc2026.Domain.Contracts;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Scheduling;

namespace BookDoc2026.Application.Abstractions;

public sealed record ContractAggregate(
    ContractAgreement Agreement,
    IReadOnlyCollection<ContractEntitlement> Entitlements);

public sealed record EntitlementReservationAggregate(
    EntitlementReservation Reservation,
    ContractEntitlement Entitlement);

public interface IContractRepository
{
    Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken);
    Task<bool> PatientExistsAsync(long patientId, CancellationToken cancellationToken);
    Task<bool> ServiceExistsAsync(long serviceId, CancellationToken cancellationToken);
    Task<bool> ResourceCategoryExistsAsync(long resourceCategoryId, CancellationToken cancellationToken);
    Task<bool> ContractNumberExistsAsync(long branchId, string contractNumber, CancellationToken cancellationToken);
    Task<Booking?> GetBookingAsync(long branchId, long bookingId, CancellationToken cancellationToken);
    Task<bool> BookingUsesResourceCategoryAsync(long bookingId, long resourceCategoryId, CancellationToken cancellationToken);
    Task AddContractAsync(ContractAgreement agreement, IReadOnlyCollection<ContractEntitlement> entitlements,
        AuditEvent auditEvent, CancellationToken cancellationToken);
    Task<ContractAggregate?> GetContractAsync(long branchId, long contractId, bool tracked, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ContractAggregate>> GetPatientActiveContractsAsync(long branchId, long patientId, CancellationToken cancellationToken);
    Task<EntitlementReservationAggregate?> GetReservationAsync(long branchId, long reservationId, bool tracked,
        CancellationToken cancellationToken);
    Task<EntitlementReservationAggregate?> GetActiveReservationForBookingAsync(long branchId, long bookingId, bool tracked,
        CancellationToken cancellationToken);
    Task<EntitlementReservationAggregate?> GetReservationByRequestAsync(long branchId, Guid requestId,
        CancellationToken cancellationToken);
    Task AddReservationAsync(EntitlementReservation reservation, AuditEvent auditEvent, CancellationToken cancellationToken);
    Task AddAuditAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
