using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

using CE.Parsing.Core.Models;



namespace CE.Parsing.Core
{
    internal static class AddressStringBuilder
    {
        #region GetAddressStringByOldId
        internal static string GetAddressStringByOldId(Guid oldId)
        {
            OldAddress oldAddress = GetOldAddressObject(oldId);
            List<GeographicalObject> geographicalObjects = GetGeographicalObjects(oldAddress);
            string addressString = BuildAddressString(oldAddress, geographicalObjects);

            return addressString;
        }


        static OldAddress GetOldAddressObject(Guid oldId)
        {
            var oldAddress = new OldAddress();
            string query =
                string.Format(@"select Country, CountryRegion, City, MetroStation, Street, BuildingNumber, AppartmentNumber
                        from SQL02.CityExpressDB_Online_ceapp.dbo.T_ADDRESS with (nolock) where AddressID = '{0}'", oldId);

            using (var connection = new SqlConnection("context connection = true"))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                    while (reader.Read())
                    {
                        oldAddress.Country = reader.IsDBNull(0) ? (Guid?)null : reader.GetGuid(0);
                        oldAddress.CountryRegion = reader.IsDBNull(1) ? (Guid?)null : reader.GetGuid(1);
                        oldAddress.City = reader.IsDBNull(2) ? (Guid?)null : reader.GetGuid(2);
                        oldAddress.MetroStation = reader.IsDBNull(3) ? (Guid?)null : reader.GetGuid(3);
                        oldAddress.Street = reader.IsDBNull(4) ? (Guid?)null : reader.GetGuid(4);
                        oldAddress.BuildingNumber = reader.IsDBNull(5) ? null : reader.GetString(5);
                        oldAddress.AppartmentNumber = reader.IsDBNull(6) ? null : reader.GetString(6);
                    }
            }

            return oldAddress;
        }


        static List<GeographicalObject> GetGeographicalObjects(OldAddress oldAddress)
        {
            var geographicalObjects = new List<GeographicalObject>();
            var geographicalObjectIds = new List<Guid>();
            if (oldAddress.Country.HasValue)
                geographicalObjectIds.Add(oldAddress.Country.Value);
            if (oldAddress.CountryRegion.HasValue)
                geographicalObjectIds.Add(oldAddress.CountryRegion.Value);
            if (oldAddress.City.HasValue)
                geographicalObjectIds.Add(oldAddress.City.Value);
            if (oldAddress.Street.HasValue)
                geographicalObjectIds.Add(oldAddress.Street.Value);

            if (!geographicalObjectIds.Any())
                return geographicalObjects;

            string geogrObjQuery = string.Format(
                @"select GeographicalObjectsID, Name from SQL02.CityExpressDB_Online_ceapp.dbo.T_GEOGRAPHICAL_OBJECTS with (nolock) where GeographicalObjectsID in ({0})",
                string.Join(", ", geographicalObjectIds.Select(g => string.Format("'{0}'", g))));

            using (var connection = new SqlConnection("context connection = true"))
            {
                connection.Open();
                using (var command = new SqlCommand(geogrObjQuery, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                    while (reader.Read())
                        geographicalObjects.Add(new GeographicalObject
                        {
                            Id = reader.GetGuid(0),
                            Name = reader.IsDBNull(1) ? null : reader.GetString(1)
                        });

                if (oldAddress.MetroStation.HasValue)
                {
                    string metroQuery =
                        string.Format(
                            @"select MetroStationID, Name from SQL02.CityExpressDB_Online_ceapp.dbo.T_METRO_STATIONS with (nolock) where MetroStationID = '{0}'",
                            oldAddress.MetroStation);
                    using (var command = new SqlCommand(metroQuery, connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                        while (reader.Read())
                            geographicalObjects.Add(new GeographicalObject
                            {
                                Id = reader.GetGuid(0),
                                Name = reader.IsDBNull(1) ? null : reader.GetString(1)
                            });
                }
            }

            return geographicalObjects;
        }


        static string BuildAddressString(OldAddress oldAddress, List<GeographicalObject> geographicalObjects)
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


        #region objects
        class GeographicalObject
        {
            public Guid Id;
            public string Name;
        }
        #endregion


        #endregion


        #region GetAddressStringByGuid
        internal static string GetAddressStringByGuid(Guid someId)
        {
            var finalHierarchy = new List<AddressHierarchyObject>();
            List<AddressHierarchyObject> addons = GetHierarchyFromAddons(someId);
            if (addons.Any())
            {
                finalHierarchy.AddRange(addons);
                int maxLevel = addons.Max(ad => ad.HierarchyLevel);
                Guid? parentId = addons.First(ad => ad.HierarchyLevel == maxLevel).ParentId;
                if (parentId.HasValue)
                    finalHierarchy.AddRange(GetHierarchyFromCurrent(parentId.Value, maxLevel + 1));
            }
            else
                finalHierarchy.AddRange(GetHierarchyFromCurrent(someId, 0));

            string addressString = string.Join(", ",
                finalHierarchy.OrderByDescending(fh => fh.HierarchyLevel).Select(fh => fh.Name));
            return addressString;
        }


        static List<AddressHierarchyObject> GetHierarchyFromAddons(Guid someId)
        {
            var hierarchyObjects = new List<AddressHierarchyObject>();
            AddressHierarchyObject houseObject = null;
            string houseQuery =
                string.Format(@"select Id, ParentId, Number, BuildNum, StructureNum from dbo.AddonAddrHouses where Id = '{0}'",
                    someId);
            using (var connection = new SqlConnection("context connection = true"))
            {
                connection.Open();
                using (var command = new SqlCommand(houseQuery, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                    while (reader.Read())
                    {
                        houseObject = new AddressHierarchyObject
                        {
                            Id = reader.GetGuid(0),
                            ParentId = reader.IsDBNull(1) ? (Guid?)null : reader.GetGuid(1),
                            HierarchyLevel = 0
                        };
                        var names = new List<string>();
                        if (!reader.IsDBNull(2))
                            names.Add(string.Format("д. {0}", reader.GetString(2)));
                        if (!reader.IsDBNull(3))
                            names.Add(string.Format("к. {0}", reader.GetString(3)));
                        if (!reader.IsDBNull(4))
                            names.Add(string.Format("стр. {0}", reader.GetString(4)));

                        if (names.Any())
                            houseObject.Name = string.Join(", ", names);

                        hierarchyObjects.Add(houseObject);
                    }

                string objectQuery = string.Format(@"with cte as
                            (
                                select aao.Id, aao.ParentId, aao.Name, aot.ShortName, {1} hierlvl
                                from dbo.AddonAddrObjects aao
                                join dbo.AddrObjectTypes aot on aot.Id = aao.TypeId
                                where aao.Id = '{0}'
                                union all
                                select aao.Id, aao.ParentId, aao.Name, aot.ShortName, cte.hierlvl + 1 hierlvl
                                from dbo.AddonAddrObjects aao 
                                join cte on cte.ParentId = aao.Id
                                join dbo.AddrObjectTypes aot on aot.Id = aao.TypeId
                            )     
                            select * from cte", houseObject != null ? houseObject.ParentId : someId,
                    houseObject != null ? houseObject.HierarchyLevel + 1 : 0);

                using (var command = new SqlCommand(objectQuery, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                    while (reader.Read())
                    {
                        var hierarchyObject = new AddressHierarchyObject
                        {
                            Id = reader.GetGuid(0),
                            ParentId = reader.IsDBNull(1) ? (Guid?)null : reader.GetGuid(1),
                            HierarchyLevel = reader.GetInt32(4)
                        };

                        if (!reader.IsDBNull(2))
                            hierarchyObject.Name = string.Format("{0}. {1}", reader.GetString(3), reader.GetString(2));

                        hierarchyObjects.Add(hierarchyObject);
                    }
            }

            return hierarchyObjects;
        }


        static List<AddressHierarchyObject> GetHierarchyFromCurrent(Guid someId, int nextHierarchyLevel)
        {
            var hierarchyObjects = new List<AddressHierarchyObject>();
            AddressHierarchyObject houseOrLandMark = null;
            string houseQuery =
                string.Format(
                    @"select Id, ParentAoId, NumberEffective, HouseNum, BuildNum, StructureNum from dbo.AddrHousesCurrent where Id = '{0}'",
                    someId);
            using (var connection = new SqlConnection("context connection = true"))
            {
                connection.Open();
                using (var command = new SqlCommand(houseQuery, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                    while (reader.Read())
                    {
                        houseOrLandMark = new AddressHierarchyObject
                        {
                            Id = reader.GetGuid(0),
                            ParentId = reader.IsDBNull(1) ? (Guid?)null : reader.GetGuid(1),
                            HierarchyLevel = nextHierarchyLevel
                        };

                        var names = new List<string>();
                        if (!reader.IsDBNull(3))
                            names.Add(string.Format("д. {0}", reader.GetString(3)));
                        else if (!reader.IsDBNull(2))
                            names.Add(string.Format("д. {0}", reader.GetString(2)));
                        if (!reader.IsDBNull(4))
                            names.Add(string.Format("к. {0}", reader.GetString(4)));
                        if (!reader.IsDBNull(5))
                            names.Add(string.Format("стр. {0}", reader.GetString(5)));

                        if (names.Any())
                            houseOrLandMark.Name = string.Join(", ", names);

                        hierarchyObjects.Add(houseOrLandMark);
                    }

                if (houseOrLandMark == null)
                {
                    string landMarkQuery =
                        string.Format("select Id, ParentAoId, Location from dbo.AddrLandMarksCurrent where Id = '{0}'", someId);
                    using (var command = new SqlCommand(landMarkQuery, connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                        while (reader.Read())
                        {
                            houseOrLandMark = new AddressHierarchyObject
                            {
                                Id = reader.GetGuid(0),
                                ParentId = reader.IsDBNull(1) ? (Guid?)null : reader.GetGuid(1),
                                Name = reader.IsDBNull(2) ? null : reader.GetString(2),
                                HierarchyLevel = nextHierarchyLevel
                            };

                            hierarchyObjects.Add(houseOrLandMark);
                        }
                }

                string objectQuery = string.Format(@"with cte as
                                                (
                                                    select aoc.Id, aoc.ParentId, aoc.OfficialName, aot.ShortName, {1} hierlvl
                                                    from dbo.AddrObjectsCurrent aoc
                                                    join dbo.AddrObjectTypes aot on aot.Id = aoc.TypeId
                                                    where aoc.Id = '{0}'
                                                    union all
                                                    select aoc.Id, aoc.ParentId, aoc.OfficialName, aot.ShortName, cte.hierlvl + 1 hierlvl
                                                    from dbo.AddrObjectsCurrent aoc 
                                                    join cte on cte.ParentId = aoc.Id
                                                    join dbo.AddrObjectTypes aot on aot.Id = aoc.TypeId
                                                )     
                                                select * from cte", houseOrLandMark != null ? houseOrLandMark.ParentId : someId,
                    houseOrLandMark != null ? houseOrLandMark.HierarchyLevel + 1 : nextHierarchyLevel);

                using (var command = new SqlCommand(objectQuery, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                    while (reader.Read())
                    {
                        var hierarchyObject = new AddressHierarchyObject
                        {
                            Id = reader.GetGuid(0),
                            ParentId = reader.IsDBNull(1) ? (Guid?)null : reader.GetGuid(1),
                            HierarchyLevel = reader.GetInt32(4)
                        };

                        if (!reader.IsDBNull(2))
                            hierarchyObject.Name = string.Format("{0}. {1}", reader.GetString(3), reader.GetString(2));

                        hierarchyObjects.Add(hierarchyObject);
                    }
            }

            return hierarchyObjects;
        }



        class AddressHierarchyObject
        {
            public Guid Id;
            public Guid? ParentId;
            public string Name;
            public int HierarchyLevel;
        }
        #endregion


        #region GetAddressStringById
        internal static string GetAddressStringById(int id)
        {
            Address address = GetAddressById(id);
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


        static Address GetAddressById(int id)
        {
            string addressQuery =
                string.Format(
                    @"select AoId, LandMarkId, HouseId, AddonAoId, AddonHouseId, Room from dbo.Addresses where Id = {0}", id);
            using (var connection = new SqlConnection("context connection = true"))
            {
                connection.Open();
                using (var command = new SqlCommand(addressQuery, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                    while (reader.Read())
                        return new Address(reader);
            }

            return null;
        }
        #endregion
    }
}
