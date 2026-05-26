using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWsecretary.Models;
using AWsecretary.Responsitories;
using CsvHelper;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace AWsecretary.Services
{
    public class MemberService : IMemberService
    {
        private readonly IMemberRepository _repo;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _config;

        public MemberService(IMemberRepository repo, IHttpClientFactory httpFactory, IConfiguration config)
        {
            _repo = repo;
            _httpFactory = httpFactory;
            _config = config;
        }

        public async Task CreateAsync(Member member)
        {
            member.CreateDate = DateTime.UtcNow;
            await _repo.AddAsync(member);
            await _repo.SaveChangesAsync();
        }

        public async Task DeleteAsync(int nid)
        {
            var e = await _repo.GetByIdAsync(nid);
            if (e == null) return;
            await _repo.DeleteAsync(e);
            await _repo.SaveChangesAsync();
        }

        public async Task<IEnumerable<Member>> GetAllAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<Member?> GetByIdAsync(int nid)
        {
            return await _repo.GetByIdAsync(nid);
        }

        public async Task<Member?> GetByMidAsync(string mid)
        {
            return await _repo.GetByMidAsync(mid);
        }

        public async Task UpdateAsync(Member member)
        {
            member.ModifyDate = DateTime.UtcNow;
            await _repo.UpdateAsync(member);
            await _repo.SaveChangesAsync();
        }

        public async Task ImportCsvAsync(IFormFile csvFile)
        {
            using var stream = csvFile.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var records = csv.GetRecords<Member>().ToList();
            foreach (var r in records)
            {
                var existing = await _repo.GetByMidAsync(r.Mid);
                if (existing != null)
                {
                    r.Nid = existing.Nid;
                    await _repo.UpdateAsync(r);
                }
                else
                {
                    await _repo.AddAsync(r);
                }
            }
            await _repo.SaveChangesAsync();
        }

        public async Task<byte[]> ExportCsvAsync()
        {
            var members = await _repo.GetAllAsync();
            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, Encoding.UTF8, leaveOpen: true);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(members);
            await writer.FlushAsync();
            ms.Position = 0;
            return ms.ToArray();
        }

        public async Task<IEnumerable<object>> GetTreeAsync()
        {
            var all = (await _repo.GetAllAsync()).ToList();
            // 以 Mid 與 ParentMid 建立樹狀結構（簡單版本）
            var lookup = all.ToLookup(m => m.ParentMid);
            List<object> Build(string? parent)
            {
                var children = lookup[parent];
                var list = new List<object>();
                foreach (var c in children)
                {
                    list.Add(new
                    {
                        id = c.Mid,
                        text = c.Name ?? c.Mid,
                        children = Build(c.Mid)
                    });
                }

                return list;
            }

            return Build(null);
        }

        public async Task SendExpiryNotificationsAsync(int withinDays)
        {
            var expiring = (await _repo.GetExpiringWithinDaysAsync(withinDays)).Where(m => !string.IsNullOrEmpty(m.FcmToken));
            if (!expiring.Any()) return;

            var fcmKey = _config["Fcm:ServerKey"];
            if (string.IsNullOrEmpty(fcmKey)) return;

            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"key={fcmKey}");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Sender", $"id={_config["Fcm:SenderId"]}");

            foreach (var member in expiring)
            {
                var payload = new
                {
                    to = member.FcmToken,
                    notification = new
                    {
                        title = "會員到期通知",
                        body = $"您好 {member.Name ?? member.Mid}，您的會員將在 {member.ContinueDate:yyyy-MM-dd} 到期，請提前續約。"
                    }
                };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                // 使用 FCM legacy endpoint
                await client.PostAsync("https://fcm.googleapis.com/fcm/send", content);
            }
        }

        // 產生重設密碼的 token（儲存在會員資料）
        public async Task<string?> GeneratePasswordResetTokenAsync(string midOrEmail)
        {
            Member? member = null;
            if (!string.IsNullOrEmpty(midOrEmail))
            {
                member = await _repo.GetByMidAsync(midOrEmail);
                if (member == null)
                {
                    member = await _repo.GetByEmailAsync(midOrEmail);
                }
            }

            if (member == null) return null;

            var token = Guid.NewGuid().ToString("N");
            member.PasswordResetToken = token;
            member.PasswordResetExpiry = DateTime.UtcNow.AddHours(2);
            await _repo.UpdateAsync(member);
            await _repo.SaveChangesAsync();

            // TODO: 實作 Email 發送，現在回傳 token / reset link 由 caller 處理（或用郵件發送）
            return token;
        }

        // 使用 token 重設密碼
        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(newPassword))
                return false;

            var member = await _repo.GetByResetTokenAsync(token);
            if (member == null) return false;
            if (!member.PasswordResetExpiry.HasValue || member.PasswordResetExpiry.Value < DateTime.UtcNow) return false;

            // 生產環境務必對密碼做雜湊
            member.Pwd = newPassword;
            member.PasswordResetToken = null;
            member.PasswordResetExpiry = null;

            await _repo.UpdateAsync(member);
            await _repo.SaveChangesAsync();
            return true;
        }
    }
}