using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

using CE.Parsing.Core.Db;
using CE.Parsing.Core.Models;



namespace CE.Parsing.Core
{
    public partial class Parser
    {
        #region GetAddressStringByOldId
        internal string GetAddressStringByOldId(Guid? oldId)
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


        string BuildAddressString(OldAddress oldAddress, List<GeographicalObject> geographicalObjects)
        {
            var addressNames = new List<string>();

            if (oldAddress.Country.HasValue)
                addressNames.Add(string.Format("{0}", geographicalObjects.First(go => go.Id == oldAddress.Country).Name));

            if (oldAddress.CountryRegion.HasValue)
                addressNames.Add(string.Format("{0}", geographicalObjects.First(go => go.Id == oldAddress.CountryRegion).Name));

            if (oldAddress.City.HasValue)
                addressNames.Add(string.Format("г. {0}", geographicalObjects.First(go => go.Id == oldAddress.City).Name));

            if (oldAddress.MetroStation.HasValue)
                addressNames.Add(string.Format("м. {0}", geographicalObjects.First(go => go.Id == oldAddress.MetroStation).Name));

            if (oldAddress.Street.HasValue)
                addressNames.Add(string.Format("ул. {0}", geographicalObjects.First(go => go.Id == oldAddress.Street).Name));

            if (!string.IsNullOrEmpty(oldAddress.BuildingNumber))
                addressNames.Add(string.Format("д. {0}", oldAddress.BuildingNumber));

            if (!string.IsNullOrEmpty(oldAddress.AppartmentNumber))
                addressNames.Add(string.Format("кв./оф. {0}", oldAddress.AppartmentNumber));

            return string.Join(", ", addressNames);
        }
        #endregion


        #region GetAddressStringByGuid
        internal string GetAddressStringByGuid(Guid? someId)
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
                    if (!string.IsNullOrEmpty(addressStringByGuid) && !string.IsNullOrEmpty(address.Room))
                        return string.Format("{0}, {1}", addressStringByGuid, address.Room);

                    return addressStringByGuid;
                }
            }

            return null;
        }

        #endregion
    }
}
