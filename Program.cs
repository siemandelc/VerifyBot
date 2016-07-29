using Discord;
using System;
using VerifyBot.Service;

namespace VerifyBot
{
    public class Program
    {
        private DiscordClient _client;

        public Program()
        {
            _client = new DiscordClient();
        }

        public void Start()
        {
            try
            {
                var verify = new WorldVerificationService(this._client);

                verify.Start();

                _client.MessageReceived += async (s, e) =>
                {
                    await verify.Process(e);
                };

                _client.ExecuteAndWait(async () =>
                {
                    await _client.Connect(Helper.SecretsReader.GetSecret("discord_token"));
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Aplication crashing. Reason: {ex}");
            }
        }

        private static void Main(string[] args) => new Program().Start();
    }
}