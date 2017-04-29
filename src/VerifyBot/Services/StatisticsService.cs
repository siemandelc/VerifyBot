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
            this.strings = strings;
        }

        public async Task GetStatistics()
        {
            try
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
                    try
                    {
                        var verifier = VerifyService.Create(dbUser.AccountID, dbUser.APIKey, manager, discordUser, strings);

                        await verifier.LoadAccount();

                        if (!counts.ContainsKey(verifier.World))
                        {
                            counts.Add(verifier.World, 0);
                        }
                        
                        counts[verifier.World] = counts[verifier.World] + 1;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"Could not load information for user {discordUser.Username} ({dbUser.APIKey})");
                    }                
                }

                foreach (var value in counts)
                {
                    Console.WriteLine($"World [{value.Key}]: {value.Value}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }
    }
}