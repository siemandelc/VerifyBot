using Discord;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VerifyBot.Gw2Api;
using VerifyBot.Models;

namespace VerifyBot.Services
{
    public class ReverifyService
    {
        private readonly IDiscordClient client;

        private readonly VerifyContext db;

        private Configuration config;

        public ReverifyService(IDiscordClient client, Configuration config)
        {
            this.client = client;
            this.config = config;
            this.db = new VerifyContext();
        }

        private bool InProgress;

        public async Task Process(IMessage message)
        {
            if (message.Channel is IGuildChannel && message.Author is IGuildUser)
            {
                if ((message.Channel as IGuildChannel).Name != this.config.AdminChannel)
                {
                    return;
                }

                var author = message.Author as IGuildUser;
                var adminRole = author.Guild.Roles.FirstOrDefault(x => x.Name == this.config.AdminRole);

                if (adminRole == null)
                {
                    return;
                }

                if (!author.Roles.Contains(adminRole))
                {
                    return;
                }
                              
                if (!message.Content.ToLower().Contains("!checkusers"))
                {
                    return;
                }
            }

            await this.CheckUsers();
        }

        private async Task CheckUsers()
        {
            var users = await this.db.Users.ToListAsync();
            var server = await this.client.GetGuildAsync(this.config.ServerID);

            var role = server.Roles.Where(x => x.Name == this.config.VerifyRole)?.FirstOrDefault();

            foreach (var user in users)
            {                
                try
                {
                    var api = new ApiFacade(user.APIKey);
                    var account = await api.GetAccountAsync();

                    if (account == null)
                    {
                        Console.WriteLine($"Error reverifying this user: {user.DiscordID} - {user.APIKey}");
                        continue;
                    }

                    bool stillValid = true;

                    if (!this.config.WorldIDs.Contains(account.WorldId))
                    {
                        stillValid = false;
                    }

                    if (stillValid)
                    {
                        continue;
                    }

                    // Remove perms.
                    var discordUser = await server.GetUserAsync(user.DiscordID);

                    if (discordUser != null)
                    {
                        var roles = discordUser.Roles.ToList();

                        roles.Remove(role);

                        await discordUser.ModifyAsync(x =>
                        {
                            x.Roles = roles;
                        });
                    }

                    this.db.Users.Remove(user);
                    await this.db.SaveChangesAsync();             
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex}");
                }
            }
        }
    }
}
