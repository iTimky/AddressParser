using System;



namespace CE.Parsing.Core.Models
{
    internal class AddrObjectType : IEquatable<AddrObjectType>
    {
        public readonly byte Id;
        public readonly string Name;
        public readonly string ShortName;
        public readonly string EngName;


        public AddrObjectType(byte id, string name, string shortName, string engName)
        {
            Id = id;
            Name = name;
            ShortName = shortName;
            EngName = engName;
        }


        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }


        public bool Equals(AddrObjectType other)
        {
            return Id == other.Id;
        }


        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}, {3}", Id, Name, ShortName, EngName);
        }
    }
}