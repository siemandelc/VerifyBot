using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace VerifyBot.Models.Gw2Api
{

    [DataContract]
    public class Character
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "race")]
        public string Race { get; set; }

        [DataMember(Name = "gender")]
        public string Gender { get; set; }

        [DataMember(Name = "profession")]
        public string Profession { get; set; }

        [DataMember(Name = "level")]
        public int Level { get; set; }

        [DataMember(Name = "guild")]
        public string GuildId { get; set; }

        [DataMember(Name = "age")]
        public int Age { get; set; }

        public string AgeDisplay
        {
            get
            {
                int secondsRemaining = this.Age;

                int secondsInHour = 3600;
                int hours = secondsRemaining / secondsInHour;
                secondsRemaining = secondsRemaining - (hours * secondsInHour);

                int secondsInMinute = 60;
                int minutes = secondsRemaining / secondsInMinute;
                secondsRemaining = secondsRemaining - (minutes * secondsInMinute);
                int seconds = secondsRemaining;

                return string.Format("{0} hours, {1} minutes", hours, minutes);
            }
        }

        [DataMember(Name = "created")]
        public string Created { get; set; }

        public DateTime Birthday
        {
            get
            {
                var date = DateTime.Parse(this.Created);
                return date.ToLocalTime();
            }
        }

        [DataMember(Name = "deaths")]
        public int Deaths { get; set; }

        [DataMember(Name = "crafting")]
        public List<Crafting> Crafting { get; set; }

        [DataMember(Name = "specializations")]
        public Specializations Specializations { get; set; }

        [DataMember(Name = "equipment")]
        public List<Equipment> Equipment { get; set; }

        [DataMember(Name = "recipes")]
        public List<int> Recipes { get; set; }

        [DataMember(Name = "bags")]
        public List<Bag> Bags { get; set; }
    }

    [DataContract]
    public class Crafting
    {
        [DataMember(Name = "discipline")]
        public string Discipline { get; set; }

        [DataMember(Name = "rating")]
        public int Rating { get; set; }

        [DataMember(Name = "active")]
        public bool Active { get; set; }
    }

    public class Pve : Specialization
    {
    }

    public class Pvp : Specialization
    {
    }

    public class Wvw : Specialization
    {
    }

    public abstract class Specialization
    {
        public int id { get; set; }
        public List<int> traits { get; set; }
    }

    public class Specializations
    {
        public List<Pve> pve { get; set; }
        public List<Pvp> pvp { get; set; }
        public List<Wvw> wvw { get; set; }
    }

    public class Equipment
    {
        public int id { get; set; }
        public string slot { get; set; }
        public List<int> upgrades { get; set; }
        public List<int?> infusions { get; set; }
        public int? skin { get; set; }
    }

    public class Inventory
    {
        public int id { get; set; }
        public int count { get; set; }
        public string binding { get; set; }
        public string bound_to { get; set; }
        public int? infix_upgrade_id { get; set; }
        public List<int?> infusions { get; set; }
        public int? skin { get; set; }
        public List<int?> upgrades { get; set; }
    }

    public class Bag
    {
        public int id { get; set; }
        public int size { get; set; }
        public List<Inventory> inventory { get; set; }
    }
}
