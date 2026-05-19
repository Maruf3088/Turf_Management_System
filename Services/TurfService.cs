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
                MorningPricePerHour = dto.MorningPricePerHour,
                EveningPricePerHour = dto.EveningPricePerHour,
                SportType = dto.SportType,

                TurfSize = dto.TurfSize,
                Amenities = dto.Amenities,
                IndoorOutdoor = dto.IndoorOutdoor,
                ContactNumber = dto.ContactNumber,
                OwnerId = ownerId,
                CreatedAt = DateTime.UtcNow,
                IsApproved = false, // Set to false, needs admin approval or KYC
                IsActive = true,
                IsDeleted = false,
                IsDraft = true // New turf starts as draft
            };

            await _unitOfWork.Turfs.AddAsync(turf);

            // Create default booking config so it's immediately bookable
            var defaultConfig = new TurfBookingConfig
            {
                TurfId = turf.Id,
                AvailableDaysMask = 127, // All days
                OpeningTime = new TimeSpan(6, 0, 0),
                ClosingTime = new TimeSpan(22, 0, 0),
                SlotDurationMinutes = 60,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.BookingConfigs.AddAsync(defaultConfig);

            await _unitOfWork.CompleteAsync();

            // Auto-generate initial slots from default config
            await GenerateSlotsFromConfigAsync(turf.Id, defaultConfig);

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
            if (dto.MorningPricePerHour.HasValue) turf.MorningPricePerHour = dto.MorningPricePerHour.Value;
            if (dto.EveningPricePerHour.HasValue) turf.EveningPricePerHour = dto.EveningPricePerHour.Value;
            if (dto.SportType != null) turf.SportType = dto.SportType;

            if (dto.TurfSize != null) turf.TurfSize = dto.TurfSize;
            if (dto.Amenities != null) turf.Amenities = dto.Amenities;
            if (dto.IndoorOutdoor != null) turf.IndoorOutdoor = dto.IndoorOutdoor;
            if (dto.ContactNumber != null) turf.ContactNumber = dto.ContactNumber;
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
                    MorningPricePerHour = t.MorningPricePerHour,
                    EveningPricePerHour = t.EveningPricePerHour,
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
                MorningPricePerHour = t.MorningPricePerHour,
                EveningPricePerHour = t.EveningPricePerHour,
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

            // Instead of deleting, we mark as Rejected and store the reason in Amenities or a dedicated field if exists
            // Since we don't have a RejectionReason field, we'll use a standard approach
            turf.IsApproved = false;
            turf.IsDraft = true; // Return to draft so owner can fix
            turf.Amenities = (turf.Amenities ?? "") + " [REJECTED: " + reason + "]";
            
            _unitOfWork.Turfs.Update(turf);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Turf rejected. Owner can see the reason and edit.");
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
            var wwwRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            string uploadsFolder = Path.Combine(wwwRootPath, "uploads", "turfs", turfId.ToString());
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
            var wwwRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            string fullPath = Path.Combine(wwwRootPath, image.ImageUrl.TrimStart('/'));
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

            var config = await _unitOfWork.BookingConfigs.FindAsync(c => c.TurfId == turfId);
            int maxDays = config?.MaxAdvanceBookingDays ?? 7;
            var today = DateOnly.FromDateTime(DateTime.Today);
            var effectiveFrom = today.AddDays(maxDays);

            // Overlap validation: only check slots that will be active when this slot starts
            var overlappingSlots = turf.Slots.Where(s => 
                (dto.DayOfWeek == null || s.DayOfWeek == null || s.DayOfWeek == dto.DayOfWeek) &&
                (dto.StartTime < s.EndTime && dto.EndTime > s.StartTime) &&
                (s.EffectiveToDate == null || s.EffectiveToDate > effectiveFrom)
            );

            if (overlappingSlots.Any())
            {
                return ApiResponse<TurfSlotDto>.FailureResponse("Slot overlaps with an existing slot on this day.");
            }

            var turfSlot = new TurfSlot
            {
                Id = Guid.NewGuid(),
                TurfId = turfId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                DayOfWeek = dto.DayOfWeek,
                IsAvailable = true,
                PricingVariant = dto.PricingVariant ?? "Morning",
                EffectiveFromDate = effectiveFrom
            };

            await _unitOfWork.TurfSlots.AddAsync(turfSlot);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<TurfSlotDto>.SuccessResponse(new TurfSlotDto
            {
                Id = turfSlot.Id,
                StartTime = turfSlot.StartTime,
                EndTime = turfSlot.EndTime,
                DayOfWeek = turfSlot.DayOfWeek,
                IsAvailable = turfSlot.IsAvailable,
                PricingVariant = turfSlot.PricingVariant
            }, $"Slot added successfully. It will be active in {maxDays} days.");
        }

        public async Task<ApiResponse<bool>> DeleteSlotAsync(Guid slotId, int ownerId)
        {
            var slot = await _unitOfWork.TurfSlots.GetByIdAsync(slotId);
            if (slot == null) return ApiResponse<bool>.FailureResponse("Slot not found.");

            var turf = await _unitOfWork.Turfs.GetByIdAsync(slot.TurfId);
            if (turf?.OwnerId != ownerId) return ApiResponse<bool>.FailureResponse("Unauthorized.");

            var config = await _unitOfWork.BookingConfigs.FindAsync(c => c.TurfId == slot.TurfId);
            int maxDays = config?.MaxAdvanceBookingDays ?? 7;
            var today = DateOnly.FromDateTime(DateTime.Today);
            var effectiveTo = today.AddDays(maxDays);

            // If the slot is in the future and hasn't become active yet, delete it immediately
            if (slot.EffectiveFromDate.HasValue && slot.EffectiveFromDate > today)
            {
                _unitOfWork.TurfSlots.Delete(slot);
                await _unitOfWork.CompleteAsync();
                return ApiResponse<bool>.SuccessResponse(true, "Slot deleted successfully.");
            }

            // Check if slot has active or pending bookings beyond the new expiration date
            var bookingCount = await _unitOfWork.Bookings.GetCountAsync(b => 
                b.SlotId == slotId && 
                b.BookingDate > effectiveTo &&
                (b.Status == BookingStatus.PendingPayment || b.Status == BookingStatus.Confirmed));
            if (bookingCount > 0)
            {
                return ApiResponse<bool>.FailureResponse("This slot has bookings beyond the advance booking window and cannot be deleted.");
            }

            // Soft delete by setting EffectiveToDate
            slot.EffectiveToDate = effectiveTo;
            _unitOfWork.TurfSlots.Update(slot);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.SuccessResponse(true, $"Slot scheduled for removal. It will be removed in {maxDays} days.");
        }

        public async Task<ApiResponse<TurfSlotDto>> UpdateSlotAsync(Guid slotId, UpdateTurfSlotDto dto, int ownerId)
        {
            var slot = await _unitOfWork.TurfSlots.GetByIdAsync(slotId);
            if (slot == null) return ApiResponse<TurfSlotDto>.FailureResponse("Slot not found.");

            var turf = await _unitOfWork.Turfs.GetTurfWithDetailsAsync(slot.TurfId);
            if (turf == null || turf.OwnerId != ownerId) return ApiResponse<TurfSlotDto>.FailureResponse("Unauthorized.");

            var config = await _unitOfWork.BookingConfigs.FindAsync(c => c.TurfId == slot.TurfId);
            int maxDays = config?.MaxAdvanceBookingDays ?? 7;
            var today = DateOnly.FromDateTime(DateTime.Today);
            var effectiveDate = today.AddDays(maxDays);

            // If the slot is in the future and hasn't become active yet, we can modify it directly
            if (slot.EffectiveFromDate.HasValue && slot.EffectiveFromDate > today)
            {
                var overlappingSlots = turf.Slots.Where(s => 
                    s.Id != slotId &&
                    (dto.DayOfWeek == null || s.DayOfWeek == null || s.DayOfWeek == dto.DayOfWeek) &&
                    (dto.StartTime < s.EndTime && dto.EndTime > s.StartTime) &&
                    (s.EffectiveToDate == null || s.EffectiveToDate > slot.EffectiveFromDate)
                );

                if (overlappingSlots.Any())
                {
                    return ApiResponse<TurfSlotDto>.FailureResponse("Slot overlaps with an existing slot on this day.");
                }

                slot.StartTime = dto.StartTime;
                slot.EndTime = dto.EndTime;
                slot.DayOfWeek = dto.DayOfWeek;
                slot.IsAvailable = dto.IsAvailable;
                slot.PricingVariant = dto.PricingVariant;

                _unitOfWork.TurfSlots.Update(slot);
                await _unitOfWork.CompleteAsync();

                return ApiResponse<TurfSlotDto>.SuccessResponse(new TurfSlotDto
                {
                    Id = slot.Id,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    DayOfWeek = slot.DayOfWeek,
                    IsAvailable = slot.IsAvailable,
                    PricingVariant = slot.PricingVariant
                }, "Pending slot updated successfully.");
            }

            // Otherwise, we expire the current slot and create a new one to take effect in maxDays
            var overlaps = turf.Slots.Where(s => 
                s.Id != slotId &&
                (dto.DayOfWeek == null || s.DayOfWeek == null || s.DayOfWeek == dto.DayOfWeek) &&
                (dto.StartTime < s.EndTime && dto.EndTime > s.StartTime) &&
                (s.EffectiveToDate == null || s.EffectiveToDate > effectiveDate)
            );

            if (overlaps.Any())
            {
                return ApiResponse<TurfSlotDto>.FailureResponse("Updated slot overlaps with an existing slot on this day.");
            }

            // Expire old slot
            slot.EffectiveToDate = effectiveDate;
            _unitOfWork.TurfSlots.Update(slot);

            // Create new slot
            var newSlot = new TurfSlot
            {
                Id = Guid.NewGuid(),
                TurfId = slot.TurfId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                DayOfWeek = dto.DayOfWeek,
                IsAvailable = dto.IsAvailable,
                PricingVariant = dto.PricingVariant,
                EffectiveFromDate = effectiveDate
            };

            await _unitOfWork.TurfSlots.AddAsync(newSlot);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<TurfSlotDto>.SuccessResponse(new TurfSlotDto
            {
                Id = newSlot.Id,
                StartTime = newSlot.StartTime,
                EndTime = newSlot.EndTime,
                DayOfWeek = newSlot.DayOfWeek,
                IsAvailable = newSlot.IsAvailable,
                PricingVariant = newSlot.PricingVariant
            }, $"Slot scheduled for update. Changes will take effect in {maxDays} days.");
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
                MorningPricePerHour = turf.MorningPricePerHour,
                EveningPricePerHour = turf.EveningPricePerHour,
                SportType = turf.SportType,
                TurfSize = turf.TurfSize,
                Amenities = turf.Amenities,
                IndoorOutdoor = turf.IndoorOutdoor,
                ContactNumber = turf.ContactNumber,
                IsApproved = turf.IsApproved,
                IsActive = turf.IsActive,
                OwnerId = turf.OwnerId,
                OwnerName = turf.Owner?.FullName ?? "Unknown",
                CreatedAt = turf.CreatedAt,
                MainImageUrl = turf.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl ?? turf.Images.FirstOrDefault()?.ImageUrl,
                Images = turf.Images.Select(i => new TurfImageDto { Id = i.Id, ImageUrl = i.ImageUrl, IsMain = i.IsMain }).ToList(),
                Slots = turf.Slots
                    .Where(s => s.EffectiveToDate == null)
                    .Select(s => new TurfSlotDto 
                    { 
                        Id = s.Id, 
                        StartTime = s.StartTime, 
                        EndTime = s.EndTime, 
                        IsAvailable = s.IsAvailable, 
                        DayOfWeek = s.DayOfWeek, 
                        PricingVariant = s.PricingVariant 
                    }).ToList()

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
        public async Task<ApiResponse<bool>> UpdateBookingConfigAsync(Guid turfId, UpdateBookingConfigDto dto, int ownerId)
        {
            var turf = await _unitOfWork.Turfs.GetByIdAsync(turfId);
            if (turf == null) return ApiResponse<bool>.FailureResponse("Turf not found.");
            if (turf.OwnerId != ownerId) return ApiResponse<bool>.FailureResponse("Unauthorized.");

            var config = await _unitOfWork.BookingConfigs.FindAsync(c => c.TurfId == turfId);
            if (config == null)
            {
                config = new TurfBookingConfig { TurfId = turfId, CreatedAt = DateTime.UtcNow };
                await _unitOfWork.BookingConfigs.AddAsync(config);
            }

            // Check if any slot-defining fields changed
            bool hoursOrDurationChanged = 
                config.OpeningTime != dto.OpeningTime ||
                config.ClosingTime != dto.ClosingTime ||
                config.SlotDurationMinutes != dto.SlotDurationMinutes ||
                config.AvailableDaysMask != dto.AvailableDaysMask;

            config.OpeningTime = dto.OpeningTime;
            config.ClosingTime = dto.ClosingTime;
            config.SlotDurationMinutes = dto.SlotDurationMinutes;
            config.MaxAdvanceBookingDays = dto.MaxAdvanceBookingDays;
            config.RequireFullPayment = dto.RequireFullPayment;
            config.AcceptBkash = dto.AcceptBkash;
            config.AcceptNagad = dto.AcceptNagad;
            config.AcceptRocket = dto.AcceptRocket;
            config.AvailableDaysMask = dto.AvailableDaysMask;
            config.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.CompleteAsync();

            // Auto-generate time slots from config ONLY if operating hours/duration/days mask changed
            if (hoursOrDurationChanged)
            {
                await GenerateSlotsFromConfigAsync(turfId, config, isInitial: false);
            }

            return ApiResponse<bool>.SuccessResponse(true, "Booking configuration updated.");
        }

        /// <summary>
        /// Regenerates TurfSlot records from the booking config's operating hours.
        /// Deletes/expires slots and recreates them based on the new config.
        /// </summary>
        private async Task GenerateSlotsFromConfigAsync(Guid turfId, TurfBookingConfig config, bool isInitial = false)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var effectiveDate = isInitial ? today : today.AddDays(config.MaxAdvanceBookingDays);

            // Fetch all current slots for the turf
            int page = 1;
            var currentSlots = new List<TurfSlot>();
            while (true)
            {
                var slotResult = await _unitOfWork.TurfSlots.GetPagedAsync(page, 100, s => s.TurfId == turfId);
                if (!slotResult.Items.Any()) break;
                currentSlots.AddRange(slotResult.Items);
                if (slotResult.Items.Count() < 100) break;
                page++;
            }

            // Collect booked slot IDs (Confirmed or PendingPayment) — must NOT be deleted
            var bookedSlotIds = new HashSet<Guid>();
            page = 1;
            while (true)
            {
                var bookingResult = await _unitOfWork.Bookings.GetPagedAsync(page, 200, null, turfId, null);
                foreach (var b in bookingResult.Items)
                {
                    if (b.Status == BookingStatus.PendingPayment || b.Status == BookingStatus.Confirmed)
                        bookedSlotIds.Add(b.SlotId);
                }
                if (bookingResult.Items.Count() < 200) break;
                page++;
            }

            // Process existing slots
            foreach (var slot in currentSlots)
            {
                // Future slot that hasn't become active yet can be deleted directly
                if (slot.EffectiveFromDate.HasValue && slot.EffectiveFromDate > today)
                {
                    _unitOfWork.TurfSlots.Delete(slot);
                }
                else
                {
                    // For active/past slots, if they are not already expired, set their expiration
                    if (slot.EffectiveToDate == null || slot.EffectiveToDate > effectiveDate)
                    {
                        if (!bookedSlotIds.Contains(slot.Id) && isInitial)
                        {
                            _unitOfWork.TurfSlots.Delete(slot);
                        }
                        else
                        {
                            slot.EffectiveToDate = effectiveDate;
                            _unitOfWork.TurfSlots.Update(slot);
                        }
                    }
                }
            }
            await _unitOfWork.CompleteAsync();

            // Generate new slots from OpeningTime → ClosingTime using SlotDurationMinutes
            var current = config.OpeningTime;
            var duration = TimeSpan.FromMinutes(config.SlotDurationMinutes > 0 ? config.SlotDurationMinutes : 60);

            while (current + duration <= config.ClosingTime)
            {
                var endTime = current + duration;
                string pricingVariant = current.Hours >= 16 ? "Evening" : "Morning";
                await _unitOfWork.TurfSlots.AddAsync(new TurfSlot
                {
                    Id = Guid.NewGuid(),
                    TurfId = turfId,
                    StartTime = current,
                    EndTime = endTime,
                    DayOfWeek = null, // null = applies to ALL days
                    IsAvailable = true,
                    PricingVariant = pricingVariant,
                    EffectiveFromDate = isInitial ? null : effectiveDate
                });
                current = endTime;
            }

            await _unitOfWork.CompleteAsync();
        }

        public async Task<ApiResponse<bool>> PublishTurfAsync(Guid turfId, int ownerId)
        {
            var turf = await _unitOfWork.Turfs.GetByIdAsync(turfId);
            if (turf == null) return ApiResponse<bool>.FailureResponse("Turf not found.");
            if (turf.OwnerId != ownerId) return ApiResponse<bool>.FailureResponse("Unauthorized.");

            // When publishing, it ALWAYS goes to admin review first
            turf.IsDraft = false;
            turf.IsApproved = false; 
            
            await _unitOfWork.CompleteAsync();
            return ApiResponse<bool>.SuccessResponse(true, "Turf submitted for approval. It will be visible after admin review.");
        }
    }
}
