namespace AddressParser.Core
{
    public static class RegexPatterns
    {
        public const string StartPattern = @"(?:^|[\,\s\.\;\)\(]+)";
        public const string EndPattern = @"(?:$|[\,\s\.\;\)\(]+)";

        public const string PostalCodePattern = @"(?<pCode>[0-9]{5,7})";

        #region Crutch
        public const string BigPattern = StartPattern + @"(большой|большая|большое|бол|б)" + EndPattern;
        public const string SmallPattern = StartPattern + @"(малый|малая|малое|мал|м)" + EndPattern;
        public const string UpperPattern = StartPattern + @"(верхний|верхняя|верхнее|верхн|верх|в\.)" + EndPattern;
        public const string MiddlePattern = StartPattern + @"(средний|средняя|среднее|сред|ср\.)" + EndPattern;
        public const string LowerPattern = StartPattern + @"(нижний|нижняя|нижнее|нижн|ниж|н)" + EndPattern;
        public const string OldPattern = StartPattern + @"(старый|старая|старое|стар|с\.)" + EndPattern;
        public const string NewPattern = StartPattern + @"(новый|новая|новое|нов|н)" + EndPattern;
        public const string NumberWordPattern = @"(?<n>\d+)(?<w>[a-zA-Zа-яА-ЯёЁ]+)";
        public const string WordNumberPattern = @"(?<w>[a-zA-Zа-яА-ЯёЁ]+)(?<n>\d+)";
        public const string NumberPattern = StartPattern + @"(?<n>\d+\-?)" + @"(?:(?<=\-)|" + EndPattern + ")";
        public const string ReplacedNumberPattern = @"(?<n>\d+\-[яйе])";
        public const string SpacePattern = @"\s+";
        public const string UmlautLowerPattern = "ё";
        public const string UmlautUpperPattern = "Ё";
        public const string MoscowPattern = StartPattern + "москва" + EndPattern;
        public const string KilometerPattern = StartPattern + @"(?<n>\d*)(?<km>км)" + EndPattern;
        public const string MkadPattern = StartPattern + @"(?<sMkad>(?<sNumber>\d*)\-?ы?й?\s*(?:км)?\s*)(?<mkad>мкад)(?<eMkad>\s*(?:км)?\s*(?<eNumber>\d*)\-?ы?й?)" + EndPattern;
        #endregion

        #region HouseInfo
        public const string TransportPattern = StartPattern +
                                    @"(?<transport>(?:(?:автобус|троллейбус|трамвай|маршрутка|автоб|тролл?|тр|а|м\.?\s?т)\.?\s*(?:номер|ном|#|№)?\s*(?:\d+[\,\s]*)+))"
                                    + EndPattern;


        public const string PorchPattern = StartPattern + @"(?<p>(?:(?:подъезд|подьезд|подезд|под)\.?\s?(?:номер|ном|#|№)?\s?\d+)|\d+\s+(?:подъезд|подьезд|подезд|под)\.?\s?)" + EndPattern;
        public const string RoomPattern = StartPattern +
                                    @"(?<r>(?<rName>(?:кв\.\/оф\.|комната|ком|помещение|помещ|пом|квартира|кв|офис|оф|каб))(?:\.|\,|\s)(?<rNum>(?:(?:\№|\#)?[\«\""\']?(?:[0-9]{1,4}[a-fA-Fа-рА-Р]{0,3}|[a-fA-Fа-вд-зА-ВД-З]{1,3}[0-9]{0,4})?[\/\-]?(?:[0-9]{1,4}[a-fA-Fа-рА-Р]{0,3}|[a-fA-Fа-вд-зА-ВД-З]{1,3}[0-9]{0,4})?[\»\""\']?(?:$|\.|\,|\s)){1,}))"
                                    + EndPattern;
        public const string VisitHoursPattern = StartPattern +
                                         @"(?<visitHours>с?\s*(?<from>\d+(?:(?:\:|\.)\d+)?)\s*(?:до|по)\s*(?<to>\d+(?:(?:\:|\.)\d+)?))"
                                         + EndPattern;


        public static readonly string FloorPattern =
            string.Format(
                @"{0}(?<floor>(?:(?:этаж|эт)\.?\s*(?:\d{{1,2}}\s*(?:\,\s*|и\s+|{1}))+)|(?:\d{{1,2}}\.?\s*(?:этаж|эт))){1}",
                StartPattern, EndPattern);


        public const string PhonePattern = StartPattern + @"(?<phone>\(?\d{3,3}\)?[\-\s]?\d{3,3}[\-\s]?\d{2,2}[\-\s]?\d{2,2})" + EndPattern;
        //public const string FloorPattern = StartPattern + @"(?<floor>(?:(?:этаж|эт)\.?\s*\d{1,2})|(?:\d{1,2}\.?\s*(?:этаж|эт)))" + EndPattern;
        public const string StructPattern = StartPattern +
                                         @"(?<s>(?:литера|литер|лит|строение|строен|стр|ст|с)\.?\s?[\«\""\']?(?<sNum>(?:(?:[0-9]+[a-fA-Fа-пА-П]?|[a-fA-Fа-зА-З][0-9]*|[a-fA-Fа-пА-П])[\/\-]?(?:[0-9]+[a-fA-Fа-пА-П]?|[a-fA-Fа-зА-З][0-9]*)?))[\»\""\']?)"
                                         + EndPattern;
        public const string BuildPatter = StartPattern +
                                       @"(?<b>(?:корпус|корп|кор|крп|к)\.?\s?[\«\""\']?(?<bNum>(?:(?:[0-9]+[a-fA-Fа-рА-Р]?|[a-fA-Fа-зА-З][0-9]*)?[\/\-\s]?(?:[0-9]+[a-fA-Fа-рА-Р]?|[a-fA-Fа-зА-З][0-9]*)?))[\»\""\']?)"
                                       + EndPattern;
        public const string HousePattern = StartPattern +
                                        @"(?<h>(?:дом|д)?\.?\s?(?<hNum>(?:[0-9]+[а-зА-З]?|[а-ге-зА-ГЕ-З][0-9]+)|(?:[0-9]+[а-ге-зА-ГЕ-З]?|[а-ге-зА-ГЕ-З][0-9]+)[\/\-\s](?:[0-9]+[а-зА-З]?|[а-зА-З][0-9]*)?)(?!\s*км))"
                                        + EndPattern;

        public const string HPattern = StartPattern + @"(дом|д\.?|^)" + EndPattern;
        public const string BPattern = StartPattern + @"(корпус|корп\.?|крп.\?|к\.?)" + EndPattern;
        public const string SPattern = StartPattern + @"(строение|строен\.?|стр\.?|с\.?)" + EndPattern;
        #endregion
    }
}
