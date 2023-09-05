using System.ComponentModel.DataAnnotations;
using System.Reflection.Emit;

namespace iOSBot.Web.Models
{
    public class DeviceViewModel
    {
        [Required]
        [Display(Name = "Audience Id")]
        [RegularExpression("[0-9a-f]{8}-([0-9a-f]{4}-){3}[0-9a-f]{12}", ErrorMessage = "Invalid Audience ID. Check link above.")]
        public string AudienceId { get; set; }
        [Required]
        [Display(Name = "Device Name")]
        public string Name { get; set; }
        [Required]
        [Display(Name = "Update Feed Friendly Name")]
        public string FriendlyName { get; set; }
        [Required]
        [Display(Name = "Current Device FW Version")]
        public string Version { get; set; }
        [Required]
        [Display(Name = "Current Device FW Build Id")]
        public string BuildId { get; set; }
        [Required]
        [Display(Name = "Device HWID (iPhone15,3)")]
        public string Product { get; set; }
        [Required]
        [Display(Name = "Device Board Id")]
        public string BoardId { get; set; }
        [Required]
        [Display(Name = "Update Feed Category Name")]
        public string Category { get; set; }
        [Display(Name = "Update Feed Changelog Url")]
        public string Changelog { get; set; }
        // Developer, Public, Release
        [Display(Name = "Update Feed Type")]
        [Required]
        public string Type { get; set; }
        [Display(Name = "Discord Embed Color")]
        [Required]
        public string Color { get; set; }
        [Display(Name = "Update Feed Asset Type")]
        [Required]
        public string AssetType { get; set; }
    }
}
