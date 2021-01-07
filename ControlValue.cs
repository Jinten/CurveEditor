using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurveEditor
{
    public class ControlValue
    {
        public float Value { get; set; }
        public float NormalizedTime { get; set; }

        public ControlValue(float value, float nTime)
        {
            Value = value;
            NormalizedTime = nTime;
        }

        public ControlValue(ControlValue src)
        {
            Value = src.Value;
            NormalizedTime = src.NormalizedTime;
        }
    }
}
