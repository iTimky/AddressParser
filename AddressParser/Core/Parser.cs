#region usings
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

using AddressParser.Core.Db;
using AddressParser.Core.Models;

#endregion


[assembly: InternalsVisibleTo("AddressParser.Test")]



namespace AddressParser.Core
{
    public partial class Parser
    {
        readonly DataContextBase _dataContext;


        public Parser(DataContextBase dataContext)
        {
            _dataContext = dataContext;
        }


        public Address ParseAddress(string searchQuery, bool useAddonHouses = true, bool? useHeavy = false,
            bool? onlyObjects = false)
        {
            if (string.IsNullOrEmpty(searchQuery))
                return null;

            try
            {
                List<Address> addresses = new List<Address>();
                List<string> searchQueries = new List<string>();
                if (searchQuery.Length > 150)
                {
                    string subQuery = searchQuery.Substring(0, 150);
                    const int length = 150;
                    while (!string.IsNullOrEmpty(subQuery))
                    {
                        searchQueries.Add(subQuery);
                        int startIndex = searchQueries.Sum(s => s.Length);

                        subQuery = searchQuery.Substring(startIndex,
                            startIndex + length > searchQuery.Length ? searchQuery.Length - startIndex : length);
                    }
                }
                else
                    searchQueries.Add(searchQuery);

                foreach (string query in searchQueries)
                {
                    Address address = ParseAddressInternal(query, useAddonHouses, useHeavy, onlyObjects);
                    if (address != null)
                        addresses.Add(address);
                }

                List<Address> possibleAddresses = GetPossibleAddresses(addresses);
                if (possibleAddresses == null)
                    return null;

                Address mainAddress = GetMainAddress(possibleAddresses);
                return mainAddress;
            }
            catch (Exception e)
            {
                var exceptionText =
                    new StringBuilder(string.Format("Query: {0}" + Environment.NewLine,
                        searchQuery == null ? "null" : string.Format("\"{0}\"", searchQuery)));
                exceptionText.Append(string.Format("{0}: ", e.GetType().Name));
                exceptionText.Append(e.Message + Environment.NewLine);
                exceptionText.Append(e.StackTrace + Environment.NewLine);
                if (e.InnerException != null)
                {
                    exceptionText.Append(string.Format("{0}: ", e.InnerException.GetType().Name));
                    exceptionText.Append(e.InnerException.Message + Environment.NewLine);
                    exceptionText.Append(e.InnerException.StackTrace + Environment.NewLine);
                }

                throw new Exception(exceptionText.ToString());
            }
        }


        Address ParseAddressInternal(string searchQuery, bool useAddonHouses, bool? useHeavy = false, bool? onlyObjects = false)
        {
            if (string.IsNullOrEmpty(searchQuery))
                return null;

            bool isHavy = useHeavy == true;
            searchQuery = InitSearchQuery(searchQuery);

            HouseInfo houseInfo = GetHouseInfo(ref searchQuery, true);
            searchQuery = RemoveBadSymbols(searchQuery);
            List<string> names = GetNames(searchQuery);
            Address russia = GetRussia(names);
            if (russia != null)
                return russia;

            List<List<string>> variations = SplitNamesBySpace(names);
            PushCrutch(variations);
            List<AddressPart> addressParts = variations.Select(v => new AddressPart(v)).ToList();
            List<NameAndType> nameAndTypes = SplitNameAndTypes(addressParts).ToList();

            if (!nameAndTypes.Any())
                return null;

            var addrObjects = new List<AddrObject>();
            var addrHouses = new List<AddrHouse>();

            bool isOnlyStreet = IsOnlyStreet(nameAndTypes);
            addrObjects.AddRange(GetAddrObjects(nameAndTypes, isHavy, isOnlyStreet));

            List<Address> addresses = addrObjects.Select(ao => new Address(ao)).ToList();

            if (addresses.Any())
                addrHouses.AddRange(FillAddrHouses(addresses, houseInfo));
            addrObjects.AddRange(FillAddonAddrObjects(addresses, nameAndTypes, isHavy));
            if (useAddonHouses)
                addrHouses.AddRange(FillAddonAddrHouses(addresses, houseInfo));

            RebuildAddressParts(addressParts);

            RemoveOverheadNamedAddresses(addresses, addressParts);
            FillIsAllWordsFound(addresses, addressParts);

            List<Address> possibleAddresses = GetPossibleAddresses(addresses);
            if (possibleAddresses == null)
            {
                if (isHavy)
                    return ParseAddressInternal(searchQuery, useAddonHouses, false);

                return null;
            }

            Address mainAddress = GetMainAddress(possibleAddresses);
            if (mainAddress.AddrHouse != null && houseInfo != null && houseInfo.Room != null)
                mainAddress.Room = houseInfo.Room.ToString();
            if (onlyObjects == false)
                _dataContext.SetAddressId(mainAddress);

            mainAddress.HouseInfo = houseInfo;

            return mainAddress;
        }


        string RemoveBadSymbols(string query)
        {
            var stringBuilder = new StringBuilder(query);
            stringBuilder.Replace("/", "");
            stringBuilder.Replace("\\", "");

            return stringBuilder.ToString();
        }


        Address GetMainAddress(List<Address> possibleAddresses)
        {
            Address mainAddress = possibleAddresses.Count == 1
                ? possibleAddresses.First()
                : possibleAddresses.First(pa => pa.IsAllWordsFound == true);

            return mainAddress;
        }


        Address GetRussia(List<string> names)
        {
            if (names.Count != 1 || names[0].ToLower() != "россия")
                return null;

            var russia = new Address(new AddrObject(GetRussiaGuid(), AddrObjectKind.AddonAddrObject));
            _dataContext.SetAddressId(russia);
            return russia;
        }


        bool IsOnlyStreet(List<NameAndType> nameAndTypes)
        {
            if (nameAndTypes.Count == 0)
                return false;

            string originAddrName = Regex.Replace(nameAndTypes.First().AddrObjectName.OriginalName, @"(?:улица|ул)\.?", "").Trim();
            if (nameAndTypes.All(n => Regex.Replace(n.AddrObjectName.OriginalName, @"(?:улица|ул)\.?", "").Trim() == originAddrName) &&
                nameAndTypes.Any(n => n.Type != null && n.Type.ShortName == "ул"))
                return true;

            return false;
        }


        static string InitSearchQuery(string query)
        {
            string addrStr = Regex.Replace(query, @"(?<=[a-zA-Zа-яА-ЯёЁ])(?:\s*\-\s*)(?=[a-zA-Zа-яА-ЯёЁ])", "-").ToLower();
            addrStr = Regex.Replace(addrStr, @"\s+\-\s+", "");
            string postalCode = Regex.Match(addrStr, RegexPatterns.PostalCodePattern).Groups["pCode"].Value;
            while (!string.IsNullOrEmpty(postalCode))
            {
                addrStr = addrStr.Replace(postalCode, "");
                postalCode = Regex.Match(addrStr, RegexPatterns.PostalCodePattern).Groups["pCode"].Value;
            }

            return addrStr;
        }


        void RemoveOverheadNamedAddresses(List<Address> addresses, IEnumerable<AddressPart> addressParts)
        {
            foreach (Address address in addresses.ToList())
            {
                List<string> allNames = address.GetAllNames();
                if (allNames.Distinct().Any(name => allNames.Count(an => an == name) > addressParts.Count(ap => ap == name)))
                    addresses.Remove(address);
            }
        }


        void FillIsAllWordsFound(IEnumerable<Address> addresses, List<AddressPart> addressParts)
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
                                r.Name == addressPart || nameNtypes.Any(n => n.AddrObjectName.Name == r.Name) ||
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
        List<string> GetNames(string addrStr)
        {
            addrStr = Regex.Replace(addrStr, @"(^|\s)пр\.?(\s|$)", " пр-кт ");

            Match matchY = Regex.Match(addrStr, @"\d[\-\s]?[ыои]?й");
            if (matchY.Success)
                addrStr = addrStr.Replace(matchY.Value, Regex.Replace(matchY.Value, @"[\-\s]?[ыои]?й", "-й"));

            Match matchAya = Regex.Match(addrStr, @"\d[\-\s]?а?я");
            if (matchAya.Success)
                addrStr = addrStr.Replace(matchAya.Value, Regex.Replace(matchAya.Value, @"[\-\s]?а?я", "-я"));

            Match matchOe = Regex.Match(addrStr, @"\d[\-\s]?ое");
            if (matchOe.Success)
                addrStr = addrStr.Replace(matchOe.Value, Regex.Replace(matchOe.Value, @"[\-\s]?ое", "-е"));

            Match kmMatch = Regex.Match(addrStr, RegexPatterns.KilometerPattern);
            if (kmMatch.Success)
                addrStr =
                    addrStr.Replace(kmMatch.Value, " " + kmMatch.Groups["n"].Value + " " + kmMatch.Groups["km"].Value + " ").Trim();

            List<StringBuilder> stringBuilders =
                addrStr.Split(new[] {",", ";", ":"}, StringSplitOptions.RemoveEmptyEntries).Where(
                    x => !string.IsNullOrWhiteSpace(x)).Select(s => new StringBuilder(s.ToLower())).ToList();

            foreach (StringBuilder name in stringBuilders)
            {
                name.Replace("\"", "");
                name.Replace("'", "''");
                name.Replace("пр-д", "проезд");
                name.Replace("пр-т", "пр-кт");
                name.Replace("р-он", "р-н");
                name.Replace("ш-се", "ш");
                name.Replace("пос.", "п.");
                name.Replace(".", ". ");
                name.Replace("ё", "е");
            }

            List<string> names =
                stringBuilders.Select(n => Regex.Replace(n.ToString(), RegexPatterns.SpacePattern, " ").Trim(' ', '-', '.'))
                    .Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();

            return names;
        }


        List<List<string>> SplitNamesBySpace(IEnumerable<string> names)
        {
            const int wordLimit = 5;
            var variations = new List<List<string>>();

            foreach (string name in names.ToList())
            {
                List<string> nameWords = name.Split(new[] {' '}).ToList();
                int taken = 0;
                List<string> limitedWords = nameWords.Skip(taken).Take(wordLimit).ToList();
                while (limitedWords.Count != 0)
                {
                    for (int i = 0; i < limitedWords.Count; i++)
                    {
                        var addWords = new List<string>();
                        string word = limitedWords[i];
                        addWords.Add(word);

                        for (int j = i + 1; j < limitedWords.Count; j++)
                        {
                            word += " " + limitedWords[j];
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

                        variations.Add(
                            addWords.Select(w => w.Trim(trimChars)).Where(
                                w => !string.IsNullOrEmpty(w) && !string.IsNullOrWhiteSpace(w)).Distinct().ToList());
                    }

                    taken += limitedWords.Count;
                    limitedWords = nameWords.Skip(taken).Take(wordLimit).ToList();
                }
            }

            var result = new List<List<string>>();

            foreach (List<string> item in variations.Where(v => v.Any() && !v.Contains("россия")))
                if (result.All(v => v.Count != item.Count || v.Any(vi => !item.Contains(vi))))
                    result.Add(item);

            return result;
        }


        #region Crutch
        void PushCrutch(IEnumerable<List<string>> variationsList)
        {
            foreach (List<string> variations in variationsList)
            {
                var newVariations = new List<string>();
                newVariations.AddRange(PushSmallBigCrutch(variations));
                newVariations.AddRange(PushLowMiddleUpCrutch(variations));
                newVariations.AddRange(PushOldNewCrutch(variations));
                newVariations.AddRange(PushWordNumberCrutch(variations));
                newVariations.AddRange(PushNumberCrutch(variations));
                newVariations.AddRange(PushParentCrutch(variations));

                variations.AddRangeIfNotExists(newVariations.Distinct());
            }
        }


        List<string> PushSmallBigCrutch(IEnumerable<string> variations)
        {
            var big = new[] {"большой", "большая", "большое", "бол.", "б."};
            var small = new[] {"малый", "малая", "малое", "мал.", "м."};

            var newVariations = new List<string>();

            foreach (string variation in variations)
            {
                if (Regex.IsMatch(variation, RegexPatterns.BigPattern))
                    foreach (string word in big)
                        newVariations.AddRangeIfNotExists(ReplaceReduct(variation, word, RegexPatterns.BigPattern));

                if (Regex.IsMatch(variation, RegexPatterns.SmallPattern))
                    foreach (string word in small)
                        newVariations.AddRangeIfNotExists(ReplaceReduct(variation, word, RegexPatterns.SmallPattern));
            }

            return newVariations;
        }


        List<string> PushLowMiddleUpCrutch(IEnumerable<string> variations)
        {
            var upper = new[] {"верхний", "верхняя", "верхнее", "верхн.", "верх.", "в."};
            var middle = new[] {"средний", "средняя", "среднее", "сред.", "ср."};
            var lower = new[] {"нижний", "нижняя", "нижнее", "нижн", "ниж.", "н."};
            var newVariations = new List<string>();
            foreach (string variation in variations)
            {
                if (Regex.IsMatch(variation, RegexPatterns.UpperPattern))
                    foreach (string word in upper)
                        newVariations.AddRangeIfNotExists(ReplaceReduct(variation, word, RegexPatterns.UpperPattern));

                if (Regex.IsMatch(variation, RegexPatterns.MiddlePattern))
                    foreach (string word in middle)
                        newVariations.AddRangeIfNotExists(ReplaceReduct(variation, word, RegexPatterns.MiddlePattern));

                if (Regex.IsMatch(variation, RegexPatterns.LowerPattern))
                    foreach (string word in lower)
                        newVariations.AddRangeIfNotExists(ReplaceReduct(variation, word, RegexPatterns.LowerPattern));
            }

            return newVariations;
        }


        List<string> PushOldNewCrutch(IEnumerable<string> variations)
        {
            var old = new[] {"старый", "старая", "старое", "стар.", "с."};
            var neew = new[] {"новый", "новая", "новое", "нов.", "н."};
            var newVariations = new List<string>();
            foreach (string variation in variations)
            {
                if (Regex.IsMatch(variation, RegexPatterns.OldPattern))
                    foreach (string word in old)
                        newVariations.AddRangeIfNotExists(ReplaceReduct(variation, word, RegexPatterns.OldPattern));

                if (Regex.IsMatch(variation, RegexPatterns.NewPattern))
                    foreach (string word in neew)
                        newVariations.AddRangeIfNotExists(ReplaceReduct(variation, word, RegexPatterns.NewPattern));
            }

            return newVariations;
        }


        List<string> PushWordNumberCrutch(IEnumerable<string> variations)
        {
            var newVariations = new List<string>();
            foreach (string variation in variations.ToList())
            {
                Match nwMatch = Regex.Match(variation, RegexPatterns.NumberWordPattern);
                if (nwMatch.Success)
                {
                    string n = nwMatch.Groups["n"].Value;
                    string w = nwMatch.Groups["w"].Value;
                    string nwReplaced = variation.Replace(n + w, string.Format("{0} {1}", n, w));
                    newVariations.AddIfNotExists(nwReplaced);
                }

                Match wnMatch = Regex.Match(variation, RegexPatterns.WordNumberPattern);
                if (wnMatch.Success)
                {
                    string w = wnMatch.Groups["w"].Value;
                    string n = wnMatch.Groups["n"].Value;
                    string wnReplaced = variation.Replace(w + n, string.Format("{0} {1}", w, n));
                    newVariations.AddIfNotExists(wnReplaced);
                }
            }

            return newVariations;
        }


        readonly char[] _numberTrimChars = {'-', ' '};


        List<string> PushNumberCrutch(IEnumerable<string> variations)
        {
            var newVariations = new List<string>();
            foreach (string variation in variations)
            {
                Match nMatch = Regex.Match(variation, RegexPatterns.NumberPattern);
                if (!nMatch.Success)
                    continue;

                string number = nMatch.Groups["n"].Value;
                string trimmedNumber = number.Trim(_numberTrimChars);
                string y = trimmedNumber + "-й";
                string ya = trimmedNumber + "-я";
                string e = trimmedNumber + "-е";
                string variationY = variation.Replace(number, " " + y + " ").Trim().RegexReplace(RegexPatterns.SpacePattern, " ");
                string variationYa = variation.Replace(number, " " + ya + " ").Trim().RegexReplace(RegexPatterns.SpacePattern, " ");
                string variationE = variation.Replace(number, " " + e + " ").Trim().RegexReplace(RegexPatterns.SpacePattern, " ");
                newVariations.AddIfNotExists(variationY);
                newVariations.AddIfNotExists(variationYa);
                newVariations.AddIfNotExists(variationE);

                newVariations.AddRangeIfNotExists(ReplaceReduct(variationY, y.Trim(), RegexPatterns.ReplacedNumberPattern));
                newVariations.AddRangeIfNotExists(ReplaceReduct(variationYa, ya.Trim(), RegexPatterns.ReplacedNumberPattern));
                newVariations.AddRangeIfNotExists(ReplaceReduct(variationE, e.Trim(), RegexPatterns.ReplacedNumberPattern));
            }

            return newVariations;
        }


        List<string> PushParentCrutch(IEnumerable<string> variations)
        {
            var newVariations = new List<string>();
            string zelenograd = variations.FirstOrDefault(n => n.ToLower() == "зеленоград");
            if (zelenograd != null)
                newVariations.AddIfNotExists("москва");

            return newVariations;
        }
        #endregion


        void RebuildAddressParts(List<AddressPart> addressParts)
        {
            addressParts.ForEach(a => a.AddWithoutChildTypes());
            addressParts.RemoveAll(a => !a.Variations.Any());
            foreach (AddressPart addressPart in addressParts.Where(ap => ap.Count == 1).ToList())
                if (addressParts.Where(ap => ap.Count > 1).Any(ap => ap == addressPart.Variations.Single()))
                    addressParts.Remove(addressPart);
        }
        #endregion


        #region House Logic
        HouseInfo GetHouseInfo(ref string searchQuery, bool cutItems = false)
        {
            string query = string.Copy(searchQuery);
            Room room = null;
            while (true)
            {
                if (!Regex.IsMatch(query, RegexPatterns.RoomPattern))
                    break;

                Match match = Regex.Match(query, RegexPatterns.RoomPattern);
                string r = match.Groups["r"].Value;
                query = query.Replace(r, " ");

                string rName = match.Groups["rName"].Value;
                string rNum = match.Groups["rNum"].Value;

                if (!string.IsNullOrEmpty(rNum) && !string.IsNullOrEmpty(rName))
                {
                    rName = Regex.Replace(rName.Replace(".", ""), "(помещение|помещ|пом)", "помещ");
                    rName = Regex.Replace(rName, "(квартира|кв)", "кв");
                    rName = Regex.Replace(rName, "(офис|оф)", "оф");
                    rNum = Regex.Replace(rNum.ToUpper().Trim(new[] {'.', ',', ' '}), @"[\«\»\""\'\№\#]", "");
                    if (!string.IsNullOrEmpty(rNum))
                        room = new Room(rName, rNum);
                }
            }

            if (Regex.IsMatch(query, RegexPatterns.PorchPattern))
                query = Regex.Replace(query, RegexPatterns.PorchPattern, " ", RegexOptions.RightToLeft);

            if (Regex.IsMatch(query, RegexPatterns.VisitHoursPattern))
                query = Regex.Replace(query, RegexPatterns.VisitHoursPattern, " ");

            if (Regex.IsMatch(query, RegexPatterns.FloorPattern))
                query = Regex.Replace(query, RegexPatterns.FloorPattern, " ", RegexOptions.RightToLeft);

            if (Regex.IsMatch(query, RegexPatterns.TransportPattern))
                query = Regex.Replace(query, RegexPatterns.TransportPattern, " ");

            if (Regex.IsMatch(query, RegexPatterns.PhonePattern))
                query = Regex.Replace(query, RegexPatterns.PhonePattern, " ");

            query = query.RegexReplace(@"кв./оф\.?", "");

            string hNum = null;
            string bNum = null;
            string sNum = null;

            if (Regex.IsMatch(query, RegexPatterns.StructPattern))
            {
                Match match = Regex.Match(query, RegexPatterns.StructPattern);
                string sNumTemp = match.Groups["sNum"].Value;
                sNum = string.IsNullOrEmpty(sNumTemp) ? null : sNumTemp;
                if (sNum != null)
                    query = query.Replace(match.Value, ", ");
            }

            if (Regex.IsMatch(query, RegexPatterns.BuildPatter))
            {
                Match match = Regex.Match(query, RegexPatterns.BuildPatter);
                string bNumTemp = match.Groups["bNum"].Value;
                bNum = string.IsNullOrEmpty(bNumTemp) ? null : bNumTemp;
                if (bNum != null)
                    query = query.Replace(match.Value, ", ");
            }

            if (Regex.IsMatch(query, RegexPatterns.HousePattern))
            {
                Match match = Regex.Match(query, RegexPatterns.HousePattern, RegexOptions.RightToLeft);
                string hNumTemp = match.Groups["hNum"].Value;
                hNum = string.IsNullOrEmpty(hNumTemp) ? null : hNumTemp;
                if (hNum != null)
                    query = query.Replace(match.Value, ", ");
            }

            if (cutItems)
                searchQuery = query;

            return new HouseInfo(hNum, bNum, sNum, room);
        }
        #endregion


        #region type split
        IEnumerable<NameAndType> SplitNameAndTypes(List<AddressPart> addressParts)
        {
            var nameAndTypes = new List<NameAndType>();
            List<AddrObjectType> types = GetTypes(addressParts);

            foreach (NameAndType nameAndType in SplitNamesBy(addressParts, types, " "))
            {
                NameAndType sameElement = nameAndTypes.FirstOrDefault(n => n.AddrObjectName == nameAndType.AddrObjectName);
                if (sameElement == null || sameElement.Type != nameAndType.Type)
                    nameAndTypes.Add(nameAndType);
            }

            foreach (NameAndType nameAndType in SplitNamesBy(addressParts, types, "."))
            {
                NameAndType sameElement = nameAndTypes.FirstOrDefault(n => n.AddrObjectName == nameAndType.AddrObjectName);
                if (sameElement == null || sameElement.Type != nameAndType.Type)
                    nameAndTypes.Add(nameAndType);
            }

            return nameAndTypes.Where(n => !string.IsNullOrEmpty(n.AddrObjectName.Name)).Distinct();
        }


        IEnumerable<NameAndType> SplitNamesBy(IEnumerable<AddressPart> addressParts, List<AddrObjectType> foundTypes,
            string separator)
        {
            var splitBy = new[] {separator};
            var nameAndTypes = new List<NameAndType>();
            foreach (AddressPart addressPart in addressParts)
                foreach (string name in addressPart.Variations)
                {
                    List<string> words =
                        name.Split(splitBy, StringSplitOptions.RemoveEmptyEntries).ToList();

                    List<AddrObjectType> nameTypes =
                        foundTypes.Where(t => words.Contains(t.Name) || words.Contains(t.ShortName) || words.Contains(t.EngName))
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


        IEnumerable<NameAndType> SplitNamesBy(IEnumerable<string> names, List<AddrObjectType> foundTypes, string separator)
        {
            var splitBy = new[] {separator};
            var nameAndTypes = new List<NameAndType>();
            foreach (string name in names)
            {
                var words = name.Split(splitBy, StringSplitOptions.RemoveEmptyEntries).Select(n => new Word(n, n.TrimEnd('.', ' '))).ToList();

                List<AddrObjectType> types =
                    foundTypes.Where(t => words.Any(w => w.Equals(t.Name)) || words.Any(w => w.Equals(t.ShortName)) || words.Any(w => w.Equals(t.EngName)))
                        .ToList();
                if (types.Any())
                    foreach (AddrObjectType type in types)
                    {
                        List<Word> notTypeWords = words.Where(
                            w => w != type.Name && w != type.ShortName && w != type.EngName).ToList();

                        if (notTypeWords.Any())
                            nameAndTypes.Add(new NameAndType(string.Join(separator, notTypeWords.Select(ntw => ntw.Existing)), name, type, null));
                    }
                else if (words.Any())
                    nameAndTypes.Add(new NameAndType(string.Join(separator, words.Select(w => w.Existing)),
                        name, null));
            }

            return nameAndTypes.Distinct();
        }



        class Word : IEquatable<string>
        {
            public readonly string Existing;
            public string Modified;

            public Word(string word)
            {
                Existing = word;
                Modified = word;
            }

            public Word(string word, string modified)
            {
                Existing = word;
                Modified = modified;
            }


            public static bool operator ==(Word word, string str)
            {
                if (ReferenceEquals(word, null) && ReferenceEquals(str, null))
                    return true;
                if (ReferenceEquals(word, null) || ReferenceEquals(str, null))
                    return false;

                return word.Equals(str);
            }


            public static bool operator !=(Word word, string str)
            {
                return !(word == str);
            }

            public static bool operator ==(string str, Word word)
            {
                return word == str;
            }


            public static bool operator !=(string str, Word word)
            {
                return !(word == str);
            }


            public bool Equals(string other)
            {
                if (other == null)
                    return false;

                return Modified == other;
            }
        }


        List<AddrObjectType> GetTypes(IEnumerable<AddressPart> addressParts)
        {
            var types = new List<AddrObjectType>();

            List<string> allWords =
                addressParts.SelectMany(v => v.Variations).SelectMany(
                    n => n.Split(new[] {".", " "}, StringSplitOptions.RemoveEmptyEntries)
                        .Where(
                            w =>
                                !Regex.IsMatch(w, RegexPatterns.BPattern) && !Regex.IsMatch(w, RegexPatterns.HPattern) &&
                                !Regex.IsMatch(w, RegexPatterns.SPattern))
                        .Select(w => w.TrimStart(' ', '.'))).Distinct().ToList();

            if (!allWords.Any())
                return types;

            return
                AddrObjectType.All.Where(
                    aot => allWords.Contains(aot.Name) || allWords.Contains(aot.ShortName) || allWords.Contains(aot.EngName)).ToList();
            //return _dataContext.GetTypes(allWords);
        }


        List<AddrObjectType> GetTypes(IEnumerable<string> names)
        {
            var types = new List<AddrObjectType>();

            List<string> allWords = names.SelectMany(
                n => n.Split(new[] {".", " "}, StringSplitOptions.RemoveEmptyEntries)
                    .Where(
                        w =>
                            !Regex.IsMatch(w, RegexPatterns.BPattern) && !Regex.IsMatch(w, RegexPatterns.HPattern) &&
                            !Regex.IsMatch(w, RegexPatterns.SPattern) || w == "с" || w == "д")
                    .Select(w => w.Trim(' ', '.', '-'))).ToList();

            if (!allWords.Any())
                return types;
//
            return
                AddrObjectType.All.Where(
                    aot => allWords.Contains(aot.Name) || allWords.Contains(aot.ShortName) || allWords.Contains(aot.EngName)).ToList();
            //return _dataContext.GetTypes(allWords);
        }


        List<string> ReplaceReduct(string variation, string replacement, string pattern)
        {
            string startReplacement = replacement + " " + Regex.Replace(variation, pattern, " ").Trim();
            string endReplacement = Regex.Replace(variation, pattern, " ").Trim() + " " + replacement;

            return new List<string>
            {
                startReplacement.Trim().RegexReplace(RegexPatterns.SpacePattern, " "),
                endReplacement.Trim().RegexReplace(RegexPatterns.SpacePattern, " ")
            };
        }
        #endregion


        #region by tuple
        public Address GetAddressByTupleId(int tupleId)
        {
            TupleOld t = _dataContext.GetTupleOld(tupleId);
            if (t == null)
                return null;
            ClearGeneralDelivery(t);
            OldAddress o = t.CreateOldAddress();

            List<string> old_id_Names = new List<string>
            {
                t.old_Country_name,
                t.old_CountryRegion_name,
                //t.old_City_type ?? " г. д. п. пгт. с. " + " " + t.old_City_name
                t.old_City_name,
                //t.old_Street_type ?? " б-р пер пл пр-кт ул ш " + " " + t.old_Street_name
                t.old_Street_name
            }.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

            return GetAddressByOldId(o, old_id_Names);
        }


        void ClearGeneralDelivery(TupleOld t)
        {
            const string r = @"\(?\s*ДО\s*ВОСТРЕБОВАНИЯ\s*\!*\s*(\(?в\s*СЕ\)?)?\s*\)?";
            if (!string.IsNullOrWhiteSpace(t.old_BuildingNumber) &&
                Regex.IsMatch(t.old_BuildingNumber, r, RegexOptions.IgnoreCase))
                t.old_BuildingNumber = Regex.Replace(t.old_BuildingNumber, r, "", RegexOptions.IgnoreCase);
            if (!string.IsNullOrWhiteSpace(t.old_AppartmentNumber) &&
                Regex.IsMatch(t.old_AppartmentNumber, r, RegexOptions.IgnoreCase))
                t.old_AppartmentNumber = Regex.Replace(t.old_AppartmentNumber, r, "", RegexOptions.IgnoreCase);
            if (!string.IsNullOrWhiteSpace(t.old_Street_name) && Regex.IsMatch(t.old_Street_name, r, RegexOptions.IgnoreCase))
                t.old_Street_name = Regex.Replace(t.old_Street_name, r, "", RegexOptions.IgnoreCase);
        }


        Address GetAddressByOldId(OldAddress o, List<string> old_id_Names)
        {
            List<string> stringNames = new List<string>();
            string s = ReplacePostalCode(o.BuildingNumber);
            if (!string.IsNullOrWhiteSpace(s))
                stringNames.Add(s);
            s = ReplacePostalCode(o.AppartmentNumber);
            if (!string.IsNullOrWhiteSpace(s))
                stringNames.Add(s);

            if ((o.Street ?? o.City ?? o.CountryRegion ?? o.Country) == null)
                if (stringNames.Count > 0)
                    return ParseAddress(string.Join(",", stringNames), false, false, false);
                else return null;

            Address address = _dataContext.GetAddressByMajorOldGeoId(o);
            // if matching already exists in AddrObjectsOldIds or AddonAddrObjects.old_id
            if (address != null)
                // it is AddrObjectCurrent or AddonAddrObject
                if (stringNames.Count > 0)
                {
                    string aoString = GetAddressStringByGuid((address.AddonAoId ?? address.AoId).Value);
                    for (int top = stringNames.Count; top >= 1; top--)
                    {
                        string addrStr = aoString + "," + string.Join(",", stringNames.Take(top));
                        Address address1 = ParseAddress(addrStr, false, false, false);
                        if (address1 != null)
                            return address1;
                    }

                    _dataContext.SetAddressId(address);
                    return address;
                }
                else
                {
                    _dataContext.SetAddressId(address);
                    return address;
                }
            else
            {
                // major old_id is not found
                if (old_id_Names == null)
                    old_id_Names = GetNames(o);

                old_id_Names = Clear(old_id_Names);

                for (int stringTop = stringNames.Count; stringTop >= 0; stringTop--)
                {
                    string addrStr = string.Join(",", old_id_Names.Concat(stringNames.Take(stringTop)));
                    address = ParseAddress(addrStr, false, false, false);
                    if (address != null)
                        return address;
                }

                //for (int top = old_id_Names.Count - 1; top >= 1; top--)
                for (int top = 4; top >= 1; top--)
                {
                    address = _dataContext.GetAddressByMajorOldGeoId(o, top);
                    if (address != null)
                        return address;

                    string addrStr = string.Join(",", old_id_Names.Take(top));
                    address = ParseAddress(addrStr, false, false, false);
                    if (address != null)
                        return address;
                }

                return null;
            }
        }


        static readonly char[] trimChars = new[] {' ', '-', '.', ',', ';', ':', '*', '_'};


        List<string> Clear(List<string> names)
        {
            return names.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim(trimChars))
                .Where(x => !string.IsNullOrEmpty(x)).ToList();
        }


        string ReplacePostalCode(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            string rv = Regex.Replace(s, RegexPatterns.PostalCodePattern, "");
            return rv;
        }
        #endregion


        #region GetAddressByOldId(SqlGuid addrOldId)
        public Address GetAddressByOldId(Guid? addrOldId, bool? onlyObjects = false)
        {
            if (!addrOldId.HasValue)
                return null;

            OldAddress o = _dataContext.GetOldAddress(addrOldId.Value);
            if (o == null)
                return null;

            return GetAddressByOldId(o, null);
        }
        #endregion


        public Guid? CreateAddonAddrHouse(Guid? parentId, string query)
        {
            if (!parentId.HasValue || string.IsNullOrEmpty(query))
                return null;

            query = InitSearchQuery(query);

            HouseInfo houseInfo = GetHouseInfo(ref query, false);
            return CreateAddonAddrHouse(parentId, houseInfo);
        }


        internal Guid? CreateAddonAddrHouse(Guid? parentId, HouseInfo houseInfo, DataContextBase customDataContext = null)
        {
            if (!parentId.HasValue || houseInfo.IsEmpty())
                return null;

            if (customDataContext != null)
                return customDataContext.CreateAddonAddrHouse(parentId.Value, houseInfo);

            return _dataContext.CreateAddonAddrHouse(parentId.Value, houseInfo);
        }
    }
}