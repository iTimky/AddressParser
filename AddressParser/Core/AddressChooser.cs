#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

using AddressParser.Core.Models;

#endregion



namespace AddressParser.Core
{
    public partial class Parser
    {
        List<Address> GetPossibleAddresses(List<Address> addresses)
        {
            if (addresses.Count == 0)
                return null;

            if (addresses.Count == 1)
                return addresses;

            Guid moscowGuid = GetMoscowGuid();
            if (addresses.All(addr => addr.AddrObject.Name == "московская" || addr.AoId == moscowGuid) &&
                addresses.Any(addr => addr.AoId == moscowGuid))
                return new List<Address> {addresses.First(addr => addr.AoId == moscowGuid)};

            List<Address> addressesCut = addresses;

            List<Address> allWordsFound = addresses.Where(a => a.IsAllWordsFound == true).ToList();
            if (allWordsFound.Count == 1)
                return allWordsFound;
            else if (allWordsFound.Count > 1)
            {
                //int maxH = allWordsFound.Max(a => a.HierarchyLevel);
                int maxH = allWordsFound.Max(a => (a.HouseId.HasValue ? 1 : 0) + a.HierarchyLevel);
                //List<Address> maxHAs = allWordsFound.Where(a => a.HierarchyLevel == maxH).ToList();
                List<Address> maxHAs = allWordsFound.Where(a => (a.HouseId.HasValue ? 1 : 0) + a.HierarchyLevel == maxH).ToList();

                if (maxHAs.Count == 1)
                    return maxHAs;
                addressesCut = maxHAs;
            }

            if (addressesCut.Any(a => !a.HasSkippedParent()))
                addressesCut.RemoveAll(a => a.HasSkippedParent());

            Address smoothLevelAddress = GetSmoothLevelAddress(addressesCut);
            if (smoothLevelAddress != null)
                return new List<Address>() {smoothLevelAddress};

            const int streetTypeId = 113;
            List<Address> streets = addressesCut.Where(a => a.AddrObject.Level == AddrLevel.Streets).ToList();
            if (streets.Count > 1 && streets.GroupBy(a => a.AddrObject.ParentId).Count() == 1 &&
                streets.GroupBy(a => a.AddrObject.Name).Count() == 1)
            {
                if (streets.Count(a => a.AddrObject.IsTypeExplicit) == 1)
                    return streets.Where(a => a.AddrObject.IsTypeExplicit).ToList();

                if (streets.Count(a => a.AddrObject.TypeId == streetTypeId) == 1)
                    return streets.Where(a => a.AddrObject.TypeId == 113).ToList();
            }

            int maxHierarchyLevel = addressesCut.Max(a => a.HierarchyLevel);
            List<Address> maxAddresses = addressesCut.Where(a => a.HierarchyLevel == maxHierarchyLevel).ToList();
            if (maxAddresses.Count == 1)
            {
                if (
                    addressesCut.Except(maxAddresses).All(
                        a => a.GetMinLevel() > AddrLevel.Regions || a.GetMinAddrObject().Id == maxAddresses[0].AddrObject.Id))
                    return maxAddresses;

                List<Address> regionedAddresses = addressesCut.Where(a => a.GetMinLevel() <= AddrLevel.Regions).ToList();
                List<Address> maxRegionedAddresses =
                    regionedAddresses.Where(a => a.HierarchyLevel == regionedAddresses.Max(ra => ra.HierarchyLevel)).ToList();
                if (maxRegionedAddresses.Count == 1)
                    return maxRegionedAddresses;
            }

            int maxNameLength = maxAddresses.Max(m => m.AddrObject.Name.Length);
            List<Address> maxNamedAddresses = maxAddresses.Where(ma => ma.AddrObject.Name.Length == maxNameLength).ToList();
            if (maxNamedAddresses.Count == 1)
            {
                string maxCanonicalName = NameToCanonical(maxNamedAddresses[0].AddrObject.Name);
                Func<string, string> nameCanonificator;
                if (maxCanonicalName == maxNamedAddresses[0].AddrObject.Name)
                    nameCanonificator = s => s;
                else
                    nameCanonificator = NameToCanonical;

                if (maxAddresses.All(ma => maxCanonicalName.Contains(nameCanonificator(ma.AddrObject.Name))))
                    return maxNamedAddresses;
            }

//            var minLevelId = maxAddresses.Min(a => a.GetMinLevel().Id);
//            var minLevelAddresses = maxAddresses.Where(a => a.GetMinLevel().Id == minLevelId).ToList();
//            if (minLevelAddresses.Count == 1)
//                return minLevelAddresses;

            Address incompleteAddress = GetIncompleteAddress(addresses);
            if (incompleteAddress != null)
                return new List<Address> {incompleteAddress};

            return null;
        }


        Address GetSmoothLevelAddress(List<Address> addresses)
        {
            if (addresses.Count == 0)
                return null;
            AddrLevel minLevel = addresses.Min(a => a.GetMinLevel());
            List<AddressJump> addressJumps = addresses
                .Where(a => a.GetMinLevel() == minLevel && a.AddrObject.Level != minLevel)
                .GroupBy(a => a.GetMinAddrObject())
                .SelectMany(a => a.Where(aa => aa.HierarchyLevel == a.Max(aaa => aaa.HierarchyLevel)))
                .Select(a => new AddressJump(a))
                .ToList();
            if (addressJumps.Count == 0)
                return null;

            foreach (AddressJump addressJump in addressJumps)
            {
                AddrObject addrObject = addressJump.Address.AddrObject;
                while (addrObject != null)
                {
                    addressJump.Jump += CalcJump(addrObject);
                    addrObject = addrObject.Parent;
                }
            }

            int minJump = addressJumps.Min(aj => aj.Jump);
            List<AddressJump> minAddressJumps = addressJumps.Where(aj => aj.Jump == minJump).ToList();
            if (minAddressJumps.Count == 1)
                return minAddressJumps[0].Address;

            return null;
        }


        int CalcJump(AddrObject addrObject)
        {
            if (addrObject == null)
                return 0;

            if (addrObject.Parent == null)
                return addrObject.Level.JumpValue;
            ;

            int diff = addrObject.Level.JumpValue - addrObject.Parent.Level.JumpValue;
            if (diff < 2)
                return 0;

            return diff;
        }



        class AddressJump
        {
            public int Jump;
            public readonly Address Address;


            public AddressJump(Address address)
            {
                Address = address;
            }
        }



        Address GetIncompleteAddress(List<Address> addresses)
        {
            ILookup<AddrLevel, AddrObject> incompleteAddrObjects =
                GetIncompleteAddrObjects(addresses.Where(a => a.AddrObject != null).Select(a => a.AddrObject)).ToLookup(
                    i => i.Level);
            foreach (AddrLevel level in AddrLevel.Levels.Reverse())
                if (incompleteAddrObjects.Contains(level))
                {
                    List<AddrObject> x = incompleteAddrObjects[level].ToList();
                    AddrObject singleAddrObject = x.Count == 1 ? x.Single() : null;
                    if (singleAddrObject != null &&
                        (singleAddrObject.Parent != null || !AddrLevel.StreetLevels.Contains(singleAddrObject.Level)))
                    {
                        List<Address> incompleteAddresses = addresses.Where(a => a.AddrObject == singleAddrObject).ToList();

                        if (incompleteAddresses.Count == 1)
                            return incompleteAddresses.Single();

                        List<Address> maxTypedAddresses = GetMaxTypedAddresses(incompleteAddresses);
                        if (maxTypedAddresses.Count == 1)
                            return maxTypedAddresses.Single();
                    }
                    else
                    {
                        int maxH = x.Max(xx => xx.HierarchyLevel);
                        List<AddrObject> typed = x.Where(o => o.IsTypeExplicit && o.HierarchyLevel == maxH).ToList();
                        if (typed.Count == 1)
                            return addresses.Single(a => a.AddrObject == typed[0]);
                    }
                }

            return null;
        }


        List<Address> GetMaxTypedAddresses(List<Address> addresses)
        {
            if (addresses.Count == 0)
                return new List<Address>();

            List<Tuple<int, Address>> typeAddresses = new List<Tuple<int, Address>>();
            foreach (Address possibleAddress in addresses)
            {
                int typedCount = 0;
                AddrObject addrObject = possibleAddress.AddrObject;

                while (addrObject != null)
                {
                    if (addrObject.IsTypeExplicit)
                        typedCount++;
                    addrObject = addrObject.Parent;
                }

                typeAddresses.Add(new Tuple<int, Address>(typedCount, possibleAddress));
            }

            int maxTypedCount = typeAddresses.Max(t => t.Item1);
            if (maxTypedCount == 0)
                return new List<Address>();

            List<Address> maxTyped = typeAddresses.Where(t => t.Item1 == maxTypedCount).Select(t => t.Item2).ToList();

            return maxTyped;
        }
    }
}