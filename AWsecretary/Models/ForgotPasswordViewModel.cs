using System.ComponentModel.DataAnnotations;

namespace AWsecretary.Models
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [Display(Name = "會員帳號或電子郵件")]
        [StringLength(100)]
        public string Identifier { get; set; } = string.Empty;
    }
}