#region usings
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using CE.Parsing.Core;
using CE.Parsing.Core.Db;
using CE.Parsing.Core.Models;

using Microsoft.SqlServer.Server;

#endregion



namespace CE.Parsing
{
    public static class AddressParser
    {
        static readonly Parser Parser = new Parser(new DataContext());

        [SqlFunction(FillRowMethodName = "FillRow", DataAccess = DataAccessKind.Read,
            SystemDataAccess = SystemDataAccessKind.Read)]
        public static IEnumerable ParseAddress(string addr, bool? isHeavy = false)
        {
            var address = Parser.ParseAddress(addr, isHeavy);
            return address == null ? null : new[] { address };
        }


        [SqlFunction(FillRowMethodName = "FillRow", DataAccess = DataAccessKind.Read)]
        public static IEnumerable GetAddressByOldId(Guid? addrOldId)
        {
            var address = Parser.GetAddressByOldId(addrOldId);
            return address == null ? null : new[] { address };
        }


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