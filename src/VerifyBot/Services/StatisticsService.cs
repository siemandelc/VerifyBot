using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VerifyBot.Models;

namespace VerifyBot.Services
{
    public class StatisticsService
    {
        private readonly Manager manager;

        private readonly UserStrings strings;

        public StatisticsService(Manager manager, UserStrings strings)
        {
            this.manager = manager;
        }

        public async Task GetStatistics()
        {
            Console.WriteLine("Calculating Statistics");

            var counts = new Dictionary<int, int>();
            var discordUsers = await manager.GetDiscordUsers();

            foreach (var discordUser in discordUsers)
            {
                var dbUser = await manager.GetDatabaseUser(discordUser.Id);

                if (dbUser == null)
                {
                    continue;
                }

                var verifier = VerifyService.Create(dbUser.AccountID, dbUser.APIKey, manager, discordUser, strings);

                await verifier.LoadAccount();

                if (!counts.ContainsKey(verifier.World))
                {
                    counts.Add(verifier.World, 0);
                }

                counts[verifier.World] = counts[verifier.World]++;
            }

            foreach (var value in counts)
            {
                Console.WriteLine($"World [{value.Key}]: {value.Value}");
            }
        }
    }
}