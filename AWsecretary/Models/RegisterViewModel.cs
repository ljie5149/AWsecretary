using System.ComponentModel.DataAnnotations;

namespace AWsecretary.Models
{
    public class RegisterViewModel
    {
        [Required]
        [StringLength(20, MinimumLength = 3)]
        [Display(Name = "會員帳號")]
        public string Mid { get; set; } = string.Empty;

        [Required]
        [StringLength(16, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "密碼")]
        public string Pwd { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "名稱")]
        public string? Name { get; set; }

        [EmailAddress]
        [StringLength(100)]
        [Display(Name = "電子信箱")]
        public string? Email { get; set; }

        [StringLength(20)]
        [Display(Name = "手機")]
        public string? Mobile { get; set; }

        [StringLength(20)]
        [Display(Name = "上線會員帳號")]
        public string? ParentMid { get; set; }
    }
}