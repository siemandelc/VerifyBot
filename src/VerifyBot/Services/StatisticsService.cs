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

                var timestamp = DateTime.Now;
                var count = 0;

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

                        counts[verifier.World] = counts[verifier.World]++;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"Could not load information for user {discordUser.Nickname ?? discordUser.Username} ({dbUser.APIKey})");
                    }

                    count++;

                    if (count >= 450 && (DateTime.Now - timestamp).Seconds < 60)
                    {
                        Console.WriteLine("Rate Limited, waiting");
                        await Task.Delay((DateTime.Now - timestamp).Seconds + 1);

                        count++;
                        timestamp = DateTime.Now;

                        Console.WriteLine("Resuming stats");
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