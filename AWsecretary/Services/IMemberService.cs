using System.Collections.Generic;
using System.Threading.Tasks;
using AWsecretary.Models;
using Microsoft.AspNetCore.Http;

namespace AWsecretary.Services
{
    public interface IMemberService
    {
        Task<IEnumerable<Member>> GetAllAsync();
        Task<Member?> GetByIdAsync(int nid);
        Task<Member?> GetByMidAsync(string mid);
        Task CreateAsync(Member member);
        Task UpdateAsync(Member member);
        Task DeleteAsync(int nid);
        Task ImportCsvAsync(IFormFile csvFile);
        Task<byte[]> ExportCsvAsync();
        Task<IEnumerable<object>> GetTreeAsync();
        Task SendExpiryNotificationsAsync(int withinDays);

        // §Ñ°O±K½X/­«³]±K½X
        Task<string?> GeneratePasswordResetTokenAsync(string midOrEmail);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
    }
}