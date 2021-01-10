using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CurveEditor.Extensions
{
    static class VectorExtension
    {
        public static Point ToPoint(this Vector me)
        {
            return new Point(me.X, me.Y); 
        }
    }
}
