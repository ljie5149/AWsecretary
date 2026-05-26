using System.ComponentModel.DataAnnotations;

namespace AWsecretary.Models
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入新密碼")]
        [StringLength(16, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "新密碼")]
        public string NewPassword { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "密碼與確認密碼不符")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "密碼與確認密碼不符")]
        [Display(Name = "確認密碼")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}