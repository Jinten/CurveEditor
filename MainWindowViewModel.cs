using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CurveEditor
{
    public class ControlValueViewModel : Livet.ViewModel
    {
        public float Value => Model.Value;
        public float NormalizedTime => Model.NormalizedTime;

        ControlValue Model { get; }

        public ControlValueViewModel(ControlValue model)
        {
            Model = model;
            Model.PropertyChanged += Model_PropertyChanged;

            CompositeDisposable.Add(() =>
            {
                Model.PropertyChanged -= Model_PropertyChanged;
            });
        }

        private void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(ControlValue.Value):
                    RaisePropertyChanged(nameof(Value));
                    break;
                case nameof(ControlValue.NormalizedTime):
                    RaisePropertyChanged(nameof(NormalizedTime));
                    break;

            }
        }
    }

    class MainWindowViewModel : Livet.ViewModel
    {
        public IEnumerable<ControlValue> ControlPoints => _ControlPoints;
        ObservableCollection<ControlValue> _ControlPoints = new ObservableCollection<ControlValue>();

        public IEnumerable<ControlValueViewModel> ControlPointVMs => _ControlPointVMs;
        ObservableCollection<ControlValueViewModel> _ControlPointVMs = new ObservableCollection<ControlValueViewModel>();

        public MainWindowViewModel()
        {
            _ControlPoints.Add(new ControlValue(0.0f, 0.0f));
            _ControlPoints.Add(new ControlValue(100.0f, 0.5f));
            _ControlPoints.Add(new ControlValue(50.0f, 0.8f));
            _ControlPoints.Add(new ControlValue(60.0f, 1.0f));

            foreach(var m in _ControlPoints)
            {
                _ControlPointVMs.Add(new ControlValueViewModel(m));
            }

            CompositeDisposable.Add(() =>
            {
                foreach(var vm in _ControlPointVMs)
                {
                    vm.Dispose();
                }
            });
        }
    }
}
