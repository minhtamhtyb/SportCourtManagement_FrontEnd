using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Services;

namespace SportCourtManagement_FrontEnd.Controllers
{
    public class WalletController : Controller
    {
        private readonly ICourtApiService _apiService;

        public WalletController(ICourtApiService apiService)
        {
            _apiService = apiService;
        }

        // GET: /Wallet
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var token = GetToken();
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/Wallet" });
            }

            var wallet = await _apiService.GetWalletBalanceAsync(token);
            var transactions = await _apiService.GetWalletTransactionsAsync(token);

            ViewBag.Wallet = wallet;
            ViewBag.Transactions = transactions;

            return View();
        }

        // POST: /Wallet/Deposit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deposit(decimal amount)
        {
            var token = GetToken();
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập." });
            }

            if (amount <= 0)
            {
                return Json(new { success = false, message = "Số tiền nạp phải lớn hơn 0." });
            }

            var qrCode = await _apiService.GetWalletDepositQrAsync(amount, token);
            if (qrCode == null)
            {
                return Json(new { success = false, message = "Không thể tạo mã QR nạp tiền." });
            }

            return Json(new
            {
                success = true,
                qrCodeUrl = qrCode.QrCodeUrl,
                amount = qrCode.Amount,
                description = qrCode.Description,
                accountNumber = qrCode.AccountNumber,
                accountName = qrCode.AccountName,
                bankBin = qrCode.BankBin
            });
        }

        private string? GetToken()
        {
            var token = HttpContext.Session.GetString(Services.Api.JwtForwardingHandler.SessionTokenKey);
            if (!string.IsNullOrWhiteSpace(token))
                return token;

            try
            {
                token = Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.GetTokenAsync(HttpContext, "access_token").GetAwaiter().GetResult();
            }
            catch { }

            if (string.IsNullOrWhiteSpace(token))
            {
                token = User.FindFirst(Services.Api.JwtForwardingHandler.AccessTokenClaimType)?.Value;
            }

            if (!string.IsNullOrWhiteSpace(token) && HttpContext.Session.IsAvailable)
            {
                HttpContext.Session.SetString(Services.Api.JwtForwardingHandler.SessionTokenKey, token);
            }

            return token;
        }
    }
}
