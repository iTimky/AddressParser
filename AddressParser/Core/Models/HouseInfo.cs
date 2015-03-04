namespace CE.Parsing.Core.Models
{
    internal class HouseInfo
    {
        public string HouseNum;
        public string BuildNum;
        public string StructureNum;
        public string Room;


        public override string ToString()
        {
            return string.Format("House: {0}, Build: {1}, Struct: {2}, Room: {3}", HouseNum, BuildNum, StructureNum, Room);
        }
    }
}