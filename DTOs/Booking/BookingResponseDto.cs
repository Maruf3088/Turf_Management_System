using turf_management_system.Models.Domain;

namespace turf_management_system.DTOs.Booking
{
    public class BookingResponseDto
    {
        public Guid Id { get; set; }
        public Guid TurfId { get; set; }
        public string TurfName { get; set; } = string.Empty;
        public string TurfCity { get; set; } = string.Empty;
        public string MainImageUrl { get; set; } = string.Empty;
        
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DateOnly BookingDate { get; set; }
        
        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
        
        public BookingStatus Status { get; set; }
        public string StatusText => Status.ToString();
        public PaymentStatus PaymentStatus { get; set; }
        public string PaymentStatusText => PaymentStatus.ToString();
        
        public string UserName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string? SpecialRequest { get; set; }
        public string? CancellationReason { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }
}
