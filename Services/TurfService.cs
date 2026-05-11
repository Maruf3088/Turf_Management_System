using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using turf_management_system.DTOs.Common;
using turf_management_system.DTOs.Turf;
using turf_management_system.Models.Domain;
using turf_management_system.Repositories.Interfaces;
using turf_management_system.Services.Interfaces;

namespace turf_management_system.Services
{
    public class TurfService : ITurfService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _environment;

        public TurfService(IUnitOfWork unitOfWork, IWebHostEnvironment environment)
        {
            _unitOfWork = unitOfWork;
            _environment = environment;
        }

        public async Task<ApiResponse<TurfResponseDto>> CreateTurfAsync(CreateTurfDto dto, int ownerId)
        {
            var turf = new Turf
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                Location = dto.Location,
                City = dto.City,
                PricePerHour = dto.PricePerHour,
                SportType = dto.SportType,
                TurfSize = dto.TurfSize,
                Amenities = dto.Amenities,
                OwnerId = ownerId,
                CreatedAt = DateTime.UtcNow,
                IsApproved = false,
                IsActive = true,
                IsDeleted = false
            };

            await _unitOfWork.Turfs.AddAsync(turf);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<TurfResponseDto>.SuccessResponse(MapToResponseDto(turf), "Turf created successfully. Awaiting admin approval.");
        }

        public async Task<ApiResponse<TurfResponseDto>> UpdateTurfAsync(Guid turfId, UpdateTurfDto dto, int ownerId)
        {
            var turf = await _unitOfWork.Turfs.GetTurfWithDetailsAsync(turfId);
            if (turf == null) return ApiResponse<TurfResponseDto>.FailureResponse("Turf not found.");
            if (turf.OwnerId != ownerId) return ApiResponse<TurfResponseDto>.FailureResponse("Unauthorized.");

            if (dto.Name != null) turf.Name = dto.Name;
            if (dto.Description != null) turf.Description = dto.Description;
            if (dto.Location != null) turf.Location = dto.Location;
            if (dto.City != null) turf.City = dto.City;
            if (dto.PricePerHour.HasValue) turf.PricePerHour = dto.PricePerHour.Value;
            if (dto.SportType != null) turf.SportType = dto.SportType;
            if (dto.TurfSize != null) turf.TurfSize = dto.TurfSize;
            if (dto.Amenities != null) turf.Amenities = dto.Amenities;
            if (dto.IsActive.HasValue) turf.IsActive = dto.IsActive.Value;

            turf.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Turfs.Update(turf);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<TurfResponseDto>.SuccessResponse(MapToResponseDto(turf), "Turf updated successfully.");
        }

        public async Task<ApiResponse<bool>> DeleteTurfAsync(Guid turfId, int requesterId, string requesterRole)
        {
            var turf = await _unitOfWork.Turfs.GetByIdAsync(turfId);
            if (turf == null) return ApiResponse<bool>.FailureResponse("Turf not found.");

            if (requesterRole != "Admin" && turf.OwnerId != requesterId)
                return ApiResponse<bool>.FailureResponse("Unauthorized.");

            turf.IsDeleted = true;
            _unitOfWork.Turfs.Update(turf);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Turf deleted successfully.");
        }

        public async Task<ApiResponse<PagedResultDto<TurfListItemDto>>> GetAllTurfsPagedAsync(int pageNumber, int pageSize, string? search, string? city, string? sportType, bool? isApproved)
        {
            var (items, totalCount) = await _unitOfWork.Turfs.GetAllPagedAsync(pageNumber, pageSize, search, city, sportType, isApproved);

            var result = new PagedResultDto<TurfListItemDto>
            {
                Items = items.Select(t => new TurfListItemDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    City = t.City,
                    SportType = t.SportType,
                    PricePerHour = t.PricePerHour,
                    TurfSize = t.TurfSize,
                    IsApproved = t.IsApproved,
                    MainImageUrl = t.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl ?? t.Images.FirstOrDefault()?.ImageUrl
                }),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ApiResponse<PagedResultDto<TurfListItemDto>>.SuccessResponse(result);
        }

        public async Task<ApiResponse<IEnumerable<TurfListItemDto>>> GetMyTurfsAsync(int ownerId)
        {
            var turfs = await _unitOfWork.Turfs.GetTurfsByOwnerIdAsync(ownerId);
            var result = turfs.Select(t => new TurfListItemDto
            {
                Id = t.Id,
                Name = t.Name,
                City = t.City,
                SportType = t.SportType,
                PricePerHour = t.PricePerHour,
                TurfSize = t.TurfSize,
                IsApproved = t.IsApproved,
                MainImageUrl = t.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl ?? t.Images.FirstOrDefault()?.ImageUrl
            });

            return ApiResponse<IEnumerable<TurfListItemDto>>.SuccessResponse(result);
        }

        public async Task<ApiResponse<TurfResponseDto>> GetTurfByIdAsync(Guid turfId)
        {
            var turf = await _unitOfWork.Turfs.GetTurfWithDetailsAsync(turfId);
            if (turf == null) return ApiResponse<TurfResponseDto>.FailureResponse("Turf not found.");

            return ApiResponse<TurfResponseDto>.SuccessResponse(MapToResponseDto(turf));
        }

        public async Task<ApiResponse<bool>> ApproveTurfAsync(Guid turfId)
        {
            var turf = await _unitOfWork.Turfs.GetByIdAsync(turfId);
            if (turf == null) return ApiResponse<bool>.FailureResponse("Turf not found.");

            turf.IsApproved = true;
            _unitOfWork.Turfs.Update(turf);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Turf approved successfully.");
        }

        public async Task<ApiResponse<bool>> RejectTurfAsync(Guid turfId, string reason)
        {
            var turf = await _unitOfWork.Turfs.GetByIdAsync(turfId);
            if (turf == null) return ApiResponse<bool>.FailureResponse("Turf not found.");

            // Optionally log reason or notify owner
            _unitOfWork.Turfs.Delete(turf); // Or set status to Rejected
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Turf rejected and removed.");
        }

        public async Task<ApiResponse<bool>> UploadTurfImageAsync(Guid turfId, IFormFile image, bool isMain, int ownerId)
        {
            var turf = await _unitOfWork.Turfs.GetByIdAsync(turfId);
            if (turf == null) return ApiResponse<bool>.FailureResponse("Turf not found.");
            if (turf.OwnerId != ownerId) return ApiResponse<bool>.FailureResponse("Unauthorized.");

            // Validate file
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(image.FileName).ToLower();
            if (!allowedExtensions.Contains(extension)) return ApiResponse<bool>.FailureResponse("Invalid file type.");
            if (image.Length > 5 * 1024 * 1024) return ApiResponse<bool>.FailureResponse("File size exceeds 5MB.");

            // Save file
            string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "turfs", turfId.ToString());
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string fileName = Guid.NewGuid().ToString() + extension;
            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            string relativePath = $"/uploads/turfs/{turfId}/{fileName}";

            if (isMain)
            {
                var existingImages = await _unitOfWork.TurfImages.FindAsync(i => i.TurfId == turfId && i.IsMain);
                if (existingImages != null)
                {
                    existingImages.IsMain = false;
                    _unitOfWork.TurfImages.Update(existingImages);
                }
            }

            var turfImage = new TurfImage
            {
                Id = Guid.NewGuid(),
                TurfId = turfId,
                ImageUrl = relativePath,
                IsMain = isMain,
                UploadedAt = DateTime.UtcNow
            };

            await _unitOfWork.TurfImages.AddAsync(turfImage);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Image uploaded successfully.");
        }

        public async Task<ApiResponse<bool>> DeleteTurfImageAsync(Guid imageId, int ownerId)
        {
            var image = await _unitOfWork.TurfImages.GetByIdAsync(imageId);
            if (image == null) return ApiResponse<bool>.FailureResponse("Image not found.");

            var turf = await _unitOfWork.Turfs.GetByIdAsync(image.TurfId);
            if (turf?.OwnerId != ownerId) return ApiResponse<bool>.FailureResponse("Unauthorized.");

            // Delete file
            string fullPath = Path.Combine(_environment.WebRootPath, image.ImageUrl.TrimStart('/'));
            if (File.Exists(fullPath)) File.Delete(fullPath);

            _unitOfWork.TurfImages.Delete(image);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Image deleted successfully.");
        }

        public async Task<ApiResponse<TurfSlotDto>> AddSlotAsync(Guid turfId, CreateTurfSlotDto dto, int ownerId)
        {
            var turf = await _unitOfWork.Turfs.GetTurfWithDetailsAsync(turfId);
            if (turf == null) return ApiResponse<TurfSlotDto>.FailureResponse("Turf not found.");
            if (turf.OwnerId != ownerId) return ApiResponse<TurfSlotDto>.FailureResponse("Unauthorized.");

            // Overlap validation
            var existingSlots = turf.Slots.Where(s => s.DayOfWeek == dto.DayOfWeek);
            foreach (var slot in existingSlots)
            {
                if (dto.StartTime < slot.EndTime && dto.EndTime > slot.StartTime)
                {
                    return ApiResponse<TurfSlotDto>.FailureResponse("Slot overlaps with an existing slot.");
                }
            }

            var turfSlot = new TurfSlot
            {
                Id = Guid.NewGuid(),
                TurfId = turfId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                DayOfWeek = dto.DayOfWeek,
                IsAvailable = true
            };

            await _unitOfWork.TurfSlots.AddAsync(turfSlot);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<TurfSlotDto>.SuccessResponse(new TurfSlotDto
            {
                Id = turfSlot.Id,
                StartTime = turfSlot.StartTime,
                EndTime = turfSlot.EndTime,
                DayOfWeek = turfSlot.DayOfWeek,
                IsAvailable = turfSlot.IsAvailable
            }, "Slot added successfully.");
        }

        public async Task<ApiResponse<bool>> DeleteSlotAsync(Guid slotId, int ownerId)
        {
            var slot = await _unitOfWork.TurfSlots.GetByIdAsync(slotId);
            if (slot == null) return ApiResponse<bool>.FailureResponse("Slot not found.");

            var turf = await _unitOfWork.Turfs.GetByIdAsync(slot.TurfId);
            if (turf?.OwnerId != ownerId) return ApiResponse<bool>.FailureResponse("Unauthorized.");

            _unitOfWork.TurfSlots.Delete(slot);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Slot deleted successfully.");
        }

        private TurfResponseDto MapToResponseDto(Turf turf)
        {
            return new TurfResponseDto
            {
                Id = turf.Id,
                Name = turf.Name,
                Description = turf.Description,
                Location = turf.Location,
                City = turf.City,
                PricePerHour = turf.PricePerHour,
                SportType = turf.SportType,
                TurfSize = turf.TurfSize,
                Amenities = turf.Amenities,
                IsApproved = turf.IsApproved,
                IsActive = turf.IsActive,
                OwnerId = turf.OwnerId,
                OwnerName = turf.Owner?.FullName ?? "Unknown",
                CreatedAt = turf.CreatedAt,
                MainImageUrl = turf.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl ?? turf.Images.FirstOrDefault()?.ImageUrl,
                Images = turf.Images.Select(i => new TurfImageDto { Id = i.Id, ImageUrl = i.ImageUrl, IsMain = i.IsMain }).ToList(),
                Slots = turf.Slots.Select(s => new TurfSlotDto { Id = s.Id, StartTime = s.StartTime, EndTime = s.EndTime, IsAvailable = s.IsAvailable, DayOfWeek = s.DayOfWeek }).ToList()
            };
        }

        public async Task<ApiResponse<IEnumerable<string>>> GetCitiesAsync()
        {
            var cities = await _unitOfWork.Turfs.GetDistinctCitiesAsync();
            return ApiResponse<IEnumerable<string>>.SuccessResponse(cities);
        }

        public async Task<ApiResponse<IEnumerable<string>>> GetSportTypesAsync()
        {
            var types = await _unitOfWork.Turfs.GetDistinctSportTypesAsync();
            return ApiResponse<IEnumerable<string>>.SuccessResponse(types);
        }
    }
}
