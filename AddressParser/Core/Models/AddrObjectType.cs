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
        public static readonly AddrObjectType Aal = new AddrObjectType(1, "аал", "Аал", "Aal");
        ///<summary>Абонентский ящик</summary>
        public static readonly AddrObjectType PostofficeBox = new AddrObjectType(2, "а/я", "Абонентский ящик", "PostofficeBox");
        ///<summary>Автодорога</summary>
        public static readonly AddrObjectType Autoroad = new AddrObjectType(3, "автодорога", "Автодорога", "Autoroad");
        ///<summary>Автономная область</summary>
        public static readonly AddrObjectType AutonomyRegion = new AddrObjectType(4, "Аобл", "Автономная область", "AutonomyRegion");
        ///<summary>Автономный округ</summary>
        public static readonly AddrObjectType AutonomyOkrug = new AddrObjectType(5, "АО", "Автономный округ", "AutonomyOkrug");
        ///<summary>Аллея</summary>
        public static readonly AddrObjectType Alley = new AddrObjectType(6, "аллея", "Аллея", "Alley");
        ///<summary>Арбан</summary>
        public static readonly AddrObjectType Arban = new AddrObjectType(7, "арбан", "Арбан", "Arban");
        ///<summary>Аул</summary>
        public static readonly AddrObjectType Aul = new AddrObjectType(8, "аул", "Аул", "Aul");
        ///<summary>Балка</summary>
        public static readonly AddrObjectType Beam = new AddrObjectType(9, "балка", "Балка", "Beam");
        ///<summary>Берег</summary>
        public static readonly AddrObjectType Bank = new AddrObjectType(10, "берег", "Берег", "Bank");
        ///<summary>Бугор</summary>
        public static readonly AddrObjectType Hill = new AddrObjectType(11, "бугор", "Бугор", "Hill");
        ///<summary>Бульвар</summary>
        public static readonly AddrObjectType Boulevard = new AddrObjectType(12, "б-р", "Бульвар", "Boulevard");
        ///<summary>Бухта</summary>
        public static readonly AddrObjectType Bay = new AddrObjectType(13, "бухта", "Бухта", "Bay");
        ///<summary>Вал</summary>
        public static readonly AddrObjectType Billow = new AddrObjectType(14, "вал", "Вал", "Billow");
        ///<summary>Волость</summary>
        public static readonly AddrObjectType Volost = new AddrObjectType(15, "волость", "Волость", "Volost");
        ///<summary>Въезд</summary>
        public static readonly AddrObjectType Movingin = new AddrObjectType(16, "въезд", "Въезд", "Movingin");
        ///<summary>Выселки(ок)</summary>
        public static readonly AddrObjectType Vyselki = new AddrObjectType(17, "высел", "Выселки(ок)", "Vyselki");
        ///<summary>Гаражно-строительный кооператив</summary>
        public static readonly AddrObjectType Gsk = new AddrObjectType(18, "гск", "Гаражно-строительный кооператив", "Gsk");
        ///<summary>Горка</summary>
        public static readonly AddrObjectType Hillock = new AddrObjectType(19, "горка", "Горка", "Hillock");
        ///<summary>Город</summary>
        public static readonly AddrObjectType City = new AddrObjectType(20, "г", "Город", "City");
        ///<summary>Городок</summary>
        public static readonly AddrObjectType MiniCity = new AddrObjectType(21, "городок", "Городок", "MiniCity");
        ///<summary>Дачное некоммерческое партнерство</summary>
        public static readonly AddrObjectType DachaNonprofitPartnership = new AddrObjectType(22, "днп", "Дачное некоммерческое партнерство", "DachaNonprofitPartnership");
        ///<summary>Дачный поселок</summary>
        public static readonly AddrObjectType DachaSettlement = new AddrObjectType(23, "дп", "Дачный поселок", "DachaSettlement");
        ///<summary>Деревня</summary>
        public static readonly AddrObjectType Village = new AddrObjectType(24, "д", "Деревня", "Village");
        ///<summary>Дом</summary>
        public static readonly AddrObjectType House = new AddrObjectType(25, "ДОМ", "Дом", "House");
        ///<summary>Дорога</summary>
        public static readonly AddrObjectType Road = new AddrObjectType(26, "дор", "Дорога", "Road");
        ///<summary>ж/д останов. (обгонный) пункт</summary>
        public static readonly AddrObjectType RailwayOvertaking = new AddrObjectType(27, "ж/д_оп", "ж/д останов. (обгонный) пункт", "RailwayOvertaking");
        ///<summary>Железнодорожная будка</summary>
        public static readonly AddrObjectType RailwayBooth = new AddrObjectType(28, "ж/д_будка", "Железнодорожная будка", "RailwayBooth");
        ///<summary>Железнодорожная казарма</summary>
        public static readonly AddrObjectType RailwayCaserne = new AddrObjectType(29, "ж/д_казарм", "Железнодорожная казарма", "RailwayCaserne");
        ///<summary>Железнодорожная платформа</summary>
        public static readonly AddrObjectType RailwayPlatform = new AddrObjectType(30, "ж/д_платф", "Железнодорожная платформа", "RailwayPlatform");
        ///<summary>Железнодорожная станция</summary>
        public static readonly AddrObjectType RailwayStation = new AddrObjectType(31, "ж/д_ст", "Железнодорожная станция", "RailwayStation");
        ///<summary>Железнодорожный пост</summary>
        public static readonly AddrObjectType RailwayPost = new AddrObjectType(32, "ж/д_пост", "Железнодорожный пост", "RailwayPost");
        ///<summary>Железнодорожный разъезд</summary>
        public static readonly AddrObjectType RailwayTravels = new AddrObjectType(33, "ж/д_рзд", "Железнодорожный разъезд", "RailwayTravels");
        ///<summary>Животноводческая точка</summary>
        public static readonly AddrObjectType AnimalPoint = new AddrObjectType(34, "жт", "Животноводческая точка", "AnimalPoint");
        ///<summary>Жилая зона</summary>
        public static readonly AddrObjectType ResidentialZone = new AddrObjectType(35, "жилзона", "Жилая зона", "ResidentialZone");
        ///<summary>Жилой район</summary>
        public static readonly AddrObjectType ResidentialRayon = new AddrObjectType(36, "жилрайон", "Жилой район", "ResidentialRayon");
        ///<summary>Заезд</summary>
        public static readonly AddrObjectType Zaezd = new AddrObjectType(37, "заезд", "Заезд", "Zaezd");
        ///<summary>Заимка</summary>
        public static readonly AddrObjectType Zaimka = new AddrObjectType(38, "заимка", "Заимка", "Zaimka");
        ///<summary>Зона</summary>
        public static readonly AddrObjectType Zone = new AddrObjectType(39, "зона", "Зона", "Zone");
        ///<summary>Казарма</summary>
        public static readonly AddrObjectType Caserne = new AddrObjectType(40, "казарма", "Казарма", "Caserne");
        ///<summary>Канал</summary>
        public static readonly AddrObjectType Canal = new AddrObjectType(41, "канал", "Канал", "Canal");
        ///<summary>Квартал</summary>
        public static readonly AddrObjectType Quarter = new AddrObjectType(42, "кв-л", "Квартал", "Quarter");
        ///<summary>Километр</summary>
        public static readonly AddrObjectType Kilometr = new AddrObjectType(43, "км", "Километр", "Kilometr");
        ///<summary>Кольцо</summary>
        public static readonly AddrObjectType Ring = new AddrObjectType(44, "кольцо", "Кольцо", "Ring");
        ///<summary>Кордон</summary>
        public static readonly AddrObjectType Cordon = new AddrObjectType(45, "кордон", "Кордон", "Cordon");
        ///<summary>Коса</summary>
        public static readonly AddrObjectType Spit = new AddrObjectType(46, "коса", "Коса", "Spit");
        ///<summary>Край</summary>
        public static readonly AddrObjectType Land = new AddrObjectType(47, "край", "Край", "Land");
        ///<summary>Курортный поселок</summary>
        public static readonly AddrObjectType ResortSettlement = new AddrObjectType(48, "кп", "Курортный поселок", "ResortSettlement");
        ///<summary>Леспромхоз</summary>
        public static readonly AddrObjectType TimberIndustryEnterprise = new AddrObjectType(49, "лпх", "Леспромхоз", "TimberIndustryEnterprise");
        ///<summary>Линия</summary>
        public static readonly AddrObjectType Line = new AddrObjectType(50, "линия", "Линия", "Line");
        ///<summary>Массив</summary>
        public static readonly AddrObjectType Array = new AddrObjectType(51, "массив", "Массив", "Array");
        ///<summary>Маяк</summary>
        public static readonly AddrObjectType LightHouse = new AddrObjectType(52, "маяк", "Маяк", "LightHouse");
        ///<summary>Местечко</summary>
        public static readonly AddrObjectType Borough = new AddrObjectType(53, "м", "Местечко", "Borough");
        ///<summary>Местность</summary>
        public static readonly AddrObjectType Locality = new AddrObjectType(54, "местность", "Местность", "Locality");
        ///<summary>Микрорайон</summary>
        public static readonly AddrObjectType Microrayon = new AddrObjectType(55, "мкр", "Микрорайон", "Microrayon");
        ///<summary>Мост</summary>
        public static readonly AddrObjectType Bridge = new AddrObjectType(56, "мост", "Мост", "Bridge");
        ///<summary>Мыс</summary>
        public static readonly AddrObjectType Cape = new AddrObjectType(57, "мыс", "Мыс", "Cape");
        ///<summary>Набережная</summary>
        public static readonly AddrObjectType Quay = new AddrObjectType(58, "наб", "Набережная", "Quay");
        ///<summary>Населенный пункт</summary>
        public static readonly AddrObjectType LivingSettlement = new AddrObjectType(59, "нп", "Населенный пункт", "LivingSettlement");
        ///<summary>Некоммерческое партнерство</summary>
        public static readonly AddrObjectType NonprofitPartnership = new AddrObjectType(60, "н/п", "Некоммерческое партнерство", "NonprofitPartnership");
        ///<summary>Область</summary>
        public static readonly AddrObjectType Region = new AddrObjectType(61, "обл", "Область", "Region");
        ///<summary>Округ</summary>
        public static readonly AddrObjectType Okrug = new AddrObjectType(62, "округ", "Округ", "Okrug");
        ///<summary>Остров</summary>
        public static readonly AddrObjectType Island = new AddrObjectType(63, "остров", "Остров", "Island");
        ///<summary>Парк</summary>
        public static readonly AddrObjectType Park = new AddrObjectType(64, "парк", "Парк", "Park");
        ///<summary>Переезд</summary>
        public static readonly AddrObjectType Crossing = new AddrObjectType(65, "переезд", "Переезд", "Crossing");
        ///<summary>Переулок</summary>
        public static readonly AddrObjectType SideStreet = new AddrObjectType(66, "пер", "Переулок", "SideStreet");
        ///<summary>Планировочный район</summary>
        public static readonly AddrObjectType PlanningRayon = new AddrObjectType(67, "п/р", "Планировочный район", "PlanningRayon");
        ///<summary>Платформа</summary>
        public static readonly AddrObjectType Platform = new AddrObjectType(68, "платф", "Платформа", "Platform");
        ///<summary>Площадка</summary>
        public static readonly AddrObjectType MiniSquare = new AddrObjectType(69, "пл-ка", "Площадка", "MiniSquare");
        ///<summary>Площадь</summary>
        public static readonly AddrObjectType Square = new AddrObjectType(70, "пл", "Площадь", "Square");
        ///<summary>Погост</summary>
        public static readonly AddrObjectType Pogost = new AddrObjectType(71, "погост", "Погост", "Pogost");
        ///<summary>Полустанок</summary>
        public static readonly AddrObjectType Halt = new AddrObjectType(72, "полустанок", "Полустанок", "Halt");
        ///<summary>Поселение</summary>
        public static readonly AddrObjectType Colony = new AddrObjectType(73, "п", "Поселение", "Colony");
        ///<summary>Поселок</summary>
        public static readonly AddrObjectType Settlement = new AddrObjectType(74, "п", "Поселок", "Settlement");
        ///<summary>Поселок городского типа</summary>
        public static readonly AddrObjectType CitySettlement = new AddrObjectType(75, "пгт", "Поселок городского типа", "CitySettlement");
        ///<summary>Поселок и(при) станция(и)</summary>
        public static readonly AddrObjectType StationSettlement = new AddrObjectType(76, "п/ст", "Поселок и(при) станция(и)", "StationSettlement");
        ///<summary>Починок</summary>
        public static readonly AddrObjectType Pochinok = new AddrObjectType(77, "починок", "Починок", "Pochinok");
        ///<summary>Почтовое отделение</summary>
        public static readonly AddrObjectType PostOffice = new AddrObjectType(78, "п/о", "Почтовое отделение", "PostOffice");
        ///<summary>Причал</summary>
        public static readonly AddrObjectType MooringLine = new AddrObjectType(79, "причал", "Причал", "MooringLine");
        ///<summary>Проезд</summary>
        public static readonly AddrObjectType Passage = new AddrObjectType(80, "проезд", "Проезд", "Passage");
        ///<summary>Промышленная зона</summary>
        public static readonly AddrObjectType FabricZone = new AddrObjectType(81, "промзона", "Промышленная зона", "FabricZone");
        ///<summary>Просек</summary>
        public static readonly AddrObjectType Stenton = new AddrObjectType(82, "просек", "Просек", "Stenton");
        ///<summary>Просека</summary>
        public static readonly AddrObjectType Glade = new AddrObjectType(83, "просека", "Просека", "Glade");
        ///<summary>Проселок</summary>
        public static readonly AddrObjectType Proselok = new AddrObjectType(84, "проселок", "Проселок", "Proselok");
        ///<summary>Проспект</summary>
        public static readonly AddrObjectType Prospekt = new AddrObjectType(85, "пр-кт", "Проспект", "Prospekt");
        ///<summary>Проток</summary>
        public static readonly AddrObjectType Protok = new AddrObjectType(86, "проток", "Проток", "Protok");
        ///<summary>Протока</summary>
        public static readonly AddrObjectType Protoka = new AddrObjectType(87, "протока", "Протока", "Protoka");
        ///<summary>Проулок</summary>
        public static readonly AddrObjectType Proulok = new AddrObjectType(88, "проулок", "Проулок", "Proulok");
        ///<summary>Рабочий поселок</summary>
        public static readonly AddrObjectType WorkingSettlement = new AddrObjectType(89, "рп", "Рабочий поселок", "WorkingSettlement");
        ///<summary>Разъезд</summary>
        public static readonly AddrObjectType Travels = new AddrObjectType(90, "рзд", "Разъезд", "Travels");
        ///<summary>Район</summary>
        public static readonly AddrObjectType Rayon = new AddrObjectType(91, "р-н", "Район", "Rayon");
        ///<summary>Республика</summary>
        public static readonly AddrObjectType Republic = new AddrObjectType(92, "Респ", "Республика", "Republic");
        ///<summary>Ряды</summary>
        public static readonly AddrObjectType Ranks = new AddrObjectType(93, "ряды", "Ряды", "Ranks");
        ///<summary>Сад</summary>
        public static readonly AddrObjectType Garden = new AddrObjectType(94, "сад", "Сад", "Garden");
        ///<summary>Садовое неком-е товарищество</summary>
        public static readonly AddrObjectType GardenNonprofitFellowship = new AddrObjectType(95, "снт", "Садовое неком-е товарищество", "GardenNonprofitFellowship");
        ///<summary>Садовое товарищество</summary>
        public static readonly AddrObjectType GardenFellowship = new AddrObjectType(96, "снт", "Садовое товарищество", "GardenFellowship");
        ///<summary>Село</summary>
        public static readonly AddrObjectType Selo = new AddrObjectType(97, "с", "Село", "Selo");
        ///<summary>Сельская администрация</summary>
        public static readonly AddrObjectType RuralAdmin = new AddrObjectType(98, "с/а", "Сельская администрация", "RuralAdmin");
        ///<summary>Сельский округ</summary>
        public static readonly AddrObjectType RuralOkrug = new AddrObjectType(99, "с/о", "Сельский округ", "RuralOkrug");
        ///<summary>Сельское муниципальное образо</summary>
        public static readonly AddrObjectType RuralMunicipal = new AddrObjectType(100, "с/мо", "Сельское муниципальное образо", "RuralMunicipal");
        ///<summary>Сельское поселение</summary>
        public static readonly AddrObjectType RuralSettlement = new AddrObjectType(101, "с/п", "Сельское поселение", "RuralSettlement");
        ///<summary>Сельсовет</summary>
        public static readonly AddrObjectType SelSovet = new AddrObjectType(102, "с/с", "Сельсовет", "SelSovet");
        ///<summary>Сквер</summary>
        public static readonly AddrObjectType PublicGarden = new AddrObjectType(103, "сквер", "Сквер", "PublicGarden");
        ///<summary>Слобода</summary>
        public static readonly AddrObjectType Sloboda = new AddrObjectType(104, "сл", "Слобода", "Sloboda");
        ///<summary>Спуск</summary>
        public static readonly AddrObjectType Descent = new AddrObjectType(105, "спуск", "Спуск", "Descent");
        ///<summary>Станица</summary>
        public static readonly AddrObjectType Stanitsa = new AddrObjectType(106, "ст-ца", "Станица", "Stanitsa");
        ///<summary>Станция</summary>
        public static readonly AddrObjectType Station = new AddrObjectType(107, "ст", "Станция", "Station");
        ///<summary>Страна</summary>
        public static readonly AddrObjectType Country = new AddrObjectType(122, "страна", "Страна", "Country");
        ///<summary>Строение</summary>
        public static readonly AddrObjectType Building = new AddrObjectType(108, "стр", "Строение", "Building");
        ///<summary>Территория</summary>
        public static readonly AddrObjectType Territory = new AddrObjectType(109, "тер", "Территория", "Territory");
        ///<summary>тоннель</summary>
        public static readonly AddrObjectType Tunnel = new AddrObjectType(110, "тоннель", "тоннель", "Tunnel");
        ///<summary>Тракт</summary>
        public static readonly AddrObjectType Highroad = new AddrObjectType(111, "тракт", "Тракт", "Highroad");
        ///<summary>Тупик</summary>
        public static readonly AddrObjectType Deadend = new AddrObjectType(112, "туп", "Тупик", "Deadend");
        ///<summary>Улица</summary>
        public static readonly AddrObjectType Street = new AddrObjectType(113, "ул", "Улица", "Street");
        ///<summary>Улус</summary>
        public static readonly AddrObjectType Ulus = new AddrObjectType(114, "у", "Улус", "Ulus");
        ///<summary>Участок</summary>
        public static readonly AddrObjectType Parcel = new AddrObjectType(115, "уч-к", "Участок", "Parcel");
        ///<summary>Ферма</summary>
        public static readonly AddrObjectType Farm = new AddrObjectType(116, "ферма", "Ферма", "Farm");
        ///<summary>Фермерское хозяйство</summary>
        public static readonly AddrObjectType FarmEconomy = new AddrObjectType(117, "ф/х", "Фермерское хозяйство", "FarmEconomy");
        ///<summary>Хутор</summary>
        public static readonly AddrObjectType Khutor = new AddrObjectType(118, "х", "Хутор", "Khutor");
        ///<summary>Чувашия</summary>
        public static readonly AddrObjectType Chuvashia = new AddrObjectType(119, "Чувашия", "Чувашия", "Chuvashia");
        ///<summary>Шоссе</summary>
        public static readonly AddrObjectType Highway = new AddrObjectType(120, "ш", "Шоссе", "Highway");
        ///<summary>Штат</summary>
        public static readonly AddrObjectType State = new AddrObjectType(123, "штат", "Штат", "State");
        ///<summary>эстакада</summary>
        public static readonly AddrObjectType Overpass = new AddrObjectType(121, "эстакада", "эстакада", "Overpass");


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