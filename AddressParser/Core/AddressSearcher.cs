#region usings
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

using CE.Parsing.Core.Db;
using CE.Parsing.Core.Models;

#endregion



namespace CE.Parsing.Core
{
    public partial class Parser
    {
        internal IEnumerable<AddrObject> GetAddrObjects(List<NameAndType> nameAndTypes, bool isHeavy)
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

            var objects = _dataContext.GetAddrObjects(nameAndTypes, isHeavy);
            List<AddrObject> addrObjects = new List<AddrObject>(objects.Count);

            foreach (string level in levels)
            {
                List<AddrObject> levelObjects = objects.Where(o => o.Level == level).ToList();
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
                addrObjects.GroupBy(a => a.Id).SelectMany(
                    g => g.All(a => a.TypeId.HasValue) || g.All(a => !a.TypeId.HasValue) ? g : g.Where(a => a.TypeId.HasValue))
                    .ToList();

            if (addrObjects.Count < 100)
                return addrObjects;

            if (addrObjects.All(a => a.Parent == null && !a.Childs.Any()))
                return GetIncompleteAddrObjects(addrObjects, levels.Reverse().ToArray());

            addrObjects.RemoveAll(a => a.Parent == null && !a.Childs.Any());

            IEnumerable<IGrouping<Guid, AddrObject>> addrObjectsById = addrObjects.GroupBy(a => a.Id);
            List<AddrObject> result = new List<AddrObject>();
            foreach (IGrouping<Guid, AddrObject> addrObject in addrObjectsById)
            {
                int maxLength = addrObject.Max(a => a.Name.Length);
                result.Add(addrObject.First(a => a.Name.Length == maxLength));
            }

            return result;
        }

        IEnumerable<AddrObject> GetIncompleteAddrObjects(IEnumerable<AddrObject> addrObjects, string[] levels)
        {
            List<AddrObject> incompleteAddrObjects = new List<AddrObject>();

            foreach (var addressLookup in addrObjects.ToLookup(a => a.HierarchyLevel).OrderByDescending(a => a.Key))
            {
                foreach (var level in levels)
                {
                    var levelObjects = addressLookup.Where(a => a.Level == level).ToList();
                    var typed = levelObjects.Where(a => a.TypeId != null).ToList();
                    if (typed.Count == 1)
                        incompleteAddrObjects.Add(typed.Single());

                    var nonTyped = levelObjects.Where(a => a.TypeId == null).ToList();
                    if (nonTyped.Count == 1)
                        incompleteAddrObjects.Add(nonTyped.Single());
                }
            }

            return incompleteAddrObjects;
        }


        internal IEnumerable<AddrObject> FillAddrLandMarks(List<Address> addresses, IEnumerable<NameAndType> names)
        {
            var addrObjects = _dataContext.GetAddressAddrLandMarks(addresses, names);

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
            var addrHouses = _dataContext.GetAddressAddrHouses(addresses, houseInfo);

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

            var addrHouse = _dataContext.GetAddressAddrHouse(address, houseInfo);
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
            foreach (var address in addresses.Where(a => a.AoId.HasValue))
            {
                var addonAddrObject = addonAddrObjects.FirstOrDefault(ado => ado.ParentId == address.AoId);
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
            var levels = new[]
            {
                "Countries",
                "Regions",
                "Cities",
                "Streets"
            };

            List<AddrObject> addrObjects = _dataContext.GetAddonAddrObjects(nameAndTypes, isHeavy);

            foreach (string level in levels)
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
                addrObjects.GroupBy(a => a.Id).SelectMany(
                    g => g.All(a => a.TypeId.HasValue) || g.All(a => !a.TypeId.HasValue) ? g : g.Where(a => a.TypeId.HasValue))
                    .ToList();

            return addrObjects.Distinct();
        }
        #endregion


        #region AddonAddrHouses
        internal IEnumerable<AddrHouse> FillAddonAddrHouses(IEnumerable<Address> addresses, HouseInfo houseInfo)
        {
            var addrObjectAddresses = addresses.Where(a => a.AddrObject != null).ToList();
            var addrHouses = _dataContext.GetAddressAddonAddrHouses(addrObjectAddresses, houseInfo);

            foreach (var addrHouse in addrHouses)
            {
                var address = addrObjectAddresses.First(a => a.AddrObject.Id == addrHouse.ParentId);
                address.SetHouse(addrHouse);
            }

            return addrHouses;
        }
        #endregion
    }
}