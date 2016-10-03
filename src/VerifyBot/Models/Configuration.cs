using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VerifyBot.Models
{
    public class Configuration
    {
        public ulong ServerID { get; set; }

        public List<int> WorldIDs { get; set; }

        public string VerifyRole { get; set; }
    }
}
