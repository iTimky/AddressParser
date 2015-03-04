using System;



namespace CE.Parsing
{
    internal class AddrHouse : IEquatable<AddrHouse>
    {
        public readonly Guid Id;
        public readonly Guid ParentId;
        public readonly bool IsAddon;


        public AddrHouse(Guid id, Guid parentId, bool isAddon = false)
        {
            Id = id;
            ParentId = parentId;
            IsAddon = isAddon;
        }


        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ ParentId.GetHashCode();
        }


        public bool Equals(AddrHouse other)
        {
            return Id == other.Id && ParentId == other.ParentId;
        }


        public override string ToString()
        {
            return string.Format("Id: {0}, ParentId: {1}, IsAddon: {2}", Id, ParentId, IsAddon);
        }
    }
}