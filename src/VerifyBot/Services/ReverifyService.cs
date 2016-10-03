using Discord;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using VerifyBot.Gw2Api;
using VerifyBot.Models;

namespace VerifyBot.Services
{
    public class ReverifyService
    {
        private readonly IDiscordClient client;

        private readonly Configuration config;

        public ReverifyService(IDiscordClient client, Configuration config)
        {
            this.client = client;
            this.config = config;
        }

        public async Task Process()
        {
            try
            {
                await this.CheckUsers();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while checking: {ex}");
            }
        }

        private async Task RemoveDatabaseUser(VerifyContext db, IGuildUser discordUser, User user, IRole role)
        {
            var roles = discordUser.Roles.ToList();

            roles.Remove(role);

            await discordUser.ModifyAsync(x =>
            {
                x.Roles = roles;
            });

            db.Users.Remove(user);
            await db.SaveChangesAsync();

            Console.WriteLine($"User {discordUser.Nickname} is no longer valid");
        }

        private async Task RemoveUser(IGuildUser user, IRole role)
        {
            var roles = user.Roles.ToList();

            roles.Remove(role);

            await user.ModifyAsync(x =>
            {
                x.Roles = roles;
            });

            Console.WriteLine($"Manually verified user {user.Nickname} is no longer valid");
        }

        private async Task CheckUsers()
        {
            var server = await this.client.GetGuildAsync(this.config.ServerID);
            var role = server.Roles.Where(x => x.Name == this.config.VerifyRole)?.FirstOrDefault();

            var discordUsers = await server.GetUsersAsync();

            foreach (var discordUser in discordUsers)
            {
                using (var db = new VerifyContext())
                {
                    var ran = false;
                    var faultCount = 0;

                    var user = await db.Users.FirstOrDefaultAsync(x => x.DiscordID == discordUser.Id);

                    if (user == null)
                    {
                        await this.RemoveUser(discordUser, role);
                        continue;
                    }

                    while (ran == false)
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
                                Console.WriteLine($"User {discordUser.Nickname} is still valid");
                                ran = true;
                                continue;
                            }

                            // Remove perms.
                            await this.RemoveDatabaseUser(db, discordUser, user, role);

                            ran = true;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: {ex}");
                            faultCount++;

                            if (faultCount > 3)
                            {
                                Console.WriteLine($"Error removing user {discordUser.Nickname} ({discordUser.Id})");
                                //await this.RemoveDatabaseUser(db, discordUser, user, role);
                                ran = true;
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Reverification process complete");
        }
    }
}