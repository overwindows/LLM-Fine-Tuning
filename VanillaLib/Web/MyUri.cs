using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VanillaLib
{
    public class MyUri : System.Uri
    {
        public MyUri(string uriString)
            : base(uriString)
        {
        }
        public int Depth;
    }
}
