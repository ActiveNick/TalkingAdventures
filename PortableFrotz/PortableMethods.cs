using System;
using System.Collections.Generic;
using System.Text;

namespace PortableFrotz
{
    public static class PortableMethods
    {
        public static String StringFromArray(char[] text, int start, int length)
        {
            return new String(text).Substring(start, length);
        }
    }
}
