using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AddressParser.Core.Models
{
    public class AddrObjectName : IEquatable<AddrObjectName>
    {
        public readonly string Name;
        public readonly string OriginalName;
        string _canonicalName;
        public string CanonicalName {get { return MakeCanonicalName(); }}


        public AddrObjectName(string name, string origin)
        {
            Name = name;
            OriginalName = origin;
        }


        public bool IsOld { get; private set; }
        public bool IsNew { get; private set; }
        public bool IsLower { get; private set; }
        public bool IsMiddle { get; private set; }
        public bool IsUpper { get; private set; }
        public bool IsSmall { get; private set; }
        public bool IsBig { get; private set; }


        string MakeCanonicalName()
        {
            if (_canonicalName != null)
                return _canonicalName;

            _canonicalName = Name;

            if (SetCanon(RegexPatterns.OldPattern, "с."))
                IsOld = true;
            if (SetCanon(RegexPatterns.NewPattern, "н."))
                IsNew = true;
            if (SetCanon(RegexPatterns.LowerPattern, "н."))
                IsLower = true;
            if (SetCanon(RegexPatterns.MiddlePattern, "ср."))
                IsMiddle = true;
            if (SetCanon(RegexPatterns.UpperPattern, "в."))
                IsUpper = true;
            if (SetCanon(RegexPatterns.SmallPattern, "м."))
                IsSmall = true;
            if (SetCanon(RegexPatterns.BigPattern, "б."))
                IsBig = true;

            if (Regex.IsMatch(_canonicalName, RegexPatterns.ReplacedNumberPattern))
            {
                string num = Regex.Match(_canonicalName, RegexPatterns.ReplacedNumberPattern).Groups["n"].Value;
                _canonicalName = num + " " + Regex.Replace(_canonicalName.Replace(num, " ").Trim(), RegexPatterns.SpacePattern, " ");
            }
            else if (Regex.IsMatch(_canonicalName, RegexPatterns.NumberPattern))
            {
                var number = Regex.Match(_canonicalName, RegexPatterns.NumberPattern).Groups["n"].Value;
                var trimmedNumber = number.Trim(_numberTrimChars);
                var y = trimmedNumber + "-й";
                _canonicalName = y + " " + Regex.Replace(_canonicalName.Replace(number, " ").Trim(), RegexPatterns.SpacePattern, " ");
            }

            _canonicalName = _canonicalName.Replace(".", " ").Trim();
            return _canonicalName;
        }
        static readonly char[] _numberTrimChars = {'-', ' '};


        bool SetCanon(string pattern, string canon)
        {
            if (Regex.IsMatch(_canonicalName, pattern))
            {
                _canonicalName = Regex.Replace(_canonicalName, pattern, " ").Trim() + " " + canon;
                return true;
            }

            return false;
        }

        


        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }


        public bool Equals(AddrObjectName other)
        {
            if (other == null)
                return false;

            return Name == other.Name;
        }


        public static bool operator ==(AddrObjectName first, AddrObjectName second)
        {
            if (ReferenceEquals(first, null) && ReferenceEquals(second, null))
                return true;
            if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
                return false;

            return first.Equals(second);
        }


        public static bool operator !=(AddrObjectName first, AddrObjectName second)
        {
            return !(first == second);
        }


        public override string ToString()
        {
            return Name;
        }
    }
}
