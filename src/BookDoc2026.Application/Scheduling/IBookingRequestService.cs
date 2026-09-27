using BookDoc2026.Contracts.Scheduling;

namespace BookDoc2026.Application.Scheduling;

public interface IBookingRequestService
{
    Task<BookingRequestResponse> SubmitRequestAsync(SubmitBookingRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<BookingRequestResponse>> GetRequestsAsync(string branchId, string? status, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken);
    Task<BookingRequestResponse> GetRequestByIdAsync(string branchId, string requestId, CancellationToken cancellationToken);
    Task<BookingRequestResponse> ApproveRequestAsync(string branchId, string requestId, ApproveBookingRequest command, CancellationToken cancellationToken);
    Task<BookingRequestResponse> DeclineRequestAsync(string branchId, string requestId, DeclineBookingRequest command, CancellationToken cancellationToken);
    Task<BookingRequestResponse> RescheduleRequestAsync(string branchId, string requestId, RescheduleBookingRequestNotice command, CancellationToken cancellationToken);
}
