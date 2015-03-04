using System;



namespace CE.Parsing.Core.Models
{
    internal class NameAndType : IEquatable<NameAndType>
    {
        public readonly AddressPart AddressPart;
        public readonly AddrObjectType Type;
        public readonly string AddrName;
        public readonly string OriginAddrName;


        public NameAndType(string addrName, string originAddrName, AddressPart addressPart)
        {
            AddrName = addrName;
            OriginAddrName = originAddrName;
            AddressPart = addressPart;
            addressPart.Childs.Add(this);
        }


        public NameAndType(string addrName, string originAddrName, AddrObjectType type, AddressPart addressPart)
            : this(addrName, originAddrName, addressPart)
        {
            Type = type;
        }


        public override int GetHashCode()
        {
            return AddrName.GetHashCode() ^ OriginAddrName.GetHashCode() ^ (Type != null ? Type.GetHashCode() : 0);
        }


        public bool Equals(NameAndType other)
        {
            return AddrName == other.AddrName && OriginAddrName == other.OriginAddrName && Type == other.Type;
        }


        public override string ToString()
        {
            return string.Format("AddrName: {0}, Origin: {1}, Type: {2}, AddressPart: {3}", AddrName, OriginAddrName,
                Type == null ? "null" : Type.ToString(), AddressPart == null ? "null" : AddressPart.ToString());
        }
    }
}