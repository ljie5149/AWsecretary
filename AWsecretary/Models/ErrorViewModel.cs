namespace AWsecretary.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        // 新增：可由 controller 傳入標題與訊息，供 Error.cshtml 顯示
        public string? Title { get; set; }

        public string? Message { get; set; }
    }
}
