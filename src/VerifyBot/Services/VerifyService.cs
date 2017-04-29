using Discord;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VerifyBot.Gw2Api;
using VerifyBot.Models;

namespace VerifyBot.Services
{
    public class VerifyService
    {
        private const int APIKeyLength = 72;
        private static readonly Regex AccountNameApiKeyRegex = new Regex(@"\s*(.+?\.\d+)\s+(.*?-.*?-.*?-.*?-.*)\s*$");
        private readonly UserStrings strings;

        public VerifyService(string accountId, string accountName, string apiKey, Manager manager, IUser requestor, UserStrings strings, IMessageChannel channel)
        {
            AccoutId = accountId;
            AccountName = accountName;
            APIKey = apiKey;
            Requestor = requestor;
            Channel = channel;
            Manager = manager;

            this.strings = strings;

            API = new ApiFacade(APIKey);

            HasValidCharacter = false;            
        }

        public Account Account { get; private set; }

        public string AccoutId { get; }

        public string AccountName { get; }

        public string APIKey { get; }

        public IMessageChannel Channel { get; }

        public int World
        {
            get
            {
                return this.Account?.WorldId ?? -999;
            }
        }

        public bool IsValid => IsValidAccount && HasValidCharacter;

        public IUser Requestor { get; }

        private ApiFacade API { get; }

        private bool HasValidCharacter { get; set; }

        private bool IsValidAccount => Account != null;

        private Manager Manager { get; }

        public static VerifyService Create(string accountName, string apiKey, Manager manager, IUser requestor, UserStrings strings, IMessageChannel channel = null)
        {
            return new VerifyService(accountName, null, apiKey, manager, requestor, strings, channel);
        }

        public static async Task<VerifyService> CreateFromRequestMessage(IMessage requestMessage, Manager manager, UserStrings strings)
        {
            var tokens = AccountNameApiKeyRegex.Split(requestMessage.Content);

            if (tokens.Length != 4)
            {
                await requestMessage.Channel.SendMessageAsync(strings.ParseError);
                Console.WriteLine($"Could not verify {requestMessage.Author.Username} - Bad # of arguments");
                return null;
            }

            if (tokens[2].Length != APIKeyLength)
            {
                await requestMessage.Channel.SendMessageAsync(strings.InvalidAPIKey);
                Console.WriteLine($"Could not verify {requestMessage.Author.Username} - Bad API Key");
                return null;
            }

            return new VerifyService(null, tokens[1], tokens[2], manager, requestMessage.Author, strings, requestMessage.Channel);
        }

        public async Task<IUserMessage> SendMessageAsync(string message)
        {
            if (Channel == null)
                return null; // Task.FromResult<object>(null);
            return await Channel.SendMessageAsync(message);
        }

        public async Task Validate(bool isReverify)
        {
            await ValidateAccount(isReverify);
            if (IsValidAccount)
                await ValidateCharacters();
        }

        public async Task LoadAccount()
        {
            var account = await API.GetAccountAsync();

            if (account != null)
            {
                Account = account;
            }           
        }

        private async Task ValidateAccount(bool isReverify)
        {
            var account = await API.GetAccountAsync();

            if (account == null)
            {
                await SendMessageAsync(this.strings.AccountNotInAPI);
                Console.WriteLine($"Could not verify {Requestor.Username} - Cannont access account in GW2 API.");
                return;
            }

            if (isReverify)
            {
                if (account.Id != AccoutId)
                {                    
                    Console.WriteLine($"Could not verify {Requestor.Username} - API Key account does not match supplied account ID.");
                    return;
                }
            }
            else
            {
                if (account.Name != AccountName)
                {
                    await SendMessageAsync(this.strings.AccountNameDoesNotMatch);
                    Console.WriteLine($"Could not verify {Requestor.Username} - API Key account does not match supplied account. (Case matters)");
                    return;
                }
            }

            if (!Manager.IsAccountOnOurWorld(account))
            {
                await SendMessageAsync(this.strings.AccountNotOnServer);
                Console.WriteLine($"Could not verify {Requestor.Username} - Not on Server.");
                return;
            }

            Account = account;
        }

        private async Task ValidateCharacters()
        {           
            if (Account.Access == "PlayForFree")
            {
                var characters = await API.GetCharactersAsync();

                var isWvWLevel = false;
                foreach (var character in characters)
                {
                    var characterObj = await API.GetCharacterAsync(character);

                    if (characterObj.Level >= 60)
                    {
                        isWvWLevel = true;
                        break;
                    }
                }

                if (!isWvWLevel)
                {
                    await SendMessageAsync(this.strings.NotValidLevel);
                    Console.WriteLine($"Could not verify {Requestor.Username} - Not elgible for WvW.");
                    return;
                }
            }

            HasValidCharacter = true;
        }
    }
}