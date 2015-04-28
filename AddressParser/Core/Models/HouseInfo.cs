using System;
using System.Collections.ObjectModel;



namespace AddressParser.Core.Models
{
    internal class HouseInfo : IEquatable<HouseInfo>
    {
        public readonly string HouseNum;
        public readonly string BuildNum;
        public readonly string StructureNum;
        public readonly Room Room;
        static readonly char[] TrimChars = new[] {'/', '\\'};


        public HouseInfo(string houseNum, string buildNum, string structureNum, Room room)
        {
            HouseNum = !string.IsNullOrEmpty(houseNum) ? houseNum.Replace(" ", "").Trim(TrimChars) : null;
            BuildNum = !string.IsNullOrEmpty(buildNum) ? buildNum.Replace(" ", "").Trim(TrimChars) : null;
            StructureNum = !string.IsNullOrEmpty(structureNum) ? structureNum.Replace(" ", "").Trim(TrimChars) : null;
            Room = room;
        }


        public bool IsEmpty()
        {
            return HouseNum == null;
        }


        public static bool operator ==(HouseInfo first, HouseInfo second)
        {
            if (ReferenceEquals(first, null) && ReferenceEquals(second, null))
                return true;
            if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
                return false;

            return first.Equals(second);
        }


        public static bool operator !=(HouseInfo first, HouseInfo second)
        {
            return !(first == second);
        }


        public bool Equals(HouseInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(HouseNum, other.HouseNum) && string.Equals(BuildNum, other.BuildNum) && string.Equals(StructureNum, other.StructureNum) && Equals(Room, other.Room);
        }


        public bool EqualsWithoutRoom(HouseInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(HouseNum, other.HouseNum) && string.Equals(BuildNum, other.BuildNum) && string.Equals(StructureNum, other.StructureNum);
        }


        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((HouseInfo)obj);
        }


        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (HouseNum != null ? HouseNum.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (BuildNum != null ? BuildNum.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (StructureNum != null ? StructureNum.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Room != null ? Room.GetHashCode() : 0);
                return hashCode;
            }
        }


        public override string ToString()
        {
            return string.Format("House: {0}, Build: {1}, Struct: {2}, Room: {3}", HouseNum, BuildNum, StructureNum, Room);
        }
    }



    internal class Room : IEquatable<Room>
    {
        internal readonly string Name;
        internal readonly string Number;
        public Room(string name, string number)
        {
            Name = name;
            Number = number;
        }


        public static bool operator ==(Room first, Room second)
        {
            if (ReferenceEquals(first, null) && ReferenceEquals(second, null))
                return true;
            if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
                return false;

            return first.Equals(second);
        }


        public static bool operator !=(Room first, Room second)
        {
            return !(first == second);
        }


        public bool Equals(Room other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name) && string.Equals(Number, other.Number);
        }


        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Room)obj);
        }


        public override int GetHashCode()
        {
            return Number != null ? Number.GetHashCode() : 0;
        }


        public override string ToString()
        {
            return string.Format("{0}. {1}", Name, Number);
        }
    }
}