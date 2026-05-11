using turf_management_system.DTOs.Common;
using turf_management_system.DTOs.Turf;

namespace turf_management_system.Services.Interfaces
{
    public interface ITurfService
    {
        Task<ApiResponse<TurfResponseDto>> CreateTurfAsync(CreateTurfDto dto, int ownerId);
        Task<ApiResponse<TurfResponseDto>> UpdateTurfAsync(Guid turfId, UpdateTurfDto dto, int ownerId);
        Task<ApiResponse<bool>> DeleteTurfAsync(Guid turfId, int requesterId, string requesterRole);
        Task<ApiResponse<PagedResultDto<TurfListItemDto>>> GetAllTurfsPagedAsync(int pageNumber, int pageSize, string? search, string? city, string? sportType, bool? isApproved);
        Task<ApiResponse<IEnumerable<TurfListItemDto>>> GetMyTurfsAsync(int ownerId);
        Task<ApiResponse<TurfResponseDto>> GetTurfByIdAsync(Guid turfId);
        Task<ApiResponse<bool>> ApproveTurfAsync(Guid turfId);
        Task<ApiResponse<bool>> RejectTurfAsync(Guid turfId, string reason);
        Task<ApiResponse<bool>> UploadTurfImageAsync(Guid turfId, IFormFile image, bool isMain, int ownerId);
        Task<ApiResponse<bool>> DeleteTurfImageAsync(Guid imageId, int ownerId);
        Task<ApiResponse<TurfSlotDto>> AddSlotAsync(Guid turfId, CreateTurfSlotDto dto, int ownerId);
        Task<ApiResponse<bool>> DeleteSlotAsync(Guid slotId, int ownerId);
    }
}
