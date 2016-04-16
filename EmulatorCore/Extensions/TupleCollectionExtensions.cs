using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmulatorCore.Extensions
{
    public static class TupleCollectionExtensions
    {
        public static void Add<T1, T2, T3>(this ICollection<Tuple<T1, T2, T3>> collection, T1 item1, T2 item2, T3 item3)
        {
            collection.Add(Tuple.Create(item1, item2, item3));
        }
    }
}
