using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace VerifyBot.Gw2Api
{
    public class ApiFacade
    {
        private const string baseUrl = "https://api.guildwars2.com/";

        public ApiFacade()
        {
        }

        public ApiFacade(string key)
        {
            this.Key = key;
        }

        public string Key { get; set; }

        #region API Call - Account

        public async Task<Account> GetAccountAsync()
        {
            var url = string.Format("{0}v2/account?access_token={1}", baseUrl, this.Key);
            return await this.CallApi<Account>(url);
        }

        #endregion API Call - Account

        #region API Call - Characters

        public async Task<Character> GetCharacterAsync(string characterName)
        {
            var name = characterName.Replace(" ", "%20");
            var url = string.Format("{0}v2/characters/{1}?access_token={2}", baseUrl, name, this.Key);
            return await this.CallApi<Character>(url);
        }

        public async Task<string[]> GetCharactersAsync()
        {
            var url = string.Format("{0}v2/characters?access_token={1}", baseUrl, this.Key);
            return await this.CallApi<string[]>(url);
        }

        #endregion API Call - Characters

        #region API Call - Worlds

        public async Task<World> GetWorldAsync(int worldId)
        {
            var url = string.Format("{0}v2/worlds?ids={1}", baseUrl, worldId);
            var worlds = await this.CallApi<World[]>(url);
            return worlds[0];
        }

        public async Task<World[]> GetWorldsAsync()
        {
            var url = string.Format("{0}v2/worlds?ids=all", baseUrl);
            return await this.CallApi<World[]>(url);
        }

        #endregion API Call - Worlds

        #region JSON Helpers

        private async Task<T> CallApi<T>(string url)
        {
            var json = await this.GetJsonAsync(url);
            return this.DeserializeJson<T>(json);
        }

        private T DeserializeJson<T>(string json)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var result = (T)serializer.ReadObject(ms);
            return result;
        }

        private async Task<string> GetJsonAsync(string url)
        {
            HttpClient http = new HttpClient();
            var response = await http.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        #endregion JSON Helpers
    }
}