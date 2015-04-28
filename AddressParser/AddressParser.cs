#region usings

#region usings

#region usings

using System.Data;
using System.Data.SqlClient;
using System;
using System.Collections;
using System.Text.RegularExpressions;
using AddressParser.Core;
using AddressParser.Core.Db;
using AddressParser.Core.Models;
using Microsoft.SqlServer.Server;

#endregion


#endregion


#endregion


// ReSharper disable UnusedMember.Global



namespace AddressParser
{
    public static class AddressParser
    {
        static AddressParser()
        {
            if (Regex.CacheSize < 100)
                Regex.CacheSize = 100;
        }


        static readonly Parser Parser = new Parser(new DataContext());


        static string ServerNameAsLocalhost(string sn)
        {
            if (!sn.Contains(@"\"))
                return "localhost";
            return "localhost" + sn.Substring(sn.IndexOf('\\'));
        }


        #region parse string
        [SqlFunction(FillRowMethodName = "FillRow", DataAccess = DataAccessKind.Read,
            SystemDataAccess = SystemDataAccessKind.Read)]
        public static int? ParseAndMergeAddress(string addr, bool isHeavy, string servername, string dbName)
        {
            Address address = Parser.ParseAddress(addr, false, isHeavy);
            if (address == null)
                return null;

            if (address.AddressId.HasValue)
                return address.AddressId.Value;

            var dx =
                new DataContextCommon("Server=" + ServerNameAsLocalhost(servername) + ";Database=" + dbName +
                                      ";Trusted_Connection=True;Enlist=False");
            return dx.MergeAddress(address);
        }


        [SqlFunction(DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read)]
        public static int? ParseAndMergeAddressWithAddonHouse(string addr, bool isHeavy, string servername, string dbName)
        {
            var address = Parser.ParseAddress(addr, true, isHeavy);
            if (address == null)
                return null;

            if (address.AddressId.HasValue && (address.HouseInfo.IsEmpty() || (address.HouseId ?? address.AddonHouseId).HasValue))
                return address.AddressId;


            DataContextCommon dx = new DataContextCommon("Server=" + ServerNameAsLocalhost(servername) + ";Database=" + dbName +
                                      ";Trusted_Connection=True;Enlist=False");

            if (!address.HouseInfo.IsEmpty() && !address.HouseId.HasValue && !address.AddonHouseId.HasValue && AddrObjectType.HouseParents.Contains(address.AddrObject.Type))
            {
                var addonHouseId = Parser.CreateAddonAddrHouse(address.AoId ?? address.AddonAoId, address.HouseInfo, dx);
                if (addonHouseId.HasValue)
                    address.SetHouse(new AddrHouse(addonHouseId.Value, true));
            }

            return dx.MergeAddress(address);
        }


        static string GetDbName()
        {
            using (SqlConnection con = new SqlConnection("context connection = true"))
            using (SqlCommand com = con.CreateCommand())
            {
                con.Open();
                com.CommandType = CommandType.Text;
                com.CommandText = @"Select DB_NAME()";

                return (string) com.ExecuteScalar();
            }
        }


        [SqlFunction(FillRowMethodName = "FillRow", DataAccess = DataAccessKind.Read,
            SystemDataAccess = SystemDataAccessKind.Read)]
        public static IEnumerable ParseAddress(string addr, bool? isHeavy = false)
        {
            Address address = Parser.ParseAddress(addr, true, isHeavy);
            return address == null ? null : new[] {address};
        }
        #endregion


        #region get string
        [SqlFunction(DataAccess = DataAccessKind.Read)]
        public static string GetAddressStringByOldId(Guid? oldId)
        {
            return Parser.GetAddressStringByOldId(oldId);
        }


        [SqlFunction(DataAccess = DataAccessKind.Read)]
        public static string GetAddressStringByGuid(Guid? someId)
        {
            return Parser.GetAddressStringByGuid(someId);
        }


        [SqlFunction(DataAccess = DataAccessKind.Read)]
        public static string GetAddressStringById(int? id)
        {
            return Parser.GetAddressStringById(id);
        }
        #endregion


        [SqlFunction(DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read)]
        public static string CompareAddresses(int? newAddress, Guid? oldAddress)
        {
            return Parser.CompareAddresses(newAddress, oldAddress).Name;
        }

        [SqlFunction(DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read)]
        public static string CompareAddressesByString(string newAddress, string oldAddress)
        {
            return Parser.CompareAddresses(newAddress, oldAddress).Name;
        }


        [SqlProcedure]
        public static void CreateAddonAddrHouse(Guid? parentId, string addr, out Guid? addonAddrHouseId)
        {
            addonAddrHouseId = Parser.CreateAddonAddrHouse(parentId, addr);
        }


        static void FillRow(object obj, out int? addressId, out Guid? aoId, out Guid? landMarkId, out Guid? houseId,
            out Guid? addonAoId, out Guid? addonHouseId, out string room, out bool? isAllWordsFound)
        {
            var address = (Address) obj;
            addressId = address.AddressId;
            aoId = address.AoId;
            landMarkId = address.LandMarkId;
            houseId = address.HouseId;
            addonAoId = address.AddonAoId;
            addonHouseId = address.AddonHouseId;
            room = address.Room;
            isAllWordsFound = address.IsAllWordsFound;
        }
    }
}



// ReSharper restore UnusedMember.Global