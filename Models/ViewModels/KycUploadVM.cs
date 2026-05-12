using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace turf_management_system.Models.ViewModels
{
    public class KycUploadVM
    {
        [Required(ErrorMessage = "National ID Front Image is required")]
        [Display(Name = "NID Front Image (JPG/PNG/PDF)")]
        public IFormFile? NidFrontImage { get; set; }

        [Required(ErrorMessage = "National ID Back Image is required")]
        [Display(Name = "NID Back Image (JPG/PNG/PDF)")]
        public IFormFile? NidBackImage { get; set; }

        [Display(Name = "Trade License (Optional, JPG/PNG/PDF)")]
        public IFormFile? TradeLicenseImage { get; set; }

        [Required(ErrorMessage = "Utility Bill is required")]
        [Display(Name = "Utility Bill / Address Proof (JPG/PNG/PDF)")]
        public IFormFile? UtilityBillImage { get; set; }
    }
}
