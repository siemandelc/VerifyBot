using Discord;
using Discord.Net.Providers.WS4Net;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using VerifyBot.Models;

namespace VerifyBot.Factories
{
    public static class DiscordClientFactory
    {
        public static async Task<DiscordSocketClient> Get(Configuration config)
        {
            DiscordSocketClient client;

            if (System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Microsoft Windows 6"))
            {
                client = new DiscordSocketClient(new DiscordSocketConfig()
                {
                    LogLevel = LogSeverity.Info,                   
                    WebSocketProvider = WS4NetProvider.Instance
                });
            }
            else
            {
                client = new DiscordSocketClient();
            }

            client.Log += (m) =>
           {
               Console.WriteLine(m.ToString());
               return Task.CompletedTask;
           };
            
            await client.LoginAsync(TokenType.Bot, config.DiscordToken);
            await client.StartAsync();
         
            var ready = false;
            client.Ready += () =>
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

            return client;
        }
    }
}