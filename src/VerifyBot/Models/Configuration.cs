using System.Collections.Generic;

namespace VerifyBot.Models
{
    public class Configuration
    {
        public ulong ServerID { get; set; }

        public List<int> WorldIDs { get; set; }

        public string VerifyRole { get; set; }
    }
}
