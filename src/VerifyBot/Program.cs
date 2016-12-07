using Discord;
using Discord.WebSocket;
using System;
using System.Threading;
using System.Threading.Tasks;
using VerifyBot.Services;

namespace VerifyBot
{
    public class Program
    {
        private const int dayInterval = 86400000;
        private Timer reverifyTimer;
        private Timer reminderTimer;
        private DiscordSocketClient client;
        private ConfigurationService configService;
        private Manager manager;

        public async Task Run()
        {
            try
            {
                this.CheckIfDatabaseExists();

                this.client = new DiscordSocketClient();
                await client.LoginAsync(TokenType.Bot, Helper.SecretsReader.GetSecret("discord_token"));
                await client.ConnectAsync();

                this.configService = new ConfigurationService();
                var config = this.configService.GetConfiguration();

                manager = new Manager(client, config);

                var verify = new WorldVerificationService(manager);
                var reverify = new ReverifyService(manager);
                var reminder = new RemindVerifyService(manager);

                var me = this.client.CurrentUser;

                client.MessageReceived += async (message) =>
                {
                    try
                    {
                        if (message.Author.IsBot)
                        {
                            return;
                        }

                        if (message.Channel is IDMChannel)
                        {
                            await verify.Process(message);
                        }

                        if (message.Channel is IGuildChannel && message.Content.Contains("!verify"))
                        {
                            if (message.Author is IGuildUser)
                            {
                                await reminder.SendInstructions(message.Author as IGuildUser);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error occured while processing message: {ex.Message}");
                    }
                };

                client.UserJoined += async (userCandidate) =>
                {
                    try
                    {
                        var user = userCandidate as IGuildUser;

                        if (user == null)
                        {
                            return;
                        }

                        var pm = await user.CreateDMChannelAsync();

                        await pm.SendMessageAsync(VerifyStrings.InitialMessage);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error occured when sending initial message: {ex.Message}");
                    }
                };

                this.reverifyTimer = new Timer(this.RunVerification, reverify, dayInterval, dayInterval);
                this.reminderTimer = new Timer(this.RemindVerify, reminder, dayInterval, dayInterval * 2);

                while (true)
                {
                    var line = Console.ReadLine();

                    if (line.Equals("reverify"))
                    {
                        Console.WriteLine("Reverifying...");
                        await reverify.Process();
                    }

                    if (line.Equals("quit"))
                    {
                        this.client.Dispose();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Aplication crashing. Reason: {ex}");
            }
        }

        private async void RemindVerify(object service)
        {
            var remind = service as RemindVerifyService;

            if (remind == null)
            {
                return;
            }

            await remind.Process();
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

            if (verify == null)
            {
                return;
            }

            await verify.Process();
        }
    }
}