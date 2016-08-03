using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VerifyBot.Service;

namespace VerifyBot
{
    public class Program
    {
        private DiscordSocketClient _client;

        static void Main(string[] args) => new Program().Run().GetAwaiter().GetResult();

        public async Task Run()
        {
            try
            {
                _client = new DiscordSocketClient();

                var verify = new WorldVerificationService(this._client);
                verify.Start();

                _client.MessageReceived += async (message) =>
                {
                    await verify.Process(message);
                };

                await _client.LoginAsync(TokenType.Bot, Helper.SecretsReader.GetSecret("discord_token"));
                await _client.ConnectAsync();

                Console.Read();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Aplication crashing. Reason: {ex}");
            }
        }

      

    }
}
