using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AWsecretary.Models
{
    [Table("data_member")]
    public class Member
    {
        [Key]
        [Column("nid")]
        public int Nid { get; set; }

        // 由系統自動設定，不需使用者填寫
        [ScaffoldColumn(false)]
        [MaxLength(20)]
        [Column("sid")]
        public string Sid { get; set; } = null!;

        // 由系統自動設定，不需使用者填寫
        [ScaffoldColumn(false)]
        [Column("create_date")]
        public DateTime CreateDate { get; set; }

        // 由系統自動設定，不需使用者填寫
        [ScaffoldColumn(false)]
        [Column("modify_date")]
        public DateTime? ModifyDate { get; set; }

        [MaxLength(20)]
        [Column("parent_mid")]
        public string? ParentMid { get; set; }

        [Required(ErrorMessage = "請輸入會員編號")]
        [MaxLength(20)]
        [Column("mid")]
        public string Mid { get; set; } = null!;

        [Required(ErrorMessage = "請輸入密碼")]
        [MaxLength(16)]
        [Column("pwd")]
        public string Pwd { get; set; } = null!;

        [Column("join_date")]
        public DateTime? JoinDate { get; set; }

        [Column("continue_date")]
        public DateTime? ContinueDate { get; set; }

        [Column("real_continue_date")]
        public DateTime? RealContinueDate { get; set; }

        [Column("hint_days")]
        public int HintDays { get; set; } = 30;

        [MaxLength(50)]
        [Column("name")]
        public string? Name { get; set; }

        [MaxLength(50)]
        [Column("eng_name")]
        public string? EngName { get; set; }

        [MaxLength(255)]
        [Column("head_img")]
        public string? HeadImg { get; set; }

        [MaxLength(255)]
        [Column("iden")]
        public string? Iden { get; set; }

        [MaxLength(10)]
        [Column("cmp_code")]
        public string? CmpCode { get; set; }

        [MaxLength(3)]
        [Column("role")]
        public string? Role { get; set; }

        [MaxLength(100)]
        [Column("authorization_page")]
        public string AuthorizationPage { get; set; } = null!;

        [MaxLength(500)]
        [Column("address")]
        public string? Address { get; set; }

        [MaxLength(20)]
        [Column("mobile")]
        public string? Mobile { get; set; }

        [MaxLength(20)]
        [Column("tel")]
        public string? Tel { get; set; }

        [MaxLength(20)]
        [Column("fax")]
        public string? Fax { get; set; }

        [MaxLength(100)]
        [Column("email")]
        public string? Email { get; set; }

        [MaxLength(2)]
        [Column("avalible")]
        public string Avalible { get; set; } = "Y";

        [Column("start_date")]
        public DateTime? StartDate { get; set; }

        [MaxLength(255)]
        [Column("signature_pic")]
        public string? SignaturePic { get; set; }

        [MaxLength(200)]
        [Column("advertising_id")]
        public string? AdvertisingId { get; set; }

        [MaxLength(200)]
        [Column("device_id")]
        public string? DeviceId { get; set; }

        [Column("fcm_token")]
        public string? FcmToken { get; set; }

        [Column("priority")]
        public int? Priority { get; set; }

        [MaxLength(20)]
        [Column("edit_sid")]
        public string? EditSid { get; set; }

        [Column("cur_coupon")]
        public int? CurCoupon { get; set; }

        [Column("cur_point")]
        public int? CurPoint { get; set; }

        [Column("script")]
        public string? Script { get; set; }

        [Column("remark")]
        public string? Remark { get; set; }

        // 新增：重設密碼 token 與到期時間
        [MaxLength(100)]
        [Column("password_reset_token")]
        public string? PasswordResetToken { get; set; }

        [Column("password_reset_expiry")]
        public DateTime? PasswordResetExpiry { get; set; }
    }
}