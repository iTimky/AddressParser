#region usings
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

using AddressParser.Core.Db;
using AddressParser.Core.Models;

#endregion



namespace AddressParser.Core
{
    public partial class Parser
    {
        #region GetAddressStringByOldId
        public string GetAddressStringByOldId(Guid? oldId)
        {
            if (!oldId.HasValue)
                return null;

            OldAddress oldAddress = _dataContext.GetOldAddress(oldId.Value);
            if (oldAddress == null)
                return null;

            List<GeographicalObject> geographicalObjects = _dataContext.GetGeographicalObjects(oldAddress);
            string addressString = BuildAddressString(oldAddress, geographicalObjects);

            return addressString;
        }


        public List<string> GetNames(OldAddress o)
        {
            if (o == null)
                return null;

            List<GeographicalObject> geos = _dataContext.GetGeographicalObjects(o);
            var l = new List<string>();
            if (o.Country.HasValue)
            {
                GeographicalObject g = geos.FirstOrDefault(go => go.Id == o.Country);
                if (g != null)
                    l.Add(g.Name);
            }
            if (o.CountryRegion.HasValue)
            {
                GeographicalObject g = geos.FirstOrDefault(go => go.Id == o.CountryRegion);
                if (g != null)
                    l.Add(g.Name);
            }
            if (o.City.HasValue)
            {
                GeographicalObject g = geos.FirstOrDefault(go => go.Id == o.City);
                if (g != null)
                    l.Add(g.Name);
            }
            if (o.Street.HasValue)
            {
                GeographicalObject g = geos.FirstOrDefault(go => go.Id == o.Street);
                if (g != null)
                    l.Add(g.Name);
            }

            return l;
        }


        public string GetAddressStringByTuple(TupleOld t)
        {
            if (t == null)
                return null;

            var l = new List<string>();

            l.Add(t.old_Country_name);
            l.Add(t.old_CountryRegion_name);
            if (t.old_City_name != null)
                l.Add(string.Format("{0} {1}", t.old_City_type, t.old_City_name));
            if (t.old_Street_name != null)
                l.Add(string.Format("{0} {1}", t.old_Street_type, t.old_Street_name));

            l.Add(t.old_BuildingNumber);
            l.Add(t.old_AppartmentNumber);

            return string.Join(", ", l.Where(x => !string.IsNullOrWhiteSpace(x)));
        }


        string BuildAddressString(OldAddress oldAddress, List<GeographicalObject> geoObjects)
        {
            var addressNames = new List<string>();
            GeographicalObject o;

            if (oldAddress.Country.HasValue)
            {
                o = geoObjects.FirstOrDefault(go => go.Id == oldAddress.Country);
                if (o != null)
                    addressNames.Add(o.Name);
            }

            if (oldAddress.CountryRegion.HasValue)
            {
                o = geoObjects.FirstOrDefault(go => go.Id == oldAddress.CountryRegion);
                if (o != null)
                    addressNames.Add(o.Name);
            }

            if (oldAddress.City.HasValue)
            {
                o = geoObjects.FirstOrDefault(go => go.Id == oldAddress.City);
                if (o != null)
                    //addressNames.Add(o.TypeName + " " + o.Name);
                    addressNames.Add(o.Name);
            }

            if (oldAddress.Street.HasValue)
            {
                o = geoObjects.FirstOrDefault(go => go.Id == oldAddress.Street);
                if (o != null)
                    //addressNames.Add(o.TypeName + " " + o.Name);
                    addressNames.Add(o.Name);
            }

            if (!string.IsNullOrEmpty(oldAddress.BuildingNumber))
                addressNames.Add(string.Format("{0}", oldAddress.BuildingNumber));

            if (!string.IsNullOrEmpty(oldAddress.AppartmentNumber))
                addressNames.Add(string.Format("кв./оф. {0}", oldAddress.AppartmentNumber));

            return string.Join(", ", addressNames);
        }
        #endregion


        #region GetAddressString
        public string GetAddressString(Address address)
        {
            if (address == null)
                return null;

            if (address.AddressId.HasValue)
                return GetAddressStringById(address.AddressId);

            Guid? guid = address.AoId ?? address.AddonAoId ?? address.HouseId ?? address.AddonHouseId ?? address.LandMarkId;
            if (guid == null)
                return null;

            string addressString = GetAddressStringByGuid(guid);
            if (address.Room == null)
                return addressString;

            return string.Format("{0}, {1}", addressString, address.Room);
        }
        #endregion


        #region GetAddressStringByGuid
        public string GetAddressStringByGuid(Guid? someId)
        {
            if (!someId.HasValue)
                return null;

            var finalHierarchy = new List<AddressHierarchyObject>();
            List<AddressHierarchyObject> addons = _dataContext.GetHierarchyFromAddons(someId.Value);
            if (addons.Any())
            {
                finalHierarchy.AddRange(addons);
                int maxLevel = addons.Max(ad => ad.HierarchyLevel);
                Guid? parentId = addons.First(ad => ad.HierarchyLevel == maxLevel).ParentId;
                if (parentId.HasValue)
                    finalHierarchy.AddRange(_dataContext.GetHierarchyFromCurrent(parentId.Value, maxLevel + 1));
            }
            else
                finalHierarchy.AddRange(_dataContext.GetHierarchyFromCurrent(someId.Value, 0));

            string addressString = string.Join(", ",
                finalHierarchy.OrderByDescending(fh => fh.HierarchyLevel).Select(fh => fh.Name));
            return addressString;
        }
        #endregion


        #region GetAddressStringById
        internal string GetAddressStringById(int? id)
        {
            if (!id.HasValue)
                return null;

            Address address = _dataContext.GetAddressById(id.Value);
            if (address != null)
            {
                Guid? guid = address.AoId ?? address.LandMarkId ?? address.HouseId ?? address.AddonAoId ?? address.AddonHouseId;

                if (guid != null)
                {
                    string addressStringByGuid = GetAddressStringByGuid(guid.Value);
                    if (!string.IsNullOrEmpty(addressStringByGuid) && !string.IsNullOrEmpty(address.Room) &&
                        !string.IsNullOrWhiteSpace(address.Room))
                        return string.Format("{0}, {1}", addressStringByGuid, address.Room);

                    return addressStringByGuid;
                }
            }

            return null;
        }
        #endregion
    }
}