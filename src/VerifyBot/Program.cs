using Discord;
using Discord.WebSocket;
using SimpleInjector;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VerifyBot.Factories;
using VerifyBot.Models;
using VerifyBot.Services;

namespace VerifyBot
{
    public class Program
    {
        private const int dayInterval = 86400000;
        private Container container;
        private Timer reminderTimer;
        private Timer reverifyTimer;

        private DiscordSocketClient _client;

        public async Task Run()
        {
            try
            {
                this.CheckIfDatabaseExists();
                this.LoadContainerAsync();

                Console.WriteLine("Creating new client");

                _client = container.GetInstance<DiscordSocketClient>();

                _client.MessageReceived += MessageReceived;
                _client.UserJoined += UserJoined;
                _client.Disconnected += Disconnected;

                Console.WriteLine("client created & events wired");

                this.reverifyTimer = new Timer(this.RunVerification, container.GetInstance<ReverifyService>(), dayInterval, dayInterval);
                this.reminderTimer = new Timer(this.RemindVerify, container.GetInstance<RemindVerifyService>(), dayInterval, dayInterval * 2);              
                
                Console.WriteLine("Verifybot running");

                while (true)
                {
                    var line = Console.ReadLine();

                    if (line.Equals("reverify"))
                    {
                        await container.GetInstance<ReverifyService>().Process();
                    }

                    if (line.Equals("stats"))
                    {
                        await container.GetInstance<StatisticsService>().GetStatistics();
                    }

                    if (line.Equals("quit"))
                    {
                        return;
                    }

                    if (line.StartsWith("lookup "))
                    {
                        var username = line.Replace("lookup ", "");
                        Console.WriteLine($"Searching for Discord accounts with the GW2 account name of {username}");
                        await container.GetInstance<LookupService>().LookupAsync(username);
                        
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Aplication crashing. Reason: {ex}");
            }
        }

        private async Task Disconnected(Exception arg)
        {
            Console.WriteLine($"Client disconnected: {arg.Message}");

            try
            {
                var config = container.GetInstance<Configuration>();

                await _client.LoginAsync(TokenType.Bot, config.DiscordToken);
                await _client.StartAsync();

                var ready = false;
                _client.Ready += () =>
                {
                    ready = true;
                    return Task.CompletedTask;
                };

                Console.WriteLine("Waiting for DiscordSocketClient to initialize...");

                while (!ready)
                {
                    await Task.Delay(1000);
                    Console.WriteLine("Waiting...");
                }

                Console.WriteLine(" DiscordSocketClient initialized.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Serious exception, program will now close. Exception: {ex.Message}");
                Environment.Exit(-1);
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

        private void LoadContainerAsync()
        {
            this.container = new Container();

            //// Configuration services
            container.Register(ConfigurationFactory.Get, Lifestyle.Singleton);

            //// Client object
            container.Register(() => DiscordClientFactory.Get(ConfigurationFactory.Get()).Result, Lifestyle.Singleton);

            //// Userstrings
            container.Register(UserStringsFactory.Get, Lifestyle.Singleton);

            //// Manager service
            container.Register<Manager>(Lifestyle.Transient);

            //// verify services
            container.Register<WorldVerificationService>(Lifestyle.Transient);
            container.Register<ReverifyService>(Lifestyle.Transient);
            container.Register<RemindVerifyService>(Lifestyle.Transient);
            container.Register<StatisticsService>(Lifestyle.Transient);

            container.Verify();
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task MessageReceived(SocketMessage message)
        {
            try
            {
                if (message.Author.IsBot)
                {
                    return;
                }

                if (message.Channel is IDMChannel)
                {
                    await container.GetInstance<WorldVerificationService>().Process(message);                    
                }

                if (message.Channel is IGuildChannel && message.Content.Contains("!verify"))
                {
                    if (message.Author is IGuildUser)
                    {
                        await container.GetInstance<RemindVerifyService>().SendInstructions(message.Author as IGuildUser);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occured while processing message: {ex.Message}");
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

        private async void RunVerification(object service)
        {
            var verify = service as ReverifyService;

            if (verify == null)
            {
                Console.WriteLine("Service Object is null");
                return;
            }

            await verify.Process();
        }

        private async Task UserJoined(SocketGuildUser userCandidate)
        {
            try
            {
                var user = userCandidate as IGuildUser;

                if (user == null)
                {
                    return;
                }

                var strings = this.container.GetInstance<UserStrings>();
                var pm = await user.GetOrCreateDMChannelAsync();

                await pm.SendMessageAsync(strings.InitialMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occured when sending initial message: {ex.Message}");
            }
        }
    }
}