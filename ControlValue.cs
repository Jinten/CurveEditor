using Livet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurveEditor
{
    public class ControlValue : NotificationObject
    {
        public float Value
        {
            get => _Value;
            set => RaisePropertyChangedIfSet(ref _Value, value);
        }
        float _Value;

        public float NormalizedTime
        {
            get => _NormalizedTime;
            set => RaisePropertyChangedIfSet(ref _NormalizedTime, value);
        }
        float _NormalizedTime;

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
