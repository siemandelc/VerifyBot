using System.Collections.Generic;
using System.Runtime.Serialization;

namespace VerifyBot.Gw2Api
{
    [DataContract]
    public class Account
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "world")]
        public int WorldId { get; set; }

        [DataMember(Name = "guilds")]
        public List<string> GuildIds { get; set; }

        [DataMember(Name = "created")]
        public string Created { get; set; }

        [DataMember(Name = "access")]
        public string Access { get; set; }

        [DataMember(Name = "fractal_level")]
        public int FractalLevel { get; set; }
    }
}