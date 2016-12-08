using Discord;
using System;
using System.Threading.Tasks;

namespace VerifyBot.Services
{
    public class WorldVerificationService
    {
        private readonly Manager manager;

        public WorldVerificationService(Manager manager)
        {
            this.manager = manager;
        }

        public async Task Process(IMessage e)
        {
            try
            {
                Console.WriteLine($"Begin verification for {e.Author.Username}");
                await e.Channel.SendMessageAsync("Starting Verification Process...");

                var request = await Verifier.CreateFromRequestMessage(e, manager);
                await request.Validate();

                if (!request.IsValid)
                    return;

                await manager.VerifyUser(request.Requestor.Id, request.Account.Id, request.APIKey);

                await e.Channel.SendMessageAsync(VerifyStrings.EndMessage);
                Console.WriteLine($"{e.Author.Username} Verified.");
            }
            catch (Exception ex)
            {
                await e.Channel.SendMessageAsync(VerifyStrings.ErrorMessage);
                Console.WriteLine($"Error: {ex.ToString()}");
            }
        }
    }
}