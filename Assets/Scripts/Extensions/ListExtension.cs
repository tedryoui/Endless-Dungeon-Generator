using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class ListExtension
    {
        public static T GetRandom<T>(this List<T> list)
        {
            var random = new System.Random();
            var orderedList = list.OrderBy(x => random.Next());
            return orderedList.First();
        }
    }
}