using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using AWsecretary.Responsitories;

namespace AWsecretary.Services
{
    public class NotificationHostedService : BackgroundService
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<NotificationHostedService> _logger;
        private readonly IConfiguration _configuration;
        private bool _firebaseInitialized;

        public NotificationHostedService(IServiceProvider provider, ILogger<NotificationHostedService> logger, IConfiguration configuration)
        {
            _provider = provider;
            _logger = logger;
            _configuration = configuration;
            _firebaseInitialized = false;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationHostedService started.");

// 初始化 FirebaseApp（只做一次）
            try
            {
                InitializeFirebaseIfNeeded();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Firebase 初始化失敗，推播功能將不可用。請確認 service-account JSON 路徑與內容正確。");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _provider.CreateScope();

                    // 直接從 repository 取得即將到期會員（避免改變現有 IMemberService 簽章）
                    var memberRepo = scope.ServiceProvider.GetRequiredService<IMemberRepository>();
                    var members = await memberRepo.GetExpiringWithinDaysAsync(30);

                    if (_firebaseInitialized)
                    {
                        var fcm = FirebaseMessaging.DefaultInstance;
                        foreach (var m in members)
                        {
                            try
                            {
                                if (string.IsNullOrWhiteSpace(m?.FcmToken)) continue;

                                var message = new Message
                                {
                                    Token = m.FcmToken,
                                    Notification = new Notification
                                    {
                                        Title = "會員續約提醒",
                                        Body = $"親愛的 {m.Name ?? m.Mid}, 您的合約即將到期，請盡速續約。"
                                    },
                                    Data = new System.Collections.Generic.Dictionary<string, string>
                                    {
                                        { "nid", m.Nid.ToString() },
                                        { "type", "expiry_reminder" }
                                    }
                                };

                                var resp = await fcm.SendAsync(message, cancellationToken: stoppingToken);
                                _logger.LogInformation("Sent FCM to {Mid} token (response: {Resp})", m.Mid, resp);
                            }
                            catch (Exception sendEx)
                            {
                                _logger.LogError(sendEx, "發送 FCM 給會員 {Mid} 失敗", m?.Mid);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Firebase 未初始化，已跳過推播發送。");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in NotificationHostedService main loop");
                }

                // 等待 1 小時後再檢查（可調）
                try
                {
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (TaskCanceledException) { /* stoppingToken 被取消 */ }
            }
        }

        private void InitializeFirebaseIfNeeded()
        {
            if (_firebaseInitialized) return;

            // 從設定讀取 credentials 路徑或 JSON 內容
            var credPath = _configuration["Firebase:CredentialsPath"];
            var credJson = _configuration["Firebase:CredentialsJson"]; // 選擇性：也可放整個 JSON 字串（建議不要硬編）

            if (string.IsNullOrWhiteSpace(credPath) && string.IsNullOrWhiteSpace(credJson))
            {
                _logger.LogWarning("未設定 Firebase credentials（Firebase:CredentialsPath / Firebase:CredentialsJson）。無法初始化 Firebase.");
                return;
            }

            if (FirebaseApp.DefaultInstance != null)
            {
                _firebaseInitialized = true;
                _logger.LogInformation("FirebaseApp 已存在，跳過初始化。");
                return;
            }

            GoogleCredential credential;
            if (!string.IsNullOrWhiteSpace(credPath))
            {
                credential = GoogleCredential.FromFile(credPath);
            }
            else
            {
                // 若從設定直接存 JSON 字串（不建議於版本控制），則使用 FromJson
                credential = GoogleCredential.FromJson(credJson);
            }

            FirebaseApp.Create(new AppOptions
            {
                Credential = credential
            });

            _firebaseInitialized = true;
            _logger.LogInformation("FirebaseApp 初始化完成。");
        }
    }
}