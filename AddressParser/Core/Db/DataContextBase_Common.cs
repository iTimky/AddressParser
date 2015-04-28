#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#endregion



namespace AddressParser.Core.Db
{
    public abstract partial class DataContextBase
    {
        protected string ConnectionString { get; set; }
    }



    public class DataContextCommon : DataContextBase
    {
        public DataContextCommon(string connectionString)
        {
            ConnectionString = connectionString;
        }
    }



    public class DataContext : DataContextBase
    {
        public DataContext()
        {
            ConnectionString = "context connection = true";
        }
    }



    public class TestDataContext : DataContextBase
    {
        public TestDataContext()
        {
            ConnectionString = "Data Source=DEV-TEST;Initial Catalog=CashDesk; Integrated Security=true";
        }
    }



    public class LocalDataContext : DataContextBase
    {
        public LocalDataContext()
        {
            ConnectionString = "Data Source=localhost;Initial Catalog=CashDesk; Integrated Security=true";
        }
    }
}