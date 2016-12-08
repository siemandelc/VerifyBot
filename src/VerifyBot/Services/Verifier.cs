using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using VerifyBot.Gw2Api;

namespace VerifyBot.Services
{
    public class Verifier
    {
        private static readonly Regex AccountNameApiKeyRegex = new Regex(@"\s*(.+?\.\d+)\s+(.*?-.*?-.*?-.*?-.*)\s*$");
        private const int APIKeyLength = 72;

        private Manager Manager { get; set; }
        public string AccoutName { get; private set; }
        public string APIKey { get; private set; }
        public IUser Requestor { get; private set; }
        public IMessageChannel Channel { get; private set; }

        private ApiFacade API { get; set; }

        public Account Account { get; private set; }
        private bool IsValidAccount { get { return Account != null; } }
        private bool HasValidCharacter { get; set; }

        public bool IsValid
        {
            get { return IsValidAccount && HasValidCharacter; }
        }

        public Verifier(string accountName, string apiKey, Manager manager, IUser requestor, IMessageChannel channel)
        {
            AccoutName = accountName;
            APIKey = apiKey;
            Requestor = requestor;
            Channel = channel;
            Manager = manager;

            API = new ApiFacade(APIKey);

            HasValidCharacter = false;
        }

        public async Task<IUserMessage> SendMessageAsync(string message)
        {
            if (Channel == null)
                return null; // Task.FromResult<object>(null);
            return await Channel.SendMessageAsync(message);
        }

        public static Verifier Create(string accountName, string apiKey, Manager manager, IUser requestor, IMessageChannel channel = null)
        {
            return new Verifier(accountName, apiKey, manager, requestor, channel);
        }
        public async static Task<Verifier> CreateFromRequestMessage(IMessage requestMessage, Manager manager)
        {
            var tokens = AccountNameApiKeyRegex.Split(requestMessage.Content);

            if (tokens.Length != 4)
            {
                await requestMessage.Channel.SendMessageAsync(VerifyStrings.ParseError);
                Console.WriteLine($"Could not verify {requestMessage.Author.Username} - Bad # of arguments");
                return null;
            }

            if (tokens[2].Length != APIKeyLength)
            {
                await requestMessage.Channel.SendMessageAsync(VerifyStrings.InvalidAPIKey);
                Console.WriteLine($"Could not verify {requestMessage.Author.Username} - Bad API Key");
                return null;
            }

            return new Verifier(tokens[1], tokens[2], manager, requestMessage.Author, requestMessage.Channel);
        }

        private async Task ValidateAccount()
        {
            var account = await API.GetAccountAsync();

            if (account == null)
            {
                await SendMessageAsync(VerifyStrings.AccountNotInAPI);
                Console.WriteLine($"Could not verify {Requestor.Username} - Cannont access account in GW2 API.");
                return;
            }

            if (account.Name.ToLower() != AccoutName.ToLower())
            {
                await SendMessageAsync(VerifyStrings.AccountNameDoesNotMatch);
                Console.WriteLine($"Could not verify {Requestor.Username} - API Key account does not match supplied account. (Case matters)");
                return;
            }

            if (!Manager.IsAccountOnOurWorld(account))
            {
                await SendMessageAsync(VerifyStrings.AccountNotOnServer);
                Console.WriteLine($"Could not verify {Requestor.Username} - Not on Server.");
                return;
            }

            Account = account;
        }

        private async Task ValidateCharacters()
        {
            var characters = await API.GetCharactersAsync();

            if (Account.Access == "PlayForFree")
            {
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
                    await SendMessageAsync(VerifyStrings.NotValidLevel);
                    Console.WriteLine($"Could not verify {Requestor.Username} - Not elgible for WvW.");
                    return;
                }
            }

            HasValidCharacter = true;
        }

        public async Task Validate()
        {
            await ValidateAccount();
            if (IsValidAccount)
                await ValidateCharacters();
        }

    }
}
