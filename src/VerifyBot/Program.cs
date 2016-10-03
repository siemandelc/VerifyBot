using Discord;
using Discord.WebSocket;
using System;
using System.Threading;
using System.Threading.Tasks;
using VerifyBot.Service;
using VerifyBot.Services;

namespace VerifyBot
{
    public class Program
    {
        private readonly int interval = 86400000;
        private readonly Timer timer;
        private DiscordSocketClient client;
        private ConfigurationService configService;

        public async Task Run()
        {
            try
            {
                this.CheckIfDatabaseExists();

                this.configService = new ConfigurationService();
                this.client = new DiscordSocketClient();

                var config = this.configService.GetConfiguration();

                var verify = new WorldVerificationService(this.client, config);
                var reverify = new ReverifyService(this.client, config);

                await client.LoginAsync(TokenType.Bot, Helper.SecretsReader.GetSecret("discord_token"));
                await client.ConnectAsync();

                var me = await this.client.GetCurrentUserAsync();

                client.MessageReceived += async (message) =>
                {
                    if (message.Author.IsBot)
                    {
                        return;
                    }

                    await verify.Process(message);
                };

                client.UserJoined += async (userCandidate) =>
                {
                    var user = userCandidate as IGuildUser;

                    if (user == null)
                    {
                        return;
                    }

                    var pm = await user.CreateDMChannelAsync();

                    await pm.SendMessageAsync(VerifyStrings.InitialMessage);
                };

                this.timer = new Timer(this.RunVerification, reverify, interval, interval);

                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Aplication crashing. Reason: {ex}");
            }
        }

        private static void Main(string[] args) => new Program().Run().GetAwaiter().GetResult();

        private void CheckIfDatabaseExists()
        {
            var path = System.IO.Path.Combine(AppContext.BaseDirectory, "Users.db");

            if (!System.IO.File.Exists(path))
            {
                Console.WriteLine("Database does not exist. Run the following command: dotnet ef database update");
                throw new Exception("No Database");
            }
        }

        private async void RunVerification(object service)
        {
            var verify = service as ReverifyService;

            await verify.Process(null);
        }
    }
}