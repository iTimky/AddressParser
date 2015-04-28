#region usings
using System;

#endregion



namespace AddressParser.Core.Models
{
    public class OldAddress
    {
        public Guid? Country;
        public Guid? City;
        public Guid? Street;
        public Guid? CountryRegion;
        public string BuildingNumber;
        public string AppartmentNumber;
    }



    public class TupleOld
    {
        /*
	old_Country uniqueidentifier, 
	old_Country_name nvarchar(256),
	old_CountryRegion uniqueidentifier, 
	old_CountryRegion_name nvarchar(256),
	old_City uniqueidentifier, 
	old_City_name nvarchar(256),
	old_City_type nvarchar(64),
	old_Street uniqueidentifier,
	old_Street_name nvarchar(256),
	old_Street_type nvarchar(64),

	old_BuildingNumber nvarchar(256),
	old_AppartmentNumber nvarchar(256),
         * */

        public int Id;

        public Guid? old_Country;
        public string old_Country_name;

        public Guid? old_CountryRegion;
        public string old_CountryRegion_name;

        public Guid? old_City;
        public string old_City_name;
        public string old_City_type;

        public Guid? old_Street;
        public string old_Street_name;
        public string old_Street_type;

        public string old_BuildingNumber;
        public string old_AppartmentNumber;


        public OldAddress CreateOldAddress()
        {
            return new OldAddress()
            {
                Country = this.old_Country,
                CountryRegion = this.old_CountryRegion,
                City = this.old_City,
                Street = this.old_Street,
                BuildingNumber = this.old_BuildingNumber,
                AppartmentNumber = this.old_AppartmentNumber
            };
        }
    }
}