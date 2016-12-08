using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace VerifyBot.Models
{
    public class VerifyDatabase : DbContext
    {
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=Users.db");
        }

        public async Task AddOrUpdateUser(string accountId, string apiKey, ulong discordId)
        {
            var existingUser = Users.FirstOrDefault(x => x.AccountID == accountId);

            if (existingUser != null)
            {
                existingUser.DiscordID = discordId;
            }
            else
            {
                Users.Add(new User()
                {
                    AccountID = accountId,
                    APIKey = apiKey,
                    DiscordID = discordId
                });
            }

            await SaveChangesAsync();
        }
    }
}
