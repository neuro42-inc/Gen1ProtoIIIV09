using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace n42_Robot_PROTO_III
{
    public class ValueChangeArgs<T> : EventArgs
    {
        public T Value { get; }

        public ValueChangeArgs(T value)
        {
            Value = value;
        }
    }
}
