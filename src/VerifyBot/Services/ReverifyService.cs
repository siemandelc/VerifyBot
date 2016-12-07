using System;
using System.Linq;
using System.Threading.Tasks;

namespace VerifyBot.Services
{
    public class ReverifyService
    {
        private readonly Manager manager;

        public ReverifyService(Manager manager)
        {
            this.manager = manager;
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

            var discordUsers = await manager.getDiscordUsers();
            var verifiedNonBotUsers = discordUsers.Where(u => !(u.IsBot || !manager.isUserVerified(u)));

            foreach (var discordUser in verifiedNonBotUsers)
            {
                var dbUser = await manager.getDatabaseUser(discordUser.Id);
                if (dbUser == null)
                { 
                    await manager.unverifyUser(discordUser, dbUser);
                    continue;
                }

                var attempts = 0;
                while (attempts < 3)
                {
                    try
                    {
                        var verifier = Verifier.create(dbUser.AccountID, dbUser.APIKey, manager, discordUser);
                        await verifier.validate();

                        if (!verifier.isValid)
                            Console.WriteLine($"User {discordUser.Nickname ?? discordUser.Username} is still valid");
                        else
                            await manager.unverifyUser(discordUser, dbUser);

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