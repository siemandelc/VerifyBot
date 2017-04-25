using System.ComponentModel.DataAnnotations;

namespace VerifyBot.Models
{
    public class User
    {
        [Key]
        public string AccountID { get; set; }

        public string APIKey { get; set; }

        public ulong DiscordID { get; set; }
    }
}