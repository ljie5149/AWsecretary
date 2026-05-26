using System.Diagnostics;
using AWsecretary.Models;
using Microsoft.AspNetCore.Mvc;

namespace AWsecretary.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // 資料庫不可用的錯誤頁面（改為回傳 Views/Shared/Error.cshtml，並帶入自訂訊息）
        public IActionResult DatabaseUnavailable()
        {
            var model = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                Title = "資料庫無法連線",
                Message = "目前無法連接到資料庫（可能資料庫服務未啟動或連線設定錯誤）。請稍後再試或聯絡系統管理員。"
            };

            return View("Error", model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
