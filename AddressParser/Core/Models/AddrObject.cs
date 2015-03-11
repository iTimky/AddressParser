#region usings
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

#endregion



namespace CE.Parsing.Core.Models
{
    public class AddrObject : IEquatable<AddrObject>
    {
        public readonly Guid Id;
        public readonly Guid? ParentId;
        public readonly List<AddrObject> Childs = new List<AddrObject>();
        public AddrObject Parent;
        public readonly string Name;
        public readonly byte? TypeId;
        public readonly string Level;
        public int HierarchyLevel;
        public readonly AddrObjectKind Kind;


        public AddrObject(Guid id, AddrObjectKind kind = AddrObjectKind.AddrObjectCurrent)
        {
            Id = id;
            Kind = kind;
        }

        public AddrObject(SqlDataReader reader, AddrObjectKind kind = AddrObjectKind.AddrObjectCurrent)
        {
            int colCount = reader.FieldCount;
            Id = reader.GetGuid(0);
            ParentId = colCount > 1 && !reader.IsDBNull(1) ? reader.GetGuid(1) : (Guid?) null;
            Name = colCount > 2 && !reader.IsDBNull(2) ? reader.GetString(2) : null;
            TypeId = colCount > 3 && !reader.IsDBNull(3) ? reader.GetByte(3) : (byte?) null;
            Level = colCount > 4 && !reader.IsDBNull(4) ? reader.GetString(4) : null;
            Kind = kind;
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
            return Id.GetHashCode() ^ (ParentId.HasValue ? ParentId.GetHashCode() : 0) ^ (TypeId.HasValue ? TypeId.Value : 0);
        }


        public bool Equals(AddrObject other)
        {
            return Id == other.Id && ParentId == other.ParentId && TypeId == other.TypeId;
        }


        public override string ToString()
        {
            return string.Format("Id: {0}, ParentId: {1}, Name: {2}, TypeId: {3}, Level: {4}, HierarchyLevel: {5}, Kind: {6}", Id,
                ParentId, Name, TypeId, Level, HierarchyLevel, Enum.GetName(typeof (AddrObjectKind), Kind));
        }
    }
}