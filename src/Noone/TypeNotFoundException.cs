using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noone
{
    public class TypeNotFoundException : Exception
    {
        public TypeNotFoundException(string message) : base(message)
        {
        }
    }
}
