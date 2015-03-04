#region usings
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

using CE.Parsing.Core.Models;

#endregion



namespace CE.Parsing.Core
{
    internal static class AddressSearcher
    {
        internal static IEnumerable<AddrObject> GetAddrObjects(List<NameAndType> nameAndTypes, bool isHeavy)
        {
            var levels = new[]
            {
                "Regions",
                "Rayons",
                "Cities",
                "CityTerritories",
                "Settlements",
                "Streets",
                "AddonTerritories",
                "AddonTerritorySlaves"
            };

            string query;

            if (isHeavy)
                query = GetHeavyAddrObjectSearchQuery(nameAndTypes);
            else
                query = string.Format(@"
                        select t.Id, t.ParentId, t.Name, cast(t.TypeId as tinyint), al.EngName from
                        (
                            select  aoc.Id, aoc.ParentId, i.Name, null as TypeId, aoc.[Level]
                            from ({0}) i    
                            join dbo.AddrObjectsCurrent aoc on aoc.FormalName = i.Name and i.TypeId is null
                            union
                            select  aoc.Id, aoc.ParentId, i.Name, null as TypeId, aoc.[Level]
                            from ({0}) i    
                            join dbo.AddrObjectsCurrent aoc on aoc.OfficialName = i.Name and i.TypeId is null
                            union
                            select  aoc.Id, aoc.ParentId, i.Name, i.TypeId, aoc.[Level]
                            from ({0}) i    
                            join dbo.AddrObjectsCurrent aoc on aoc.FormalName = i.Name and i.TypeId = aoc.TypeId
                            union
                            select  aoc.Id, aoc.ParentId, i.Name, i.TypeId, aoc.[Level]
                            from ({0}) i    
                            join dbo.AddrObjectsCurrent aoc on aoc.OfficialName = i.Name and i.TypeId = aoc.TypeId
                        ) t
                        join dbo.AddrLevels al on al.Id = t.Level;",
                    GetNameAndTypeSelect(nameAndTypes)
                    );

            List<AddrObject> objects = SelectAddrObjects(query);
            List<AddrObject> addrObjects = new List<AddrObject>(objects.Count);

            foreach (string level in levels)
            {
                List<AddrObject> levelObjects = objects.Where(o => o.Level == level).ToList();
                if (levelObjects.Any())
                {
                    if (addrObjects.Any())
                        foreach (AddrObject levelObject in levelObjects)
                        {
                            AddrObject parent = addrObjects.FirstOrDefault(ao => ao.Id == levelObject.ParentId);
                            if (parent == null)
                                continue;

                            levelObject.Parent = parent;
                            levelObject.HierarchyLevel = parent.HierarchyLevel + 1;
                            parent.Childs.Add(levelObject);
                        }
                    addrObjects.AddRange(levelObjects);
                }
            }

            addrObjects =
                addrObjects.GroupBy(a => a.Id).SelectMany(
                    g => g.All(a => a.TypeId.HasValue) || g.All(a => !a.TypeId.HasValue) ? g : g.Where(a => a.TypeId.HasValue))
                    .ToList();

            if (addrObjects.Count < 100)
                return addrObjects;

            if (addrObjects.All(a => a.Parent == null && !a.Childs.Any()))
                return GetIncompleteAddrObjects(addrObjects, levels.Reverse().ToArray());

            addrObjects.RemoveAll(a => a.Parent == null && !a.Childs.Any());

            IEnumerable<IGrouping<Guid, AddrObject>> addrObjectsById = addrObjects.GroupBy(a => a.Id);
            List<AddrObject> result = new List<AddrObject>();
            foreach (IGrouping<Guid, AddrObject> addrObject in addrObjectsById)
            {
                int maxLength = addrObject.Max(a => a.Name.Length);
                result.Add(addrObject.First(a => a.Name.Length == maxLength));
            }

            return result;
        }

        static IEnumerable<AddrObject> GetIncompleteAddrObjects(IEnumerable<AddrObject> addrObjects, string[] levels)
        {
            List<AddrObject> incompleteAddrObjects = new List<AddrObject>();

            foreach (var addressLookup in addrObjects.ToLookup(a => a.HierarchyLevel).OrderByDescending(a => a.Key))
            {
                foreach (var level in levels)
                {
                    var levelObjects = addressLookup.Where(a => a.Level == level).ToList();
                    var typed = levelObjects.Where(a => a.TypeId != null).ToList();
                    if (typed.Count == 1)
                        incompleteAddrObjects.Add(typed.Single());

                    var nonTyped = levelObjects.Where(a => a.TypeId == null).ToList();
                    if (nonTyped.Count == 1)
                        incompleteAddrObjects.Add(nonTyped.Single());
                }
            }

            return incompleteAddrObjects;
        }

        static string GetHeavyAddrObjectSearchQuery(List<NameAndType> nameAndTypes)
        {
            var stringBuilder =
                new StringBuilder("select t.Id, t.ParentId, t.Name, cast(t.TypeId as tinyint), al.EngName from(" +
                                  Environment.NewLine);
            var nameTypes = nameAndTypes.Select(n => new {Id = n.Type == null ? 0 : n.Type.Id, Name = n.AddrName})
                .Union(nameAndTypes.Select(n => new {Id = 0, Name = n.OriginAddrName})).Where(n => !string.IsNullOrEmpty(n.Name));

            foreach (var nameType in nameTypes)
            {
                string subQuery = string.Format(@" select aoc.Id, aoc.ParentId, '{0}' as Name, {3} as TypeId, aoc.[Level]
    from dbo.AddrObjectsCurrent aoc
    where contains ((FormalName, OfficialName), '""{1}""') and (FormalName like '%{0}%' or OfficialName like '%{0}%'){2} union",
                    nameType.Name.Trim(),
                    nameType.Name.Trim(),
                    nameType.Id == 0 ? "" : string.Format(" and aoc.TypeId = {0}", nameType.Id),
                    nameType.Id == 0 ? "null" : nameType.Id.ToString());
                stringBuilder.Append(subQuery);
            }

            stringBuilder.Remove(stringBuilder.Length - "union".Length, "union".Length);
            stringBuilder.Append(@") t join dbo.AddrLevels al on al.Id = t.Level");

            string query = stringBuilder.ToString();

            return query;
        }


        internal static IEnumerable<AddrObject> FillAddrLandMarks(List<Address> addresses, IEnumerable<NameAndType> names)
        {
            var addrObjects = new List<AddrObject>();

            string query = string.Format(@"select   almc.Id,
                                                    almc.ParentAoId,
                                                    i.String
                                     from dbo.AddrLandMarksCurrent almc
                                     join ({1}) i on almc.Location = i.String or almc.Location like '% ' + i.String + ' %' 
                                     where almc.ParentAoId in ({0})",
                string.Join(",", addresses.Select(ad => string.Format("'{0}'", ad.AoId))),
                string.Join(" union ", names.Select(n => string.Format("select '{0}' String", n.AddrName))));

            using (var connection = new SqlConnection("context connection = true"))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                    while (reader.Read())
                    {
                        var addrObject = new AddrObject(reader, AddrObjectKind.AddrLandMarkCurrent);

                        addrObjects.Add(addrObject);
                    }
            }

            foreach (AddrObject addrObject in addrObjects)
            {
                Address address = addresses.First(ad => ad.AoId == addrObject.ParentId);
                addrObject.HierarchyLevel = address.HierarchyLevel + 1;
                address.SetAddrObject(addrObject);
            }

            return addrObjects;
        }


        #region AddrHouses
        internal static IEnumerable<AddrHouse> FillAddrHouses(List<Address> addresses, HouseInfo houseInfo)
        {
            var addrHouses = new List<AddrHouse>();

            string hNumPred = string.IsNullOrEmpty(houseInfo.HouseNum)
                ? "ahc.HouseNum is null and ahc.NumberEffective is null"
                : string.Format("replace(ahc.HouseNum, ' ', '') = '{0}' or replace(ahc.NumberEffective, ' ', '') = '{0}'",
                    houseInfo.HouseNum);
            string bNumPred = string.IsNullOrEmpty(houseInfo.BuildNum)
                ? "ahc.BuildNum is null"
                : string.Format("replace(ahc.BuildNum, ' ', '') = '{0}'", houseInfo.BuildNum);
            string sNumPred = string.IsNullOrEmpty(houseInfo.StructureNum)
                ? "ahc.StructureNum is null"
                : string.Format("replace(ahc.StructureNum, ' ', '') = '{0}'", houseInfo.StructureNum);
            string allInNumberEffectivePred = string.Format("replace(ahc.NumberEffective, ' ', '') = '{0}'",
                houseInfo.HouseNum + (!string.IsNullOrEmpty(houseInfo.BuildNum) ? "-" + houseInfo.BuildNum : "") +
                (!string.IsNullOrEmpty(houseInfo.StructureNum) ? "-" + houseInfo.StructureNum : ""));

            string query = string.Format(@"select ahc.Id, ahc.ParentAoId
                                from dbo.AddrHousesCurrent ahc
                                where (({1}) and {2} and {3}{4}
                                and ahc.ParentAoId in ({0})",
                string.Join(",", addresses.Where(a => a.AoId.HasValue).Select(a => string.Format("'{0}'", a.AoId))),
                hNumPred, bNumPred, sNumPred,
                allInNumberEffectivePred.Contains("-")
                    ? " or " + allInNumberEffectivePred + ")"
                    : ")");

            using (var connection = new SqlConnection("context connection = true"))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                    while (reader.Read())
                    {
                        var addrHouse = new AddrHouse(reader.GetGuid(0), reader.GetGuid(1));
                        addrHouses.Add(addrHouse);
                    }
            }

            foreach (IGrouping<Guid, AddrHouse> addrHouse in addrHouses.ToLookup(a => a.ParentId))
            {
                Address address = addresses.First(a => a.AoId == addrHouse.Key);
                address.SetHouse(addrHouse.First());
            }

            return addrHouses;
        }
        #endregion


        internal static void FillAddressHouse(Address address, HouseInfo houseInfo)
        {
            if (address.AoId == null && address.AddonAoId == null)
                return;

            string bNumPred = string.IsNullOrEmpty(houseInfo.BuildNum)
                ? "house.BuildNum is null"
                : string.Format("replace(house.BuildNum, ' ', '') = '{0}'", houseInfo.BuildNum);
            string sNumPred = string.IsNullOrEmpty(houseInfo.StructureNum)
                ? "house.StructureNum is null"
                : string.Format("replace(house.StructureNum, ' ', '') = '{0}'", houseInfo.StructureNum);

            using (var connection = new SqlConnection("context connection = true"))
            {
                connection.Open();

                string hNumPred = string.IsNullOrEmpty(houseInfo.HouseNum)
                    ? "house.Number is null"
                    : string.Format("replace(house.Number, ' ', '') = '{0}'", houseInfo.HouseNum);

                string query = string.Format(@"select house.Id, house.ParentId
                            from dbo.AddonAddrHouses house                                     
                            where house.ParentId = '{0}'
                            and ({1}) and {2} and {3};", address.AddonAoId ?? address.AoId, hNumPred, bNumPred, sNumPred);

                using (var command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                    while (reader.Read())
                    {
                        var addrHouse = new AddrHouse(reader.GetGuid(0), reader.GetGuid(1), true);
                        address.SetHouse(addrHouse);
                    }

                if (address.AddonHouseId == null && address.AoId != null)
                {
                    hNumPred = string.IsNullOrEmpty(houseInfo.HouseNum)
                        ? "house.HouseNum is null and house.NumberEffective is null"
                        : string.Format(
                            "replace(house.HouseNum, ' ', '') = '{0}' or replace(house.NumberEffective, ' ', '') = '{0}'",
                            houseInfo.HouseNum);

                    query = string.Format(@"select house.Id, house.ParentAoId
                            from dbo.AddrHousesCurrent house                                     
                            where house.ParentAoId = '{0}'
                            and ({1}) and {2} and {3};", address.AoId, hNumPred, bNumPred, sNumPred);

                    using (var command = new SqlCommand(query, connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                        while (reader.Read())
                        {
                            var addrHouse = new AddrHouse(reader.GetGuid(0), reader.GetGuid(1));
                            address.SetHouse(addrHouse);
                        }
                }
            }
        }


        #region AddonAddrObjects
        internal static IEnumerable<AddrObject> FillAddonAddrObjects(List<Address> addresses, List<NameAndType> names,
            bool isHeavy)
        {
            IEnumerable<AddrObject> addrObjects = GetAddonAddrObjects(names, isHeavy).ToList();
            if (addresses.Any(a => a.AoId != null))
                FillAddonAddrObjectsByAddrObjects(addrObjects, addresses.Where(a => a.AoId != null).ToList());

            if (addresses.All(a => a.AddonAoId == null))
                addresses.AddRange(addrObjects.Select(addonAo => new Address(addonAo)));

            return addrObjects;
        }


        static void FillAddonAddrObjectsByAddrObjects(IEnumerable<AddrObject> addonObjects, List<Address> addresses)
        {
            using (var connection = new SqlConnection("context connection = true"))
            {
                connection.Open();
                foreach (AddrObject addonObject in addonObjects)
                    foreach (Address address in addresses)
                    {
                        string query = string.Format(@"if exists (select 1 from dbo.AddonAddrObjects aao
                                                            join dbo.AddrObjectsOldIds aooi on aooi.old_id = aao.old_id
                                                            where aao.Id = '{0}' and aooi.Id = '{1}')
                                                        select cast (1 as bit)
                                                    else
                                                        select cast (0 as bit)", addonObject.Id, address.AoId);
                        using (var command = new SqlCommand(query, connection))
                        {
                            var exists = (bool) command.ExecuteScalar();
                            if (exists)
                                address.SetAddrObject(addonObject);
                        }
                    }
            }
        }


        static IEnumerable<AddrObject> GetAddonAddrObjects(List<NameAndType> nameAndTypes, bool isHeavy)
        {
            var levels = new[]
            {
                "Countries",
                "Regions",
                "Cities",
                "Streets"
            };

            List<AddrObject> addrObjects = new List<AddrObject>();

            foreach (string level in levels)
            {
                string query = string.Format(@"
                        select aao.Id, aao.ParentId, i.Name, cast(i.TypeId as tinyint)
                        from ({2}) i 
                        join dbo.AddonAddrObjects aao on (aao.Name {1} or aao.EnglishName {1}) and (i.TypeId is null or aao.TypeId = i.TypeId)
                        join dbo.AddrLevels al on al.Id = aao.Level
                        where al.EngName = '{0}';", level, isHeavy ? "like '%' + i.Name + '%'" : "= i.Name",
                    GetNameAndTypeSelect(nameAndTypes));

                List<AddrObject> levelObjects = SelectAddrObjects(query, AddrObjectKind.AddonAddrObject);
                if (levelObjects.Any())
                {
                    if (addrObjects.Any())
                        foreach (AddrObject levelObject in levelObjects)
                        {
                            AddrObject parent = addrObjects.FirstOrDefault(ao => ao.Id == levelObject.ParentId);
                            if (parent == null)
                                continue;

                            levelObject.Parent = parent;
                            levelObject.HierarchyLevel = parent.HierarchyLevel + 1;
                            parent.Childs.Add(levelObject);
                        }
                    addrObjects.AddRange(levelObjects);
                }
            }

            addrObjects =
                addrObjects.GroupBy(a => a.Id).SelectMany(
                    g => g.All(a => a.TypeId.HasValue) || g.All(a => !a.TypeId.HasValue) ? g : g.Where(a => a.TypeId.HasValue))
                    .ToList();

            return addrObjects.Distinct();
        }
        #endregion


        #region AddonAddrHouses
        internal static IEnumerable<AddrHouse> FillAddonAddrHouses(IEnumerable<Address> addresses, HouseInfo houseInfo)
        {
            var addrHouses = new List<AddrHouse>();

            string hNumPred = string.IsNullOrEmpty(houseInfo.HouseNum)
                ? "aah.Number is null"
                : string.Format("aah.Number = '{0}'", houseInfo.HouseNum);
            string bNumPred = string.IsNullOrEmpty(houseInfo.BuildNum)
                ? "aah.BuildNum is null"
                : string.Format("aah.BuildNum = '{0}'", houseInfo.BuildNum);
            string sNumPred = string.IsNullOrEmpty(houseInfo.StructureNum)
                ? "aah.StructureNum is null"
                : string.Format("aah.StructureNum = '{0}'", houseInfo.StructureNum);

            string queryTemplate = @"select aah.Id, aah.ParentId
                                from dbo.AddonAddrHouses aah                                     
                                where aah.ParentId = '{0}'" +
                                   string.Format(" and {0} and {1} and {2};", hNumPred, bNumPred, sNumPred);

            foreach (Address address in addresses)
            {
                string query;
                if (address.AddonAoId != null)
                    query = string.Format(queryTemplate, address.AddonAoId);
                else if (address.AoId != null)
                    query = string.Format(queryTemplate, address.AoId);
                else
                    continue;

                using (var connection = new SqlConnection("context connection = true"))
                {
                    connection.Open();
                    using (var command = new SqlCommand(query, connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                        if (reader.Read())
                        {
                            var addrHouse = new AddrHouse(reader.GetGuid(0), reader.GetGuid(1), true);
                            addrHouses.Add(addrHouse);
                            address.SetHouse(addrHouse);
                        }
                }
            }

            return addrHouses;
        }
        #endregion


        static List<AddrObject> SelectAddrObjects(string query, AddrObjectKind kind = AddrObjectKind.AddrObjectCurrent)
        {
            var result = new List<AddrObject>();

            using (var connection = new SqlConnection("context connection = true"))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                    while (reader.Read())
                        result.Add(new AddrObject(reader, kind));
            }

            return result;
        }


        static string GetNameAndTypeSelect(List<NameAndType> nameAndTypes)
        {
            var nameTypes = nameAndTypes.Select(n => new {Id = n.Type == null ? (byte?) null : n.Type.Id, Name = n.AddrName})
                .Union(nameAndTypes.Select(n => new {Id = (byte?) null, Name = n.OriginAddrName})).Where(
                    n => !string.IsNullOrEmpty(n.Name));

            return string.Join(" union ",
                nameTypes.Select(
                    n => string.Format("select {0} TypeId, '{1}' Name", n.Id.HasValue ? n.Id.Value.ToString() : "null", n.Name)));
        }


        internal static void FindAddress(Address address)
        {
            string aoIdPred = address.AoId.HasValue ? string.Format("a.AoId = '{0}'", address.AoId) : "a.AoId is null";
            string landMarkPred = address.LandMarkId.HasValue
                ? string.Format("a.LandMarkId = '{0}'", address.LandMarkId)
                : "a.LandMarkId is null";
            string houseIdPred = address.HouseId.HasValue
                ? string.Format("a.HouseId = '{0}'", address.HouseId)
                : "a.HouseId is null";
            string addonAoIdPred = address.AddonAoId.HasValue
                ? string.Format("a.AddonAoId = '{0}'", address.AddonAoId)
                : "a.AddonAoId is null";
            string addonHouseIdPred = address.AddonHouseId.HasValue
                ? string.Format("a.AddonHouseId = '{0}'", address.AddonHouseId)
                : "a.AddonHouseId is null";
            string roomPred = address.Room != null ? string.Format("a.Room like '%{0}%'", address.Room) : "a.Room is null";
            string wheres = string.Format("{0} and {1} and {2} and {3} and {4} and {5}", aoIdPred, landMarkPred, houseIdPred,
                addonAoIdPred, addonHouseIdPred, roomPred);
            string fullQuery = string.Format("select a.Id from dbo.Addresses a where {0}", wheres);

            using (var connection = new SqlConnection("context connection = true"))
            {
                connection.Open();

                using (var command = new SqlCommand(fullQuery, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                    if (reader.Read())
                        address.AddressId = reader.GetInt32(0);
            }
        }


        internal static List<AddrObjectType> FindTypes(IEnumerable<string> words)
        {
            var types = new List<AddrObjectType>();

            string select = string.Format(
                @"select distinct aot.Id,
                            lower(aot.Name),
                            lower(aot.ShortName),
                            lower(aot.EngName)
                            from dbo.AddrObjectTypes aot
                            join ({0}) t on t.String = aot.Name or t.String = aot.ShortName or t.String = EngName",
                string.Join(" union ", words.Select(n => string.Format("select '{0}' String", n))));
            using (var connection = new SqlConnection("context connection = true"))
            using (var command = new SqlCommand(select, connection))
            {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                    while (reader.Read())
                    {
                        byte id = reader.GetByte(0);
                        string name = reader.GetString(1);
                        string shortName = reader.GetString(2);
                        string engName = reader.GetString(3);
                        var addrObjectType = new AddrObjectType(id, name, shortName, engName);

                        if (!types.Contains(addrObjectType))
                            types.Add(addrObjectType);
                    }
            }

            return types;
        }
    }
}