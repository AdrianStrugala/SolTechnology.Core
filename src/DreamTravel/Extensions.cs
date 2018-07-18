using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DreamTravel
{
    public static class Extensions
    {
        public static async Task ForEachAsync<T>(this List<T> list, Func<T, Task> func)
        {
            foreach (var value in list)
            {
                await func(value);
            }
        }

        public static async Task ForEachAsync(this int iterator, Func<int, Task> func)
        {
            for (int i = 0; i < iterator; i++)
            {
                await func(i);
            }
        }
    }
}