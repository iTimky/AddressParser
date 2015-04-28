using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AddressParser.Core
{
    public static class Sugar
    {
        public static void AddIfNotExists<T>(this IList<T> items, T item)
        {
            if (!items.Contains(item))
                items.Add(item);
        }

        public static void AddRangeIfNotExists<T>(this List<T> items, IEnumerable<T> itemsToAdd)
        {
            items.AddRange(itemsToAdd.ToList().Where(i => !items.Contains(i)));
        }


        public static string RegexReplace(this string input, string pattern, string replacement)
        {
            return Regex.Replace(input, pattern, replacement);
        }
    }
}
