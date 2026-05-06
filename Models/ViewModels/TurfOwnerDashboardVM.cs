namespace turf_management_system.Models.ViewModels
{
    public class TurfOwnerDashboardVM
    {
        public string FullName { get; set; } = string.Empty;
        public int MyTurfs { get; set; }
        public int TodaysBookings { get; set; }
        public int TotalBookings { get; set; }
        public bool IsActive { get; set; }
    }
}
