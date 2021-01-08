using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CurveEditor
{
    class MainWindowViewModel : Livet.ViewModel
    {
        public IEnumerable<ControlValue> ControlPoints => _ControlPoints;
        ObservableCollection<ControlValue> _ControlPoints = new ObservableCollection<ControlValue>();

        public MainWindowViewModel()
        {
            _ControlPoints.Add(new ControlValue(0.0f, 0.0f));
            _ControlPoints.Add(new ControlValue(30.0f, 0.5f));
            _ControlPoints.Add(new ControlValue(50.0f, 0.8f));
            _ControlPoints.Add(new ControlValue(60.0f, 1.0f));
        }
    }
}
