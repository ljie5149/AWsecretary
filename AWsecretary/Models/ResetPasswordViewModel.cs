using System.ComponentModel.DataAnnotations;

namespace AWsecretary.Models
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [StringLength(16, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "·s±K½X")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "±K½X»P½T»{±K½X¤£²Å")]
        [Display(Name = "½T»{±K½X")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}