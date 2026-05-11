using System.ComponentModel.DataAnnotations;

namespace turf_management_system.DTOs.Turf
{
    public class CreateTurfDto
    {
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [StringLength(300)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [Range(0, 1000000)]
        public decimal PricePerHour { get; set; }

        [Required]
        [StringLength(100)]
        public string SportType { get; set; } = string.Empty;

        [StringLength(100)]
        public string? TurfSize { get; set; }

        public string? Amenities { get; set; }
    }

    public class UpdateTurfDto
    {
        [StringLength(150)]
        public string? Name { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(300)]
        public string? Location { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [Range(0, 1000000)]
        public decimal? PricePerHour { get; set; }

        [StringLength(100)]
        public string? SportType { get; set; }

        [StringLength(100)]
        public string? TurfSize { get; set; }

        public string? Amenities { get; set; }
        
        public bool? IsActive { get; set; }
    }

    public class TurfResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Location { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public decimal PricePerHour { get; set; }
        public string SportType { get; set; } = string.Empty;
        public string? TurfSize { get; set; }
        public string? Amenities { get; set; }
        public bool IsApproved { get; set; }
        public bool IsActive { get; set; }
        public int OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string? MainImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TurfImageDto> Images { get; set; } = new();
        public List<TurfSlotDto> Slots { get; set; } = new();
    }

    public class TurfListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string SportType { get; set; } = string.Empty;
        public decimal PricePerHour { get; set; }
        public string? TurfSize { get; set; }
        public bool IsApproved { get; set; }
        public string? MainImageUrl { get; set; }
    }

    public class TurfImageDto
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsMain { get; set; }
    }

    public class TurfSlotDto
    {
        public Guid Id { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsAvailable { get; set; }
        public int? DayOfWeek { get; set; }
    }

    public class CreateTurfSlotDto
    {
        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public int? DayOfWeek { get; set; }
    }
}
