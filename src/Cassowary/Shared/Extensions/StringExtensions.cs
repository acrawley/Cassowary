using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Shared.Extensions
{
    public static class StringExtensions
    {
        public static string FormatInvariant(this string str, params object[] args)
        {
            return String.Format(CultureInfo.InvariantCulture, str, args);
        }

        public static string FormatCurrentCulture(this string str, params object[] args)
        {
            return String.Format(CultureInfo.CurrentCulture, str, args);
        }
    }
}
