using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apotheca.Db.Exceptions
{
    public class DbContextInitialisationException : Exception
    {
        public DbContextInitialisationException(string? message) : base(message)
            { }
    }
}
