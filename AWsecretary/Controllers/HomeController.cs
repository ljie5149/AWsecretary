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

        // 資料庫不可用的錯誤頁面（回傳 Views/Shared/DatabaseUnavailable.cshtml）
        public IActionResult DatabaseUnavailable()
        {
            return View("DatabaseUnavailable");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
