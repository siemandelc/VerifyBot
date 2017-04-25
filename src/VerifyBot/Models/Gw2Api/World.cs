using System.Runtime.Serialization;

namespace VerifyBot.Gw2Api
{
    [DataContract]
    public class World
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "population")]
        public string Population { get; set; }

        public string ServerLanguage
        {
            get
            {
                var id = this.Id.ToString();
                if (id.StartsWith("2"))
                {
                    if (id.StartsWith("21"))
                    {
                        return "French";
                    }
                    else if (id.StartsWith("22"))
                    {
                        return "German";
                    }
                    else if (id.StartsWith("23"))
                    {
                        return "Spanish";
                    }
                }

                return "Unspecified";
            }
        }

        /// <summary>Gets the server location.</summary>
        /// <remarks>The second digit of the  language: 1 means French, 2 means German, and 3 means Spanish.</remarks>
        public string ServerLocation
        {
            get
            {
                var id = this.Id.ToString();
                if (id.StartsWith("1"))
                {
                    return "North America";
                }
                else if (id.StartsWith("2"))
                {
                    return "Europe";
                }

                return "Unknown";
            }
        }
    }
}