using Discord;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace VerifyBot.Services
{
    public class RemindVerifyService
    {
        private readonly Manager manager;

        public RemindVerifyService(Manager manager)
        {
            this.manager = manager;
        }

        public async Task Process()
        {
            try
            {
                await RemindUsers();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while checking: {ex}");
            }
        }

        private async Task RemindUsers()
        {
            var verifyRoleId = manager.VerifyRoleId;

            var allUsers = await manager.getDiscordUsers();
            var unverifiedUsers = allUsers.Where(u => !manager.isUserVerified(u));

            foreach (var user in unverifiedUsers)
            {
                var channel = await user.CreateDMChannelAsync();
                await channel.SendMessageAsync(VerifyStrings.VerificationReminder);
                Console.WriteLine($"reminded {user.Nickname} ({user.Id})");
                await channel.CloseAsync();
            }
        }

        public async Task SendInstructions(IGuildUser user)
        {
            var channel = await user.CreateDMChannelAsync();

            if (manager.isUserVerified(user))
            {
                await channel.SendMessageAsync(VerifyStrings.AccountAlreadyVerified);
            }
            else
            {
                await channel.SendMessageAsync(VerifyStrings.VerificationReminder);
                Console.WriteLine($"Instructed {user.Nickname} ({user.Id})");
            }

            await channel.CloseAsync();
        }
    }
}
