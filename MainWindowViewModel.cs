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
        public float RangeValue => Model.RangeValue;
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
                case nameof(ControlValue.RangeValue):
                    RaisePropertyChanged(nameof(RangeValue));
                    break;
                case nameof(ControlValue.NormalizedTime):
                    RaisePropertyChanged(nameof(NormalizedTime));
                    break;
            }
        }
    }

    class MainWindowViewModel : Livet.ViewModel
    {
        public bool IsClampEnabled
        {
            get => _IsClampEnabled;
            set => RaisePropertyChangedIfSet(ref _IsClampEnabled, value);
        }
        bool _IsClampEnabled = false;

        public bool IsRangeEnabled
        {
            get => _IsRangeEnabled;
            set => RaisePropertyChangedIfSet(ref _IsRangeEnabled, value);
        }
        bool _IsRangeEnabled = false;

        public bool IsReadOnlyCurveType
        {
            get => _IsReadOnlyCurveType;
            set => RaisePropertyChangedIfSet(ref _IsReadOnlyCurveType, value);
        }
        bool _IsReadOnlyCurveType = true;

        public bool IsReadOnlyClampFlag
        {
            get => _IsReadOnlyClampFlag;
            set => RaisePropertyChangedIfSet(ref _IsReadOnlyClampFlag, value);
        }
        bool _IsReadOnlyClampFlag = false;

        public bool IsReadOnlyRangeFlag
        {
            get => _IsReadOnlyRangeFlag;
            set => RaisePropertyChangedIfSet(ref _IsReadOnlyRangeFlag, value);
        }
        bool _IsReadOnlyRangeFlag = false;

        public CurveType CurveType
        {
            get => _CurveType;
            set => RaisePropertyChangedIfSet(ref _CurveType, value);
        }
        CurveType _CurveType = CurveType.CatmullRom;

        public float MinValue
        {
            get => _MinValue;
            set => RaisePropertyChangedIfSet(ref _MinValue, value);
        }
        float _MinValue = 0;

        public float MaxValue
        {
            get => _MaxValue;
            set => RaisePropertyChangedIfSet(ref _MaxValue, value);
        }
        float _MaxValue = 100;

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

            foreach (var m in _ControlPoints)
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
