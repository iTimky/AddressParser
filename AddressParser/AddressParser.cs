#region usings
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.SqlServer.Server;

#endregion


public class AddressParser
{
    #region ParseAddress(SqlString addr)
    [Microsoft.SqlServer.Server.SqlFunction(FillRowMethodName = "FillRow", DataAccess = DataAccessKind.Read,
        SystemDataAccess = SystemDataAccessKind.Read)]
    public static IEnumerable ParseAddress(string addr, bool? isHeavy = false)
    {
        var addresses = new List<Address>();
        if (string.IsNullOrEmpty(addr))
            return addresses;

        if (!isHeavy.HasValue)
            isHeavy = false;

        string addrStr = Regex.Replace(addr, @"\s*\-\s*", "-").ToLower();

        string postalCode = GetPostalCode(addrStr);
        if (!string.IsNullOrEmpty(postalCode))
            addrStr = addrStr.Replace(postalCode, "");

        List<string> names = GetNames(addrStr);

        HouseInfo houseInfo = GetHouseInfo(names, true);

        List<AddressPart> addressParts = SplitNamesBySpace(names);
        //throw new Exception(houseInfo.ToString());
        //throw new Exception(string.Join(Environment.NewLine, names));
        List<NameAndType> nameAndTypes = SplitNameAndTypes(addressParts).ToList();
        //throw new Exception(string.Join(Environment.NewLine, nameAndTypes));
        PushCrutch(nameAndTypes);

        if (!nameAndTypes.Any())
            return addresses;
        //throw new Exception(string.Join(Environment.NewLine, houseInfo.FoundIn));
        //throw new Exception(houseInfo.ToString());
        //throw new Exception(postalCode);

        var addrObjects = new List<AddrObject>();
        var addrHouses = new List<AddrHouse>();

        if (!addresses.Any())
        {
            addrObjects.AddRange(GetAddrObjects(nameAndTypes, isHeavy.Value));
            //throw new Exception(string.Join(Environment.NewLine, addrObjects));
            addresses =
                addrObjects.Select(
                    ao => new Address() {AoId = ao.Id, HierarchyLevel = ao.HierarchyLevel}).ToList();

            //throw new Exception(string.Join(Environment.NewLine, addresses.Max(a => a.HierarchyLevel)));
            //throw new Exception(string.Join(Environment.NewLine, validAddresses.Count));
            if (addresses.Any())
            {
                addrObjects.AddRange(FillAddrLandMarks(addresses, nameAndTypes));
                addrHouses.AddRange(FillAddrHouses(addresses, houseInfo));
            }
            addrObjects.AddRange(FillAddonAddrObjects(addresses, nameAndTypes, isHeavy.Value));
            addrHouses.AddRange(FillAddonAddrHouses(addresses, houseInfo));
        }

        //throw new Exception(string.Join(Environment.NewLine, possibleAddresess));
        //throw new Exception(string.Join(Environment.NewLine, addrObjects.Where(ad => ad.HierarchyLevel == addrObjects.Max(ao => ao.HierarchyLevel))));

        //throw new Exception(string.Join(Environment.NewLine, addresses.Count));

        addressParts.ForEach(a => a.AddWithoutChildTypes());
        addressParts = addressParts.Where(a => a.Variations.Any() && a != "Россия").ToList();

        ILookup<AddressPart, NameAndType> nameAndTypesByAddrParts =
            nameAndTypes.Where(n => n.AddressPart != "Россия" && n.AddrName != "россия").ToLookup(n => n.AddressPart);

        foreach (Address possibleAddress in addresses)
        {
            AddrObject addrObject;
            Guid? houseId = possibleAddress.AddonHouseId ?? possibleAddress.HouseId;

            if (houseId.HasValue)
                addrObject = addrObjects.First(ao => ao.Id == addrHouses.First(ah => ah.Id == houseId.Value).ParentId);
            else if (possibleAddress.LandMarkId.HasValue)
                addrObject = addrObjects.First(ao => ao.Id == possibleAddress.LandMarkId.Value);
            else
            {
                Guid? aoId = possibleAddress.AddonAoId ?? possibleAddress.AoId;
                if (aoId.HasValue)
                    addrObject = addrObjects.First(ao => ao.Id == aoId.Value);
                else
                    continue;
            }

            List<AddrObject> aObjects = addrObject.PlainParent();

            possibleAddress.IsAllWordsFound = true;

            for (int i = 0; i < addressParts.Count; i++)
            {
                AddressPart addressPart = addressParts[i];
                IEnumerable<NameAndType> nameNtypes = nameAndTypesByAddrParts[addressPart];
                //if (possibleAddress.HouseId == new Guid("1D14C8E3-A1C9-462F-9B6B-713CD643A51D"))
                //    throw new Exception(string.Join(Environment.NewLine, aObjects) + Environment.NewLine + Environment.NewLine + addressPart + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine, nameNtypes));

                if (
                    aObjects.Any(
                        r =>
                            r.Name == addressPart || nameNtypes.Any(n => n.AddrName == r.Name) ||
                            (i != 0 &&
                             (addressParts[i - 1].Variations.Where(v => v.Contains(addressPart.Variations.First())).Any(
                                 v => r.Name == v) || addressParts[i - 1].Variations.First() == addressPart.Variations.First()))))
                    continue;

                //if (possibleAddress.HouseId == new Guid("c1e2c115-1c09-4cb3-a0cd-2270e018b172"))
                    //throw new Exception(string.Join(Environment.NewLine, nameNtypes));
                    //throw new Exception(string.Join(Environment.NewLine, nameAndTypes.Skip(10)));
                //    throw new Exception(string.Join(Environment.NewLine, aObjects) + Environment.NewLine + Environment.NewLine + addressParts[i-1] + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine, nameNtypes));

                possibleAddress.IsAllWordsFound = false;
                break;
            }
        }

        List<Address> possibleAddresses;

        int maxHierarchy = addresses.Any() ? addresses.Max(ad => ad.HierarchyLevel) : 0;
        //throw new Exception(maxHierarchy.ToString());

        if (addresses.Count(a => a.IsAllWordsFound == true && a.HierarchyLevel == maxHierarchy) == 1)
            possibleAddresses = addresses.Where(a => a.IsAllWordsFound == true && a.HierarchyLevel == maxHierarchy).ToList();
        else
            possibleAddresses = addresses.Where(a => a.IsAllWordsFound == true || a.HierarchyLevel == maxHierarchy).ToList();

        //throw new Exception(string.Join(Environment.NewLine, addresses));
        //throw new Exception(string.Join(Environment.NewLine, possibleAddresses));

        if (possibleAddresses.Count != 1 && possibleAddresses.Count(pa => pa.IsAllWordsFound == true) != 1)
        {
            //throw new Exception(string.Join(Environment.NewLine, possibleAddresess));
            if (isHeavy.Value && possibleAddresses.Count > 1)
                return ParseAddress(addr, false);

            return Enumerable.Empty<Address>();
        }

        //throw new Exception(string.Join(Environment.NewLine, possibleAddresess));

        Address mainAddress = possibleAddresses.Count == 1
            ? possibleAddresses.First()
            : possibleAddresses.First(pa => pa.IsAllWordsFound == true);
        mainAddress.Room = houseInfo.Room;

        FindAddress(mainAddress);

        return new List<Address>() {mainAddress};
    }


    static List<string> GetNames(string addrStr)
    {
        List<string> names =
            addrStr.Split(new[] {",", ";", ":"}, StringSplitOptions.RemoveEmptyEntries).Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.ToLower().Replace("пр-т", "пр-кт").Replace("\"", "").Replace("'", "''"))
                .Select(x => Regex.Replace(x, @"(^|\s)пр\-д(\s|$)", " проезд "))
                .Select(x => Regex.Replace(x, @"(^|\s)пр\.?(\s|$)", " пр-кт "))
                .Select(x => Regex.Replace(x, "-ый", "-й"))
                .Select(x => Regex.Replace(x, "-ая", "-я"))
                .Select(x => Regex.Replace(x, "-ой", "-й"))
                .Select(x => Regex.Replace(x, "-ий", "-й"))
                .Select(s => Regex.Replace(s, @"\s+", " "))
                .Select(s => s.Trim())
                //.Select(s => Regex.Replace(s, @"\.\s*", ". "))
                //.Select(x => Regex.Replace(x, @"(^|\s|,)россия($\s|,)", ""))
                .Where(x => !string.IsNullOrEmpty(x)).ToList();

        return names;
    }


    static List<AddressPart> SplitNamesBySpace(IEnumerable<string> names, bool isSpelling = false)
    {
        List<AddressPart> addressParts = new List<AddressPart>();

        ILookup<string, string> spellingRules = null;

        if (isSpelling)
            spellingRules = GetSpellingRules();

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
                        List<string> threeWords = word.Split(new[] { ' ' }).ToList();
                        var secondWord = threeWords[1];
                        threeWords.Remove(secondWord);
                        threeWords.Add(secondWord);
                        addWords.Add(string.Join(" ", threeWords));

                        secondWord = threeWords[1];
                        threeWords.Remove(secondWord);
                        threeWords.Insert(0, secondWord);
                        addWords.Add(string.Join(" ", threeWords));
                    }
                }

                //List<string[]> dotWordsList =
                //    wordsToAdd.Where(w => w.Contains(".")).Select(
                //        w =>
                //            w.Split(new[] {'.'}).Where(d => !string.IsNullOrEmpty(d) && !string.IsNullOrWhiteSpace(d)).Select(
                //                s => s + ".").ToArray()).ToList();

                //foreach (string[] dotWords in dotWordsList)
                //{

                //    for (int j = 0; j < dotWords.Length; j++)
                //    {
                //        var dotWord = dotWords[j];
                //        wordsToAdd.Add(dotWord);

                //        for (int k = j + 1; k < dotWords.Length; k++)
                //        {
                //            dotWord += " " + dotWords[k];
                //            wordsToAdd.Add(dotWord);
                //        }
                //    }

                //    wordsToAdd.AddRange(dotWords);
                //}

                if (isSpelling)
                    foreach (string addWord in addWords.ToList())
                        addWords.AddRange(GetSpellingCorrects(addWord, spellingRules));

                addressParts.Add(new AddressPart(addWords.Where(w => !string.IsNullOrEmpty(w) && !string.IsNullOrWhiteSpace(w))));
            }
        }
        //throw new Exception(string.Join(Environment.NewLine, addressParts));
        return addressParts;
    }


    static ILookup<string, string> GetSpellingRules()
    {
        return new List<KeyValuePair<string, string>>()
        {
            new KeyValuePair<string, string>("а", "о"),
            new KeyValuePair<string, string>("о", "а"),
            new KeyValuePair<string, string>("тся", "ться"),
            new KeyValuePair<string, string>("ться", "тся"),
            new KeyValuePair<string, string>("с", "сс"),
            new KeyValuePair<string, string>("сс", "с"),
            new KeyValuePair<string, string>("у", "ю"),
            new KeyValuePair<string, string>("ю", "у"),
            new KeyValuePair<string, string>("и", "е"),
            new KeyValuePair<string, string>("е", "и"),
            new KeyValuePair<string, string>("м", "мм"),
            new KeyValuePair<string, string>("мм", "м"),
            new KeyValuePair<string, string>("ф", "фф"),
            new KeyValuePair<string, string>("фф", "ф"),
            new KeyValuePair<string, string>("с", "з"),
            new KeyValuePair<string, string>("з", "с"),
            new KeyValuePair<string, string>("ч", "д"),
            new KeyValuePair<string, string>("д", "ч"),
            new KeyValuePair<string, string>("г", "хг"),
            new KeyValuePair<string, string>("хг", "г"),
        }.ToLookup(kvp => kvp.Key, kvp => kvp.Value);
    }


    static IEnumerable<string> GetSpellingCorrects(string word, ILookup<string, string> spellingRules)
    {
        var corrections = new List<string>();

        return corrections;
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


    static List<NameAndType> ReplaceReduct(NameAndType nameAndType, string replacement, string pattern)
    {
        string startReplacement = replacement + " " + Regex.Replace(nameAndType.AddrName, pattern, "").Trim();
        string endReplacement = Regex.Replace(nameAndType.AddrName, pattern, "").Trim() + " " + replacement;
        var startAppended = new NameAndType(startReplacement, startReplacement, nameAndType.AddressPart);
        var endAppended = new NameAndType(endReplacement, endReplacement, nameAndType.AddressPart);

        return new List<NameAndType>() {startAppended, endAppended};
    }


    static string GetPostalCode(string addr)
    {
        const string postalCodePattern = @"(?<pCode>[0-9]{5,6})";
        return Regex.Match(addr, postalCodePattern).Groups["pCode"].Value;
    }


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

        //foreach (NameAndType nameAndType in nameAndTypes.Where(n => n.Type != null).ToList())
        //{
        //    List<string> words = nameAndType.AddrName.Split(new[] { ' ' }).ToList();

        //    if (words.Count == 2)
        //    {
        //        words.Reverse();
        //        var reversedNameAndType = new NameAndType(string.Join(" ", words), nameAndType.OriginAddrName, nameAndType.Type,
        //            nameAndType.AddressPart);
        //        nameAndType.AddressPart.Childs.Add(reversedNameAndType);
        //        nameAndTypes.Add(reversedNameAndType);
        //    }
        //}
        //throw new Exception(string.Join(Environment.NewLine, nameAndTypes));
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
                //var currentNameAndTypes = new List<NameAndType>();
                List<string> words =
                    name.Split(splitBy, StringSplitOptions.RemoveEmptyEntries).Select(w => w.TrimEnd('.')).ToList();

                List<AddrObjectType> nameTypes =
                    types.Where(t => words.Contains(t.Name) || words.Contains(t.ShortName) || words.Contains(t.EngName)).ToList();
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
                    //nameAndTypes.AddRange(currentNameAndTypes);
                }
                else if (words.Any())
                    nameAndTypes.Add(new NameAndType(string.Join(separator, words.Select(w => w.Trim())),
                        name, addressPart));
            }

        //throw new Exception(string.Join(Environment.NewLine, nameAndTypes.Distinct()));
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
                    .Where(w => !Regex.IsMatch(w, bNumPattern) && !Regex.IsMatch(w, hNumPattern) && !Regex.IsMatch(w, sNumPattern))
                    .Select(w => w.TrimStart(' ', '.'))).ToList();

        if (!allWords.Any())
            return types;

        //if (allWords.Any(w => w == "пр") && allWords.All(w => w != "пр-кт"))
        //    allWords.Add("пр-кт");

        string select = string.Format(
            @"select distinct aot.Id,
                            lower(aot.Name),
                            lower(aot.ShortName),
                            lower(aot.EngName)
                            from dbo.AddrObjectTypes aot
                            join ({0}) t on t.String = aot.Name or t.String = aot.ShortName or t.String = EngName",
            string.Join(" union ", allWords.Select(n => string.Format("select '{0}' String", n))));
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

        //throw new Exception(string.Join(Environment.NewLine + Environment.NewLine, types.Select(t => string.Format("{0}; {1}; {2}; {3};", t.Id, t.Name, t.ShortName, t.EngName))));

        return types;
    }
    #endregion


    #region AddrObjects
    static IEnumerable<AddrObject> GetAddrObjects(List<NameAndType> nameAndTypes, bool isHeavy)
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

        //NameAndType explicitCity = nameAndTypes.FirstOrDefault(n => n.Type != null && n.Type.EngName == "city");
        //if (explicitCity != null && !IsRussianCity(explicitCity))
        //throw new Exception(explicitCity.AddrName);
        //    return addrObjects;

        string query;

        if (isHeavy)
            query = GetHeavyAddrObjectSearchQuery(nameAndTypes);
        else
            query = string.Format(@"
                        select t.Id, t.ParentId, t.Name, al.EngName from
                        (
                            select  aoc.Id, aoc.ParentId, i.String as Name, aoc.[Level]
                            from ({0}) i    
                            join dbo.AddrObjectsCurrent aoc on aoc.FormalName = i.String and i.Id = 0
                            union
                            select  aoc.Id, aoc.ParentId, i.String as Name, aoc.[Level]
                            from ({0}) i    
                            join dbo.AddrObjectsCurrent aoc on aoc.OfficialName = i.String and i.Id = 0
                            union
                            select  aoc.Id, aoc.ParentId, i.String as Name, aoc.[Level]
                            from ({0}) i    
                            join dbo.AddrObjectsCurrent aoc on aoc.FormalName = i.String and i.Id = aoc.TypeId
                            union
                            select  aoc.Id, aoc.ParentId, i.String as Name, aoc.[Level]
                            from ({0}) i    
                            join dbo.AddrObjectsCurrent aoc on aoc.OfficialName = i.String and i.Id = aoc.TypeId
                        ) t
                        join dbo.AddrLevels al on al.Id = t.Level;",
                GetNameAndTypeSelect(nameAndTypes)
                );

        //throw new Exception(query);

        List<AddrObject> objects = SelectAddrObjects(query);
        List<AddrObject> addrObjects = new List<AddrObject>(objects.Count);

        //throw new Exception(query);
        //throw new Exception(query.Substring(1000));

        foreach (string level in levels)
        {
            List<AddrObject> levelObjects = objects.Where(o => o.Level == level).ToList();
            //throw new Exception(string.Join(Environment.NewLine, levelObjects));
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

        //throw new Exception(addrObjects.Count.ToString());

        if (addrObjects.Count < 100)
            return addrObjects;

        foreach (AddrObject addrObject in addrObjects.ToList())
            if (addrObject.Parent == null && !addrObject.Childs.Any())
                addrObjects.Remove(addrObject);

        IEnumerable<IGrouping<Guid, AddrObject>> addrObjectsById = addrObjects.GroupBy(a => a.Id);

        List<AddrObject> result = new List<AddrObject>();

        foreach (IGrouping<Guid, AddrObject> addrObject in addrObjectsById)
        {
            int maxLength = addrObject.Max(a => a.Name.Length);
            result.Add(addrObject.First(a => a.Name.Length == maxLength));
        }
        //throw new Exception(string.Join(Environment.NewLine, addrObjects));
        return result;
    }


    static string GetHeavyAddrObjectSearchQuery(List<NameAndType> nameAndTypes)
    {
        var stringBuilder = new StringBuilder("select t.Id, t.ParentId, t.Name, al.EngName from(" + Environment.NewLine);
        var nameTypes = nameAndTypes.Select(n => new {Id = n.Type == null ? 0 : n.Type.Id, Name = n.AddrName})
            .Union(nameAndTypes.Select(n => new {Id = 0, Name = n.OriginAddrName})).Where(n => !string.IsNullOrEmpty(n.Name));

        foreach (var nameType in nameTypes)
        {
            string subQuery = string.Format(@" select aoc.Id, aoc.ParentId, '{0}' as Name, aoc.[Level]
    from dbo.AddrObjectsCurrent aoc
    where contains ((FormalName, OfficialName), '""{1}""') and (FormalName like '%{0}%' or OfficialName like '%{0}%'){2} union",
                nameType.Name.Trim(),
                nameType.Name.Trim(),
                nameType.Id == 0 ? "" : string.Format(" and aoc.TypeId = {0}", nameType.Id));
            stringBuilder.Append(subQuery);
        }

        stringBuilder.Remove(stringBuilder.Length - "union".Length, "union".Length);
        stringBuilder.Append(@") t join dbo.AddrLevels al on al.Id = t.Level");

        string query = stringBuilder.ToString();

        return query;
    }


    static bool IsRussianCity(NameAndType nameAndType)
    {
        string query =
            string.Format(
                @"select 1 from dbo.AddrObjectsCurrent aoc where (aoc.FormalName = '{0}' or aoc.OfficialName = '{0}') and TypeId = {1}",
                nameAndType.AddrName, nameAndType.Type.Id);
        //throw new Exception(query);
        using (var connection = new SqlConnection("context connection = true"))
        {
            connection.Open();
            using (var command = new SqlCommand(query, connection))
            using (SqlDataReader reader = command.ExecuteReader())
                while (reader.Read())
                    return true;
        }

        return false;
    }
    #endregion


    #region AddrLandMarks
    static IEnumerable<AddrObject> FillAddrLandMarks(List<Address> addresses, IEnumerable<NameAndType> names)
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
                    var addrObject = new AddrObject(id: reader.GetGuid(0),
                        parentId: reader.GetGuid(1),
                        name: reader.GetString(2),
                        level: null);

                    addrObjects.Add(addrObject);
                }
        }

        foreach (AddrObject addrObject in addrObjects)
        {
            Address address = addresses.First(ad => ad.AoId == addrObject.ParentId);
            addrObject.HierarchyLevel = address.HierarchyLevel + 1;
            address.LandMarkId = addrObject.Id;
            address.AoId = null;
            address.HierarchyLevel++;
        }

        return addrObjects;
    }
    #endregion


    #region AddrHouses
    static IEnumerable<AddrHouse> FillAddrHouses(List<Address> addresses, HouseInfo houseInfo)
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
            address.HouseId = addrHouse.First().Id;
            address.AoId = null;
            address.HierarchyLevel++;
        }

        return addrHouses;
    }
    #endregion


    #region AddonAddrObjects
    static IEnumerable<AddrObject> FillAddonAddrObjects(List<Address> addresses, List<NameAndType> names, bool isHeavy)
    {
        IEnumerable<AddrObject> addrObjects = GetAddonAddrObjects(names, isHeavy).ToList();
        IEnumerable<Guid> addonAddrObjectIds =
            addrObjects.Where(ao => ao.HierarchyLevel == addrObjects.Max(tao => tao.HierarchyLevel)).Select(a => a.Id).ToList();
        if (addresses.Any(a => a.AoId != null))
            FillAddonAddrObjectsByAddrObjects(addonAddrObjectIds, addresses.Where(a => a.AoId != null).ToList());

        if (addresses.All(a => a.AddonAoId == null))
            addresses.AddRange(addonAddrObjectIds.Select(addonAoId => new Address() {AddonAoId = addonAoId}));

        return addrObjects;
    }


    static void FillAddonAddrObjectsByAddrObjects(IEnumerable<Guid> addonObjIds, List<Address> addresses)
    {
        using (var connection = new SqlConnection("context connection = true"))
        {
            connection.Open();
            foreach (Guid addonObjId in addonObjIds)
                foreach (Address address in addresses)
                {
                    string query = string.Format(@"if exists (select 1 from dbo.AddonAddrObjects aao
                                                            join dbo.AddrObjectsOldIds aooi on aooi.old_id = aao.old_id
                                                            where aao.Id = '{0}' and aooi.Id = '{1}')
                                                        select cast (1 as bit)
                                                    else
                                                        select cast (0 as bit)", addonObjId, address.AoId);
                    using (var command = new SqlCommand(query, connection))
                    {
                        var exists = (bool) command.ExecuteScalar();
                        if (exists)
                        {
                            address.AoId = null;
                            address.AddonAoId = addonObjId;
                            address.HierarchyLevel++;
                        }
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
                        select aao.Id, aao.ParentId, i.String
                        from ({2}) i 
                        join dbo.AddonAddrObjects aao on (aao.Name {1} or aao.EnglishName {1}) and (i.Id = 0 or aao.TypeId = i.Id)
                        join dbo.AddrLevels al on al.Id = aao.Level
                        where al.EngName = '{0}';", level, isHeavy ? "like '%' + i.String + '%'" : "= i.String",
                GetNameAndTypeSelect(nameAndTypes));

            //throw new Exception(query);
            List<AddrObject> levelObjects = SelectAddrObjects(query);
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

        return addrObjects.Distinct();
    }
    #endregion


    #region AddonAddrHouses
    static IEnumerable<AddrHouse> FillAddonAddrHouses(IEnumerable<Address> addresses, HouseInfo houseInfo)
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

        //string allQuers = null;
        foreach (Address address in addresses)
        {
            string query;
            if (address.AddonAoId != null)
                query = string.Format(queryTemplate, address.AddonAoId);
            else if (address.AoId != null)
                query = string.Format(queryTemplate, address.AoId);
            else
                continue;
            //allQuers += Environment.NewLine + Environment.NewLine + query;

            using (var connection = new SqlConnection("context connection = true"))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                    if (reader.Read())
                    {
                        var addrHouse = new AddrHouse(reader.GetGuid(0), reader.GetGuid(1));
                        addrHouses.Add(addrHouse);

                        address.AddonHouseId = addrHouse.Id;
                        address.AoId = null;
                        address.AddonAoId = null;
                        address.HierarchyLevel++;
                    }
            }
        }

        return addrHouses;

        //throw new Exception(allQuers);
    }
    #endregion


    static List<AddrObject> SelectAddrObjects(string query)
    {
        var result = new List<AddrObject>();

        using (var connection = new SqlConnection("context connection = true"))
        {
            connection.Open();
            using (var command = new SqlCommand(query, connection))
            using (SqlDataReader reader = command.ExecuteReader())
                while (reader.Read())
                    result.Add(new AddrObject(id: reader.GetGuid(0),
                        parentId: reader.IsDBNull(1) ? (Guid?) null : reader.GetGuid(1),
                        name: reader.GetString(2),
                        level: reader.FieldCount == 3 ? null : reader.GetString(3)));
        }

        return result;
    }


    static string GetNameAndTypeSelect(List<NameAndType> nameAndTypes)
    {
        var nameTypes = nameAndTypes.Select(n => new {Id = n.Type == null ? 0 : n.Type.Id, Name = n.AddrName})
            .Union(nameAndTypes.Select(n => new {Id = 0, Name = n.OriginAddrName})).Where(n => !string.IsNullOrEmpty(n.Name));

        return string.Join(" union ", nameTypes.Select(n => string.Format("select {0} Id, '{1}' String", n.Id, n.Name)));
    }


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

                //names.RemoveAll(toDelete.Contains);

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

        //throw new Exception(string.Join(Environment.NewLine, names));

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

        return new HouseInfo()
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


    #region objects
    class AddrObjectType : IEquatable<AddrObjectType>
    {
        public readonly byte Id;
        public readonly string Name;
        public readonly string ShortName;
        public readonly string EngName;


        public AddrObjectType(byte id, string name, string shortName, string engName)
        {
            Id = id;
            Name = name;
            ShortName = shortName;
            EngName = engName;
        }


        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }


        public bool Equals(AddrObjectType other)
        {
            return Id == other.Id;
        }


        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}, {3}", Id, Name, ShortName, EngName);
        }
    }



    class AddrObject : IEquatable<AddrObject>
    {
        public readonly Guid Id;
        public readonly Guid? ParentId;
        public readonly List<AddrObject> Childs = new List<AddrObject>();
        public AddrObject Parent;
        public readonly string Name;
        public readonly string Level;
        public int HierarchyLevel;


        public AddrObject(Guid id, Guid? parentId, string name, string level)
        {
            Id = id;
            ParentId = parentId;
            Name = name;
            Level = level;
        }


        public List<AddrObject> PlainParent()
        {
            var result = new List<AddrObject>();
            result.Add(this);
            AddrObject currentParent = Parent;

            while (currentParent != null)
            {
                result.Add(currentParent);
                currentParent = currentParent.Parent;
            }

            return result;
        }


        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ (ParentId.HasValue ? ParentId.GetHashCode() : 0);
        }


        public bool Equals(AddrObject other)
        {
            return Id == other.Id && ParentId == other.ParentId;
        }


        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}, {3}, {4}", Id, ParentId, Name, Level, HierarchyLevel);
        }
    }



    class AddrHouse : IEquatable<AddrHouse>
    {
        public readonly Guid Id;
        public readonly Guid ParentId;


        public AddrHouse(Guid id, Guid parentId)
        {
            Id = id;
            ParentId = parentId;
            //Parent = parent;
        }


        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ ParentId.GetHashCode();
        }


        public bool Equals(AddrHouse other)
        {
            return Id == other.Id && ParentId == other.ParentId;
        }
    }



    class HouseInfo
    {
        public string HouseNum;
        public string BuildNum;
        public string StructureNum;
        public string Room;


        public override string ToString()
        {
            return string.Format("House: {0}, Build: {1}, Struct: {2}, Room: {3}", HouseNum, BuildNum, StructureNum, Room);
        }
    }



    class AddressPart : IEquatable<AddressPart>
    {
        readonly List<string> _variations;
        public readonly List<NameAndType> Childs = new List<NameAndType>();

        public IEnumerable<string> Variations { get { return _variations; } }


        public AddressPart(IEnumerable<string> addrs)
        {
            _variations = addrs.Distinct().ToList();
        }


        public void AddWithoutChildTypes()
        {
            foreach (NameAndType nameAndType in Childs.Where(c => c.Type != null))
            {
                var toAdd = new List<string>();
                List<string> sameNames = _variations.Where(v => v == nameAndType.OriginAddrName).ToList();
                AddrObjectType type = nameAndType.Type;
                var trimChars = new[] {' ', '.'};
                var spaceChars = new[] {' '};

                toAdd.AddRange(
                    sameNames.Select(
                        v =>
                            Regex.Replace(v, string.Format(@"(^|\s){0}(\.|\s|$)", type.Name), " ").Trim(trimChars)));
                toAdd.AddRange(
                    sameNames.Select(
                        v =>
                            Regex.Replace(v, string.Format(@"(^|\s){0}(\.|\s|$)", type.ShortName), " ").Trim(trimChars)));
                toAdd.AddRange(
                    sameNames.Select(
                        v =>
                            Regex.Replace(v, string.Format(@"(^|\s){0}(\.|\s|$)", type.EngName), " ").Trim(trimChars)));

                List<string> distinctVariations =
                    _variations.Union(toAdd).Union(Childs.Select(c => c.AddrName))
                        .Select(s => Regex.Replace(s, @"\s+", " "))
                        //.Select(s => Regex.Replace(s, @"\.\s*", ". "))
                        .Select(s => s.Trim(trimChars))
                        .Where(s => !string.IsNullOrEmpty(s) && !string.IsNullOrWhiteSpace(s))
                        .Where(s => s != type.Name && s != type.ShortName && s != type.EngName)
                        .OrderBy(s => s.Split(spaceChars).Length).ToList();
                _variations.Clear();
                _variations.AddRange(distinctVariations);
            }
        }


        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 19;
                foreach (string variation in _variations)
                    hash = hash*31 + variation.GetHashCode();
                return hash;
            }
        }


        public bool Equals(AddressPart other)
        {
            return _variations.Count == other._variations.Count && _variations.All(v => other.Variations.Contains(v));
        }


        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AddressPart) obj);
        }


        public override string ToString()
        {
            return string.Join(";", _variations);
        }


        #region Operators
        public static bool operator ==(AddressPart part, string addr)
        {
            if (ReferenceEquals(part, null) && ReferenceEquals(addr, null))
                return true;

            if (ReferenceEquals(part, null) || ReferenceEquals(addr, null))
                return false;

            return part._variations.Any(v => string.Compare(v, addr, StringComparison.OrdinalIgnoreCase) == 0);
        }


        public static bool operator !=(AddressPart part, string addr)
        {
            return !(part == addr);
        }


        public static bool operator ==(string addr, AddressPart part)
        {
            return part == addr;
        }


        public static bool operator !=(string addr, AddressPart part)
        {
            return !(part == addr);
        }
        #endregion
    }



    class NameAndType : IEquatable<NameAndType>
    {
        public readonly AddressPart AddressPart;
        public readonly AddrObjectType Type;
        public readonly string AddrName;
        public readonly string OriginAddrName;


        public NameAndType(string addrName, string originAddrName, AddressPart addressPart)
        {
            AddrName = addrName;
            OriginAddrName = originAddrName;
            AddressPart = addressPart;
            addressPart.Childs.Add(this);
        }


        public NameAndType(string addrName, string originAddrName, AddrObjectType type, AddressPart addressPart)
            : this(addrName, originAddrName, addressPart)
        {
            Type = type;
        }


        public override int GetHashCode()
        {
            return AddrName.GetHashCode() ^ OriginAddrName.GetHashCode() ^ (Type != null ? Type.GetHashCode() : 0);
        }


        public bool Equals(NameAndType other)
        {
            return AddrName == other.AddrName && OriginAddrName == other.OriginAddrName && Type == other.Type;
        }


        public override string ToString()
        {
            return string.Format("AddrName: {0}, Origin: {1}, Type: {2}, AddressPart: {3}", AddrName, OriginAddrName,
                Type == null ? "null" : Type.ToString(), AddressPart == null ? "null" : AddressPart.ToString());
        }
    }
    #endregion


    #endregion


    #region GetAddressByOldId(SqlGuid addrOldId)
    [Microsoft.SqlServer.Server.SqlFunction(FillRowMethodName = "FillRow", DataAccess = DataAccessKind.Read)]
    public static IEnumerable GetAddressByOldId(Guid? addrOldId)
    {
        if (!addrOldId.HasValue)
            return new List<Address>();

        OldAddress oldAddress = GetOldAddress(addrOldId.Value);
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
            return ParseAddress(addressStr);
        }

        if (names.Any() && (address.AoId.HasValue || address.AddonAoId.HasValue))
        {
            string aoName = GetAddressStringByGuid(address.AddonAoId ?? address.AoId);
            string addrStr = string.Join(",", names.Union(new[] {aoName}));
            return ParseAddress(addrStr);
        }

        HouseInfo houseInfo = GetHouseInfo(names);
        address.Room = houseInfo.Room;

        FillAddressHouse(address, houseInfo);
        FindAddress(address);

        return new List<Address>() {address};
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
            string addrObjQuery = string.Format(@"select  aooi.Id
                                        from dbo.AddrObjectsOldIds aooi
                                        where aooi.old_id = '{0}';", searchingObject);
            using (var command = new SqlCommand(addrObjQuery, connection))
            using (SqlDataReader reader = command.ExecuteReader())
                while (reader.Read())
                    address = new Address() {AoId = reader.GetGuid(0)};

            if (address != null)
                return address;

            string addonObjQuery = string.Format(@"select  aao.Id
                                        from dbo.AddonAddrObjects aao
                                        where aao.old_id = '{0}';", searchingObject);

            using (var command = new SqlCommand(addonObjQuery, connection))
            using (SqlDataReader reader = command.ExecuteReader())
                while (reader.Read())
                    address = new Address() {AddonAoId = reader.GetGuid(0)};
        }

        return address;
    }
    #endregion


    #region FillAddressHouse
    static void FillAddressHouse(Address address, HouseInfo houseInfo)
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

            string query = string.Format(@"select house.Id
                            from dbo.AddonAddrHouses house                                     
                            where house.ParentId = '{0}'
                            and ({1}) and {2} and {3};", address.AddonAoId ?? address.AoId, hNumPred, bNumPred, sNumPred);

            using (var command = new SqlCommand(query, connection))
            using (SqlDataReader reader = command.ExecuteReader())
                while (reader.Read())
                {
                    address.AddonHouseId = reader.GetGuid(0);
                    address.AddonAoId = null;
                    address.AoId = null;
                }

            if (address.AddonHouseId == null && address.AoId != null)
            {
                hNumPred = string.IsNullOrEmpty(houseInfo.HouseNum)
                    ? "house.HouseNum is null and house.NumberEffective is null"
                    : string.Format(
                        "replace(house.HouseNum, ' ', '') = '{0}' or replace(house.NumberEffective, ' ', '') = '{0}'",
                        houseInfo.HouseNum);

                query = string.Format(@"select house.Id
                            from dbo.AddrHousesCurrent house                                     
                            where house.ParentAoId = '{0}'
                            and ({1}) and {2} and {3};", address.AoId, hNumPred, bNumPred, sNumPred);

                using (var command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                    while (reader.Read())
                    {
                        address.HouseId = reader.GetGuid(0);
                        address.AoId = null;
                    }
            }
        }
    }
    #endregion


    #endregion


    #region GetAddressStringByOldId
    [Microsoft.SqlServer.Server.SqlFunction(DataAccess = DataAccessKind.Read)]
    public static string GetAddressStringByOldId(Guid? oldId)
    {
        if (!oldId.HasValue)
            return null;

        OldAddress oldAddress = GetOldAddressObject(oldId.Value);
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
                    oldAddress.Country = reader.IsDBNull(0) ? (Guid?) null : reader.GetGuid(0);
                    oldAddress.CountryRegion = reader.IsDBNull(1) ? (Guid?) null : reader.GetGuid(1);
                    oldAddress.City = reader.IsDBNull(2) ? (Guid?) null : reader.GetGuid(2);
                    oldAddress.MetroStation = reader.IsDBNull(3) ? (Guid?) null : reader.GetGuid(3);
                    oldAddress.Street = reader.IsDBNull(4) ? (Guid?) null : reader.GetGuid(4);
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
                    geographicalObjects.Add(new GeographicalObject()
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
                        geographicalObjects.Add(new GeographicalObject()
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
    [Microsoft.SqlServer.Server.SqlFunction(DataAccess = DataAccessKind.Read)]
    public static string GetAddressStringByGuid(Guid? someId)
    {
        if (!someId.HasValue)
            return null;

        var finalHierarchy = new List<AddressHierarchyObject>();
        List<AddressHierarchyObject> addons = GetHierarchyFromAddons(someId.Value);
        if (addons.Any())
        {
            finalHierarchy.AddRange(addons);
            int maxLevel = addons.Max(ad => ad.HierarchyLevel);
            Guid? parentId = addons.First(ad => ad.HierarchyLevel == maxLevel).ParentId;
            if (parentId.HasValue)
                finalHierarchy.AddRange(GetHierarchyFromCurrent(parentId.Value, maxLevel + 1));
        }
        else
            finalHierarchy.AddRange(GetHierarchyFromCurrent(someId.Value, 0));

        string addressString = string.Join(", ", finalHierarchy.OrderByDescending(fh => fh.HierarchyLevel).Select(fh => fh.Name));
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
                    houseObject = new AddressHierarchyObject()
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

            using (var command = new SqlCommand(objectQuery, connection))
            using (SqlDataReader reader = command.ExecuteReader())
                while (reader.Read())
                {
                    var hierarchyObject = new AddressHierarchyObject()
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
                    houseOrLandMark = new AddressHierarchyObject()
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
                using (var command = new SqlCommand(landMarkQuery, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                    while (reader.Read())
                    {
                        houseOrLandMark = new AddressHierarchyObject()
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

            using (var command = new SqlCommand(objectQuery, connection))
            using (SqlDataReader reader = command.ExecuteReader())
                while (reader.Read())
                {
                    var hierarchyObject = new AddressHierarchyObject()
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

        return hierarchyObjects;
    }



    class AddressHierarchyObject
    {
        public Guid Id;
        public Guid? ParentId;
        public string Name;
        public string TypeShortName;
        public int HierarchyLevel;
    }
    #endregion


    #region GetAddressStringById
    [Microsoft.SqlServer.Server.SqlFunction(DataAccess = DataAccessKind.Read)]
    public static string GetAddressStringById(int? id)
    {
        if (!id.HasValue)
            return null;

        Address address = GetAddressById(id.Value);
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
            string.Format(@"select AoId, LandMarkId, HouseId, AddonAoId, AddonHouseId, Room from dbo.Addresses where Id = {0}", id);
        using (var connection = new SqlConnection("context connection = true"))
        {
            connection.Open();
            using (var command = new SqlCommand(addressQuery, connection))
            using (SqlDataReader reader = command.ExecuteReader())
                while (reader.Read())
                    return new Address()
                    {
                        AoId = reader.IsDBNull(0) ? (Guid?) null : reader.GetGuid(0),
                        LandMarkId = reader.IsDBNull(1) ? (Guid?) null : reader.GetGuid(1),
                        HouseId = reader.IsDBNull(2) ? (Guid?) null : reader.GetGuid(2),
                        AddonAoId = reader.IsDBNull(3) ? (Guid?) null : reader.GetGuid(3),
                        AddonHouseId = reader.IsDBNull(4) ? (Guid?) null : reader.GetGuid(4),
                        Room = reader.IsDBNull(5) ? null : reader.GetString(5)
                    };
        }

        return null;
    }
    #endregion


    #region CreateAddonAddrHouse
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void CreateAddonAddrHouse(Guid? parentId, string addr, out Guid? addonAddrHouseId)
    {
        addonAddrHouseId = null;
        if (!parentId.HasValue || string.IsNullOrEmpty(addr))
            return;

        string addrStr = Regex.Replace(addr, @"\s*\-\s*", "-").ToLower();

        string postalCode = GetPostalCode(addrStr);
        if (!string.IsNullOrEmpty(postalCode))
            addrStr = addrStr.Replace(postalCode, "");

        List<string> names =
            addrStr.Split(new[] {",", ";", ":"}, StringSplitOptions.RemoveEmptyEntries).Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.ToLower().Trim().Replace("пр-т", "пр-кт").Replace("\"", ""))
                .Select(x => Regex.Replace(x, @"\sпр\.?(\s|$)", " пр-кт"))
                .Where(x => !string.IsNullOrEmpty(x)).ToList();

        if (!names.Any())
            return;

        HouseInfo houseInfo = GetHouseInfo(names);

        if (string.IsNullOrEmpty(houseInfo.HouseNum))
            return;

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

        addonAddrHouseId = houseId;
    }
    #endregion


    #region common
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


    #region FindAddress
    static void FindAddress(Address address)
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
                while (reader.Read())
                {
                    address.AddressId = reader.GetInt32(0);
                    address.HierarchyLevel++;
                    address.AoId = null;
                    address.LandMarkId = null;
                    address.HouseId = null;
                    address.AddonAoId = null;
                    address.AddonHouseId = null;
                    address.Room = null;
                }
        }
    }
    #endregion


    public class Address
    {
        public int? AddressId;
        public Guid? AoId;
        public Guid? LandMarkId;
        public Guid? HouseId;
        public Guid? AddonAoId;
        public Guid? AddonHouseId;
        public string Room;
        public bool? IsAllWordsFound;
        public int HierarchyLevel;


        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}", AddressId, AoId, LandMarkId, HouseId, AddonAoId,
                AddonHouseId, HierarchyLevel, IsAllWordsFound);
        }
    }



    class OldAddress
    {
        //public Guid AddressId;
        public Guid? Country;
        public Guid? City;
        public Guid? Street;
        public Guid? CountryRegion;
        public Guid? MetroStation;
        public string BuildingNumber;
        public string AppartmentNumber;
    }
    #endregion
}