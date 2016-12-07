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

        private Manager manager { get; set; }
        public string AccoutName { get; private set; }
        public string APIKey { get; private set; }
        public IUser Requestor { get; private set; }
        public IMessageChannel Channel { get; private set; }

        private ApiFacade API { get; set; }

        public Account Account { get; private set; }
        private bool isValidAccount { get { return Account != null; } }
        private bool hasValidCharacter { get; set; }

        public bool isValid
        {
            get { return isValidAccount && hasValidCharacter; }
        }

        public Verifier(string accountName, string apiKey, Manager manager, IUser requestor, IMessageChannel channel)
        {
            AccoutName = accountName;
            APIKey = apiKey;
            Requestor = requestor;
            Channel = channel;
            this.manager = manager;

            API = new ApiFacade(APIKey);

            hasValidCharacter = false;
        }

        public async Task<IUserMessage> sendMessageAsync(string message)
        {
            if (Channel == null)
                return null; // Task.FromResult<object>(null);
            return await Channel.SendMessageAsync(message);
        }

        public static Verifier create(string accountName, string apiKey, Manager manager, IUser requestor, IMessageChannel channel = null)
        {
            return new Verifier(accountName, apiKey, manager, requestor, channel);
        }
        public async static Task<Verifier> createFromRequestMessage(IMessage requestMessage, Manager manager)
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

        private async Task validateAccount()
        {
            var account = await API.GetAccountAsync();

            if (account == null)
            {
                await sendMessageAsync(VerifyStrings.AccountNotInAPI);
                Console.WriteLine($"Could not verify {Requestor.Username} - Cannont access account in GW2 API.");
                return;
            }

            if (account.Name.ToLower() != AccoutName.ToLower())
            {
                await sendMessageAsync(VerifyStrings.AccountNameDoesNotMatch);
                Console.WriteLine($"Could not verify {Requestor.Username} - API Key account does not match supplied account. (Case matters)");
                return;
            }

            if (!manager.isAccountOnOurWorld(account))
            {
                await sendMessageAsync(VerifyStrings.AccountNotOnServer);
                Console.WriteLine($"Could not verify {Requestor.Username} - Not on Server.");
                return;
            }

            Account = account;
        }

        private async Task validateCharacters()
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
                    await sendMessageAsync(VerifyStrings.NotValidLevel);
                    Console.WriteLine($"Could not verify {Requestor.Username} - Not elgible for WvW.");
                    return;
                }
            }

            hasValidCharacter = true;
        }

        public async Task validate()
        {
            await validateAccount();
            if (isValidAccount)
                await validateCharacters();
        }

    }
}
