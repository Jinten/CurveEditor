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
    static class MathUtility
    {
        public static double Clamp(double value, double minValue, double maxValue)
        {
            return Math.Min(Math.Max(value, minValue), maxValue);
        }
    }

    class ControlPoint : UIElement
    {
        internal const float Size = 9;
        internal const float HalfSize = Size * 0.5f;
        internal static Size FullSize { get; } = new Size(Size, Size);
        internal static Point RenderOffset { get; } = new Point(-Math.Ceiling(HalfSize), -Math.Ceiling(HalfSize));
        internal static Point ComputeOffset { get; } = new Point(Math.Ceiling(HalfSize), Math.Ceiling(HalfSize));

        double LimitedXPos => _ActualAreaSize.Width - HalfSize;
        double LimitedYPos => _ActualAreaSize.Height - HalfSize;
        double ControlAreaWidth => _ActualAreaSize.Width - FullSize.Width;
        double ControlAreaHeight => _ActualAreaSize.Height - FullSize.Height;

        internal double PositionX => Translate.X;
        internal double PositionY => Translate.Y;

        bool _IsCapturing = false;

        Pen _CapturingPen = null;
        Brush _CapturingBrush = null;

        Pen _DefaultPen = null;
        Brush _DefaultBrush = null;

        float _Delta = 0.0f;
        Size _ActualAreaSize = new Size();

        ControlValue Value { get; } = null;
        Vector _CapturedLocalPosition = new Vector(0, 0);

        TranslateTransform Translate { get; } = new TranslateTransform(0, 0);

        public ControlPoint(ControlValue value, float delta, float maxWidth, float maxHeight)
        {
            Value = new ControlValue(value);

            _Delta = delta;
            _ActualAreaSize = new Size(maxWidth, maxHeight);

            Placement(_Delta, _ActualAreaSize);

            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(Translate);

            RenderTransform = transformGroup;
        }

        public void Capture(Point capturedPosition)
        {
            _CapturedLocalPosition.X = capturedPosition.X;
            _CapturedLocalPosition.Y = capturedPosition.Y;
            _IsCapturing = true;

            InvalidateVisual();
        }

        public void Release()
        {
            _CapturedLocalPosition.X = 0;
            _CapturedLocalPosition.Y = 0;
            _IsCapturing = false;

            InvalidateVisual();
        }

        public void Update(float delta, Size actualSize)
        {
            _Delta = delta;
            _ActualAreaSize = actualSize;

            Placement(delta, actualSize);
        }

        public void UpdatePosition(Point mousePosition)
        {
            Translate.X = MathUtility.Clamp(mousePosition.X - _CapturedLocalPosition.X, ComputeOffset.X, LimitedXPos);
            Translate.Y = MathUtility.Clamp(mousePosition.Y - _CapturedLocalPosition.Y, ComputeOffset.Y, LimitedYPos);

            var xValue = (Translate.X - ComputeOffset.X);
            var yValue = (Translate.Y - ComputeOffset.Y);
            Value.NormalizedTime = (float)(xValue / ControlAreaWidth);
            Value.Value = (float)(yValue / ControlAreaHeight * _Delta);

            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawRectangle(GetBrush(), GetPen(), new Rect(RenderOffset, FullSize));
        }

        void Placement(float delta, Size actualSize)
        {
            var x = Value.NormalizedTime * actualSize.Width;
            var y = Value.Value / delta * actualSize.Height;
            Translate.X = MathUtility.Clamp(x, ComputeOffset.X, actualSize.Width - HalfSize);
            Translate.Y = MathUtility.Clamp(y, ComputeOffset.Y, actualSize.Height - HalfSize);

            InvalidateVisual();
        }

        Pen GetPen()
        {
            if (_CapturingPen == null)
            {
                _CapturingPen = new Pen(Brushes.Aqua, 1);
                _CapturingPen.DashStyle = new DashStyle(new double[] { 1 }, 0);
                _CapturingPen.Freeze();
            }

            if (_DefaultPen == null)
            {
                _DefaultPen = new Pen(Brushes.Black, 1);
                _DefaultPen.Freeze();
            }

            return _IsCapturing ? _CapturingPen : _DefaultPen;
        }

        Brush GetBrush()
        {
            if (_CapturingBrush == null)
            {
                _CapturingBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                _CapturingBrush.Freeze();
            }

            if (_DefaultBrush == null)
            {
                _DefaultBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                _DefaultBrush.Opacity = 0.3;
                _DefaultBrush.Freeze();
            }


            return _IsCapturing ? _CapturingBrush : _DefaultBrush;
        }
    }

    class CurveEditorCanvas : Canvas
    {
        public double ControlAreaStartX => -ControlPoint.RenderOffset.X;
        public double ControlAreaStartY => -ControlPoint.RenderOffset.Y;
        public double ControlAreaEndX => ActualWidth - ControlAreaStartX;
        public double ControlAreaEndY => ActualHeight - ControlAreaStartY;

        CurveEditor _Owner = null;
        Pen _LinePen = null;

        static Typeface Typeface = new Typeface("Verdana");

        internal void SetOwner(CurveEditor owner)
        {
            _Owner = owner;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            foreach (var cp in Children.OfType<ControlPoint>())
            {
                cp.Update(_Owner.MaxValue - _Owner.MinValue, arrangeSize);
            }

            return base.ArrangeOverride(arrangeSize);
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (_LinePen == null)
            {
                _LinePen = new Pen(Brushes.DimGray, 1);
                _LinePen.DashStyle = new DashStyle(new double[] { 2 }, 0);
                _LinePen.Freeze();
            }

            int num = 5;
            double rcp = 1.0 / num;

            {   // Actual size border line.
                var p0 = new Point(0, 0);
                var p1 = new Point(ActualWidth, ActualHeight);
                dc.DrawRectangle(null, new Pen(Brushes.Black, 1), new Rect(p0, p1));
            }

            {   // Inner controllable area border line.
                var p0 = new Point(ControlAreaStartX - 1, ControlAreaStartY);
                var p1 = new Point(ControlAreaEndX, ControlAreaEndY);
                dc.DrawRectangle(null, new Pen(Brushes.Black, 1), new Rect(p0, p1));
            }

            for (int i = 1; i < num; ++i)
            {
                var y = i * rcp * ControlAreaEndX;
                var p0 = new Point(ControlAreaStartX, y);
                var p1 = new Point(ControlAreaEndX, y);
                dc.DrawLine(_LinePen, p0, p1);
            }

            for (int i = 1; i < num; ++i)
            {
                var x = i * rcp * ControlAreaEndX;
                var p0 = new Point(x, ControlAreaStartY);
                var p1 = new Point(x, ControlAreaEndX - 1);
                dc.DrawLine(_LinePen, p0, p1);
            }

            // render curve
            if (Children.Count > 0)
            {
                var geometry = new StreamGeometry();

                using (var ctx = geometry.Open())
                {
                    var controlPoints = Children.OfType<ControlPoint>().ToArray();

                    var firstPoint = controlPoints[0];
                    ctx.BeginFigure(new Point(firstPoint.PositionX, firstPoint.PositionY), false, false);

                    var points = new List<Point>();
                    for (int i = 1; i < controlPoints.Length; ++i)
                    {
                        var cp = controlPoints[i];
                        points.Add(new Point(cp.PositionX, cp.PositionY));
                    }

                    ctx.PolyLineTo(points, true, false);

                }
                dc.DrawGeometry(null, new Pen(Brushes.Pink, 1), geometry);
            }

            {
                var text = new FormattedText($"{_Owner.MinValue}", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface, 8, Brushes.Black, 1.0);
                dc.DrawText(text, new Point(ControlPoint.HalfSize + 1, ControlAreaEndY - ControlPoint.FullSize.Height - 1));
            }

            {
                var text = new FormattedText($"{_Owner.MaxValue}", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface, 8, Brushes.Black, 1.0);
                dc.DrawText(text, new Point(ControlPoint.HalfSize + 1, ControlPoint.HalfSize + 1));
            }
        }
    }

    public class CurveEditor : Selector
    {
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

        float DeltaValue => MaxValue - MinValue;

        Pen _UnitPen = null;
        Pen _BorderPen = null;
        ControlPoint _DraggingControlPoint = null;
        CurveEditorCanvas _CurveEditorCanvas = null;
        List<ControlValue> DelayToBindList { get; } = new List<ControlValue>();


        static CurveEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CurveEditor), new FrameworkPropertyMetadata(typeof(CurveEditor)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _CurveEditorCanvas = GetTemplateChild("__CurveEditorCanvas__") as CurveEditorCanvas;
            _CurveEditorCanvas.SetOwner(this);

            AddControlPoints(DelayToBindList.ToArray());
        }

        internal void UpdateBrush()
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

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            if (oldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= CollectionChanged;
            }

            if (newValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += CollectionChanged;
            }

            if (_CurveEditorCanvas == null)
            {
                if (newValue != null)
                {
                    DelayToBindList.AddRange(newValue.OfType<ControlValue>());
                }
            }
            else
            {
                ClearControlPoints();

                if (newValue != null)
                {
                    AddControlPoints(newValue.OfType<ControlValue>().ToArray());
                }
            }

            InvalidateVisual();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                _DraggingControlPoint?.Release();
                _DraggingControlPoint = null;
                return;
            }
            var pos = e.GetPosition(_CurveEditorCanvas);
            var limited_x = MathUtility.Clamp(pos.X, _CurveEditorCanvas.ControlAreaStartX, _CurveEditorCanvas.ControlAreaEndX);
            var limited_y = MathUtility.Clamp(pos.Y, _CurveEditorCanvas.ControlAreaStartY, _CurveEditorCanvas.ControlAreaEndY);
            var limitedPos = new Point(limited_x, limited_y);

            _DraggingControlPoint?.UpdatePosition(limitedPos);
            _CurveEditorCanvas.InvalidateVisual();
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            _DraggingControlPoint?.Release();
            _DraggingControlPoint = null;
        }

        void ClearControlPoints()
        {
            foreach (var child in _CurveEditorCanvas.Children)
            {
                (child as UIElement).MouseLeftButtonDown -= ControlPoint_MouseLeftButtonDown;
            }
            _CurveEditorCanvas.Children.Clear();
        }

        void AddControlPoints(ControlValue[] values)
        {
            foreach (var value in values)
            {
                // EndX/Y is not still initialized when construct control in the first time.
                // So need to be not minus value.
                var w = (float)Math.Max(0, _CurveEditorCanvas.ControlAreaEndX);
                var h = (float)Math.Max(0, _CurveEditorCanvas.ControlAreaEndY);
                var cp = new ControlPoint(value, DeltaValue, w, h);
                cp.MouseLeftButtonDown += ControlPoint_MouseLeftButtonDown;

                _CurveEditorCanvas.Children.Add(cp);
            }
        }

        void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    ClearControlPoints();
                    break;
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        var cp = new ControlPoint((ControlValue)item, DeltaValue, (float)ActualWidth, (float)ActualHeight);
                        cp.MouseLeftButtonDown += ControlPoint_MouseLeftButtonDown;

                        _CurveEditorCanvas.Children.Insert(e.NewStartingIndex, cp);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    for (int i = 0; i < e.OldItems.Count; i++)
                    {
                        var cp = _CurveEditorCanvas.Children[e.OldStartingIndex];
                        cp.MouseLeftButtonDown -= ControlPoint_MouseLeftButtonDown;

                        _CurveEditorCanvas.Children.RemoveAt(e.OldStartingIndex);
                    }
                    break;
            }

            InvalidateVisual();
        }

        void ControlPoint_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var cp = sender as ControlPoint;

            _DraggingControlPoint = cp;
            _DraggingControlPoint.Capture(e.GetPosition(cp));
        }
    }
}
