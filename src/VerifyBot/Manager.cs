using Discord;
using Discord.WebSocket;
using DL.GuildWars2Api.Models.V2;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VerifyBot.Models;

namespace VerifyBot
{
    public class Manager
    {
        private readonly VerifyDatabase db = new VerifyDatabase();

        private bool isInitialized;

        public Manager(DiscordSocketClient client, Configuration config)
        {
            this.DiscordClient = client;
            this.Config = config;

            Initialize().Wait();
        }

        public IRole VerifyRole { get; private set; }

        public ulong VerifyRoleId { get { return VerifyRole.Id; } }

        private Configuration Config { get; set; }

        private IGuild Discord { get; set; }

        private IDiscordClient DiscordClient { get; set; }

        public async Task<User> GetDatabaseUser(ulong discordId)
        {
            return await db.Users.FirstOrDefaultAsync(x => x.DiscordID == discordId);
        }

        public async Task<IGuildUser> GetDiscordUser(ulong id)
        {            
            return await Discord.GetUserAsync(id);
        }

        public async Task<IReadOnlyCollection<IGuildUser>> GetDiscordUsers()
        {
            return await Discord.GetUsersAsync();
        }

        public bool IsAccountOnOurWorld(Account account)
        {
            return Config.WorldIds.Contains(account.WorldId);
        }

        public bool IsUserVerified(IGuildUser user)
        {
            if ((user?.RoleIds?.Count ?? 0) == 0)
            {
                return false;
            }

            return user.RoleIds.Contains(VerifyRoleId);
        }

        public async Task UnverifyUser(IGuildUser discordUser, User dbUser = null)
        {
            try
            {
                //// Can't remove @everyone role.
                var everyone = Discord.Roles.FirstOrDefault(x => x.Name == "@everyone");
                var rolesToRemove = new List<IRole>();

                foreach (var roleId in discordUser.RoleIds)
                {
                    if (roleId == everyone.Id)
                    {
                        continue;
                    }

                    rolesToRemove.Add(Discord.GetRole(roleId));                    
                }

                await discordUser.RemoveRolesAsync(rolesToRemove);

                if (dbUser == null)
                    dbUser = await GetDatabaseUser(discordUser.Id);

                if (dbUser != null)
                {
                    db.Users.Remove(dbUser);
                    await db.SaveChangesAsync();
                    Console.WriteLine($"User {discordUser.Nickname ?? discordUser.Username} is no longer valid");
                }
                else
                {
                    Console.WriteLine($"Manually verified user {discordUser.Nickname ?? discordUser.Username} is no longer valid");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public async Task VerifyUser(ulong discordId, string accountId, string apiKey)
        {
            if (!isInitialized)
            {
                await Initialize();
            }

            if (VerifyRole == null)
            {
                throw new NullReferenceException("Verified User Role isn't set.");
            }
                                
            var user = await GetDiscordUser(discordId);

            if (user == null)
            {
                /// User is offline, notify
                
            }

            if (!IsUserVerified(user))
                await user.AddRoleAsync(VerifyRole);

            await db.AddOrUpdateUser(accountId, apiKey, discordId);
        }

        private async Task Initialize()
        {            
            Discord = await DiscordClient.GetGuildAsync(Config.ServerId);

            // set verify role id
            VerifyRole = Discord.Roles.Where(x => x.Name == Config.VerifyRole)?.FirstOrDefault();

            if (VerifyRole == null)
            {
                var msg = $"Unable to find server role matching verify role config of '{Config.VerifyRole}'.";
                Console.WriteLine(msg);
                throw new InvalidOperationException(msg);                
            }

            isInitialized = true;
        }
    }
}