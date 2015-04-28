#region usings
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using AddressParser.Core.Models;

#endregion



namespace AddressParser.Core.Db
{
    public abstract partial class DataContextBase
    {
        public TupleOld GetTupleOld(int id)
        {
            using (var con = new SqlConnection(ConnectionString))
            using (SqlCommand com = con.CreateCommand())
            {
                com.CommandType = CommandType.Text;
                com.CommandText = @"select * from addresses_matching t where id=@id";
                com.Parameters.AddWithValue("id", id);

                con.Open();

                TupleOld t = null;
                using (SqlDataReader r = com.ExecuteReader())
                    if (r.Read())
                    {
                        t = new TupleOld();

                        t.old_Country = GetNullableVal<Guid>(r["old_Country"]);
                        t.old_Country_name = GetNullableString(r["old_Country_name"]);
                        t.old_CountryRegion = GetNullableVal<Guid>(r["old_CountryRegion"]);
                        t.old_CountryRegion_name = GetNullableString(r["old_CountryRegion_name"]);
                        t.old_City = GetNullableVal<Guid>(r["old_City"]);
                        t.old_City_name = GetNullableString(r["old_City_name"]);
                        t.old_City_type = GetNullableString(r["old_City_type"]);
                        t.old_Street = GetNullableVal<Guid>(r["old_Street"]);
                        t.old_Street_name = GetNullableString(r["old_Street_name"]);
                        t.old_Street_type = GetNullableString(r["old_Street_type"]);

                        t.old_BuildingNumber = GetNullableString(r["old_BuildingNumber"]);
                        t.old_AppartmentNumber = GetNullableString(r["old_AppartmentNumber"]);

                        t.Id = id;
                    }
                return t;
            }
        }


        protected static T? GetNullableVal<T>(object o, T? def = default(T?)) where T : struct
        {
            return o == null || o == DBNull.Value ? def : (T) o;
        }


        protected static string GetNullableString(object o, string def = null)
        {
            return GetNullableRef<string>(o, def);
        }


        protected static T GetNullableRef<T>(object o, T def = default(T)) where T : class
        {
            return o == null || o is DBNull ? def : (T) o;
        }


        #region GetOldAddress
        internal OldAddress GetOldAddress(Guid id)
        {
            OldAddress oldAddress = null;
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                string query = string.Format(@"select  ta.Country,
                                                ta.City,
                                                ta.Street,
                                                ta.CountryRegion,
                                                ta.BuildingNumber,
                                                ta.AppartmentNumber
                                        from SQL02.CityExpressDB_Online_ceapp.dbo.T_ADDRESS ta with (nolock)
                                        where ta.AddressId = '{0}';", id);
                using (var command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                    if (reader.Read())
                    {
                        oldAddress = new OldAddress();
                        oldAddress.Country = reader.IsDBNull(0) ? (Guid?) null : reader.GetGuid(0);
                        oldAddress.CountryRegion = reader.IsDBNull(3) ? (Guid?) null : reader.GetGuid(3);
                        oldAddress.City = reader.IsDBNull(1) ? (Guid?) null : reader.GetGuid(1);
                        oldAddress.Street = reader.IsDBNull(2) ? (Guid?) null : reader.GetGuid(2);
                        oldAddress.BuildingNumber = reader.IsDBNull(4) ? null : reader.GetString(4);
                        oldAddress.AppartmentNumber = reader.IsDBNull(5) ? null : reader.GetString(5);
                    }
            }

            if (oldAddress != null)
            {
                if (oldAddress.City.HasValue && oldAddress.City.Value == Guid.Parse("8A8B35FD-F4DF-4003-88CF-BCFE08BE4D5D"))
                    // Moscow
                    oldAddress.CountryRegion = null;

                if (oldAddress.BuildingNumber != null)
                {
                    oldAddress.BuildingNumber = oldAddress.BuildingNumber.Trim();
                    if (!Regex.IsMatch(oldAddress.BuildingNumber, @"[\da-zA-Zа-яА-Я]"))
                        oldAddress.BuildingNumber = null;
                }
                if (oldAddress.AppartmentNumber != null)
                {
                    oldAddress.AppartmentNumber = oldAddress.AppartmentNumber.Trim();
                    if (!Regex.IsMatch(oldAddress.AppartmentNumber, @"[\da-zA-Zа-яА-Я]"))
                        oldAddress.AppartmentNumber = null;
                }
            }
            return oldAddress;
        }
        #endregion


        #region GetAddressByOldAddress
        /// <summary>
        /// Does not recognizing! Returns object only if old_id exists in AddrObjectsOldIds or AddonAddrObjects.old_id
        /// </summary>
        /// <param name="majorness">4 means street, 3 city, 2 region, 1 country</param>
        internal Address GetAddressByMajorOldGeoId(OldAddress oldAddress, int majorness = 4)
        {
            Address address = null;
            Guid? searchingObject = (majorness >= 4 ? oldAddress.Street : null) ??
                                    (majorness >= 3 ? oldAddress.City : null) ??
                                    (majorness >= 2 ? oldAddress.CountryRegion : null) ??
                                    oldAddress.Country;
            if (searchingObject == null)
                return null;

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                string addrObjQuery = string.Format(@"select  aoc.Id, aoc.ParentId, aoc.OfficialName, aoc.TypeId, cast(1 as bit) as IsTypeExplicit
                                        from dbo.AddrObjectsCurrent aoc
                                            join dbo.AddrObjectsOldIds aooi on aooi.Id = aoc.Id
                                        where aooi.old_id = '{0}';", searchingObject);
                using (var command = new SqlCommand(addrObjQuery, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                        if (reader.Read())
                            address = new Address(new AddrObject(reader, AddrObjectKind.AddrObjectCurrent));

                    if (address != null)
                        return address;

                    string addonObjQuery = string.Format(@"select  aao.Id, aao.ParentId, aao.Name, aao.TypeId, cast(1 as bit) as IsTypeExplicit
                                        from dbo.AddonAddrObjects aao
                                        where aao.old_id = '{0}';", searchingObject);

                    command.CommandText = addonObjQuery;
                    using (SqlDataReader reader = command.ExecuteReader())
                        if (reader.Read())
                            address = new Address(new AddrObject(reader, AddrObjectKind.AddonAddrObject));
                }
            }

            return address;
        }
        #endregion


        #region GetAddressAddrLandMarks
        internal List<AddrObject> GetAddressAddrLandMarks(IEnumerable<Address> addresses, IEnumerable<NameAndType> names)
        {
            var addrObjects = new List<AddrObject>();

            string query = string.Format(@"select   almc.Id,
                                                    almc.ParentAoId,
                                                    i.String
                                     from dbo.AddrLandMarksCurrent almc
                                     join ({1}) i on almc.Location = i.String or almc.Location like '% ' + i.String + ' %' 
                                     where almc.ParentAoId in ({0})",
                string.Join(",", addresses.Select(ad => string.Format("'{0}'", ad.AoId))),
                string.Join(" union ", names.Select(n => string.Format("select '{0}' String", n.AddrObjectName))));

            using (var connection = new SqlConnection(ConnectionString))
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

            return addrObjects;
        }
        #endregion


        #region GetAddressAddrHouses
        internal List<AddrHouse> GetAddressAddrHouses(List<Address> addresses, HouseInfo houseInfo)
        {
            var addrHouses = new List<AddrHouse>();
            if (!addresses.Any())
                return addrHouses;

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

            using (var connection = new SqlConnection(ConnectionString))
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

            return addrHouses;
        }
        #endregion


        #region GetAddressAddrHouse
        internal AddrHouse GetAddressAddrHouse(Address address, HouseInfo houseInfo)
        {
            if (address.AoId == null && address.AddonAoId == null)
                return null;

            AddrHouse addrHouse = null;

            string bNumPred = string.IsNullOrEmpty(houseInfo.BuildNum)
                ? "house.BuildNum is null"
                : string.Format("replace(house.BuildNum, ' ', '') = '{0}'", houseInfo.BuildNum);
            string sNumPred = string.IsNullOrEmpty(houseInfo.StructureNum)
                ? "house.StructureNum is null"
                : string.Format("replace(house.StructureNum, ' ', '') = '{0}'", houseInfo.StructureNum);

            using (var connection = new SqlConnection(ConnectionString))
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
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                        if (reader.Read())
                            addrHouse = new AddrHouse(reader.GetGuid(0), reader.GetGuid(1), true);

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

                        //using (var command = new SqlCommand(query, connection))
                        command.CommandText = query;
                        using (SqlDataReader reader = command.ExecuteReader())
                            if (reader.Read())
                                addrHouse = new AddrHouse(reader.GetGuid(0), reader.GetGuid(1));
                    }
                }
            }

            return addrHouse;
        }
        #endregion


        #region GetAddressAddonAddrHouses
        internal List<AddrHouse> GetAddressAddonAddrHouses(List<Address> addresses, HouseInfo houseInfo)
        {
            var addrHouses = new List<AddrHouse>();

            if (!addresses.Any())
                return addrHouses;

            string hNumPred = string.IsNullOrEmpty(houseInfo.HouseNum)
                ? "aah.Number is null"
                : string.Format("aah.Number = '{0}'", houseInfo.HouseNum);
            string bNumPred = string.IsNullOrEmpty(houseInfo.BuildNum)
                ? "aah.BuildNum is null"
                : string.Format("aah.BuildNum = '{0}'", houseInfo.BuildNum);
            string sNumPred = string.IsNullOrEmpty(houseInfo.StructureNum)
                ? "aah.StructureNum is null"
                : string.Format("aah.StructureNum = '{0}'", houseInfo.StructureNum);

            string query = string.Format(@"select aah.Id, aah.ParentId
                                from dbo.AddonAddrHouses aah                                     
                                where aah.ParentId in ({0})
                                and {1} and {2} and {3};",
                string.Join(", ", addresses.Select(a => string.Format("'{0}'", a.AddrObject.Id))), hNumPred, bNumPred, sNumPred);

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                    while (reader.Read())
                    {
                        var addrHouse = new AddrHouse(reader.GetGuid(0), reader.GetGuid(1), true);
                        addrHouses.Add(addrHouse);
                    }
            }

            return addrHouses;
        }
        #endregion


        internal void SetAddressId(Address address)
        {
            if (address.AddressId.HasValue)
                return;

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
            //string roomPred = address.Room != null ? string.Format("a.Room like '%{0}%'", address.Room) : "a.Room is null";
            string roomPred = address.Room != null ? string.Format("a.Room = '{0}'", address.Room) : "a.Room is null";
            string wheres = string.Format("{0} and {1} and {2} and {3} and {4} and {5}", aoIdPred, landMarkPred, houseIdPred,
                addonAoIdPred, addonHouseIdPred, roomPred);
            string fullQuery = string.Format("select a.Id from dbo.Addresses a where {0}", wheres);

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(fullQuery, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                    if (reader.Read())
                        address.AddressId = reader.GetInt32(0);
            }
        }


        #region MergeAddress
        internal int MergeAddress(Address a)
        {
            string aoIdPred = a.AoId.HasValue ? "a.AoId = s.AoId" : "a.AoId is null";
            string landMarkPred = a.LandMarkId.HasValue ? "a.LandMarkId = s.LandMarkId" : "a.LandMarkId is null";
            string houseIdPred = a.HouseId.HasValue ? "a.HouseId = s.HouseId" : "a.HouseId is null";
            string addonAoIdPred = a.AddonAoId.HasValue ? "a.AddonAoId = s.AddonAoId" : "a.AddonAoId is null";
            string addonHouseIdPred = a.AddonHouseId.HasValue ? "a.AddonHouseId = s.AddonHouseId" : "a.AddonHouseId is null";
            string roomPred = a.Room != null ? "a.Room = s.Room" : "a.Room is null";

            string fullQuery = @"
declare @t table(id int);
;
merge into dbo.Addresses a
using (select @AoId AoId, @LandMarkId LandMarkId, @HouseId HouseId, @AddonAoId AddonAoId, @AddonHouseId AddonHouseId, @Room Room) s
on 
	" + string.Format("{0} and {1} and {2} and {3} and {4} and {5}",
                aoIdPred, landMarkPred, houseIdPred, addonAoIdPred, addonHouseIdPred, roomPred) +
                               @"
when not matched then insert
	(AoId, LandMarkId, HouseId, AddonAoId, AddonHouseId, Room)
	values
	(s.AoId, s.LandMarkId, s.HouseId, s.AddonAoId, s.AddonHouseId, s.Room)
when matched then update set
	AoId=s.AoId -- fake update
output Inserted.Id into @t
;
select id from @t;
";

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(fullQuery, connection))
            {
                command.CommandTimeout = 600;
                connection.Open();

                command.Parameters.AddWithValue("AoId", a.AoId ?? (object) DBNull.Value);
                command.Parameters.AddWithValue("LandMarkId", a.LandMarkId ?? (object) DBNull.Value);
                command.Parameters.AddWithValue("HouseId", a.HouseId ?? (object) DBNull.Value);
                command.Parameters.AddWithValue("AddonAoId", a.AddonAoId ?? (object) DBNull.Value);
                command.Parameters.AddWithValue("AddonHouseId", a.AddonHouseId ?? (object) DBNull.Value);
                command.Parameters.AddWithValue("Room", a.Room ?? (object) DBNull.Value);

                return (int) command.ExecuteScalar();
            }
        }
        #endregion


        #region GetAddressById
        internal Address GetAddressById(int id)
        {
            string addressQuery =
                string.Format(
                    @"select AoId, LandMarkId, HouseId, AddonAoId, AddonHouseId, Room from dbo.Addresses where Id = {0}", id);
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(addressQuery, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                    if (reader.Read())
                        return new Address(reader);
            }

            return null;
        }
        #endregion
    }
}