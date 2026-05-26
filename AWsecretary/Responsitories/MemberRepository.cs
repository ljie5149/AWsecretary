using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AWsecretary.Data;
using AWsecretary.Models;

namespace AWsecretary.Responsitories
{
    public class MemberRepository : IMemberRepository
    {
        private readonly ApplicationDbContext _db;

        public MemberRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Member entity)
        {
            await _db.Members.AddAsync(entity);
        }

        public async Task DeleteAsync(Member entity)
        {
            _db.Members.Remove(entity);
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<Member>> GetAllAsync()
        {
            return await _db.Members.AsNoTracking().ToListAsync();
        }

        public async Task<Member?> GetByIdAsync(object id)
        {
            if (id is int nid)
                return await _db.Members.FindAsync(nid);
            return null;
        }

        public async Task<Member?> GetByMidAsync(string mid)
        {
            return await _db.Members.AsNoTracking().FirstOrDefaultAsync(m => m.Mid == mid);
        }

        public async Task<Member?> GetBySidAsync(string sid)
        {
            if (string.IsNullOrEmpty(sid)) return null;
            return await _db.Members.AsNoTracking().FirstOrDefaultAsync(m => m.Sid == sid);
        }

        public async Task<IEnumerable<Member>> GetChildrenAsync(string parentMid)
        {
            return await _db.Members.AsNoTracking()
                .Where(m => m.ParentMid == parentMid)
                .ToListAsync();
        }

        public async Task<IEnumerable<Member>> GetExpiringWithinDaysAsync(int days)
        {
            var now = System.DateTime.UtcNow;
            var cutoff = now.AddDays(days);
            return await _db.Members.AsNoTracking()
                .Where(m => m.ContinueDate.HasValue && m.ContinueDate.Value <= cutoff)
                .ToListAsync();
        }

        public async Task UpdateAsync(Member entity)
        {
            _db.Members.Update(entity);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }

        public async Task<Member?> GetByResetTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            return await _db.Members.FirstOrDefaultAsync(m => m.PasswordResetToken == token);
        }

        public async Task<Member?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email)) return null;
            return await _db.Members.AsNoTracking().FirstOrDefaultAsync(m => m.Email == email);
        }
    }
}