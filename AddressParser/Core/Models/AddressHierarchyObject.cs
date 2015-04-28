using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddressParser.Core.Models
{
    public class AddressHierarchyObject
    {
        public Guid Id;
        public Guid? ParentId;
        public string Name;
        public int HierarchyLevel;
    }
}
