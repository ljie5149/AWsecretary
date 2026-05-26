using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AWsecretary.Services
{
    public class NotificationHostedService : BackgroundService
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<NotificationHostedService> _logger;

        public NotificationHostedService(IServiceProvider provider, ILogger<NotificationHostedService> logger)
        {
            _provider = provider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationHostedService started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _provider.CreateScope();
                    var memberService = scope.ServiceProvider.GetRequiredService<IMemberService>();
                    // 以每日一次檢查示範；可改為更頻繁或調度
                    await memberService.SendExpiryNotificationsAsync(30);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in NotificationHostedService");
                }

                // 等待 1 小時後再檢查（可調）
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}