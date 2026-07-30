using System.ComponentModel.DataAnnotations;

namespace turf_management_system.Models.ViewModels
{
    public class ForgotPasswordVM
    {
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        public string Email { get; set; } = string.Empty;
    }
}
