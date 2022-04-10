using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apotheca.Db.Models
{
    public class DbParameter
    {
        public DbParameter(string key, object value)
        {
            this.Key = key;
            this.Value = value;
        }

        public string Key { get; set; }

        public object Value { get; set; }
    }
}
