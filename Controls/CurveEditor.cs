using CurveEditor.CurveLibs;
using CurveEditor.Extensions;
using CurveEditor.Windows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CurveEditor.Controls
{
    public class CurveEditor : Selector
    {
        public CurveType Type
        {
            get => (CurveType)GetValue(CurveTypeProperty);
            set => SetValue(CurveTypeProperty, value);
        }
        public static readonly DependencyProperty CurveTypeProperty =
            DependencyProperty.Register(nameof(Type), typeof(CurveType), typeof(CurveEditor), new FrameworkPropertyMetadata(CurveType.Linear, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool IsReadOnlyType
        {
            get => (bool)GetValue(IsReadOnlyTypeProperty);
            set => SetValue(IsReadOnlyTypeProperty, value);
        }
        public static readonly DependencyProperty IsReadOnlyTypeProperty =
            DependencyProperty.Register(nameof(IsReadOnlyType), typeof(bool), typeof(CurveEditor), new FrameworkPropertyMetadata(false));

        public float MaxValue
        {
            get => (float)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }
        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register(nameof(MaxValue), typeof(float), typeof(CurveEditor), new PropertyMetadata(1.0f));

        public float MinValue
        {
            get => (float)GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }
        public static readonly DependencyProperty MinValueProperty =
            DependencyProperty.Register(nameof(MinValue), typeof(float), typeof(CurveEditor), new PropertyMetadata(0.0f));

        bool IsOpenedCurveEditor
        {
            get => (bool)GetValue(IsOpenedCurveEditorProperty);
            set => SetValue(IsOpenedCurveEditorProperty, value);
        }
        public static readonly DependencyProperty IsOpenedCurveEditorProperty = DependencyProperty.Register(
            nameof(IsOpenedCurveEditor), typeof(bool), typeof(CurveEditor), new FrameworkPropertyMetadata(false));

        public Brush Color
        {
            get => (Brush)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(nameof(Color), typeof(Brush), typeof(CurveEditor), new FrameworkPropertyMetadata(Brushes.WhiteSmoke, ColorPropertyChanged));

        static void ColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as CurveEditor).UpdateBrush();
        }

        Pen _UnitPen = null;
        Pen _BorderPen = null;

        CurveEditorWindow _CurveEditorWindow = null;

        static CurveEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CurveEditor), new FrameworkPropertyMetadata(typeof(CurveEditor)));
        }

        public CurveEditor()
        {
            Unloaded += (object sender, RoutedEventArgs e) =>
            {
                _CurveEditorWindow?.Close();
                _CurveEditorWindow = null;

                IsOpenedCurveEditor = false;
            };
        }

        void UpdateBrush()
        {
            if (_UnitPen == null)
            {
                _UnitPen = new Pen(Color, 1);
                _UnitPen.Freeze();
            }

            if (_BorderPen == null)
            {
                _BorderPen = new Pen(Background, 1);
                _BorderPen.Freeze();
            }
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            if (_CurveEditorWindow != null)
            {
                return;
            }

            IsOpenedCurveEditor = true;

            var screenPosition = PointToScreen(e.GetPosition(this));

            _CurveEditorWindow = new CurveEditorWindow(this)
            {
                Background = Background,
                ResizeMode = ResizeMode.NoResize,
                Left = screenPosition.X,
                Top = screenPosition.Y,
            };
            _CurveEditorWindow.Closed += CurveEditorWindow_Closed;
            _CurveEditorWindow.Show();

            e.Handled = true;
        }

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            _CurveEditorWindow?.ItemsSourceChanged(oldValue, newValue);
        }

        void CurveEditorWindow_Closed(object sender, EventArgs e)
        {
            _CurveEditorWindow.Closed -= CurveEditorWindow_Closed;
            _CurveEditorWindow = null;
        }
    }
}
