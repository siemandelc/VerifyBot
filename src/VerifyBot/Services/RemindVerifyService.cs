using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VerifyBot.Models;

namespace VerifyBot.Services
{
    public class RemindVerifyService
    {
        private readonly IDiscordClient client;

        private readonly Configuration config;

        public RemindVerifyService(IDiscordClient client, Configuration config)
        {
            this.client = client;
            this.config = config;
        }

        public async Task Process()
        {
            try
            {
                await this.RemindUsers();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while checking: {ex}");
            }
        }

        private async Task RemindUsers()
        {
            var server = await this.client.GetGuildAsync(this.config.ServerID);
            var role = server.Roles.Where(x => x.Name == this.config.VerifyRole)?.FirstOrDefault();

            var discordUsers = await server.GetUsersAsync();

            discordUsers = discordUsers.Where(x => !x.Roles.Contains(role)).ToList();

            foreach (var discordUser in discordUsers)
            {
                var channel = await discordUser.CreateDMChannelAsync();
                await channel.SendMessageAsync(VerifyStrings.VerificationReminder);
                Console.WriteLine($"reminded {discordUser.Nickname} ({discordUser.Id})");
            }
        }

        public async Task SendInstructions(IGuildUser user)
        {
            var server = await this.client.GetGuildAsync(this.config.ServerID);
            var role = server.Roles.Where(x => x.Name == this.config.VerifyRole)?.FirstOrDefault();

            var channel = await user.CreateDMChannelAsync();

            if (user.Roles.Contains(role))
            {
                await channel.SendMessageAsync(VerifyStrings.AccountAlreadyVerified);
            }
            else
            {
                await channel.SendMessageAsync(VerifyStrings.VerificationReminder);
                Console.WriteLine($"Instructed {user.Nickname} ({user.Id})");
            }                      
        }
    }
}
