using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;



namespace AddressParser.Core.Models
{
    public class AddrObjectType : IEquatable<AddrObjectType>
    {
        public readonly byte Id;
        public readonly string Name;
        public readonly string ShortName;
        public readonly string EngName;


        public AddrObjectType(byte id, string shortName, string name, string engName)
        {
            Id = id;
            Name = name.ToLower();
            ShortName = shortName.ToLower();
            EngName = engName.ToLower();
        }


        public static bool operator ==(AddrObjectType first, AddrObjectType second)
        {
            if (ReferenceEquals(first, null) && ReferenceEquals(second, null))
                return true;
            if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
                return false;

            return first.Equals(second);
        }


        public static bool operator !=(AddrObjectType first, AddrObjectType second)
        {
            return !(first == second);
        }


        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }


        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AddrObjectType)obj);
        }


        public bool Equals(AddrObjectType other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;// && string.Equals(Name, other.Name) && string.Equals(ShortName, other.ShortName) && string.Equals(EngName, other.EngName);
        }


        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}, {3}", Id, Name, ShortName, EngName);
        }


        #region Types
        public static readonly AddrObjectType Aal = new AddrObjectType(1, "���", "���", "Aal");
        ///<summary>����������� ����</summary>
        public static readonly AddrObjectType PostofficeBox = new AddrObjectType(2, "�/�", "����������� ����", "PostofficeBox");
        ///<summary>����������</summary>
        public static readonly AddrObjectType Autoroad = new AddrObjectType(3, "����������", "����������", "Autoroad");
        ///<summary>���������� �������</summary>
        public static readonly AddrObjectType AutonomyRegion = new AddrObjectType(4, "����", "���������� �������", "AutonomyRegion");
        ///<summary>���������� �����</summary>
        public static readonly AddrObjectType AutonomyOkrug = new AddrObjectType(5, "��", "���������� �����", "AutonomyOkrug");
        ///<summary>�����</summary>
        public static readonly AddrObjectType Alley = new AddrObjectType(6, "�����", "�����", "Alley");
        ///<summary>�����</summary>
        public static readonly AddrObjectType Arban = new AddrObjectType(7, "�����", "�����", "Arban");
        ///<summary>���</summary>
        public static readonly AddrObjectType Aul = new AddrObjectType(8, "���", "���", "Aul");
        ///<summary>�����</summary>
        public static readonly AddrObjectType Beam = new AddrObjectType(9, "�����", "�����", "Beam");
        ///<summary>�����</summary>
        public static readonly AddrObjectType Bank = new AddrObjectType(10, "�����", "�����", "Bank");
        ///<summary>�����</summary>
        public static readonly AddrObjectType Hill = new AddrObjectType(11, "�����", "�����", "Hill");
        ///<summary>�������</summary>
        public static readonly AddrObjectType Boulevard = new AddrObjectType(12, "�-�", "�������", "Boulevard");
        ///<summary>�����</summary>
        public static readonly AddrObjectType Bay = new AddrObjectType(13, "�����", "�����", "Bay");
        ///<summary>���</summary>
        public static readonly AddrObjectType Billow = new AddrObjectType(14, "���", "���", "Billow");
        ///<summary>�������</summary>
        public static readonly AddrObjectType Volost = new AddrObjectType(15, "�������", "�������", "Volost");
        ///<summary>�����</summary>
        public static readonly AddrObjectType Movingin = new AddrObjectType(16, "�����", "�����", "Movingin");
        ///<summary>�������(��)</summary>
        public static readonly AddrObjectType Vyselki = new AddrObjectType(17, "�����", "�������(��)", "Vyselki");
        ///<summary>�������-������������ ����������</summary>
        public static readonly AddrObjectType Gsk = new AddrObjectType(18, "���", "�������-������������ ����������", "Gsk");
        ///<summary>�����</summary>
        public static readonly AddrObjectType Hillock = new AddrObjectType(19, "�����", "�����", "Hillock");
        ///<summary>�����</summary>
        public static readonly AddrObjectType City = new AddrObjectType(20, "�", "�����", "City");
        ///<summary>�������</summary>
        public static readonly AddrObjectType MiniCity = new AddrObjectType(21, "�������", "�������", "MiniCity");
        ///<summary>������ �������������� �����������</summary>
        public static readonly AddrObjectType DachaNonprofitPartnership = new AddrObjectType(22, "���", "������ �������������� �����������", "DachaNonprofitPartnership");
        ///<summary>������ �������</summary>
        public static readonly AddrObjectType DachaSettlement = new AddrObjectType(23, "��", "������ �������", "DachaSettlement");
        ///<summary>�������</summary>
        public static readonly AddrObjectType Village = new AddrObjectType(24, "�", "�������", "Village");
        ///<summary>���</summary>
        public static readonly AddrObjectType House = new AddrObjectType(25, "���", "���", "House");
        ///<summary>������</summary>
        public static readonly AddrObjectType Road = new AddrObjectType(26, "���", "������", "Road");
        ///<summary>�/� �������. (��������) �����</summary>
        public static readonly AddrObjectType RailwayOvertaking = new AddrObjectType(27, "�/�_��", "�/� �������. (��������) �����", "RailwayOvertaking");
        ///<summary>��������������� �����</summary>
        public static readonly AddrObjectType RailwayBooth = new AddrObjectType(28, "�/�_�����", "��������������� �����", "RailwayBooth");
        ///<summary>��������������� �������</summary>
        public static readonly AddrObjectType RailwayCaserne = new AddrObjectType(29, "�/�_������", "��������������� �������", "RailwayCaserne");
        ///<summary>��������������� ���������</summary>
        public static readonly AddrObjectType RailwayPlatform = new AddrObjectType(30, "�/�_�����", "��������������� ���������", "RailwayPlatform");
        ///<summary>��������������� �������</summary>
        public static readonly AddrObjectType RailwayStation = new AddrObjectType(31, "�/�_��", "��������������� �������", "RailwayStation");
        ///<summary>��������������� ����</summary>
        public static readonly AddrObjectType RailwayPost = new AddrObjectType(32, "�/�_����", "��������������� ����", "RailwayPost");
        ///<summary>��������������� �������</summary>
        public static readonly AddrObjectType RailwayTravels = new AddrObjectType(33, "�/�_���", "��������������� �������", "RailwayTravels");
        ///<summary>���������������� �����</summary>
        public static readonly AddrObjectType AnimalPoint = new AddrObjectType(34, "��", "���������������� �����", "AnimalPoint");
        ///<summary>����� ����</summary>
        public static readonly AddrObjectType ResidentialZone = new AddrObjectType(35, "�������", "����� ����", "ResidentialZone");
        ///<summary>����� �����</summary>
        public static readonly AddrObjectType ResidentialRayon = new AddrObjectType(36, "��������", "����� �����", "ResidentialRayon");
        ///<summary>�����</summary>
        public static readonly AddrObjectType Zaezd = new AddrObjectType(37, "�����", "�����", "Zaezd");
        ///<summary>������</summary>
        public static readonly AddrObjectType Zaimka = new AddrObjectType(38, "������", "������", "Zaimka");
        ///<summary>����</summary>
        public static readonly AddrObjectType Zone = new AddrObjectType(39, "����", "����", "Zone");
        ///<summary>�������</summary>
        public static readonly AddrObjectType Caserne = new AddrObjectType(40, "�������", "�������", "Caserne");
        ///<summary>�����</summary>
        public static readonly AddrObjectType Canal = new AddrObjectType(41, "�����", "�����", "Canal");
        ///<summary>�������</summary>
        public static readonly AddrObjectType Quarter = new AddrObjectType(42, "��-�", "�������", "Quarter");
        ///<summary>��������</summary>
        public static readonly AddrObjectType Kilometr = new AddrObjectType(43, "��", "��������", "Kilometr");
        ///<summary>������</summary>
        public static readonly AddrObjectType Ring = new AddrObjectType(44, "������", "������", "Ring");
        ///<summary>������</summary>
        public static readonly AddrObjectType Cordon = new AddrObjectType(45, "������", "������", "Cordon");
        ///<summary>����</summary>
        public static readonly AddrObjectType Spit = new AddrObjectType(46, "����", "����", "Spit");
        ///<summary>����</summary>
        public static readonly AddrObjectType Land = new AddrObjectType(47, "����", "����", "Land");
        ///<summary>��������� �������</summary>
        public static readonly AddrObjectType ResortSettlement = new AddrObjectType(48, "��", "��������� �������", "ResortSettlement");
        ///<summary>����������</summary>
        public static readonly AddrObjectType TimberIndustryEnterprise = new AddrObjectType(49, "���", "����������", "TimberIndustryEnterprise");
        ///<summary>�����</summary>
        public static readonly AddrObjectType Line = new AddrObjectType(50, "�����", "�����", "Line");
        ///<summary>������</summary>
        public static readonly AddrObjectType Array = new AddrObjectType(51, "������", "������", "Array");
        ///<summary>����</summary>
        public static readonly AddrObjectType LightHouse = new AddrObjectType(52, "����", "����", "LightHouse");
        ///<summary>��������</summary>
        public static readonly AddrObjectType Borough = new AddrObjectType(53, "�", "��������", "Borough");
        ///<summary>���������</summary>
        public static readonly AddrObjectType Locality = new AddrObjectType(54, "���������", "���������", "Locality");
        ///<summary>����������</summary>
        public static readonly AddrObjectType Microrayon = new AddrObjectType(55, "���", "����������", "Microrayon");
        ///<summary>����</summary>
        public static readonly AddrObjectType Bridge = new AddrObjectType(56, "����", "����", "Bridge");
        ///<summary>���</summary>
        public static readonly AddrObjectType Cape = new AddrObjectType(57, "���", "���", "Cape");
        ///<summary>����������</summary>
        public static readonly AddrObjectType Quay = new AddrObjectType(58, "���", "����������", "Quay");
        ///<summary>���������� �����</summary>
        public static readonly AddrObjectType LivingSettlement = new AddrObjectType(59, "��", "���������� �����", "LivingSettlement");
        ///<summary>�������������� �����������</summary>
        public static readonly AddrObjectType NonprofitPartnership = new AddrObjectType(60, "�/�", "�������������� �����������", "NonprofitPartnership");
        ///<summary>�������</summary>
        public static readonly AddrObjectType Region = new AddrObjectType(61, "���", "�������", "Region");
        ///<summary>�����</summary>
        public static readonly AddrObjectType Okrug = new AddrObjectType(62, "�����", "�����", "Okrug");
        ///<summary>������</summary>
        public static readonly AddrObjectType Island = new AddrObjectType(63, "������", "������", "Island");
        ///<summary>����</summary>
        public static readonly AddrObjectType Park = new AddrObjectType(64, "����", "����", "Park");
        ///<summary>�������</summary>
        public static readonly AddrObjectType Crossing = new AddrObjectType(65, "�������", "�������", "Crossing");
        ///<summary>��������</summary>
        public static readonly AddrObjectType SideStreet = new AddrObjectType(66, "���", "��������", "SideStreet");
        ///<summary>������������� �����</summary>
        public static readonly AddrObjectType PlanningRayon = new AddrObjectType(67, "�/�", "������������� �����", "PlanningRayon");
        ///<summary>���������</summary>
        public static readonly AddrObjectType Platform = new AddrObjectType(68, "�����", "���������", "Platform");
        ///<summary>��������</summary>
        public static readonly AddrObjectType MiniSquare = new AddrObjectType(69, "��-��", "��������", "MiniSquare");
        ///<summary>�������</summary>
        public static readonly AddrObjectType Square = new AddrObjectType(70, "��", "�������", "Square");
        ///<summary>������</summary>
        public static readonly AddrObjectType Pogost = new AddrObjectType(71, "������", "������", "Pogost");
        ///<summary>����������</summary>
        public static readonly AddrObjectType Halt = new AddrObjectType(72, "����������", "����������", "Halt");
        ///<summary>���������</summary>
        public static readonly AddrObjectType Colony = new AddrObjectType(73, "�", "���������", "Colony");
        ///<summary>�������</summary>
        public static readonly AddrObjectType Settlement = new AddrObjectType(74, "�", "�������", "Settlement");
        ///<summary>������� ���������� ����</summary>
        public static readonly AddrObjectType CitySettlement = new AddrObjectType(75, "���", "������� ���������� ����", "CitySettlement");
        ///<summary>������� �(���) �������(�)</summary>
        public static readonly AddrObjectType StationSettlement = new AddrObjectType(76, "�/��", "������� �(���) �������(�)", "StationSettlement");
        ///<summary>�������</summary>
        public static readonly AddrObjectType Pochinok = new AddrObjectType(77, "�������", "�������", "Pochinok");
        ///<summary>�������� ���������</summary>
        public static readonly AddrObjectType PostOffice = new AddrObjectType(78, "�/�", "�������� ���������", "PostOffice");
        ///<summary>������</summary>
        public static readonly AddrObjectType MooringLine = new AddrObjectType(79, "������", "������", "MooringLine");
        ///<summary>������</summary>
        public static readonly AddrObjectType Passage = new AddrObjectType(80, "������", "������", "Passage");
        ///<summary>������������ ����</summary>
        public static readonly AddrObjectType FabricZone = new AddrObjectType(81, "��������", "������������ ����", "FabricZone");
        ///<summary>������</summary>
        public static readonly AddrObjectType Stenton = new AddrObjectType(82, "������", "������", "Stenton");
        ///<summary>�������</summary>
        public static readonly AddrObjectType Glade = new AddrObjectType(83, "�������", "�������", "Glade");
        ///<summary>��������</summary>
        public static readonly AddrObjectType Proselok = new AddrObjectType(84, "��������", "��������", "Proselok");
        ///<summary>��������</summary>
        public static readonly AddrObjectType Prospekt = new AddrObjectType(85, "��-��", "��������", "Prospekt");
        ///<summary>������</summary>
        public static readonly AddrObjectType Protok = new AddrObjectType(86, "������", "������", "Protok");
        ///<summary>�������</summary>
        public static readonly AddrObjectType Protoka = new AddrObjectType(87, "�������", "�������", "Protoka");
        ///<summary>�������</summary>
        public static readonly AddrObjectType Proulok = new AddrObjectType(88, "�������", "�������", "Proulok");
        ///<summary>������� �������</summary>
        public static readonly AddrObjectType WorkingSettlement = new AddrObjectType(89, "��", "������� �������", "WorkingSettlement");
        ///<summary>�������</summary>
        public static readonly AddrObjectType Travels = new AddrObjectType(90, "���", "�������", "Travels");
        ///<summary>�����</summary>
        public static readonly AddrObjectType Rayon = new AddrObjectType(91, "�-�", "�����", "Rayon");
        ///<summary>����������</summary>
        public static readonly AddrObjectType Republic = new AddrObjectType(92, "����", "����������", "Republic");
        ///<summary>����</summary>
        public static readonly AddrObjectType Ranks = new AddrObjectType(93, "����", "����", "Ranks");
        ///<summary>���</summary>
        public static readonly AddrObjectType Garden = new AddrObjectType(94, "���", "���", "Garden");
        ///<summary>������� �����-� ������������</summary>
        public static readonly AddrObjectType GardenNonprofitFellowship = new AddrObjectType(95, "���", "������� �����-� ������������", "GardenNonprofitFellowship");
        ///<summary>������� ������������</summary>
        public static readonly AddrObjectType GardenFellowship = new AddrObjectType(96, "���", "������� ������������", "GardenFellowship");
        ///<summary>����</summary>
        public static readonly AddrObjectType Selo = new AddrObjectType(97, "�", "����", "Selo");
        ///<summary>�������� �������������</summary>
        public static readonly AddrObjectType RuralAdmin = new AddrObjectType(98, "�/�", "�������� �������������", "RuralAdmin");
        ///<summary>�������� �����</summary>
        public static readonly AddrObjectType RuralOkrug = new AddrObjectType(99, "�/�", "�������� �����", "RuralOkrug");
        ///<summary>�������� ������������� ������</summary>
        public static readonly AddrObjectType RuralMunicipal = new AddrObjectType(100, "�/��", "�������� ������������� ������", "RuralMunicipal");
        ///<summary>�������� ���������</summary>
        public static readonly AddrObjectType RuralSettlement = new AddrObjectType(101, "�/�", "�������� ���������", "RuralSettlement");
        ///<summary>���������</summary>
        public static readonly AddrObjectType SelSovet = new AddrObjectType(102, "�/�", "���������", "SelSovet");
        ///<summary>�����</summary>
        public static readonly AddrObjectType PublicGarden = new AddrObjectType(103, "�����", "�����", "PublicGarden");
        ///<summary>�������</summary>
        public static readonly AddrObjectType Sloboda = new AddrObjectType(104, "��", "�������", "Sloboda");
        ///<summary>�����</summary>
        public static readonly AddrObjectType Descent = new AddrObjectType(105, "�����", "�����", "Descent");
        ///<summary>�������</summary>
        public static readonly AddrObjectType Stanitsa = new AddrObjectType(106, "��-��", "�������", "Stanitsa");
        ///<summary>�������</summary>
        public static readonly AddrObjectType Station = new AddrObjectType(107, "��", "�������", "Station");
        ///<summary>������</summary>
        public static readonly AddrObjectType Country = new AddrObjectType(122, "������", "������", "Country");
        ///<summary>��������</summary>
        public static readonly AddrObjectType Building = new AddrObjectType(108, "���", "��������", "Building");
        ///<summary>����������</summary>
        public static readonly AddrObjectType Territory = new AddrObjectType(109, "���", "����������", "Territory");
        ///<summary>�������</summary>
        public static readonly AddrObjectType Tunnel = new AddrObjectType(110, "�������", "�������", "Tunnel");
        ///<summary>�����</summary>
        public static readonly AddrObjectType Highroad = new AddrObjectType(111, "�����", "�����", "Highroad");
        ///<summary>�����</summary>
        public static readonly AddrObjectType Deadend = new AddrObjectType(112, "���", "�����", "Deadend");
        ///<summary>�����</summary>
        public static readonly AddrObjectType Street = new AddrObjectType(113, "��", "�����", "Street");
        ///<summary>����</summary>
        public static readonly AddrObjectType Ulus = new AddrObjectType(114, "�", "����", "Ulus");
        ///<summary>�������</summary>
        public static readonly AddrObjectType Parcel = new AddrObjectType(115, "��-�", "�������", "Parcel");
        ///<summary>�����</summary>
        public static readonly AddrObjectType Farm = new AddrObjectType(116, "�����", "�����", "Farm");
        ///<summary>���������� ���������</summary>
        public static readonly AddrObjectType FarmEconomy = new AddrObjectType(117, "�/�", "���������� ���������", "FarmEconomy");
        ///<summary>�����</summary>
        public static readonly AddrObjectType Khutor = new AddrObjectType(118, "�", "�����", "Khutor");
        ///<summary>�������</summary>
        public static readonly AddrObjectType Chuvashia = new AddrObjectType(119, "�������", "�������", "Chuvashia");
        ///<summary>�����</summary>
        public static readonly AddrObjectType Highway = new AddrObjectType(120, "�", "�����", "Highway");
        ///<summary>����</summary>
        public static readonly AddrObjectType State = new AddrObjectType(123, "����", "����", "State");
        ///<summary>��������</summary>
        public static readonly AddrObjectType Overpass = new AddrObjectType(121, "��������", "��������", "Overpass");


        public static AddrObjectType Get(byte id)
        {
            return AllDictionary[id];
        }

        public static readonly ReadOnlyCollection<AddrObjectType> All =
            new ReadOnlyCollection<AddrObjectType>(new List<AddrObjectType> { Aal, PostofficeBox, Autoroad, AutonomyRegion, AutonomyOkrug, Alley, Arban, Aul, Beam, Bank, Hill, Boulevard, Bay, Billow, Volost, Movingin, Vyselki, Gsk, Hillock, City, MiniCity, DachaNonprofitPartnership, DachaSettlement, Village, House, Road, RailwayOvertaking, RailwayBooth, RailwayCaserne, RailwayPlatform, RailwayStation, RailwayPost, RailwayTravels, AnimalPoint, ResidentialZone, ResidentialRayon, Zaezd, Zaimka, Zone, Caserne, Canal, Quarter, Kilometr, Ring, Cordon, Spit, Land, ResortSettlement, TimberIndustryEnterprise, Line, Array, LightHouse, Borough, Locality, Microrayon, Bridge, Cape, Quay, LivingSettlement, NonprofitPartnership, Region, Okrug, Island, Park, Crossing, SideStreet, PlanningRayon, Platform, MiniSquare, Square, Pogost, Halt, Colony, Settlement, CitySettlement, StationSettlement, Pochinok, PostOffice, MooringLine, Passage, FabricZone, Stenton, Glade, Proselok, Prospekt, Protok, Protoka, Proulok, WorkingSettlement, Travels, Rayon, Republic, Ranks, Garden, GardenNonprofitFellowship, GardenFellowship, Selo, RuralAdmin, RuralOkrug, RuralMunicipal, RuralSettlement, SelSovet, PublicGarden, Sloboda, Descent, Stanitsa, Station, Country, Building, Territory, Tunnel, Highroad, Deadend, Street, Ulus, Parcel, Farm, FarmEconomy, Khutor, Chuvashia, Highway, State, Overpass });


        static readonly ReadOnlyDictionary<byte, AddrObjectType> AllDictionary =
            new ReadOnlyDictionary<byte, AddrObjectType>(All.ToDictionary(aot => aot.Id));


        public static readonly ReadOnlyCollection<AddrObjectType> HouseParents =
            new ReadOnlyCollection<AddrObjectType>(new List<AddrObjectType> { Aal, Alley, AnimalPoint, Aul, Bank, Bay, Beam, Billow, Borough, Boulevard, Bridge, Building, Canal, Caserne, Colony, Crossing, Deadend, Descent, Farm, Garden, GardenNonprofitFellowship, Glade, Gsk, Halt, Highroad, Highway, Hill, Hillock, Island, Khutor, Kilometr, LightHouse, Line, LivingSettlement, Locality, Microrayon, MiniCity, MiniSquare, Movingin, Parcel, Park, Passage, PlanningRayon, Platform, PostOffice, PostofficeBox, Proselok, Prospekt, Protok, Proulok, PublicGarden, Quarter, Quay, RailwayBooth, RailwayCaserne, RailwayOvertaking, RailwayPlatform, RailwayPost, RailwayStation, RailwayTravels, Ranks, Ring, Road, Selo, SideStreet, Sloboda, Spit, Square, Station, StationSettlement, Stenton, Street, Territory, TimberIndustryEnterprise, Travels, Tunnel, Village, Vyselki, Zaezd, Zone });

        #endregion
    }
}