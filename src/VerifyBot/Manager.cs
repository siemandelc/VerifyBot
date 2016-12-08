using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;
using VerifyBot.Models;
using VerifyBot.Gw2Api;

namespace VerifyBot
{
    public class Manager : IDisposable
    {
        private IDiscordClient DiscordClient { get; set; }
        private IGuild Discord { get; set; }
        private Configuration Config { get; set; }

        private readonly VerifyDatabase DB = new VerifyDatabase();

        public IRole VerifyRole { get; private set; }
        public ulong VerifyRoleId { get { return VerifyRole.Id; } }

        public Manager(DiscordSocketClient client, Configuration config)
        {
            DiscordClient = client;
            Config = config;

            Initialize();
        }

        private async void Initialize()
        {
            Discord = await DiscordClient.GetGuildAsync(Config.ServerID);

            // set verify role id
            VerifyRole = Discord.Roles.Where(x => x.Name == Config.VerifyRole)?.FirstOrDefault();
            if (VerifyRole == null)
            {
                var msg = $"Unable to find server role matching verify role config of '{Config.VerifyRole}'.";
                Console.WriteLine(msg);
                throw new InvalidOperationException(msg);
            }
        }

        public async Task<IReadOnlyCollection<IGuildUser>> GetDiscordUsers()
        {
            return await Discord.GetUsersAsync();
        }
        public async Task<IGuildUser> GetDiscordUser(ulong id)
        {
            return await Discord.GetUserAsync(id);
        }

        public bool IsUserVerified(IGuildUser user)
        {
            return user.RoleIds.Contains(VerifyRoleId);
        }

        public bool IsAccountOnOurWorld(Account account)
        {
            return Config.WorldIDs.Contains(account.WorldId);
        }

        public async Task VerifyUser(ulong discordId, string accountId, string apiKey)
        {
            var user = await GetDiscordUser(discordId);

            if (!IsUserVerified(user))
                await user.AddRolesAsync(VerifyRole);

            await DB.AddOrUpdateUser(accountId, apiKey, discordId);
        }

        public async Task UnverifyUser(IGuildUser discordUser, User dbUser = null)
        {
            var userRoles = new IRole[discordUser.RoleIds.Count];
            var i = 0;
            foreach (var roleId in discordUser.RoleIds)
                userRoles[i++] = Discord.GetRole(roleId);
            await discordUser.RemoveRolesAsync(userRoles);

            if (dbUser == null)
                dbUser = await GetDatabaseUser(discordUser.Id);

            if (dbUser != null)
            {
                DB.Users.Remove(dbUser);
                await DB.SaveChangesAsync();
                Console.WriteLine($"User {discordUser.Nickname ?? discordUser.Username} is no longer valid");
            }
            else
            {
                Console.WriteLine($"Manually verified user {discordUser.Nickname ?? discordUser.Username} is no longer valid");
            }
        }

        public async Task<User> GetDatabaseUser(ulong discordId)
        {
            return await DB.Users.FirstOrDefaultAsync(x => x.DiscordID == discordId);
        }

        #region IDisposeable
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    DB.Dispose();
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
