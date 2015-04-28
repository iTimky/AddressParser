#region usings
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

#endregion



namespace AddressParser.Core.Models
{
    public class Address : IEquatable<Address>
    {
        int? _addressId;


        public int? AddressId
        {
            get { return _addressId; }
            set
            {
                //ClearIds();
                // Если решили оставлять все Id, то и Room необходимо оставить.
                // Вот откуда IS-121 баг распознавания адресов по OldId - room
                //Room = null;
                _addressId = value;
                HierarchyLevel++;
            }
        }


        public Guid? AoId { get; private set; }
        public Guid? LandMarkId { get; private set; }

        public Guid? HouseId { get; private set; }

        public Guid? AddonAoId { get; private set; }

        public Guid? AddonHouseId { get; private set; }

        public string Room;
        public bool? IsAllWordsFound;
        public int HierarchyLevel { get; private set; }

        public AddrObject AddrObject { get; private set; }
        public AddrHouse AddrHouse { get; private set; }
        internal HouseInfo HouseInfo { get; set; }

        // For test only
        public Address(AddrHouse ah)
        {
            SetHouse(ah);
        }


        public Address(int id)
        {
            AddressId = id;
        }

        public Address(AddrObject ao)
        {
            SetAddrObject(ao);
        }


        public Address(SqlDataReader reader)
        {
            AoId = reader.IsDBNull(0) ? (Guid?) null : reader.GetGuid(0);
            LandMarkId = reader.IsDBNull(1) ? (Guid?) null : reader.GetGuid(1);
            HouseId = reader.IsDBNull(2) ? (Guid?) null : reader.GetGuid(2);
            AddonAoId = reader.IsDBNull(3) ? (Guid?) null : reader.GetGuid(3);
            AddonHouseId = reader.IsDBNull(4) ? (Guid?) null : reader.GetGuid(4);
            Room = reader.IsDBNull(5) ? null : reader.GetString(5);
        }


        void ClearIds()
        {
            AoId = null;
            LandMarkId = null;
            AddonAoId = null;
            HouseId = null;
            AddonHouseId = null;
        }


        public void SetAddrObject(AddrObject addrObject)
        {
            AddrHouse = null;
            AddrObject = addrObject;
            ClearIds();

            if (addrObject.Kind == AddrObjectKind.AddrObjectCurrent)
                AoId = addrObject.Id;
            else if (addrObject.Kind == AddrObjectKind.AddrLandMarkCurrent)
                LandMarkId = addrObject.Id;
            else if (addrObject.Kind == AddrObjectKind.AddonAddrObject)
                AddonAoId = addrObject.Id;

            HierarchyLevel = addrObject.HierarchyLevel;
        }


        public void SetHouse(AddrHouse ah)
        {
            AddrHouse = ah;
            ClearIds();

            if (ah.IsAddon)
                AddonHouseId = ah.Id;
            else
                HouseId = ah.Id;

            //HierarchyLevel++;
        }


        public List<string> GetAllNames()
        {
            var names = new List<string>();

            var addrObject = AddrObject;

            while (addrObject != null)
            {
                names.Add(addrObject.Name);
                addrObject = addrObject.Parent;
            }

            return names;
        }


        public AddrLevel GetMinLevel()
        {
            if (AddrObject == null)
                return null;

            var addrObject = AddrObject;
            while (true)
            {
                if (addrObject.Parent == null)
                    return addrObject.Level;

                addrObject = addrObject.Parent;
            }
        }


        public AddrObject GetMinAddrObject()
        {
            if (AddrObject == null)
                return null;

            var addrObject = AddrObject;
            while (true)
            {
                if (addrObject.Parent == null)
                    return addrObject;

                addrObject = addrObject.Parent;
            }
        }


        public bool HasSkippedParent()
        {
            var addrObject = AddrObject;
            while (addrObject != null)
            {
                if (addrObject.Parent != null && addrObject.ParentId != addrObject.Parent.Id)
                    return true;

                addrObject = addrObject.Parent;
            }

            return false;
        }


        public bool Equals(Address other)
        {
            return other != null && AddressId == other.AddressId && AoId == other.AoId && HouseId == other.HouseId &&
                   AddonAoId == other.AddonAoId &&
                   AddonHouseId == other.AddonHouseId && LandMarkId == other.LandMarkId && Room == other.Room;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Address);
        }


        public override int GetHashCode()
        {
            return (AddressId ?? 0).GetHashCode() ^ 
                (AoId ?? Guid.Empty).GetHashCode() ^
                (HouseId ?? Guid.Empty).GetHashCode() ^
                (AddonAoId ?? Guid.Empty).GetHashCode() ^
                (AddonHouseId ?? Guid.Empty).GetHashCode() ^
                (LandMarkId ?? Guid.Empty).GetHashCode() ^ 
                (Room ?? string.Empty).GetHashCode();
        }


        public override string ToString()
        {
            return
                string.Format(
                    "AddressId: {0}, AoId: {1}, LandMarkId: {2}, HouseId: {3}, AddonAoId: {4}, AddonHouseId: {5}, HierarchyLevel: {6}, IsAllWordsFound: {7}",
                    AddressId, AoId, LandMarkId, HouseId, AddonAoId,
                    AddonHouseId, HierarchyLevel, IsAllWordsFound);
        }
    }
}