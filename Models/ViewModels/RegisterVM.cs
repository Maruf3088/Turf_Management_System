using System.ComponentModel.DataAnnotations;

namespace turf_management_system.Models.ViewModels
{
    public class RegisterVM
    {
        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "I want to register as")]
        public int RoleId { get; set; }

        // Turf Owner specific fields
        [Display(Name = "Business Name")]
        public string? BusinessName { get; set; }

        [Display(Name = "Business Address")]
        public string? BusinessAddress { get; set; }

        [Display(Name = "Business Contact Number")]
        public string? ContactNumber { get; set; }
    }
}
