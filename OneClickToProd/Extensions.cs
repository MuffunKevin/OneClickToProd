using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OneClickToProd
{
    public static class Extensions
    {
        public static bool isNullOrEmpty(this string item)
        {
            return String.IsNullOrEmpty(item);
        }
    }
}
