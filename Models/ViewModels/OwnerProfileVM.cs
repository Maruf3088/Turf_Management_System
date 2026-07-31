using turf_management_system.DTOs.Turf;

namespace turf_management_system.Models.ViewModels
{
    public class OwnerProfileVM
    {
        public int OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public string? BusinessAddress { get; set; }
        public string? ContactNumber { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime MemberSince { get; set; }
        public List<TurfResponseDto> Turfs { get; set; } = new();
    }
}
