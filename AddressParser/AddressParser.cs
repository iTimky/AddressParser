#region usings
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using CE.Parsing.Core;
using CE.Parsing.Core.Models;

using Microsoft.SqlServer.Server;

#endregion



namespace CE.Parsing
{
    public class AddressParser
    {
        [SqlFunction(FillRowMethodName = "FillRow", DataAccess = DataAccessKind.Read,
            SystemDataAccess = SystemDataAccessKind.Read)]
        public static IEnumerable ParseAddress(string addr, bool? isHeavy = false)
        {
            if (string.IsNullOrEmpty(addr))
                return null;

            return AddressParserInternal.ParseAddress(InitSearchQuery(addr), isHeavy == true);
        }


        [SqlFunction(FillRowMethodName = "FillRow", DataAccess = DataAccessKind.Read)]
        public static IEnumerable GetAddressByOldId(Guid? addrOldId)
        {
            if (!addrOldId.HasValue)
                return new List<Address>();

            return AddressParserInternal.GetAddressByOldId(addrOldId.Value);
        }


        [SqlFunction(DataAccess = DataAccessKind.Read)]
        public static string GetAddressStringByOldId(Guid? oldId)
        {
            if (!oldId.HasValue)
                return null;

            return AddressStringBuilder.GetAddressStringByOldId(oldId.Value);
        }


        [SqlFunction(DataAccess = DataAccessKind.Read)]
        public static string GetAddressStringByGuid(Guid? someId)
        {
            if (!someId.HasValue)
                return null;

            return AddressStringBuilder.GetAddressStringByGuid(someId.Value);
        }


        [SqlFunction(DataAccess = DataAccessKind.Read)]
        public static string GetAddressStringById(int? id)
        {
            if (!id.HasValue)
                return null;

            return AddressStringBuilder.GetAddressStringById(id.Value);
        }


        [SqlProcedure]
        public static void CreateAddonAddrHouse(Guid? parentId, string addr, out Guid? addonAddrHouseId)
        {
            addonAddrHouseId = null;
            if (!parentId.HasValue || string.IsNullOrEmpty(addr))
                return;

            addonAddrHouseId = AddressParserInternal.CreateAddonAddrHouse(parentId.Value, InitSearchQuery(addr));
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


        static string InitSearchQuery(string query)
        {
            string addrStr = Regex.Replace(query, @"\s*\-\s*", "-").ToLower();
            string postalCode = GetPostalCode(addrStr);
            string result = string.IsNullOrEmpty(postalCode) ? addrStr : addrStr.Replace(postalCode, "");
            return result;
        }


        static string GetPostalCode(string addr)
        {
            const string postalCodePattern = @"(?<pCode>[0-9]{5,6})";
            return Regex.Match(addr, postalCodePattern).Groups["pCode"].Value;
        }
    }
}