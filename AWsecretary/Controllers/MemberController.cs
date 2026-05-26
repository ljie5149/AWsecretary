using System.Collections.Generic;
using System.Security.Claims;
using System.Collections.Generic;
using System.Security.Claims;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AWsecretary.Models;
using AWsecretary.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AWsecretary.Controllers
{
    public class MemberController : Controller
    {
        private readonly IMemberService _memberService;

        public MemberController(IMemberService memberService)
        {
            _memberService = memberService;
        }

        // 列表（選用）
        public async Task<IActionResult> Index()
        {
            var members = await _memberService.GetAllAsync();
            return View(members);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var exists = await _memberService.GetByMidAsync(model.Mid);
            if (exists != null)
            {
                ModelState.AddModelError(string.Empty, "此會員帳號已被使用。");
                return View(model);
            }

            var member = new AWsecretary.Models.Member
            {
                Mid = model.Mid,
                Pwd = model.Pwd, // 生產環境請改為雜湊儲存
                Name = model.Name,
                Email = model.Email,
                Mobile = model.Mobile,
                ParentMid = model.ParentMid,
                AuthorizationPage = string.Empty
            };

            await _memberService.CreateAsync(member);

            TempData["Success"] = "註冊成功。";
            return RedirectToAction(nameof(Register));
        }

        // GET: 登入頁
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new RegisterViewModel());
        }

        // POST: 登入
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var member = await _memberService.GetByMidAsync(model.Mid);
            if (member == null || member.Pwd != model.Pwd)
            {
                ModelState.AddModelError(string.Empty, "帳號或密碼錯誤。");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, member.Mid),
                new Claim("mid", member.Mid)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var props = new AuthenticationProperties
            {
                IsPersistent = false
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        // 忘記密碼 (GET)
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        // 忘記密碼 (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var token = await _memberService.GeneratePasswordResetTokenAsync(model.Identifier);
            // 若需要，這裡應該發送 Email；目前示範回傳 token 到確認頁（生產應移除，並改為 Email 發送）
            ViewData["ResetToken"] = token;
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // 顯示重設密碼頁 (GET)
        [HttpGet]
        public IActionResult ResetPassword(string? token)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction(nameof(ForgotPassword));

            var vm = new ResetPasswordViewModel { Token = token };
            return View(vm);
        }

        // 重設密碼 (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var ok = await _memberService.ResetPasswordAsync(model.Token, model.NewPassword);
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, "重設密碼失敗（token 無效或已過期）。");
                return View(model);
            }

            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        // POST: 登出
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}