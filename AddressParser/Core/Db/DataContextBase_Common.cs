using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CE.Parsing.Core.Db
{
    public abstract partial class DataContextBase
    {
        protected abstract string ConnectionString { get; }
    }


    public class DataContext : DataContextBase
    {
        protected override string ConnectionString { get { return "context connection = true"; } }
    }


    public class TestDataContext : DataContextBase
    {
        protected override string ConnectionString { get { return "Data Source=DEV-TEST;Initial Catalog=CashDesk; Integrated Security=true"; } }
    }
}
