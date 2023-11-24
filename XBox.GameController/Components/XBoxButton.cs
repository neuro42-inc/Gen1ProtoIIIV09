using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace n42_Robot_PROTO_III
{ 
    public class XBoxButton : XBoxComponent<bool>
    {
        public XBoxButton(bool initialValue = false) : base(initialValue) { }
    }
}
