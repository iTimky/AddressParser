#region usings
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;

using CE.Parsing.Core.Models;

#endregion



namespace CE.Parsing.Core
{
    internal static class AddressParserInternal
    {
        internal static IEnumerable<Address> ParseAddress(string searchQuery, bool isHeavy)
        {
            List<string> names = GetNames(searchQuery);

            HouseInfo houseInfo = GetHouseInfo(names, true);

            List<AddressPart> addressParts = SplitNamesBySpace(names);
            List<NameAndType> nameAndTypes = SplitNameAndTypes(addressParts).ToList();

            PushCrutch(nameAndTypes);

            if (!nameAndTypes.Any())
                return null;

            var addrObjects = new List<AddrObject>();
            var addrHouses = new List<AddrHouse>();

            addrObjects.AddRange(AddressSearcher.GetAddrObjects(nameAndTypes, isHeavy));

            List<Address> addresses = new List<Address>();
            addresses.AddRange(addrObjects.Select(ao => new Address(ao)));

            if (addresses.Any())
            {
                addrObjects.AddRange(AddressSearcher.FillAddrLandMarks(addresses, nameAndTypes));
                addrHouses.AddRange(AddressSearcher.FillAddrHouses(addresses, houseInfo));
            }
            addrObjects.AddRange(AddressSearcher.FillAddonAddrObjects(addresses, nameAndTypes, isHeavy));
            addrHouses.AddRange(AddressSearcher.FillAddonAddrHouses(addresses, houseInfo));

            addressParts.ForEach(a => a.AddWithoutChildTypes());
            addressParts = addressParts.Where(a => a.Variations.Any() && a != "Россия").ToList();

            FillIsAllWordsFound(addresses, addressParts);

            List<Address> possibleAddresses = GetPossibleAddresses(addresses);

            if (possibleAddresses == null)
            {
                if (isHeavy)
                    return ParseAddress(searchQuery, false);

                return Enumerable.Empty<Address>();
            }

            Address mainAddress = possibleAddresses.Count == 1
                ? possibleAddresses.First()
                : possibleAddresses.First(pa => pa.IsAllWordsFound == true);

            mainAddress.Room = houseInfo.Room;
            AddressSearcher.FindAddress(mainAddress);

            return new List<Address> {mainAddress};
        }


        static List<Address> GetPossibleAddresses(List<Address> addresses)
        {
            List<Address> possibleAddresses;

            int maxHierarchy = addresses.Any() ? addresses.Max(ad => ad.HierarchyLevel) : 0;

            if (addresses.Count(a => a.IsAllWordsFound == true && a.HierarchyLevel == maxHierarchy) == 1)
                possibleAddresses = addresses.Where(a => a.IsAllWordsFound == true && a.HierarchyLevel == maxHierarchy).ToList();
            else
                possibleAddresses = addresses.Where(a => a.IsAllWordsFound == true || a.HierarchyLevel == maxHierarchy).ToList();

            if (possibleAddresses.Count == 1 || possibleAddresses.Count(pa => pa.IsAllWordsFound == true) == 1)
                return possibleAddresses;

            List<AddrObject> addrObjects = possibleAddresses.Select(a => a.AddrObject).ToList();
            int aoMaxHierarchy = addrObjects.Max(ao => ao.HierarchyLevel);
            List<AddrObject> addrObjectsWithMaxHierarchy = addrObjects.Where(ao => ao.HierarchyLevel == aoMaxHierarchy).ToList();
            if (addrObjectsWithMaxHierarchy.Count == 1)
                return
                    possibleAddresses.Where(a => ReferenceEquals(a.AddrObject, addrObjectsWithMaxHierarchy.Single()))
                        .ToList();

            return null;
        }


        static void FillIsAllWordsFound(IEnumerable<Address> addresses, List<AddressPart> addressParts)
        {
            foreach (Address possibleAddress in addresses)
            {
                List<AddrObject> aObjects = possibleAddress.AddrObject.PlainParent();
                possibleAddress.IsAllWordsFound = true;

                for (int i = 0; i < addressParts.Count; i++)
                {
                    AddressPart addressPart = addressParts[i];
                    IEnumerable<NameAndType> nameNtypes = addressPart.Childs;

                    if (
                        aObjects.Any(
                            r =>
                                r.Name == addressPart || nameNtypes.Any(n => n.AddrName == r.Name) ||
                                (i != 0 &&
                                 (addressParts[i - 1].Variations.Where(v => v.Contains(addressPart.Variations.First())).Any(
                                     v => r.Name == v) || addressParts[i - 1].Variations.First() == addressPart.Variations.First()))))
                        continue;

                    possibleAddress.IsAllWordsFound = false;
                    break;
                }
            }
        }


        #region Query parsing
        static List<string> GetNames(string addrStr)
        {
            List<string> names =
                addrStr.Split(new[] {",", ";", ":"}, StringSplitOptions.RemoveEmptyEntries).Where(
                    x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.ToLower().Replace("пр-т", "пр-кт").Replace("\"", "").Replace("'", "''"))
                    .Select(x => Regex.Replace(x, @"(^|\s)пр\-д(\s|$)", " проезд "))
                    .Select(x => Regex.Replace(x, @"(^|\s)пр\.?(\s|$)", " пр-кт "))
                    .Select(x => Regex.Replace(x, "-ый", "-й"))
                    .Select(x => Regex.Replace(x, "-ая", "-я"))
                    .Select(x => Regex.Replace(x, "-ой", "-й"))
                    .Select(x => Regex.Replace(x, "-ий", "-й"))
                    .Select(s => Regex.Replace(s, @"\s+", " "))
                    .Select(s => s.Trim())
                    .Where(x => !string.IsNullOrEmpty(x)).ToList();

            return names;
        }


        static List<AddressPart> SplitNamesBySpace(IEnumerable<string> names)
        {
            List<AddressPart> addressParts = new List<AddressPart>();

            foreach (string name in names.ToList())
            {
                List<string> nameWords = name.Split(new[] {' '}).ToList();

                for (int i = 0; i < nameWords.Count; i++)
                {
                    var addWords = new List<string>();
                    string word = nameWords[i];
                    addWords.Add(word);

                    for (int j = i + 1; j < nameWords.Count; j++)
                    {
                        word += " " + nameWords[j];
                        addWords.Add(word);

                        if (j == (i + 1))
                        {
                            List<string> twoWords = word.Split(new[] {' '}).ToList();
                            twoWords.Reverse();
                            addWords.Add(string.Join(" ", twoWords));
                        }
                        else if (j == (i + 2))
                        {
                            List<string> threeWords = word.Split(new[] {' '}).ToList();
                            string secondWord = threeWords[1];
                            threeWords.Remove(secondWord);
                            threeWords.Add(secondWord);
                            addWords.Add(string.Join(" ", threeWords));

                            secondWord = threeWords[1];
                            threeWords.Remove(secondWord);
                            threeWords.Insert(0, secondWord);
                            addWords.Add(string.Join(" ", threeWords));
                        }
                    }

                    addressParts.Add(
                        new AddressPart(addWords.Where(w => !string.IsNullOrEmpty(w) && !string.IsNullOrWhiteSpace(w))));
                }
            }
            return addressParts;
        }


        static void PushCrutch(List<NameAndType> nameAndTypes)
        {
            const string bPattern = @"((\s|^)б\.|(\s|^)б(\s|$))";
            const string bigPattern = @"(большой|большая|большое)";
            const string sPattern = @"((\s|^)м\.|(\s|^)м(\s|$))";
            const string smallPattern = @"(малый|малая|малое)";
            var b = new[] {"б."};
            var big = new[] {"большой", "большая", "большое"};
            var s = new[] {"м."};
            var small = new[] {"малый", "малая", "малое"};
            foreach (NameAndType nameAndType in nameAndTypes.ToList())
            {
                if (Regex.IsMatch(nameAndType.AddrName, bigPattern))
                    foreach (string word in b)
                        nameAndTypes.AddRange(ReplaceReduct(nameAndType, word, bigPattern));

                if (Regex.IsMatch(nameAndType.AddrName, bPattern))
                    foreach (string word in big)
                        nameAndTypes.AddRange(ReplaceReduct(nameAndType, word, bPattern));

                if (Regex.IsMatch(nameAndType.AddrName, smallPattern))
                    foreach (string word in s)
                        nameAndTypes.AddRange(ReplaceReduct(nameAndType, word, smallPattern));

                if (Regex.IsMatch(nameAndType.AddrName, sPattern))
                    foreach (string word in small)
                        nameAndTypes.AddRange(ReplaceReduct(nameAndType, word, sPattern));
            }
        }
        #endregion


        #region House Logic
        static HouseInfo GetHouseInfo(List<string> names, bool cutItems = false)
        {
            const string housePattern =
                @"(?<h>(^|(^|\s)(дом|д)?\.?)\s?(?<hNum>((([0-9]*[а-зА-З]?|[а-ге-зА-ГЕ-З]?[0-9]*)|([0-9]*[а-ге-зА-ГЕ-З]?|[а-ге-зА-ГЕ-З]?[0-9]*)[\/\-\s]([0-9]*[а-зА-З]?|[а-зА-З]?[0-9]*)?)))($|[\,\s]*)|^)(?<b>($|((корпус|корп|крп|к)\.?\s?[\«\""\']?(?<bNum>(([0-9]*[a-fA-Fа-рА-Р]?|[a-fA-Fа-зА-З]?[0-9]*)?[\/\-]?([0-9]*[a-fA-Fа-рА-Р]?|[a-fA-Fа-зА-З]?[0-9]*)?))[\»\""\']?($|[\,\s]*)|^|)))(?<s>($|(литера|литер|лит|строение|строен|стр|с)\.?\s?[\«\""\']?(?<sNum>(([0-9]*[a-fA-Fа-рА-Р]?|[a-fA-Fа-зА-З]?[0-9]*)?[\/\-]?([0-9]*[a-fA-Fа-рА-Р]?|[a-fA-Fа-зА-З]?[0-9]*)?))[\»\""\']?($|[\,\s]*$)))";
            const string roomPattern =
                @".*(?<r>(?<rName>(помещение|помещ|пом|квартира|кв|офис|оф)\.?)(?<rNum>((\.|\,|\s)(\№|\#)?([\«\""\']?[0-9]*[a-fA-Fа-рА-Р]?|[a-fA-Fа-зА-З]?[0-9]*)?[\/\-]?([0-9]*[a-fA-Fа-рА-Р]?|[a-fA-Fа-зА-З]?[0-9]*)[\»\""\']?){1,}))(\s|$)";

            string room = null;

            if (cutItems)
            {
                var toDelete = new List<string>();
                string finalStr = null;
                if (names.Any(n => Regex.IsMatch(n, roomPattern)))
                {
                    string match = names.First(n => Regex.IsMatch(n, roomPattern));
                    toDelete.Add(match);
                    finalStr = match;

                    for (int i = names.IndexOf(match) + 1; i < names.Count; i++)
                    {
                        string nextItem = names[i];
                        string temp = string.Format("{0},{1}", finalStr, nextItem);

                        if (Regex.IsMatch(temp, roomPattern))
                        {
                            toDelete.Add(nextItem);
                            finalStr = temp;
                        }
                        else
                            break;
                    }
                }

                if (finalStr != null)
                {
                    foreach (string delItem in toDelete)
                        names[names.IndexOf(delItem)] = Regex.Replace(names[names.IndexOf(delItem)], roomPattern, "");

                    string rName = Regex.Match(finalStr, roomPattern).Groups["rName"].Value;
                    string rNum = Regex.Match(finalStr, roomPattern).Groups["rNum"].Value;

                    if (!string.IsNullOrEmpty(rNum) && !string.IsNullOrEmpty(rName))
                    {
                        rName = Regex.Replace(rName.Replace(".", ""), "(помещение|помещ|пом)", "помещ");
                        rName = Regex.Replace(rName, "(квартира|кв)", "кв");
                        rName = Regex.Replace(rName, "(офис|оф)", "оф");
                        rNum = Regex.Replace(rNum.ToUpper().TrimStart(' ').TrimStart('.').TrimStart(' '), @"[\«\»\""\'\№\#]", "");
                        room = string.Format("{0}. {1}", rName, rNum);
                    }
                }
            }

            string hNum =
                names.Where(n => Regex.IsMatch(n, housePattern)).Select(n => Regex.Match(n, housePattern).Groups["hNum"].Value)
                    .FirstOrDefault(v => !string.IsNullOrEmpty(v));
            string bNum =
                names.Where(n => Regex.IsMatch(n, housePattern)).Select(n => Regex.Match(n, housePattern).Groups["bNum"].Value)
                    .FirstOrDefault(v => !string.IsNullOrEmpty(v));
            string sNum =
                names.Where(n => Regex.IsMatch(n, housePattern)).Select(n => Regex.Match(n, housePattern).Groups["sNum"].Value)
                    .FirstOrDefault(v => !string.IsNullOrEmpty(v));

            if (cutItems)
            {
                if (sNum != null)
                    CutItem(names, housePattern, "s");

                if (bNum != null)
                    CutItem(names, housePattern, "b");

                if (hNum != null)
                    CutItem(names, housePattern, "h");
            }

            return new HouseInfo
            {
                HouseNum = hNum != null ? hNum.Replace(" ", "") : null,
                BuildNum = bNum != null ? bNum.Replace(" ", "") : null,
                StructureNum = sNum != null ? sNum.Replace(" ", "") : null,
                Room = room
            };
        }


        static void CutItem(List<string> names, string pattern, string group)
        {
            foreach (string name in names.ToList())
                if (Regex.IsMatch(name, pattern))
                {
                    string match = Regex.Match(name, pattern).Groups[group].Value;
                    if (!string.IsNullOrEmpty(match))
                    {
                        if (name == match)
                            names.Remove(name);
                        else
                            names[names.IndexOf(name)] = name.Replace(match, "").Trim();
                        break;
                    }
                }
        }
        #endregion


        #region type split
        static IEnumerable<NameAndType> SplitNameAndTypes(List<AddressPart> addressParts)
        {
            var nameAndTypes = new List<NameAndType>();
            List<AddrObjectType> types = GetTypes(addressParts);

            foreach (NameAndType nameAndType in SplitNamesBy(addressParts, types, " "))
            {
                NameAndType sameElement = nameAndTypes.FirstOrDefault(n => n.AddrName == nameAndType.AddrName);
                if (sameElement == null || sameElement.Type != nameAndType.Type)
                    nameAndTypes.Add(nameAndType);
            }

            foreach (NameAndType nameAndType in SplitNamesBy(addressParts, types, "."))
            {
                NameAndType sameElement = nameAndTypes.FirstOrDefault(n => n.AddrName == nameAndType.AddrName);
                if (sameElement == null || sameElement.Type != nameAndType.Type)
                    nameAndTypes.Add(nameAndType);
            }

            return nameAndTypes.Where(n => !string.IsNullOrEmpty(n.AddrName)).Distinct();
        }


        static IEnumerable<NameAndType> SplitNamesBy(IEnumerable<AddressPart> addressParts, List<AddrObjectType> types,
            string separator)
        {
            var splitBy = new[] {separator};
            var nameAndTypes = new List<NameAndType>();
            foreach (AddressPart addressPart in addressParts)
                foreach (string name in addressPart.Variations)
                {
                    List<string> words =
                        name.Split(splitBy, StringSplitOptions.RemoveEmptyEntries).Select(w => w.TrimEnd('.')).ToList();

                    List<AddrObjectType> nameTypes =
                        types.Where(t => words.Contains(t.Name) || words.Contains(t.ShortName) || words.Contains(t.EngName))
                            .ToList();
                    if (nameTypes.Any())
                    {
                        foreach (AddrObjectType type in nameTypes)
                        {
                            List<string> notTypeWords = words.Where(
                                w => w != type.Name && w != type.ShortName && w != type.EngName).Select(
                                    w => w.Trim())
                                .ToList();

                            if (notTypeWords.Any())
                                nameAndTypes.Add(new NameAndType(string.Join(separator, notTypeWords), name, type, addressPart));
                        }

                        string pureName =
                            string.Join(separator,
                                words.Where(w => nameTypes.All(ntt => w != ntt.Name && w != ntt.ShortName && w != ntt.EngName)))
                                .Trim();
                        nameAndTypes.AddRange(nameTypes.Select(nt => new NameAndType(pureName, name, nt, addressPart)));
                    }
                    else if (words.Any())
                        nameAndTypes.Add(new NameAndType(string.Join(separator, words.Select(w => w.Trim())),
                            name, addressPart));
                }

            return nameAndTypes.Distinct();
        }


        static List<AddrObjectType> GetTypes(IEnumerable<AddressPart> addressParts)
        {
            var types = new List<AddrObjectType>();
            const string hNumPattern = @"^(дом|д\.?|^)$";
            const string bNumPattern = @"^(корпус|корп\.?|крп.\?|к\.?)$";
            const string sNumPattern = @"^(строение|строен\.?|стр\.?|с\.?)$";

            List<string> allWords =
                addressParts.SelectMany(v => v.Variations).SelectMany(
                    n => n.Split(new[] {".", " "}, StringSplitOptions.RemoveEmptyEntries)
                        .Where(
                            w =>
                                !Regex.IsMatch(w, bNumPattern) && !Regex.IsMatch(w, hNumPattern) && !Regex.IsMatch(w, sNumPattern))
                        .Select(w => w.TrimStart(' ', '.'))).ToList();

            if (!allWords.Any())
                return types;

            return AddressSearcher.FindTypes(allWords);
        }


        static List<NameAndType> ReplaceReduct(NameAndType nameAndType, string replacement, string pattern)
        {
            string startReplacement = replacement + " " + Regex.Replace(nameAndType.AddrName, pattern, "").Trim();
            string endReplacement = Regex.Replace(nameAndType.AddrName, pattern, "").Trim() + " " + replacement;
            var startAppended = new NameAndType(startReplacement, startReplacement, nameAndType.AddressPart);
            var endAppended = new NameAndType(endReplacement, endReplacement, nameAndType.AddressPart);

            return new List<NameAndType> {startAppended, endAppended};
        }
        #endregion


        #region GetAddressByOldId(SqlGuid addrOldId)
        public static IEnumerable<Address> GetAddressByOldId(Guid addrOldId)
        {
            OldAddress oldAddress = GetOldAddress(addrOldId);
            if (oldAddress == null)
                return new List<Address>();

            var names = new List<string>();
            if (!string.IsNullOrEmpty(oldAddress.BuildingNumber))
                names.Add(oldAddress.BuildingNumber);
            if (!string.IsNullOrEmpty(oldAddress.AppartmentNumber))
                names.Add(oldAddress.AppartmentNumber);

            Address address = GetAddressByOldAddress(oldAddress);
            if (address == null)
            {
                if (!names.Any())
                    return new List<Address>();

                string addressStr = string.Join(",", names);
                return ParseAddress(addressStr, false);
            }

            if (names.Any() && (address.AoId.HasValue || address.AddonAoId.HasValue))
            {
                string aoName = AddressStringBuilder.GetAddressStringByGuid((address.AddonAoId ?? address.AoId).Value);
                string addrStr = string.Join(",", names.Union(new[] {aoName}));
                return ParseAddress(addrStr, false);
            }

            HouseInfo houseInfo = GetHouseInfo(names);
            address.Room = houseInfo.Room;

            AddressSearcher.FillAddressHouse(address, houseInfo);
            AddressSearcher.FindAddress(address);

            return new List<Address> {address};
        }


        #region GetOldAddress
        static OldAddress GetOldAddress(Guid id)
        {
            var oldAddresses = new List<OldAddress>();
            using (var connection = new SqlConnection("context connection = true"))
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
                    while (reader.Read())
                    {
                        var oldAddress = new OldAddress();
                        oldAddress.Country = reader.IsDBNull(0) ? (Guid?) null : reader.GetGuid(0);
                        oldAddress.City = reader.IsDBNull(1) ? (Guid?) null : reader.GetGuid(1);
                        oldAddress.Street = reader.IsDBNull(2) ? (Guid?) null : reader.GetGuid(2);
                        oldAddress.CountryRegion = reader.IsDBNull(3) ? (Guid?) null : reader.GetGuid(3);
                        oldAddress.BuildingNumber = reader.IsDBNull(4) ? null : reader.GetString(4);
                        oldAddress.AppartmentNumber = reader.IsDBNull(5) ? null : reader.GetString(5);

                        oldAddresses.Add(oldAddress);
                    }
            }

            return oldAddresses.FirstOrDefault();
        }
        #endregion


        #region GetAddressByOldAddress
        static Address GetAddressByOldAddress(OldAddress oldAddress)
        {
            Address address = null;
            Guid? searchingObject = oldAddress.Street ?? oldAddress.City ?? oldAddress.CountryRegion ?? oldAddress.Country;
            if (searchingObject == null)
                return null;

            using (var connection = new SqlConnection("context connection = true"))
            {
                connection.Open();
                string addrObjQuery = string.Format(@"select  aoc.Id, aoc.ParentId, aoc.OfficialName, aoc.TypeId
                                        from dbo.AddrObjectsCurrent aoc
                                        join dbo.AddrObjectsOldIds aooi on aooi.Id = aoc.Id
                                        where aooi.old_id = '{0}';", searchingObject);
                using (var command = new SqlCommand(addrObjQuery, connection))
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

                using (var command = new SqlCommand(addonObjQuery, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                    if (reader.Read())
                    {
                        var addrObject = new AddrObject(reader, AddrObjectKind.AddonAddrObject);
                        address = new Address(addrObject);
                    }
            }

            return address;
        }
        #endregion


        #endregion


        public static Guid? CreateAddonAddrHouse(Guid parentId, string query)
        {
            List<string> names = GetNames(query);

            if (!names.Any())
                return null;

            HouseInfo houseInfo = GetHouseInfo(names);

            if (string.IsNullOrEmpty(houseInfo.HouseNum))
                return null;

            Guid houseId = Guid.NewGuid();
            string createAddonHouse = string.Format(@"insert dbo.AddonAddrHouses (Id, ParentId, Number, BuildNum, StructureNum)
                                                select '{0}', '{1}', '{2}', {3}, {4}", houseId,
                parentId, houseInfo.HouseNum,
                string.IsNullOrEmpty(houseInfo.BuildNum) ? "null" : string.Format("'{0}'", houseInfo.BuildNum),
                string.IsNullOrEmpty(houseInfo.StructureNum) ? "null" : string.Format("'{0}'", houseInfo.StructureNum));

            using (var connection = new SqlConnection("context connection = true"))
            {
                connection.Open();
                using (var command = new SqlCommand(createAddonHouse, connection))
                    command.ExecuteNonQuery();
            }

            return houseId;
        }
    }
}