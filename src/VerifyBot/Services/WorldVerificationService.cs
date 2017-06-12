using Discord;
using System;
using System.Threading.Tasks;
using VerifyBot.Models;

namespace VerifyBot.Services
{
    public class WorldVerificationService
    {
        private readonly Manager manager;

        private readonly UserStrings strings;

        public WorldVerificationService(Manager manager, UserStrings strings)
        {
            this.manager = manager;
            this.strings = strings;
        }

        public async Task Process(IMessage e)
        {
            try
            {
                Console.WriteLine($"Message: {e.Content}");

                Console.WriteLine($"Begin verification for {e.Author.Username}");
                await e.Channel.SendMessageAsync("Starting Verification Process...");

                if (e.Author.Status == UserStatus.Invisible)
                {
                    await e.Channel.SendMessageAsync("You cannot be set to invisible while Verifying. Please change your discord status to Online");
                    return;
                }

                var request = await VerifyService.CreateFromRequestMessage(e, manager, this.strings);

                if (request == null)
                {
                    return;
                }

                await request.Validate(false);

                if (!request.IsValid)
                    return;

                await manager.VerifyUser(request.Requestor.Id, request.Account.Id, request.APIKey);

                await e.Channel.SendMessageAsync(this.strings.EndMessage);
                Console.WriteLine($"{e.Author.Username} Verified.");
            }
            catch (Exception ex)            {
                
                await e.Channel.SendMessageAsync(this.strings.ErrorMessage);
                Console.WriteLine($"Error: {ex.ToString()}");                
            }
        }
    }
}