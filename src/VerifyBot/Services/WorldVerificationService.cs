using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VerifyBot.Gw2Api;
using VerifyBot.Models;

namespace VerifyBot.Service
{
    public class WorldVerificationService
    {
        private const string AccountNameApiKeyRegex = @"\s*(.+?\.\d+)\s+(.*?-.*?-.*?-.*?-.*)\s*$";
        private const int APIKeyLength = 72;

        private readonly IDiscordClient client;

        private readonly VerifyContext db;

        private Configuration config;

        public WorldVerificationService(IDiscordClient client, Configuration config)
        {
            this.client = client;
            this.config = config;
            this.db = new VerifyContext();
        }

        public async Task Process(IMessage e)
        {
            try
            {
                if (e.Channel is IDMChannel)
                {
                    await this.PerformVerification(e);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.ToString()}");
            }
        }

        private async Task PerformVerification(IMessage e)
        {
            try
            {
                Console.WriteLine($"Begin verification for {e.Author.Username}");
                await e.Channel.SendMessageAsync("Starting Verification Process...");

                var tokens = new Regex(AccountNameApiKeyRegex).Split(e.Content);

                if (tokens.Length != 4)
                {
                    await e.Channel.SendMessageAsync(VerifyStrings.ParseError);
                    Console.WriteLine($"Could not verify {e.Author.Username} - Bad # of arguments");
                    return;
                }

                if (tokens[2].Length != 72)
                {
                    await e.Channel.SendMessageAsync(VerifyStrings.InvalidAPIKey);
                    Console.WriteLine($"Could not verify {e.Author.Username} - Bad API Key");
                    return;
                }

                // Check GW2 server
                var api = new ApiFacade(tokens[2]);
                var account = await api.GetAccountAsync();
                //var characters = await api.GetCharactersAsync

                if (account == null)
                {
                    await e.Channel.SendMessageAsync(VerifyStrings.AccountNotInAPI);
                    Console.WriteLine($"Could not verify {e.Author.Username} - Cannont access account in GW2 API.");
                    return;
                }

                if (account.Name.ToLower() != tokens[1].ToLower())
                {
                    await e.Channel.SendMessageAsync(VerifyStrings.AccountNameDoesNotMatch);
                    Console.WriteLine($"Could not verify {e.Author.Username} - API Key account does not match supplied account. (Case matters)");
                    return;
                }

                if (!this.config.WorldIDs.Contains(account.WorldId))
                {
                    await e.Channel.SendMessageAsync(VerifyStrings.AccountNotOnServer);
                    Console.WriteLine($"Could not verify {e.Author.Username} - Not on Server.");
                    return;
                }

                
               

                var existingUser = this.db.Users.FirstOrDefault(x => x.AccountID == account.Id);

                if (existingUser != null)
                {
                    existingUser.DiscordID = e.Author.Id;
                }
                else
                {
                    this.db.Users.Add(new User()
                    {
                        AccountID = account.Id,
                        APIKey = tokens[2],
                        DiscordID = e.Author.Id
                    });
                }

                await this.db.SaveChangesAsync();
                                
                var guild = await client.GetGuildAsync(this.config.ServerID);
                var user = await guild.GetUserAsync(e.Author.Id);                               
                var role = guild.Roles.Where(x => x.Name == this.config.VerifyRole)?.FirstOrDefault();

                var currentRoles = user.Roles.ToList();

                if (!currentRoles.Contains(role))
                {
                    currentRoles.Add(role);

                    await user.ModifyAsync(x =>
                    {
                        x.Roles = currentRoles;
                    });
                }

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