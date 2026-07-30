using System.ComponentModel.DataAnnotations;

namespace turf_management_system.Models.ViewModels
{
    public class VerifyOtpVM
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "OTP is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
        public string OTP { get; set; } = string.Empty;
    }
}
