using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VerifyBot.Gw2Api;
using VerifyBot.Models;

namespace VerifyBot
{
    public class Manager : IDisposable
    {
        private readonly VerifyDatabase db = new VerifyDatabase();

        public Manager(DiscordSocketClient client, Configuration config)
        {
            discordClient = client;
            this.config = config;

            Initialize().Wait();
        }

        public IRole verifyRole { get; private set; }

        public ulong verifyRoleId { get { return verifyRole.Id; } }

        private Configuration config { get; set; }

        private IGuild discord { get; set; }

        private IDiscordClient discordClient { get; set; }

        public async Task<User> GetDatabaseUser(ulong discordId)
        {
            return await db.Users.FirstOrDefaultAsync(x => x.DiscordID == discordId);
        }

        public async Task<IGuildUser> GetDiscordUser(ulong id)
        {
            return await discord.GetUserAsync(id);
        }

        public async Task<IReadOnlyCollection<IGuildUser>> GetDiscordUsers()
        {
            return await discord.GetUsersAsync();
        }

        public bool IsAccountOnOurWorld(Account account)
        {
            return config.WorldIds.Contains(account.WorldId);
        }

        public bool IsUserVerified(IGuildUser user)
        {
            if ((user?.RoleIds?.Count ?? 0) == 0)
            {
                return false;
            }

            return user.RoleIds.Contains(verifyRoleId);
        }

        public async Task UnverifyUser(IGuildUser discordUser, User dbUser = null)
        {
            try
            {
                //// Can't remove @everyone role.
                var everyone = discord.Roles.FirstOrDefault(x => x.Name == "@everyone");
                var rolesToRemove = new List<IRole>();

                foreach (var roleId in discordUser.RoleIds)
                {
                    if (roleId == everyone.Id)
                    {
                        continue;
                    }

                    rolesToRemove.Add(discord.GetRole(roleId));                    
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
            var user = await GetDiscordUser(discordId);

            if (!IsUserVerified(user))
                await user.AddRoleAsync(verifyRole);

            await db.AddOrUpdateUser(accountId, apiKey, discordId);
        }

        private async Task Initialize()
        {
            discord = await discordClient.GetGuildAsync(config.ServerId);

            // set verify role id
            verifyRole = discord.Roles.Where(x => x.Name == config.VerifyRole)?.FirstOrDefault();

            if (verifyRole == null)
            {
                var msg = $"Unable to find server role matching verify role config of '{config.VerifyRole}'.";
                Console.WriteLine(msg);
                throw new InvalidOperationException(msg);
            }
        }

        #region IDisposeable

        private bool disposedValue = false; // To detect redundant calls

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    db.Dispose();
                disposedValue = true;
            }
        }

        #endregion IDisposeable
    }
}