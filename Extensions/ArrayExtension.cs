using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurveEditor.Extensions
{
    static class ArrayExtension
    {
        public static void Fill<T>(this T[] array, T initialValue)
        {
            for (int i = 0; i < array.Length; ++i)
            {
                array.SetValue(initialValue, i);
            }
        }

        public static int IndexOf<T>(this T[] array, T value)
        {
            for (int i = 0; i < array.Length; ++i)
            {                
                if (array[i].Equals(value))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
