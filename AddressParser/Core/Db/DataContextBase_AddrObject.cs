using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

using AddressParser.Core.Models;



namespace AddressParser.Core.Db
{
    public abstract partial class DataContextBase
    {
        #region GetTypes
        internal List<AddrObjectType> GetTypes(IEnumerable<string> words)
        {
            var types = new List<AddrObjectType>();

            string query = string.Format(
                @"select distinct aot.Id,
                            lower(aot.Name),
                            lower(aot.ShortName),
                            lower(aot.EngName)
                            from dbo.AddrObjectTypes aot
                            join ({0}) t on t.String = aot.Name or t.String = aot.ShortName or t.String = EngName",
                string.Join(" union ", words.Distinct().Select(n => string.Format("select '{0}' String", n))));
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(query, connection))
            {
                command.Connection.Open();
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
        #endregion


        #region GetAddrObjects
        internal List<AddrObject> GetAddrObjects(List<NameAndType> nameAndTypes, bool isHeavy)
        {
            string query;

            if (isHeavy)
                query = GetHeavyAddrObjectSearchQuery(nameAndTypes);
            else
                query = string.Format(@"
                        select t.Id, t.ParentId, t.Name, cast(t.TypeId as tinyint), t.IsTypeExplicit, t.Level, t.RegionId, aoc.ParentId as ParentParentId from
                        (
                            select  aoc.Id, aoc.ParentId, i.Name, aoc.TypeId, cast(0 as bit) as IsTypeExplicit, aoc.[Level], aoc.RegionId
                            from ({0}) i    
                            join dbo.AddrObjectsCurrent aoc on aoc.FormalName = i.Name and i.TypeId is null
                            union
                            select  aoc.Id, aoc.ParentId, i.Name, aoc.TypeId, cast(0 as bit) as IsTypeExplicit, aoc.[Level], aoc.RegionId
                            from ({0}) i    
                            join dbo.AddrObjectsCurrent aoc on aoc.OfficialName = i.Name and i.TypeId is null
                            union
                            select  aoc.Id, aoc.ParentId, i.Name, aoc.TypeId, cast(1 as bit) as IsTypeExplicit, aoc.[Level], aoc.RegionId
                            from ({0}) i    
                            join dbo.AddrObjectsCurrent aoc on aoc.FormalName = i.Name and i.TypeId = aoc.TypeId
                            union
                            select  aoc.Id, aoc.ParentId, i.Name, aoc.TypeId, cast(1 as bit) as IsTypeExplicit, aoc.[Level], aoc.RegionId
                            from ({0}) i    
                            join dbo.AddrObjectsCurrent aoc on aoc.OfficialName = i.Name and i.TypeId = aoc.TypeId
                        ) t
                        left join dbo.AddrObjectsCurrent aoc on aoc.Id = t.ParentId;",
                    GetNameAndTypeSelect(nameAndTypes)
                    );

            return SelectAddrObjects(query);
        }


        string GetNameAndTypeSelect(List<NameAndType> nameAndTypes)
        {
            var nameTypes = nameAndTypes.Select(n => new { Id = n.Type == null ? (byte?)null : n.Type.Id, Name = n.AddrObjectName.Name })
                .Union(nameAndTypes.Select(n => new { Id = (byte?)null, Name = n.AddrObjectName.OriginalName })).Where(
                    n => !string.IsNullOrEmpty(n.Name));

            return string.Join(" union ",
                nameTypes.Select(
                    n => string.Format("select {0} TypeId, '{1}' Name", n.Id.HasValue ? n.Id.Value.ToString() : "null", n.Name)));
        }


        List<AddrObject> SelectAddrObjects(string query, AddrObjectKind kind = AddrObjectKind.AddrObjectCurrent)
        {
            var result = new List<AddrObject>();

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                    while (reader.Read())
                        result.Add(new AddrObject(reader, kind));
            }

            return result;
        }


        string GetHeavyAddrObjectSearchQuery(List<NameAndType> nameAndTypes)
        {
            var stringBuilder =
                new StringBuilder("select t.Id, t.ParentId, t.Name, cast(t.TypeId as tinyint), t.IsTypeExplicit, t.Level from(" +
                                  Environment.NewLine);
            var nameTypes = nameAndTypes.Select(n => new { Id = n.Type == null ? 0 : n.Type.Id, Name = n.AddrObjectName.Name })
                .Union(nameAndTypes.Select(n => new { Id = 0, Name = n.AddrObjectName.OriginalName })).Where(n => !string.IsNullOrEmpty(n.Name));

            foreach (var nameType in nameTypes)
            {
                string subQuery = string.Format(@" select aoc.Id, aoc.ParentId, '{0}' as Name, aoc.TypeId, {3} as IsTypeExplicit, aoc.[Level]
    from dbo.AddrObjectsCurrent aoc
    where contains ((FormalName, OfficialName), '""{1}""') and (FormalName like '%{0}%' or OfficialName like '%{0}%'){2} union",
                    nameType.Name.Trim(),
                    nameType.Name.Trim(),
                    nameType.Id == 0 ? "" : string.Format(" and aoc.TypeId = {0}", nameType.Id),
                    nameType.Id == 0 ? "cast(0 as bit)" : "cast(1 as bit)");
                stringBuilder.Append(subQuery);
            }

            stringBuilder.Remove(stringBuilder.Length - "union".Length, "union".Length);
            stringBuilder.Append(@") t");

            string query = stringBuilder.ToString();

            return query;
        }
        #endregion


        #region GetAddonAddrObjects
        internal List<AddrObject> GetAddonAddrObjects(List<NameAndType> nameAndTypes, bool isHeavy)
        {
            string query = string.Format(@"
                        select aao.Id,
                                aao.ParentId,
                                i.Name,
                                aao.TypeId,
                                cast ((case when i.TypeId is null then 0 else 1 end) as bit) as IsTypeExplicit,
                                aao.Level
                        from ({1}) i 
                        join dbo.AddonAddrObjects aao on (aao.Name {0} or aao.EnglishName {0}) and (i.TypeId is null or aao.TypeId = i.TypeId);", isHeavy ? "like '%' + i.Name + '%'" : "= i.Name", GetNameAndTypeSelect(nameAndTypes));

            return SelectAddrObjects(query, AddrObjectKind.AddonAddrObject);
        }
        #endregion


        #region GetGeographicalObjects
        internal List<GeographicalObject> GetGeographicalObjects(OldAddress oldAddress)
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
                @"select o.GeographicalObjectsID, o.Name, tgot.Abbreviation
from SQL02.CityExpressDB_Online_ceapp.dbo.T_GEOGRAPHICAL_OBJECTS o with (nolock) 
    left join SQL02.CityExpressDB_Online_ceapp.dbo.T_GEOGRAPHICAL_OBJECTS_TYPE tgot (nolock) on o.GeographicalObjectsType = tgot.GeographicalObjectsTypeID
where GeographicalObjectsID in ({0})",
                string.Join(", ", geographicalObjectIds.Select(g => string.Format("'{0}'", g))));

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(geogrObjQuery, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                        while (reader.Read())
                        {
                            GeographicalObject o = new GeographicalObject
                            {
                                Id = reader.GetGuid(0),
                                Name = reader.IsDBNull(1) ? null : reader.GetString(1).Trim(),
                                TypeName = reader.IsDBNull(2) ? null : reader.GetString(2).Trim(),
                            };

                            if (o.Name != "." && o.Name != "" && o.Name != "-" && o.Name != "*" && o.Name != "," && o.Name != "др" &&
                                o.Name != "111" && o.Name != "000" && o.Name != "***" && o.Name != "др." &&
                            !(o.Id == oldAddress.City && (o.Name.Contains("др.") || o.Name.Contains("другие")) ||
                              o.Id == oldAddress.Street && (o.Name.Contains("улицы") || o.Name.Contains("др. ул."))))
                            geographicalObjects.Add(o);
                        }
                }
            }

            return geographicalObjects;
        }
        #endregion


        #region GetHierarchyFromAddons
        internal List<AddressHierarchyObject> GetHierarchyFromAddons(Guid someId)
        {
            var hierarchyObjects = new List<AddressHierarchyObject>();
            AddressHierarchyObject houseObject = null;
            string houseQuery =
                string.Format(@"select Id, ParentId, Number, BuildNum, StructureNum from dbo.AddonAddrHouses where Id = '{0}'",
                    someId);
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(houseQuery, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                        while (reader.Read())
                        {
                            houseObject = new AddressHierarchyObject
                            {
                                Id = reader.GetGuid(0),
                                ParentId = reader.IsDBNull(1) ? (Guid?) null : reader.GetGuid(1),
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

                    //using (var command = new SqlCommand(objectQuery, connection))
                    command.CommandText = objectQuery;
                    using (SqlDataReader reader = command.ExecuteReader())
                        while (reader.Read())
                        {
                            var hierarchyObject = new AddressHierarchyObject
                            {
                                Id = reader.GetGuid(0),
                                ParentId = reader.IsDBNull(1) ? (Guid?) null : reader.GetGuid(1),
                                HierarchyLevel = reader.GetInt32(4)
                            };

                            if (!reader.IsDBNull(2))
                                hierarchyObject.Name = string.Format("{0}. {1}", reader.GetString(3), reader.GetString(2));

                            hierarchyObjects.Add(hierarchyObject);
                        }
                }
            }

            return hierarchyObjects;
        }
        #endregion


        #region GetHierarchyFromCurrent
        internal List<AddressHierarchyObject> GetHierarchyFromCurrent(Guid someId, int nextHierarchyLevel)
        {
            var hierarchyObjects = new List<AddressHierarchyObject>();
            AddressHierarchyObject houseOrLandMark = null;
            string houseQuery =
                string.Format(
                    @"select Id, ParentAoId, NumberEffective, HouseNum, BuildNum, StructureNum from dbo.AddrHousesCurrent where Id = '{0}'",
                    someId);
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(houseQuery, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                        while (reader.Read())
                        {
                            houseOrLandMark = new AddressHierarchyObject
                            {
                                Id = reader.GetGuid(0),
                                ParentId = reader.IsDBNull(1) ? (Guid?) null : reader.GetGuid(1),
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
                        //using (var command = new SqlCommand(landMarkQuery, connection))
                        command.CommandText = landMarkQuery;
                        using (SqlDataReader reader = command.ExecuteReader())
                            while (reader.Read())
                            {
                                houseOrLandMark = new AddressHierarchyObject
                                {
                                    Id = reader.GetGuid(0),
                                    ParentId = reader.IsDBNull(1) ? (Guid?) null : reader.GetGuid(1),
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

                    //using (var command = new SqlCommand(objectQuery, connection))
                    command.CommandText = objectQuery;
                    using (SqlDataReader reader = command.ExecuteReader())
                        while (reader.Read())
                        {
                            var hierarchyObject = new AddressHierarchyObject
                            {
                                Id = reader.GetGuid(0),
                                ParentId = reader.IsDBNull(1) ? (Guid?) null : reader.GetGuid(1),
                                HierarchyLevel = reader.GetInt32(4)
                            };

                            if (!reader.IsDBNull(2))
                                hierarchyObject.Name = string.Format("{0}. {1}", reader.GetString(3), reader.GetString(2));

                            hierarchyObjects.Add(hierarchyObject);
                        }
                }
            }

            return hierarchyObjects;
        }
        #endregion


        #region CreateAddonAddrHouse
        internal Guid? CreateAddonAddrHouse(Guid parentId, HouseInfo houseInfo)
        {
            Guid houseId = Guid.NewGuid();
            string createAddonHouse = string.Format(@"insert dbo.AddonAddrHouses (Id, ParentId, Number, BuildNum, StructureNum)
                                                select '{0}', '{1}', '{2}', {3}, {4}", houseId,
                parentId, houseInfo.HouseNum,
                string.IsNullOrEmpty(houseInfo.BuildNum) ? "null" : string.Format("'{0}'", houseInfo.BuildNum),
                string.IsNullOrEmpty(houseInfo.StructureNum) ? "null" : string.Format("'{0}'", houseInfo.StructureNum));

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(createAddonHouse, connection))
                    command.ExecuteNonQuery();
            }

            return houseId;
        }
        #endregion
    }
}
