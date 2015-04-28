using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AddressParser.Core.Models
{
    public class AddrLevel : IEquatable<AddrLevel>, IComparable<AddrLevel>
    {
        public byte Id { get; private set; }
        public string Name { get; private set; }
        public string EngName { get; private set; }
        public readonly int JumpValue;


        private AddrLevel(byte id, string name, string engName)
        {
            Id = id;
            Name = name;
            EngName = engName;


            if (id == 91)
                JumpValue = 10;
            else if (id == 90)
                JumpValue = 9;
            else
                JumpValue = id;
        }


        public bool Equals(AddrLevel other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }


        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AddrLevel)obj);
        }


        public int CompareTo(AddrLevel other)
        {
            if (ReferenceEquals(other, null))
                return 1;
            return Id.CompareTo(other.Id);
        }


        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}", Id, Name, EngName);
        }


        public static bool operator ==(AddrLevel first, AddrLevel second)
        {
            if (ReferenceEquals(first, null) && ReferenceEquals(second, null))
                return true;
            if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
                return false;

            return first.Equals(second);
        }


        public static bool operator !=(AddrLevel first, AddrLevel second)
        {
            return !(first == second);
        }


        public static bool operator >(AddrLevel first, AddrLevel second)
        {
            if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
                return false;

            return first.Id > second.Id;
        }


        public static bool operator <(AddrLevel first, AddrLevel second)
        {
            return !(first > second);
        }

        public static bool operator >=(AddrLevel first, AddrLevel second)
        {
            if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
                return false;

            return first.Id >= second.Id;
        }


        public static bool operator <=(AddrLevel first, AddrLevel second)
        {
            if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
                return false;

            return first.Id <= second.Id;
        }


        public static explicit operator AddrLevel(byte id)
        {
            return IdAddrLevelDict[id];
        }


        public static explicit operator AddrLevel(string engName)
        {
            return EngNameAddrLevelDict[engName];
        }



        public static readonly AddrLevel Countries = new AddrLevel(0, "Страна", "Countries");
        public static readonly AddrLevel Regions = new AddrLevel(1, "Регион", "Regions");
        public static readonly AddrLevel Autonomies = new AddrLevel(2, "Автономный округ", "Autonomies");
        public static readonly AddrLevel Rayons = new AddrLevel(3, "Район", "Rayons");
        public static readonly AddrLevel Cities = new AddrLevel(4, "Город", "Cities");
        public static readonly AddrLevel CityTerritories = new AddrLevel(5, "Внутригородская территория", "CityTerritories");
        public static readonly AddrLevel Settlements = new AddrLevel(6, "Населенный пункт", "Settlements");
        public static readonly AddrLevel Streets = new AddrLevel(7, "Улица", "Streets");
        public static readonly AddrLevel Houses = new AddrLevel(8, "Дом", "Houses");
        public static readonly AddrLevel AddonTerritories = new AddrLevel(90, "Дополнительная территория", "AddonTerritories");
        public static readonly AddrLevel AddonTerritorySlaves = new AddrLevel(91, "Подчиненный дополнительным территориям объект", "AddonTerritorySlaves");


        static AddrLevel()
        {
            Levels = new List<AddrLevel> { Countries, Regions, Autonomies, Rayons, Cities, CityTerritories, Settlements, Streets, Houses, AddonTerritories, AddonTerritorySlaves }.AsReadOnly();
            StreetLevels = new List<AddrLevel> { Settlements, Streets, Houses, AddonTerritories, AddonTerritorySlaves }.AsReadOnly();
            MainLevels = new List<AddrLevel> { Countries, Regions, Cities, Streets, AddonTerritorySlaves }.AsReadOnly();
            IdAddrLevelDict = new ReadOnlyDictionary<byte, AddrLevel>(Levels.ToDictionary(l => l.Id));
            EngNameAddrLevelDict = new ReadOnlyDictionary<string, AddrLevel>(Levels.ToDictionary(l => l.EngName));
        }
        public static readonly ReadOnlyCollection<AddrLevel> Levels;
        public static readonly ReadOnlyCollection<AddrLevel> StreetLevels;
        public static readonly ReadOnlyCollection<AddrLevel> MainLevels;
        static readonly ReadOnlyDictionary<byte, AddrLevel> IdAddrLevelDict;
        static readonly ReadOnlyDictionary<string, AddrLevel> EngNameAddrLevelDict;
    }
}
