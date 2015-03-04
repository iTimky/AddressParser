using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;



namespace CE.Parsing.Core.Models
{
    internal class AddressPart : IEquatable<AddressPart>
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
                var trimChars = new[] { ' ', '.' };
                var spaceChars = new[] { ' ' };

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
                    hash = hash * 31 + variation.GetHashCode();
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
            return Equals((AddressPart)obj);
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
}