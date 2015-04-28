#region usings
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using AddressParser.Core.Db;
using AddressParser.Core.Models;

#endregion



namespace AddressParser.Core
{
    public partial class Parser
    {
        internal Guid GetRussiaGuid()
        {
            return new Guid("5B15F30D-CD90-42B8-8277-20FB9C2499C0");
        }

        internal Guid GetMoscowGuid()
        {
            return new Guid("0C5B2444-70A0-4932-980C-B4DC0D3F02B5");
        }


        internal IEnumerable<AddrObject> GetAddrObjects(List<NameAndType> nameAndTypes, bool isHeavy, bool isOnlyStreet)
        {
            List<AddrObject> objects = _dataContext.GetAddrObjects(nameAndTypes, isHeavy)
                .GroupBy(a => a.Id)
                .Select(
                    g =>
                        g.All(a => a.IsTypeExplicit) || g.All(a => !a.IsTypeExplicit)
                            ? g.First()
                            : g.First(a => a.IsTypeExplicit))
                .ToList();

            List<AddrObject> addrObjects = new List<AddrObject>(objects.Count);
            foreach (var level in AddrLevel.Levels)
            {
                List<AddrObject> levelObjects = objects.Where(o => o.Level == level).ToList();
                if (levelObjects.Any())
                {
                    if (addrObjects.Any())
                        foreach (AddrObject levelObject in levelObjects)
                        {
                            if (!levelObject.ParentId.HasValue)
                                continue;

                            AddrObject parent = addrObjects.FirstOrDefault(ao => ao.Id == levelObject.ParentId);
                            if (parent == null && levelObject.ParentParentId.HasValue)
                                parent = addrObjects.FirstOrDefault(ao => ao.Id == levelObject.ParentParentId);
                            
                            if (parent == null)
                                continue;

                            levelObject.Parent = parent;
                            levelObject.HierarchyLevel = parent.HierarchyLevel + 1;
                            parent.Childs.Add(levelObject);
                        }
                    addrObjects.AddRange(levelObjects);
                }
            }

            if (addrObjects.Count < 100)
                return GetMoscowForStreetsWithoutParents(addrObjects);

            if (addrObjects.All(a => a.Parent == null && !a.Childs.Any()))
            {
                if (isOnlyStreet)
                    return GetMoscowForStreetsWithoutParents(addrObjects);

                return GetMoscowForStreetsWithoutParents(GetIncompleteAddrObjects(addrObjects));
            }

            addrObjects.RemoveAll(a => a.Parent == null && !a.Childs.Any() && a.TypeId == null);

            IEnumerable<IGrouping<Guid, AddrObject>> addrObjectsById = addrObjects.GroupBy(a => a.Id);
            List<AddrObject> result = new List<AddrObject>();
            foreach (IGrouping<Guid, AddrObject> addrObject in addrObjectsById)
            {
                int maxLength = addrObject.Max(a => a.Name.Length);
                result.Add(addrObject.First(a => a.Name.Length == maxLength));
            }

            return GetMoscowForStreetsWithoutParents(result);
        }


        IEnumerable<AddrObject> GetMoscowForStreetsWithoutParents(IEnumerable<AddrObject> addrObjects)
        {
            Guid moscowGuid = GetMoscowGuid();
            return
                addrObjects.Where(
                    a => a.Parent != null || a.ParentId == moscowGuid || !AddrLevel.StreetLevels.Contains(a.Level) || Regex.IsMatch(a.Name, RegexPatterns.MkadPattern));
        }


        internal List<AddrObject> GetIncompleteAddrObjects(IEnumerable<AddrObject> addrObjects)
        {
            List<AddrObject> incompleteAddrObjects = new List<AddrObject>();
            foreach (
                IGrouping<int, AddrObject> addressLookup in
                    addrObjects.ToLookup(a => a.HierarchyLevel).OrderByDescending(a => a.Key))
                foreach (var level in AddrLevel.Levels.Reverse())
                {
                    List<AddrObject> levelObjects = addressLookup.Where(a => a.Level == level).ToList();
                    if (levelObjects.Count == 0)
                        continue;

                    List<AddrObject> typed = levelObjects.Where(a => a.IsTypeExplicit).ToList();
                    if (typed.Count == 1 && (!IsNumericNamed(typed.First()) || AddrLevel.StreetLevels.Contains(level)))
                        incompleteAddrObjects.Add(typed.Single());

                    List<AddrObject> nonTyped = levelObjects.Where(a => !a.IsTypeExplicit).ToList();
                    if (nonTyped.Count == 1 && (!IsNumericNamed(nonTyped.First()) || AddrLevel.StreetLevels.Contains(level)))
                        incompleteAddrObjects.Add(nonTyped.Single());
                }

            return incompleteAddrObjects;
        }


        bool IsNumericNamed(AddrObject addrObject)
        {
            int checker;
            return int.TryParse(addrObject.Name, out checker);
        }


        internal IEnumerable<AddrObject> FillAddrLandMarks(List<Address> addresses, IEnumerable<NameAndType> names)
        {
            List<AddrObject> addrObjects = _dataContext.GetAddressAddrLandMarks(addresses, names);

            foreach (AddrObject addrObject in addrObjects)
            {
                Address address = addresses.First(ad => ad.AoId == addrObject.ParentId);
                addrObject.HierarchyLevel = address.HierarchyLevel + 1;
                address.SetAddrObject(addrObject);
            }

            return addrObjects;
        }


        #region AddrHouses
        internal IEnumerable<AddrHouse> FillAddrHouses(List<Address> addresses, HouseInfo houseInfo)
        {
            List<AddrHouse> addrHouses = _dataContext.GetAddressAddrHouses(addresses, houseInfo);

            foreach (IGrouping<Guid, AddrHouse> addrHouse in addrHouses.ToLookup(a => a.ParentId))
            {
                Address address = addresses.First(a => a.AoId == addrHouse.Key);
                address.SetHouse(addrHouse.First());
            }

            return addrHouses;
        }
        #endregion


        internal void FillAddressHouse(Address address, HouseInfo houseInfo)
        {
            if (address.AoId == null && address.AddonAoId == null)
                return;

            AddrHouse addrHouse = _dataContext.GetAddressAddrHouse(address, houseInfo);
            if (addrHouse != null)
                address.SetHouse(addrHouse);
        }


        #region AddonAddrObjects
        internal IEnumerable<AddrObject> FillAddonAddrObjects(List<Address> addresses, List<NameAndType> names,
            bool isHeavy)
        {
            List<AddrObject> addonAddrObjects = GetAddonAddrObjects(names, isHeavy).ToList();
            if (addresses.Any(a => a.AoId != null))
                FillAddonAddrObjectsByAddrObjects(addonAddrObjects, addresses);

            if (addresses.All(a => a.AddonAoId == null))
                addresses.AddRange(addonAddrObjects.Select(addonAo => new Address(addonAo)));

            return addonAddrObjects;
        }


        void FillAddonAddrObjectsByAddrObjects(List<AddrObject> addonAddrObjects, IEnumerable<Address> addresses)
        {
            foreach (Address address in addresses.Where(a => a.AoId.HasValue))
            {
                AddrObject addonAddrObject = addonAddrObjects.FirstOrDefault(ado => ado.ParentId == address.AoId);
                if (addonAddrObject == null)
                    continue;

                addonAddrObject.HierarchyLevel = address.AddrObject.HierarchyLevel + 1;
                addonAddrObject.Parent = address.AddrObject;
                address.AddrObject.Childs.Add(addonAddrObject);
                address.SetAddrObject(addonAddrObject);
            }
        }


        IEnumerable<AddrObject> GetAddonAddrObjects(List<NameAndType> nameAndTypes, bool isHeavy)
        {

            List<AddrObject> addrObjects = _dataContext.GetAddonAddrObjects(nameAndTypes, isHeavy);

            foreach (var level in AddrLevel.MainLevels)
            {
                List<AddrObject> levelObjects = addrObjects.Where(ao => ao.Level == level).ToList();
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

            addrObjects =
                addrObjects.GroupBy(a => a.Id).Select(
                    g =>
                        g.All(a => a.IsTypeExplicit) || g.All(a => !a.IsTypeExplicit)
                            ? g.First()
                            : g.First(a => a.IsTypeExplicit))
                    .ToList();

            return addrObjects.Distinct();
        }
        #endregion


        #region AddonAddrHouses
        internal List<AddrHouse> FillAddonAddrHouses(IEnumerable<Address> addresses, HouseInfo houseInfo)
        {
            List<Address> addrObjectAddresses = addresses.Where(a => a.AddrObject != null).ToList();
            List<AddrHouse> addrHouses = _dataContext.GetAddressAddonAddrHouses(addrObjectAddresses, houseInfo);

            foreach (AddrHouse addrHouse in addrHouses)
            {
                Address address = addrObjectAddresses.First(a => a.AddrObject.Id == addrHouse.ParentId);
                address.SetHouse(addrHouse);
            }

            return addrHouses;
        }
        #endregion
    }
}