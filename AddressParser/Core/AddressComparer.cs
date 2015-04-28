using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using AddressParser.Core.Models;



namespace AddressParser.Core
{
    public partial class Parser
    {
        #region AddressComponents
        class AddressComponents
        {
            internal readonly List<NameAndTypeCollection> NameAndTypes;
            internal readonly HouseInfo HouseInfo;
            internal int Reduces { get; private set; }


            public AddressComponents(IEnumerable<IGrouping<string, NameAndType>> nameAndTypes, HouseInfo houseInfo)
            {
                NameAndTypes = nameAndTypes.Select(n => new NameAndTypeCollection(n)).ToList();
                HouseInfo = houseInfo;
            }


            public void Reduce(NameAndTypeCollection item)
            {
                NameAndTypes.Remove(item);
                Reduces++;
            }

            internal class NameAndTypeCollection : IReadOnlyCollection<NameAndType>
            {
                public readonly string UnionName;
                readonly List<NameAndType> _nameAndTypes;


                public NameAndTypeCollection(IEnumerable<NameAndType> grouping)
                {
                    _nameAndTypes = grouping.ToList();
                    UnionName = _nameAndTypes[0].AddrObjectName.OriginalName;
                }


                public NameAndTypeCollection(NameAndType item)
                {
                    _nameAndTypes = new List<NameAndType>(){item};
                    UnionName = _nameAndTypes[0].AddrObjectName.OriginalName;
                }

                public IEnumerator<NameAndType> GetEnumerator()
                {
                    return _nameAndTypes.GetEnumerator();
                }


                IEnumerator IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }


                public int Count { get { return _nameAndTypes.Count; } }

                public override string ToString()
                {
                    return string.Format(@"""{0}"", {1}", UnionName, Count);
                }
            }
        }
        #endregion

        #region CompareResult
        internal class CompareResult
        {
            public string Name { get; private set; }


            CompareResult(string name)
            {
                Name = name;
            }


            public override string ToString()
            {
                return Name;
            }


            public static readonly CompareResult Exact = new CompareResult("Exact");
            public static readonly CompareResult WithoutRoom = new CompareResult("Without Room");
            public static readonly CompareResult WithoutHouse = new CompareResult("Without House");
            public static readonly CompareResult Partial = new CompareResult("Partial");
            public static readonly CompareResult Far = new CompareResult("Far"); 

        }
        #endregion

        internal CompareResult CompareAddresses(int? newId, Guid? oldId)
        {
            if (!newId.HasValue && !oldId.HasValue)
                return CompareResult.Exact;
            if (!newId.HasValue || !oldId.HasValue)
                return CompareResult.Far;

            string newAddressStr = GetAddressStringById(newId.Value);
            string oldAddressStr = GetAddressStringByOldId(oldId.Value);

            return CompareAddresses(newAddressStr, oldAddressStr);
        }


        internal CompareResult CompareAddresses(string newAddressStr, string oldAddressStr)
        {
            try
            {
                return CompareAddressesPrivate(newAddressStr, oldAddressStr);
            }
            catch (Exception e)
            {
                var exceptionText =
                    new StringBuilder(string.Format("NewAddressString: {0}" + Environment.NewLine + "OldAddressString: {1}",
                        newAddressStr == null ? "null" : string.Format("\"{0}\"", newAddressStr),
                        oldAddressStr == null ? "null" : string.Format("\"{0}\"", oldAddressStr)));
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


        CompareResult CompareAddressesPrivate(string newAddressStr, string oldAddressStr)
        {
            if (string.IsNullOrEmpty(newAddressStr) && string.IsNullOrEmpty(oldAddressStr))
                return CompareResult.Exact;

            if (string.IsNullOrEmpty(newAddressStr) || string.IsNullOrEmpty(oldAddressStr))
                return CompareResult.Far;

            var mkadCompareResult = CompareMkads(newAddressStr, oldAddressStr);
            if (mkadCompareResult != null)
                return mkadCompareResult;

            AddressComponents newComponents = GetComponents(newAddressStr, true);
            AddressComponents oldComponents = GetComponents(oldAddressStr);
            ReduceComponents(newComponents, oldComponents);
            SplitOldComponents(newComponents, oldComponents);

            if (newComponents.NameAndTypes.Count - oldComponents.NameAndTypes.Count > 1)
                return CompareResult.Far;

            var matches = new Dictionary<AddressComponents.NameAndTypeCollection, bool>();
            //bool hasMistake = false;
            foreach (var newNatLookup in newComponents.NameAndTypes)
            {
                if (newNatLookup.Any(newNat =>
                    oldComponents.NameAndTypes.Any(oldNatLookup => oldNatLookup.Any(oldNat =>
                        AreNameAndTypesEqual(newNat, oldNat)))))
                {
                    matches[newNatLookup] = true;
                }
                else
                    matches[newNatLookup] = false;
            }

            if (matches.Any(m => m.Value == false && (m.Key.All(k => k.Type != AddrObjectType.Rayon) || oldComponents.NameAndTypes.Any(o => o.Any(oc => m.Key.Any(mm => mm.Type == oc.Type))))))
                return CompareResult.Far;
            //if (hasMistake)
            //    return CompareResult.Far;

            if (matches.Count < 2 && newComponents.Reduces > 0 && oldComponents.Reduces > 0)
                return CompareResult.Far;

            if ((newComponents.NameAndTypes.Count == oldComponents.NameAndTypes.Count && matches.All(m => m.Value == true)) ||
                (newComponents.NameAndTypes.Count - oldComponents.NameAndTypes.Count == 1 &&
                matches.Count(m => m.Value == false && m.Key.Any(k => k.Type == AddrObjectType.Rayon)) == 1))
            {
                if (newComponents.HouseInfo == oldComponents.HouseInfo)
                    return CompareResult.Exact;

                if (newComponents.HouseInfo.EqualsWithoutRoom(oldComponents.HouseInfo))
                    return CompareResult.WithoutRoom;

                return CompareResult.WithoutHouse;
            }

            return CompareResult.Partial;
        }

        AddressComponents GetComponents(string addrStr, bool excludeNonTyped = false)
        {
            addrStr = InitSearchQuery(addrStr);
            var houseInfo = GetHouseInfo(ref addrStr, true);
            string twinHousePattern = houseInfo.HouseNum != null ? string.Format("{0}?{1}{2}", RegexPatterns.HPattern, houseInfo.HouseNum, RegexPatterns.EndPattern) : "";
            List<string> names = GetNames(addrStr)
                .Select(n => n.RegexReplace(twinHousePattern, "").Trim(' ', ',', '.'))
                .Where(x => x != "россия" && x.Length != 1)
                .Select(n => n.Replace(AddrObjectType.Chuvashia.ShortName, "").RegexReplace(@"(?<!г\.\s)москва", " "))
                .ToList();
            //NamesToCanonical(names);
            List<AddrObjectType> types = GetTypes(names).Where(t => t.Id != AddrObjectType.Chuvashia.Id).ToList();
            List<NameAndType> nameAndTypes = SplitNamesBy(names, types, " ").Where(
                    n => !excludeNonTyped || n.Type != null).ToList();
            foreach (NameAndType nameAndType in nameAndTypes.ToList())
                if (nameAndType.Type != null)
                    nameAndTypes.RemoveAll(n => n.AddrObjectName.OriginalName == nameAndType.AddrObjectName.OriginalName && n.Type == null);
            return new AddressComponents(nameAndTypes.GroupBy(n => new {n.AddrObjectName.Name, n.Type}).Select(n => n.First()).ToLookup(n => n.AddrObjectName.OriginalName), houseInfo);
        }


        void SplitOldComponents(AddressComponents newComponents, AddressComponents oldComponents)
        {
            foreach (var nameAndTypeCollection in oldComponents.NameAndTypes.ToList())
            {
                Dictionary<NameAndType, IEnumerable<string>> splits = new Dictionary<NameAndType, IEnumerable<string>>();
                bool splittedByDot = false;

                foreach (var nameAndType in nameAndTypeCollection)
                {
                    var splitItemList = new List<string>();
                    var list = nameAndType.AddrObjectName.Name.Split(' ').ToList();
                    foreach (var item in list)
                    {
                        if (item.EndsWith("."))
                        {
                            splittedByDot = true;
                            splitItemList.Add(item.TrimEnd('.'));
                        }
                        else
                            splitItemList.Add(item);
                    }

                    splits[nameAndType] = splitItemList;
                }

                if (nameAndTypeCollection.All(n => n.Type == null) && !splittedByDot)
                    continue;

                foreach (var split in splits)
                {
                    var exactNames = split.Value.Select(s => newComponents.NameAndTypes.FirstOrDefault(n => n.Any(nn => nn.AddrObjectName.Name == s))).Where(s => s != null).ToList();
                    if (exactNames.Count != 2)
                        continue;

                    var firstPart = new NameAndType(exactNames[0].First().AddrObjectName.Name, exactNames[0].First().AddrObjectName.Name, null, null);
                    var secondPart = new NameAndType(exactNames[1].First().AddrObjectName.Name, exactNames[1].First().AddrObjectName.Name, null, null);
                    oldComponents.NameAndTypes.Remove(nameAndTypeCollection);
                    oldComponents.NameAndTypes.Add(new AddressComponents.NameAndTypeCollection(firstPart));
                    oldComponents.NameAndTypes.Add(new AddressComponents.NameAndTypeCollection(secondPart));
                }
            }
        }


        void NamesToCanonical(List<string> names)
        {
            for (int i = 0; i < names.Count; i++)
                names[i] = NameToCanonical(names[i]);
        }


        string NameToCanonical(string name)
        {
            if (name.Contains(RegexPatterns.UmlautLowerPattern))
                name = name.Replace(RegexPatterns.UmlautLowerPattern, "е");

            if (name.Contains(RegexPatterns.UmlautUpperPattern))
                name = name.Replace(RegexPatterns.UmlautLowerPattern, "Е");

            if (Regex.IsMatch(name, RegexPatterns.BigPattern))
                name = Regex.Replace(name, RegexPatterns.BigPattern, " ").Trim() + " " + "б.";

            if (Regex.IsMatch(name, RegexPatterns.SmallPattern))
                name = Regex.Replace(name, RegexPatterns.SmallPattern, " ").Trim() + " " + "м.";

            if (Regex.IsMatch(name, RegexPatterns.OldPattern))
                name = Regex.Replace(name, RegexPatterns.OldPattern, " ").Trim() + " " + "с.";

            if (Regex.IsMatch(name, RegexPatterns.NewPattern))
                name = Regex.Replace(name, RegexPatterns.NewPattern, " ").Trim() + " " + "н.";

            if (Regex.IsMatch(name, RegexPatterns.UpperPattern))
                name = Regex.Replace(name, RegexPatterns.UpperPattern, " ").Trim() + " " + "в.";

            if (Regex.IsMatch(name, RegexPatterns.LowerPattern))
                name = Regex.Replace(name, RegexPatterns.LowerPattern, " ").Trim() + " " + "н.";

            if (Regex.IsMatch(name, RegexPatterns.ReplacedNumberPattern))
            {
                string num = Regex.Match(name, RegexPatterns.ReplacedNumberPattern).Groups["n"].Value;
                name = num + " " + Regex.Replace(name.Replace(num, " ").Trim(), RegexPatterns.SpacePattern, " ");
            }
            else if (Regex.IsMatch(name, RegexPatterns.NumberPattern))
            {
                var number = Regex.Match(name, RegexPatterns.NumberPattern).Groups["n"].Value;
                var trimmedNumber = number.Trim(_numberTrimChars);
                var y = trimmedNumber + "-й";
                name = y + " " + Regex.Replace(name.Replace(number, " ").Trim(), RegexPatterns.SpacePattern, " ");
            }

            var result = name.Replace(".", " ").Trim();
            return result;
        }


        void ReduceComponents(AddressComponents newComponents, AddressComponents oldComponents)
        {
            foreach (var newNatLookup in newComponents.NameAndTypes.ToList())
            {
                if (IsReducePossible(newNatLookup, oldComponents.NameAndTypes, AddrObjectType.Region))
                    newComponents.Reduce(newNatLookup);

                if (IsReducePossible(newNatLookup, oldComponents.NameAndTypes, AddrObjectType.Republic))
                    newComponents.Reduce(newNatLookup);

                if (IsReducePossible(newNatLookup, oldComponents.NameAndTypes, AddrObjectType.Country))
                    newComponents.Reduce(newNatLookup);

                if (newNatLookup.Any(n => n.Type == AddrObjectType.City && n.AddrObjectName.Name == "москва") &&
                    oldComponents.NameAndTypes.All(n => n.All(nn => nn.AddrObjectName.Name != "москва")))
                    newComponents.Reduce(newNatLookup);
            }

            foreach (var oldNatLookup in oldComponents.NameAndTypes.ToList())
            {
                if (IsReducePossible(oldNatLookup, newComponents.NameAndTypes, AddrObjectType.Region))
                    oldComponents.Reduce(oldNatLookup);

                if (IsReducePossible(oldNatLookup, newComponents.NameAndTypes, AddrObjectType.Republic))
                    oldComponents.Reduce(oldNatLookup);

                if (IsReducePossible(oldNatLookup, newComponents.NameAndTypes, AddrObjectType.Country))
                    oldComponents.Reduce(oldNatLookup);
            }
        }


        bool IsReducePossible(IEnumerable<NameAndType> any, IEnumerable<AddressComponents.NameAndTypeCollection> all, AddrObjectType type)
        {
            var withSuchType = any.FirstOrDefault(nn => nn.Type == type);
            if (withSuchType != null)
            {
                if (all.All(on => on.All(oon => oon.Type != type && oon.AddrObjectName.Name != withSuchType.AddrObjectName.Name)))
                    return true;
            }

            return false;
        }


        CompareResult CompareMkads(string first, string second)
        {
            first = InitSearchQuery(first);
            second = InitSearchQuery(second);
            var mkadFirst = GetMkad(first);
            if (mkadFirst == null)
                return null;
            var mkadSecond = GetMkad(second);
            if (mkadSecond == null)
                return null;

            if (mkadFirst.Kilometer != mkadSecond.Kilometer)
                return CompareResult.Far;

            if (mkadFirst.Definition != null)
                first = first.Replace(mkadFirst.Definition, " ");
            if (mkadSecond.Definition != null)
                second = second.Replace(mkadSecond.Definition, " ");
            var firstHouseInfo = GetHouseInfo(ref first, false);
            var secondHouseInfo = GetHouseInfo(ref second, false);

            if (firstHouseInfo == secondHouseInfo)
                return CompareResult.Exact;

            if (firstHouseInfo.EqualsWithoutRoom(secondHouseInfo))
                return CompareResult.WithoutRoom;

            return CompareResult.WithoutHouse;
        }


        Mkad GetMkad(string query)
        {
            var match = Regex.Match(query, RegexPatterns.MkadPattern);
            if (!match.Success)
                return null;

            var sNumber = match.Groups["sNumber"].Value;
            if (!string.IsNullOrEmpty(sNumber))
            {
                var sDefinition = match.Groups["sMkad"].Value + "мкад";
                var sMkad = new Mkad(byte.Parse(sNumber), sDefinition);
                return sMkad;
            }

            var eNumber = match.Groups["eNumber"].Value;
            if (string.IsNullOrEmpty(eNumber))
                return new Mkad(-1, null);

            var eDefinition = "мкад" + match.Groups["eMkad"].Value;
            var eMkad = new Mkad(byte.Parse(eNumber), eDefinition);
            return eMkad;
        }



        class Mkad
        {
            public readonly short Kilometer;
            public readonly string Definition;
            public Mkad(short kilometer, string definition)
            {
                Kilometer = kilometer;
                Definition = definition;
            }
        }



        bool AreNameAndTypesEqual(NameAndType first, NameAndType second)
        {
            if (first.AddrObjectName == second.AddrObjectName)
                return AreAddrObjectTypesEqual(first.Type, second.Type);

            if (first.AddrObjectName.OriginalName == second.AddrObjectName.OriginalName || first.AddrObjectName.Name == second.AddrObjectName.OriginalName ||
                first.AddrObjectName.OriginalName == second.AddrObjectName.Name || first.AddrObjectName.CanonicalName == second.AddrObjectName.CanonicalName)
                return true;

            return false;
        }


        bool AreAddrObjectTypesEqual(AddrObjectType first, AddrObjectType second)
        {
            if (first == null || second == null || first == second)
                return true;

            if ((first == AddrObjectType.Settlement || first == AddrObjectType.CitySettlement) && (second == AddrObjectType.Settlement || second == AddrObjectType.CitySettlement))
                return true;

            return false;
        }
    }
}
