using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VerifyBot.Models;

namespace VerifyBot.Services
{
    public class LookupService
    {
        private readonly Manager manager;

        private readonly UserStrings strings;

        public LookupService(Manager manager, UserStrings strings)
        {
            this.manager = manager;
            this.strings = strings;
        }

        public async Task LookupAsync(string accountName)
        {
            var counts = new Dictionary<int, int>();
            var discordUsers = await manager.GetDiscordUsers();

            foreach (var discordUser in discordUsers)
            {
                var dbUser = await manager.GetDatabaseUser(discordUser.Id);

                if (dbUser == null)
                {
                    continue;
                }

                try
                {
                    var verifier = VerifyService.Create(dbUser.AccountID, dbUser.APIKey, manager, discordUser, strings);

                    await verifier.LoadAccount();

                    if (verifier.AccountName == accountName)
                    {
                        Console.WriteLine($"Account {accountName} found, Discord Name: {discordUser.Nickname ?? discordUser.Username}");
                        return;
                    }                                        
                }
                catch (Exception)
                {
                    Console.WriteLine($"Could not load information for user {discordUser.Username} ({dbUser.APIKey})");
                }
            }

            Console.WriteLine($"Account {accountName} not found");
        }
    }
}
