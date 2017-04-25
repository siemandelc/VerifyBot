using System.Collections.Generic;

namespace VerifyBot.Models
{
    public class Configuration
    {
        public ulong ServerId { get; set; }

        public string VerifyRole { get; set; }

        public string DiscordToken { get; set; }

        public List<int> WorldIds { get; set; }
    }
}