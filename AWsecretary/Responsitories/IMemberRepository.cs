using System.Collections.Generic;
using System.Threading.Tasks;
using AWsecretary.Models;

namespace AWsecretary.Responsitories
{
    public interface IMemberRepository : IRepository<Member>
    {
        Task<Member?> GetByMidAsync(string mid);
        Task<IEnumerable<Member>> GetChildrenAsync(string parentMid);
        Task<IEnumerable<Member>> GetExpiringWithinDaysAsync(int days);
        Task<Member?> GetByResetTokenAsync(string token);
        Task<Member?> GetByEmailAsync(string email);
        Task<Member?> GetBySidAsync(string sid);
    }
}