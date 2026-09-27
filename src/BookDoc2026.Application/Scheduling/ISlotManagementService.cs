using BookDoc2026.Contracts.Scheduling;

namespace BookDoc2026.Application.Scheduling;

public interface ISlotManagementService
{
    Task<IReadOnlyCollection<DoctorSlotResponse>> GenerateSlotsAsync(GenerateDoctorSlotsRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DoctorSlotResponse>> GetSlotsAsync(string branchId, string? practitionerId, string? serviceId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DoctorSlotResponse>> GetAvailableSlotsAsync(string branchId, string? practitionerId, string? serviceId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken);
    Task<DoctorSlotResponse> BlockSlotAsync(string branchId, string slotId, BlockDoctorSlotRequest request, CancellationToken cancellationToken);
    Task<DoctorSlotResponse> UnblockSlotAsync(string branchId, string slotId, CancellationToken cancellationToken);
    Task<DoctorSlotResponse> CancelSlotAsync(string branchId, string slotId, CancellationToken cancellationToken);
    Task<DirectSlotBookingConfirmationResponse> BookSlotDirectAsync(string branchId, BookSlotDirectRequest request, CancellationToken cancellationToken);
}
