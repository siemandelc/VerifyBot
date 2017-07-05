using Discord;
using System;
using System.Linq;
using System.Threading.Tasks;
using VerifyBot.Models;

namespace VerifyBot.Services
{
    public class RemindVerifyService
    {
        private readonly Manager manager;

        private readonly UserStrings strings;

        public RemindVerifyService(Manager manager, UserStrings strings)
        {
            this.manager = manager;
            this.strings = strings;
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

        public async Task SendInstructions(IGuildUser user)
        {
            var channel = await user.GetOrCreateDMChannelAsync();

            if (manager.IsUserVerified(user))
            {
                await channel.SendMessageAsync(this.strings.AccountAlreadyVerified);
            }
            else
            {
                await channel.SendMessageAsync(this.strings.VerificationReminder);
                Console.WriteLine($"Instructed {user.Nickname} ({user.Id})");
            }

            await channel.CloseAsync();
        }

        private async Task RemindUsers()
        {
            var verifyRoleId = manager.VerifyRoleId;

            var allUsers = await manager.GetDiscordUsers();
            var unverifiedUsers = allUsers.Where(u => !manager.IsUserVerified(u));

            foreach (var user in unverifiedUsers)
            {
                var channel = await user.GetOrCreateDMChannelAsync();
                await channel.SendMessageAsync(this.strings.VerificationReminder);
                Console.WriteLine($"reminded {user.Nickname} ({user.Id})");
                await channel.CloseAsync();
            }
        }
    }
}