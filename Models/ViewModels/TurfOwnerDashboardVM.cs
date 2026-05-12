namespace turf_management_system.Models.ViewModels
{
    public class TurfOwnerDashboardVM
    {
        public string FullName { get; set; } = string.Empty;
        public int MyTurfs { get; set; }
        public int TodaysBookings { get; set; }
        public int TotalBookings { get; set; }
        public bool IsActive { get; set; }
        public turf_management_system.Models.Enums.VerificationStatus VerificationStatus { get; set; }
        public List<turf_management_system.Models.Domain.Booking> RecentBookings { get; set; } = new();
    }
}
