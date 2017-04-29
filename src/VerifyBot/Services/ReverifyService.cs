using System;
using System.Linq;
using System.Threading.Tasks;
using VerifyBot.Models;

namespace VerifyBot.Services
{
    public class ReverifyService
    {
        private readonly Manager manager;

        private readonly UserStrings strings;

        public ReverifyService(Manager manager, UserStrings strings)
        {
            this.manager = manager;
            this.strings = strings;
        }

        public async Task Process()
        {
            try
            {
                await CheckUsers();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while checking: {ex}");
            }
        }

        private async Task CheckUsers()
        {
            Console.WriteLine("Reverification process beginning");

            var discordUsers = await manager.GetDiscordUsers();
            var verifiedNonBotUsers = discordUsers.Where(u => !(u.IsBot || !manager.IsUserVerified(u)));

            foreach (var discordUser in verifiedNonBotUsers)
            {
                var dbUser = await manager.GetDatabaseUser(discordUser.Id);
                if (dbUser == null)
                {
                    if (discordUser.GuildPermissions.Administrator)
                    {
                        continue;
                    }

                    await manager.UnverifyUser(discordUser, dbUser);
                    continue;
                }

                var attempts = 0;
                while (attempts < 3)
                {
                    try
                    {                       
                        var verifier = VerifyService.Create(dbUser.AccountID, dbUser.APIKey, manager, discordUser, strings);
                        await verifier.Validate(true);

                        if (verifier.IsValid)
                            Console.WriteLine($"User {discordUser.Nickname ?? discordUser.Username} is still valid");
                        else
                            await manager.UnverifyUser(discordUser, dbUser);

                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error removing user {discordUser.Nickname ?? discordUser.Username} ({discordUser.Id})");
                        Console.WriteLine($"Error: {ex}");
                        attempts++;
                    }
                }
            }

            Console.WriteLine("Reverification process complete");
        }
    }
}