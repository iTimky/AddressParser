using System;



namespace AddressParser.Core.Models
{
    internal class NameAndType : IEquatable<NameAndType>
    {
        public readonly AddressPart AddressPart;
        public readonly AddrObjectType Type;
        public readonly AddrObjectName AddrObjectName;
        //public readonly string AddrName;
        //public readonly string OriginAddrName;


        public NameAndType(string addrName, string originAddrName, AddressPart addressPart)
        {
            AddrObjectName = new AddrObjectName(addrName, originAddrName);
            AddressPart = addressPart;
            if (addressPart != null)
                addressPart.Childs.Add(this);
        }


        public NameAndType(string addrName, string originAddrName, AddrObjectType type, AddressPart addressPart)
            : this(addrName, originAddrName, addressPart)
        {
            Type = type;
        }


        public override int GetHashCode()
        {
            return AddrObjectName.GetHashCode() ^ (Type != null ? Type.GetHashCode() : 0);
        }


        public bool Equals(NameAndType other)
        {
            return AddrObjectName == other.AddrObjectName && Type == other.Type;
        }


        public override string ToString()
        {
            return string.Format("AddrName: {0}, Type: {1}, AddressPart: {2}", AddrObjectName,
                Type == null ? "null" : Type.ToString(), AddressPart == null ? "null" : AddressPart.ToString());
        }
    }
}