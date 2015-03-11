using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CE.Parsing.Core.Models;



namespace CE.Parsing.Core.Db
{
    public abstract partial class DataContextBase
    {
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
                        oldAddress.Country = reader.IsDBNull(0) ? (Guid?)null : reader.GetGuid(0);
                        oldAddress.City = reader.IsDBNull(1) ? (Guid?)null : reader.GetGuid(1);
                        oldAddress.Street = reader.IsDBNull(2) ? (Guid?)null : reader.GetGuid(2);
                        oldAddress.CountryRegion = reader.IsDBNull(3) ? (Guid?)null : reader.GetGuid(3);
                        oldAddress.BuildingNumber = reader.IsDBNull(4) ? null : reader.GetString(4);
                        oldAddress.AppartmentNumber = reader.IsDBNull(5) ? null : reader.GetString(5);
                    }
            }

            return oldAddress;
        }
        #endregion


        #region GetAddressByOldAddress
        internal Address GetAddressByOldAddress(OldAddress oldAddress)
        {
            Address address = null;
            Guid? searchingObject = oldAddress.Street ?? oldAddress.City ?? oldAddress.CountryRegion ?? oldAddress.Country;
            if (searchingObject == null)
                return null;

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                string addrObjQuery = string.Format(@"select  aoc.Id, aoc.ParentId, aoc.OfficialName, aoc.TypeId
                                        from dbo.AddrObjectsCurrent aoc
                                        join dbo.AddrObjectsOldIds aooi on aooi.Id = aoc.Id
                                        where aooi.old_id = '{0}';", searchingObject);
                using (var command = new SqlCommand(addrObjQuery, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                        if (reader.Read())
                        {
                            var addrObject = new AddrObject(reader);
                            address = new Address(addrObject);
                        }

                    if (address != null)
                        return address;

                    string addonObjQuery = string.Format(@"select  aao.Id, aao.ParentId, aao.Name, aao.TypeId
                                        from dbo.AddonAddrObjects aao
                                        where aao.old_id = '{0}';", searchingObject);

                    command.CommandText = addonObjQuery;
                    //using (var command = new SqlCommand(addonObjQuery, connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                        if (reader.Read())
                        {
                            var addrObject = new AddrObject(reader, AddrObjectKind.AddonAddrObject);
                            address = new Address(addrObject);
                        }
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
                string.Join(" union ", names.Select(n => string.Format("select '{0}' String", n.AddrName))));

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
        internal List<AddrHouse> GetAddressAddrHouses(IEnumerable<Address> addresses, HouseInfo houseInfo)
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
        internal List<AddrHouse> GetAddressAddonAddrHouses(IEnumerable<Address> addresses, HouseInfo houseInfo)
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

            string query = string.Format(@"select aah.Id, aah.ParentId
                                from dbo.AddonAddrHouses aah                                     
                                where aah.ParentId in ({0})
                                and {1} and {2} and {3};", string.Join(", ", addresses.Where(a => a.AddrObject != null).Select(a => string.Format("'{0}'", a.AddrObject.Id))), hNumPred, bNumPred, sNumPred);

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


        #region FindAddress
        internal void FindAddress(Address address)
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

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(fullQuery, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                    if (reader.Read())
                        address.AddressId = reader.GetInt32(0);
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
