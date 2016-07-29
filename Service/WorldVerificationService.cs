using Discord;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using VerifyBot.Gw2Api;

namespace VerifyBot.Service
{
    public class WorldVerificationService
    {
        private readonly DiscordClient client;

        private ulong ServerID = 207497020888449024;

        private List<int> WorldIDs;

        private const int APIKeyLength = 72;

        public WorldVerificationService(DiscordClient client)
        {
            this.client = client;
        }

        public void Start()
        {
            LoadConfiguration();
            CheckIfDatabaseExists();
        }

        public async Task Process(MessageEventArgs e)
        {
            try
            {
                if (e.Message.IsAuthor)
                {
                    return;
                }

                if (!(e.Channel.IsPrivate || e.Channel.Name == "verify"))
                {
                    return;
                }

                if (!e.Channel.IsPrivate)
                {
                    if (!e.Message.Text.ToLower().Contains("!verify"))
                    {
                        await e.Message.Delete();
                        return;
                    }
                    var pm = await e.User.CreatePMChannel();

                    await pm.SendMessage($"Respond to this bot with the following information: {{account-name}} {{api-key}}");
                    await e.Message.Delete();

                    return;
                }

                await this.PerformVerification(e);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.ToString()}");
            }
        }

        private void LoadConfiguration()
        {
            this.WorldIDs = new List<int>();
            var worlds = Helper.SecretsReader.GetSecret("world_id");

            foreach (var world in worlds.Split(','))
            {
                int worldID = 0;
                if (int.TryParse(world, out worldID))
                {
                    this.WorldIDs.Add(worldID);
                }
                else
                {
                    throw new Exception("Missing WorldID(s) field in configuration file");
                }
            }

            var serverCandidate = Helper.SecretsReader.GetSecret("server_id");

            ulong serverID = 0;
            if (ulong.TryParse(serverCandidate, out serverID))
            {
                this.ServerID = serverID;
            }
            else
            {
                throw new Exception("Missing ServerID Field in configuration file");
            }
        }

        private void CheckIfDatabaseExists()
        {
            if (!System.IO.File.Exists("verify.sqlite"))
            {
                Console.WriteLine("Just entered to create Sync DB");
                SQLiteConnection.CreateFile("verify.sqlite");

                using (var sqlite = new SQLiteConnection("Data Source=verify.sqlite"))
                {
                    sqlite.Open();
                    string sql = "create table verify (account_id varchar(16) PRIMARY KEY, api_key varchar(72), discord_id integer)";
                    SQLiteCommand command = new SQLiteCommand(sql, sqlite);
                    command.ExecuteNonQuery();
                }
            }
        }

        private async Task PerformVerification(MessageEventArgs e)
        {
            try
            {
                Console.WriteLine($"Begin verification for user {e.User.Name}");
                await e.Channel.SendMessage("Starting Verification Process...");

                var tokens = e.Message.Text.Split(' ');

                if (tokens.Length != 2)
                {
                    await e.Channel.SendMessage("Invalid arguments.");
                    Console.WriteLine($"Could not verify {e.User.Name} - Bad # of arguments");
                    return;
                }

                if (tokens[1].Length != 72)
                {
                    await e.Channel.SendMessage("Invalid API Key.");
                    Console.WriteLine($"Could not verify {e.User.Name} - Bad API Key");
                    return;
                }

                // Check GW2 server
                var api = new ApiFacade(tokens[1]);
                var account = await api.GetAccountAsync();

                if (account == null)
                {
                    await e.Channel.SendMessage("Could not find that account in the GW2 API.");
                    Console.WriteLine($"Could not verify {e.User.Name} - Cannont access account in GW2 API.");
                    return;
                }

                if (account.Name != tokens[0])
                {
                    await e.Channel.SendMessage("API Key account does not match supplied account name. (Case matters)");
                    Console.WriteLine($"Could not verify {e.User.Name} - API Key account does not match supplied account. (Case matters)");
                    return;
                }

                if (!WorldIDs.Contains(account.WorldId))
                {
                    await e.Channel.SendMessage("Account is not on JQ.");
                    Console.WriteLine($"Could not verify {e.User.Name} - Not on JQ.");
                    return;
                }

                var conn = new SQLiteConnection("Data Source=verify.sqlite");

                conn.Open();

                // Check if already verified
                using (var cmd = new SQLiteCommand("select * from verify where account_id = @account_id", conn))
                {
                    cmd.Parameters.Add(new SQLiteParameter("@account_id", account.Id));

                    var response = await cmd.ExecuteScalarAsync();

                    if (response != null)
                    {
                        await e.Channel.SendMessage("Account is already verified. If you are having issues message a verifer");
                        return;
                    }
                }

                // Log entry to a server
                using (var cmd = new SQLiteCommand("insert into verify VALUES (@account_id,@api_key,@discord_id)", conn))
                {
                    cmd.Parameters.Add(new SQLiteParameter("@account_id", account.Id));
                    cmd.Parameters.Add(new SQLiteParameter("@api_key", tokens[1]));
                    cmd.Parameters.Add(new SQLiteParameter("@discord_id", e.User.Id));

                    await cmd.ExecuteNonQueryAsync();
                }

                var server = client.GetServer(ServerID);
                var role = server.FindRoles("Verified")?.FirstOrDefault();
                var user = server.GetUser(e.User.Id);

                await user.AddRoles(role);

                await e.Channel.SendMessage("Verification Process Complete. Welcome to JQ discord");
                Console.WriteLine($"{e.User.Name} Verified.");
            }
            catch (Exception ex)
            {
                await e.Channel.SendMessage("Error processing your verification request. An entry has been logged.");
                Console.WriteLine($"Error: {ex.ToString()}");
            }
        }
    }
}