using System.ComponentModel.DataAnnotations;

namespace AWsecretary.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "請輸入會員帳號")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "會員帳號長度需在 {2} 到 {1} 個字元之間")]
        [Display(Name = "會員帳號")]
        public string Mid { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入密碼")]
        [StringLength(16, MinimumLength = 6, ErrorMessage = "密碼長度需在 {2} 到 {1} 個字元之間")]
        [DataType(DataType.Password)]
        [Display(Name = "密碼")]
        public string Pwd { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "姓名")]
        public string? Name { get; set; }

        [EmailAddress(ErrorMessage = "請輸入正確的 Email")]
        [StringLength(100)]
        [Display(Name = "電子郵件")]
        public string? Email { get; set; }

        [StringLength(20)]
        [Display(Name = "手機")]
        public string? Mobile { get; set; }

        [StringLength(20)]
        [Display(Name = "上線會員帳號")]
        public string? ParentMid { get; set; }
    }
}