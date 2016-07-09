using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noone
{
    public class UndefinedParamsConstructorException : Exception
    {
        public UndefinedParamsConstructorException(string message):base(message)
        {

        }
    }
}
