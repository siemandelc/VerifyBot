using Newtonsoft.Json;
using System.IO;
using VerifyBot.Models;

namespace VerifyBot.Factories
{
    public static class UserStringsFactory
    {
        private const string UserStringsFile = "strings.json";

        public static UserStrings Get()
        {
            if (!File.Exists(UserStringsFile))
            {
                throw new FileNotFoundException($"Could not find the {UserStringsFile} file");
            }

            var file = File.ReadAllText(UserStringsFile);
            var userStrings = JsonConvert.DeserializeObject<UserStrings>(file);

            return userStrings;
        }
    }
}