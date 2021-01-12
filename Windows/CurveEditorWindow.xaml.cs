using CurveEditor.Controls;
using CurveEditor.CurveLibs;
using CurveEditor.Extensions;
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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CurveEditor.Windows
{
    static class MathUtility
    {
        public static int Clamp(int value, int minValue, int maxValue)
        {
            return Math.Min(Math.Max(value, minValue), maxValue);
        }

        public static double Clamp(double value, double minValue, double maxValue)
        {
            return Math.Min(Math.Max(value, minValue), maxValue);
        }
    }

    class FontRenderer
    {
        internal static Typeface Typeface { get; } = new Typeface("Verdana");

        FormattedText _Text = null;
        Brush _Foreground = null;
        double _FontSize = 8.0;

        public FontRenderer(Brush foreground, double emSize = 8, FlowDirection flowDirection = FlowDirection.LeftToRight)
        {
            _FontSize = emSize;

            _Foreground = foreground.Clone();
            _Foreground.Freeze();

            _Text = new FormattedText("Default", CultureInfo.CurrentCulture, flowDirection, Typeface, emSize, _Foreground, 1.0);
        }

        public void Render(DrawingContext dc, in string text, in Point pos, double emSize = 8, FlowDirection flowDirection = FlowDirection.LeftToRight)
        {
            if (_Text.Text != text)
            {
                _FontSize = emSize;
                _Text = new FormattedText(text, CultureInfo.CurrentCulture, flowDirection, Typeface, _FontSize, _Foreground, 1.0);
            }
            else
            {
                if (_FontSize != emSize)
                {
                    _FontSize = emSize;
                    _Text.SetFontSize(_FontSize);
                }
                if (_Text.FlowDirection != flowDirection)
                {
                    _Text.FlowDirection = flowDirection;
                }
            }

            dc.DrawText(_Text, pos);
        }
    }

    abstract class ControlPoint : UIElement
    {
        internal const float Size = 9;
        internal const float HalfSize = Size * 0.5f;
        internal static Size FullSize { get; } = new Size(Size, Size);
        internal static Vector RenderOffset { get; } = new Vector(-Math.Ceiling(HalfSize), -Math.Ceiling(HalfSize));
        internal static Vector ComputeOffset { get; } = new Vector(Math.Ceiling(HalfSize), Math.Ceiling(HalfSize));

        internal abstract double PositionX { get; }
        internal abstract double PositionY { get; }

        internal abstract float Value { get; }
        internal abstract float NormalizedTime { get; }

        internal abstract void Capture(Point capturedPosition);
        internal abstract void Release();

        internal abstract ControlValue GetControlValue();
    }

    class RangeControlPoint : ControlPoint
    {
        internal override double PositionX => Translate.X;
        internal override double PositionY => Translate.Y;

        internal override float Value => Owner.RangedValue;
        internal override float NormalizedTime => Owner.NormalizedTime;

        bool _IsCapturing = false;

        Pen _CapturingPen = null;
        Brush _CapturingBrush = null;

        Pen _DefaultPen = null;
        Brush _DefaultBrush = null;

        Vector _CapturedLocalPosition = new Vector(0, 0);

        DefaultControlPoint Owner { get; }
        FontRenderer ValueText { get; } = new FontRenderer(Brushes.Black);
        TranslateTransform Translate { get; } = new TranslateTransform(0, 0);

        internal RangeControlPoint(DefaultControlPoint owner)
        {
            Owner = owner;

            Translate.X = Owner.PositionX;
            Translate.Y = Owner.PositionY;

            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(Translate);

            RenderTransform = transformGroup;
            RenderTransformOrigin = new Point(0.5, 0.5);
        }

        internal override ControlValue GetControlValue()
        {
            return Owner.GetControlValue();
        }

        internal override void Capture(Point capturedPosition)
        {
            _CapturedLocalPosition.X = capturedPosition.X;
            _CapturedLocalPosition.Y = capturedPosition.Y;
            _IsCapturing = true;
        }

        internal override void Release()
        {
            _CapturedLocalPosition.X = 0;
            _CapturedLocalPosition.Y = 0;
            _IsCapturing = false;
        }

        internal void UpdateTime()
        {
            Translate.X = Owner.PositionX;

            InvalidateVisual();
        }

        internal void UpdateValue(Point mousePosition)
        {
            Translate.Y = MathUtility.Clamp(mousePosition.Y - _CapturedLocalPosition.Y, ComputeOffset.Y, Owner.LimitedYPos);

            var v = DefaultControlPoint.GetControlValue(new Point(Translate.X, Translate.Y), Owner.ActualAreaSize, Owner.Delta);
            Owner.UpdateRangedValue(v.Value);

            InvalidateVisual();
        }

        internal void Placement()
        {
            var y = Owner.ControlAreaHeight - (Owner.RangedValue / Owner.Delta * Owner.ControlAreaHeight);
            Translate.X = Owner.PositionX;
            Translate.Y = MathUtility.Clamp(y, 0, Owner.ActualAreaSize.Height) + ComputeOffset.Y;

            InvalidateVisual();
        }

        Pen GetPen()
        {
            if (_CapturingPen == null)
            {
                _CapturingPen = new Pen(Brushes.Yellow, 1);
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
                _CapturingBrush = new SolidColorBrush(Color.FromRgb(128, 80, 160));
                _CapturingBrush.Freeze();
            }

            if (_DefaultBrush == null)
            {
                _DefaultBrush = new SolidColorBrush(Color.FromRgb(128, 80, 160));
                _DefaultBrush.Opacity = 0.3;
                _DefaultBrush.Freeze();
            }

            return _IsCapturing ? _CapturingBrush : _DefaultBrush;
        }

        protected override void OnRender(DrawingContext dc)
        {
            var text = string.Format("(v={0:F2},t={1:F2})", Value, NormalizedTime);

            dc.DrawRectangle(GetBrush(), GetPen(), new Rect(RenderOffset.ToPoint(), FullSize));
            ValueText.Render(dc, text, new Point(0, 4));
        }
    }

    class DefaultControlPoint : ControlPoint
    {
        internal Size ActualAreaSize { get; private set; } = new Size();
        internal float Delta { get; private set; } = 0.0f;

        internal double LimitedYPos => ActualAreaSize.Height - Math.Ceiling(HalfSize);

        internal double ControlAreaWidth => ActualAreaSize.Width - FullSize.Width - 1;
        internal double ControlAreaHeight => ActualAreaSize.Height - FullSize.Height - 1;

        internal override double PositionX => Translate.X;
        internal override double PositionY => Translate.Y;
        internal override float Value => ControlValue.Value;
        internal override float NormalizedTime => ControlValue.NormalizedTime;

        internal float RangedValue => ControlValue.RangeValue;
        internal RangeControlPoint RangeControlPoint { get; } = null;

        ControlValue ControlValue { get; } = null;

        bool _IsCapturing = false;

        Pen _CapturingPen = null;
        Brush _CapturingBrush = null;

        Pen _DefaultPen = null;
        Brush _DefaultBrush = null;

        Vector _CapturedLocalPosition = new Vector(0, 0);

        FontRenderer ValueText { get; } = new FontRenderer(Brushes.Black);
        TranslateTransform Translate { get; } = new TranslateTransform(0, 0);

        public DefaultControlPoint(ControlValue value, float delta, float maxWidth, float maxHeight)
        {
            ControlValue = value;

            Delta = delta;
            ActualAreaSize = new Size(maxWidth, maxHeight);

            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(Translate);

            RenderTransform = transformGroup;
            RenderTransformOrigin = new Point(0.5, 0.5);

            RangeControlPoint = new RangeControlPoint(this);

            Placement();
        }

        internal override ControlValue GetControlValue()
        {
            return ControlValue;
        }

        internal void UpdateRangedValue(float value)
        {
            ControlValue.RangeValue = value;
        }

        internal override void Capture(Point capturedPosition)
        {
            _CapturedLocalPosition.X = capturedPosition.X;
            _CapturedLocalPosition.Y = capturedPosition.Y;
            _IsCapturing = true;
        }

        internal override void Release()
        {
            _CapturedLocalPosition.X = 0;
            _CapturedLocalPosition.Y = 0;
            _IsCapturing = false;
        }

        public void Update(float delta, Size actualSize)
        {
            Delta = delta;
            ActualAreaSize = actualSize;

            Placement();
        }

        public void UpdatePosition(Point mousePosition, double minX, double maxX)
        {
            Translate.X = MathUtility.Clamp(mousePosition.X - _CapturedLocalPosition.X, minX, maxX);
            Translate.Y = MathUtility.Clamp(mousePosition.Y - _CapturedLocalPosition.Y, ComputeOffset.Y, LimitedYPos);

            var v = GetControlValue(new Point(Translate.X, Translate.Y), ActualAreaSize, Delta);
            ControlValue.Value = v.Value;
            ControlValue.NormalizedTime = v.NormalizedTime;

            RangeControlPoint.UpdateTime();

            InvalidateVisual();
        }

        internal static ControlValue GetControlValue(in Point pos, Size actualSize, double valueDelta)
        {
            var xPos = MathUtility.Clamp(pos.X, ComputeOffset.X, actualSize.Width) - ComputeOffset.X;
            var yPos = MathUtility.Clamp(pos.Y, ComputeOffset.Y, actualSize.Height) - ComputeOffset.Y;

            var value = (float)((1.0 - yPos / (actualSize.Height - FullSize.Height - 1)) * valueDelta);
            var nTime = (float)(xPos / (actualSize.Width - FullSize.Width - 1));

            return new ControlValue(value, nTime);
        }

        internal static Size GetControlAreaSize(in Size actualSize)
        {
            double w = actualSize.Width - FullSize.Width - 1;
            double h = actualSize.Height - FullSize.Height - 1;

            return new Size(w, h);
        }

        protected override void OnRender(DrawingContext dc)
        {
            var text = string.Format("(v={0:F2},t={1:F2})", ControlValue.Value, ControlValue.NormalizedTime);

            dc.DrawRectangle(GetBrush(), GetPen(), new Rect(RenderOffset.ToPoint(), FullSize));
            ValueText.Render(dc, text, new Point(0, 4));
        }

        void Placement()
        {
            var x = ControlValue.NormalizedTime * ControlAreaWidth;
            var y = ControlAreaHeight - (ControlValue.Value / Delta * ControlAreaHeight);
            Translate.X = MathUtility.Clamp(x, 0, ActualAreaSize.Width) + ComputeOffset.X;
            Translate.Y = MathUtility.Clamp(y, 0, ActualAreaSize.Height) + ComputeOffset.Y;

            RangeControlPoint.Placement();

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

        public bool IsClamped
        {
            get => _IsClamped;
            set => UpdateClampEnabled(value);
        }
        bool _IsClamped = false;

        public bool IsRanged
        {
            get => _IsRanged;
            set => UpdateRangeEnabled(value);
        }
        bool _IsRanged = false;

        Controls.CurveEditor _Editor = null;

        Pen _GridLinePen = null;
        Pen _OuterBorderLinePen = null;
        Pen _InnerBorderLinePen = null;

        bool _IsScanning = false;
        float _ScanningTime = 0.0f;
        float _ScanningValue = 0.0f;
        float _ScanningRangeValue = 0.0f;
        Point _ScanningPoint = new Point();
        ControlPoint _DraggingControlPoint = null;

        float DeltaValue => _Editor.MaxValue - _Editor.MinValue;

        FontRenderer MinValueFont = new FontRenderer(Brushes.Black);
        FontRenderer MaxValueFont = new FontRenderer(Brushes.Black);
        FontRenderer ScanningValueFont = new FontRenderer(Brushes.Red);
        FontRenderer ScanningRangeValueFont = new FontRenderer(Brushes.Yellow);


        internal void SetEditor(Controls.CurveEditor editor)
        {
            _Editor = editor;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            foreach (var cp in Children.OfType<DefaultControlPoint>())
            {
                cp.Update(DeltaValue, arrangeSize);
            }

            return base.ArrangeOverride(arrangeSize);
        }

        public void MoveDraggingControlPosition(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                _DraggingControlPoint?.Release();
                _DraggingControlPoint = null;
                return;
            }

            UpdateDraggingControlPosition(e);
        }

        public void ReleaseDraggingControlPoint()
        {
            _DraggingControlPoint?.Release();
            _DraggingControlPoint = null;
        }

        public void BeginScanning(MouseButtonEventArgs e)
        {
            _IsScanning = true;
            _ScanningPoint = e.GetPosition(this);

            UpdateScanningValue(e);

            InvalidateVisual();
        }

        public void MoveScanning(MouseEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Released)
            {
                _IsScanning = false;
            }
            else if (_IsScanning)
            {
                UpdateScanningValue(e);
            }
            InvalidateVisual();
        }

        public void EndScanning(MouseButtonEventArgs e)
        {
            _IsScanning = false;

            InvalidateVisual();
        }

        public void AddControlPoints(bool isRange, ControlValue[] values)
        {
            foreach (var value in values)
            {
                // EndX/Y is not still initialized when construct control in the first time.
                // So need to be not minus value.
                var w = (float)Math.Max(0, ControlAreaEndX);
                var h = (float)Math.Max(0, ControlAreaEndY);
                var cp = new DefaultControlPoint(value, DeltaValue, w, h);
                cp.MouseLeftButtonDown += ControlPoint_MouseLeftButtonDown;
                cp.RangeControlPoint.MouseLeftButtonDown += ControlPoint_MouseLeftButtonDown;

                cp.RangeControlPoint.Visibility = IsRanged ? Visibility.Visible : Visibility.Collapsed;

                Children.Add(cp);
                Children.Add(cp.RangeControlPoint);
            }

            InvalidateVisual();
        }

        public void InsertControlPoints(bool isRange, ControlValue[] cpTbl)
        {
            foreach (var item in cpTbl)
            {
                var cp = new DefaultControlPoint(item, DeltaValue, (float)ActualWidth, (float)ActualHeight);
                cp.MouseLeftButtonDown += ControlPoint_MouseLeftButtonDown;
                cp.RangeControlPoint.MouseLeftButtonDown += ControlPoint_MouseLeftButtonDown;

                cp.RangeControlPoint.Visibility = IsRanged ? Visibility.Visible : Visibility.Collapsed;

                Children.Add(cp);
                Children.Add(cp.RangeControlPoint);
            }

            InvalidateVisual();
        }

        public void RemoveControlPoints(ControlValue[] cpTbl)
        {
            var removeList = Children.OfType<DefaultControlPoint>().Where(arg => cpTbl.Contains(arg.GetControlValue())).ToArray();
            for (int i = 0; i < removeList.Length; i++)
            {
                var cp = removeList[i];
                cp.MouseLeftButtonDown -= ControlPoint_MouseLeftButtonDown;
                cp.RangeControlPoint.MouseLeftButtonDown -= ControlPoint_MouseLeftButtonDown;

                Children.Remove(cp);
                Children.Remove(cp.RangeControlPoint);
            }
        }

        public void ClearControlPoints()
        {
            foreach (var child in Children.OfType<ControlPoint>())
            {
                child.MouseLeftButtonDown -= ControlPoint_MouseLeftButtonDown;
            }
            Children.Clear();

            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (_GridLinePen == null)
            {
                _GridLinePen = new Pen(new SolidColorBrush(Color.FromRgb(60, 60, 60)), 1);
                _GridLinePen.DashStyle = new DashStyle(new double[] { 2 }, 0);
                _GridLinePen.Freeze();
            }

            if (_OuterBorderLinePen == null)
            {
                _OuterBorderLinePen = new Pen(Brushes.Black, 1);
                _OuterBorderLinePen.Freeze();
            }
            if (_InnerBorderLinePen == null)
            {
                _InnerBorderLinePen = new Pen(Brushes.Black, 1);
                _InnerBorderLinePen.DashStyle = new DashStyle(new double[] { 3.0 }, 0.0);
            }

            // Render for hit testing.
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));

            int nGrid = 5;
            double rcpGrid = 1.0 / nGrid;

            {   // Actual size border line.
                var p0 = new Point(0, 0);
                var p1 = new Point(ActualWidth, ActualHeight);
                dc.DrawRectangle(null, _OuterBorderLinePen, new Rect(p0, p1));
            }

            {   // Inner controllable area border line.
                var p0 = new Point(ControlAreaStartX, ControlAreaStartY);
                var p1 = new Point(ControlAreaEndX, ControlAreaEndY);
                dc.DrawRectangle(null, _InnerBorderLinePen, new Rect(p0, p1));
            }

            for (int i = 1; i < nGrid; ++i)
            {
                var y = i * rcpGrid * ControlAreaEndX;
                var p0 = new Point(ControlAreaStartX, y);
                var p1 = new Point(ControlAreaEndX, y);
                dc.DrawLine(_GridLinePen, p0, p1);
            }

            for (int i = 1; i < nGrid; ++i)
            {
                var x = i * rcpGrid * ControlAreaEndX;
                var p0 = new Point(x, ControlAreaStartY);
                var p1 = new Point(x, ControlAreaEndX - 1);
                dc.DrawLine(_GridLinePen, p0, p1);
            }

            var controls = Children.OfType<DefaultControlPoint>().ToArray();
            var rangeControls = Children.OfType<RangeControlPoint>().ToArray();

            RenderCurve(dc, Brushes.Pink, controls);
            if (IsRanged)
            {
                RenderCurve(dc, Brushes.Purple, rangeControls);
            }

            if (_IsScanning)
            {
                var sz = DefaultControlPoint.GetControlAreaSize(new Size(ActualWidth, ActualHeight));

                // Scanning line.
                double line_x = _ScanningTime * sz.Width + ControlPoint.ComputeOffset.X;
                dc.DrawLine(new Pen(Brushes.Red, 1), new Point(line_x, 0), new Point(line_x, ActualHeight));

                // Subtract ControlPoint.HalfSize is for adjusting to render layout.
                // _ScanningValue / _Owner.MaxValue is for normalizing vertical value line.
                // Multiply sz.Height for stretching control area vertical and subtract from sz.Height for inverting Y axis.

                // Value
                if (controls.Length > 0)
                {
                    var pos = new Point(line_x, sz.Height - (_ScanningValue / _Editor.MaxValue * sz.Height) + ControlPoint.ComputeOffset.Y);

                    dc.DrawRectangle(Brushes.DarkRed, null, new Rect(pos + ControlPoint.RenderOffset, ControlPoint.FullSize));

                    var text = string.Format("(v={0:F2},t={1:F2})", _ScanningValue, _ScanningTime);
                    ScanningValueFont.Render(dc, text, new Point(_ScanningPoint.X + 2, pos.Y + ControlPoint.FullSize.Height));
                }

                // Range value
                if (IsRanged && rangeControls.Length > 0)
                {
                    var pos = new Point(line_x, sz.Height - (_ScanningRangeValue / _Editor.MaxValue * sz.Height) + ControlPoint.ComputeOffset.Y);

                    dc.DrawRectangle(Brushes.Purple, null, new Rect(pos + ControlPoint.RenderOffset, ControlPoint.FullSize));

                    var text = string.Format("(v={0:F2},t={1:F2})", _ScanningRangeValue, _ScanningTime);
                    ScanningRangeValueFont.Render(dc, text, new Point(_ScanningPoint.X + 2, pos.Y + ControlPoint.FullSize.Height));
                }
            }

            {
                var pos = new Point(ControlPoint.HalfSize + 3, ControlAreaEndY - ControlPoint.FullSize.Height - 3);
                MinValueFont.Render(dc, $"{_Editor.MinValue}", pos);
            }

            {
                var pos = new Point(ControlPoint.HalfSize + 3, ControlPoint.HalfSize + 3);
                MaxValueFont.Render(dc, $"{_Editor.MaxValue}", pos);
            }
        }

        void RenderCurve(DrawingContext dc, Brush curveBrush, ControlPoint[] controls)
        {
            if (controls.Length == 0)
            {
                return;
            }

            // render curve
            var geometry = new StreamGeometry();

            switch (_Editor.Type)
            {
                case CurveType.Linear:
                    using (var ctx = geometry.Open())
                    {
                        var firstPoint = controls[0];
                        ctx.BeginFigure(new Point(firstPoint.PositionX, firstPoint.PositionY), false, false);

                        var points = new List<Point>();
                        for (int i = 1; i < controls.Length; ++i)
                        {
                            var cp = controls[i];
                            points.Add(new Point(cp.PositionX, cp.PositionY));
                        }

                        ctx.PolyLineTo(points, true, false);
                    }
                    break;
                case CurveType.CatmullRom:
                    using (var ctx = geometry.Open())
                    {
                        var firstPoint = controls[0];
                        ctx.BeginFigure(new Point(firstPoint.PositionX, firstPoint.PositionY), false, false);

                        // convert position in area unit.
                        var sz = DefaultControlPoint.GetControlAreaSize(new Size(ActualWidth, ActualHeight));
                        var controlValues = controls.Select(arg => arg.Value).ToArray();

                        int nSectionDivision = 256;

                        float[] curveValues;
                        if (IsClamped)
                        {
                            curveValues = CatmullRomSpline.Compute(nSectionDivision, controlValues, _Editor.MinValue, _Editor.MaxValue);
                        }
                        else
                        {
                            curveValues = CatmullRomSpline.Compute(nSectionDivision, controlValues);
                        }

                        var rcp = 1.0f / nSectionDivision;

                        var points = new List<Point>();

                        for (int i = 0; i < controlValues.Length - 1; ++i)
                        {
                            var t0 = controls[i + 0].NormalizedTime;
                            var t1 = controls[i + 1].NormalizedTime;
                            var dt = t1 - t0;

                            int index = i * nSectionDivision;
                            for (int j = 0; j < nSectionDivision; ++j)
                            {
                                var x = (dt * (j * rcp) + t0) * sz.Width + ControlPoint.ComputeOffset.X;
                                var y = sz.Height - curveValues[index + j] / _Editor.MaxValue * sz.Height + ControlPoint.ComputeOffset.Y;
                                points.Add(new Point(x, y));
                            }

                        }
                        ctx.PolyLineTo(points, true, false);
                    }
                    break;
                case CurveType.B_Spline:
                    throw new NotImplementedException();
            }

            dc.DrawGeometry(null, new Pen(curveBrush, 1), geometry);
        }

        void UpdateClampEnabled(bool enable)
        {
            _IsClamped = enable;

            InvalidateVisual();
        }

        void UpdateRangeEnabled(bool enable)
        {
            _IsRanged = enable;

            var rangeControlPoints = Children.OfType<RangeControlPoint>().ToArray();
            foreach (var rcp in rangeControlPoints)
            {
                rcp.Visibility = _IsRanged ? Visibility.Visible : Visibility.Collapsed;
            }

            InvalidateVisual();
        }

        void UpdateDraggingControlPosition(MouseEventArgs e)
        {
            if (_DraggingControlPoint == null)
            {
                return;
            }

            // depends on not ranged value position.
            var controls = Children.OfType<DefaultControlPoint>().ToArray();

            int index = controls.IndexOf(_DraggingControlPoint);
            var min_x = index > 0 ? controls[index - 1].PositionX : ControlAreaStartX;
            var max_x = index < controls.Length - 1 ? controls[index + 1].PositionX : ControlAreaEndX;

            var pos = e.GetPosition(this);
            switch (_DraggingControlPoint)
            {
                case DefaultControlPoint dcp:
                    dcp.UpdatePosition(pos, min_x, max_x);
                    break;
                case RangeControlPoint rcp:
                    rcp.UpdateValue(pos);
                    break;
            }

            InvalidateVisual();
        }

        void UpdateScanningValue(MouseEventArgs e)
        {
            var controls = Children.OfType<DefaultControlPoint>().ToArray();
            _ScanningValue = GetScanningValue(e, controls);

            var rangeControls = Children.OfType<RangeControlPoint>().ToArray();
            _ScanningRangeValue = GetScanningValue(e, rangeControls);
        }

        float GetScanningValue(MouseEventArgs e, ControlPoint[] controls)
        {
            if (controls.Length == 0)
            {
                return 0;
            }

            var p0 = controls.First();
            var p1 = controls.Last();

            _ScanningPoint = e.GetPosition(this);
            _ScanningPoint.X = MathUtility.Clamp(_ScanningPoint.X, p0.PositionX, p1.PositionX);

            _ScanningTime = (float)((_ScanningPoint.X - Math.Ceiling(ControlPoint.HalfSize)) / (ControlAreaEndX - ControlAreaStartX));

            switch (_Editor.Type)
            {
                case CurveType.Linear:
                    return GetScanningLinearValue(_ScanningTime, controls);
                case CurveType.CatmullRom:
                    return GetScanningCatmullRomValue(_ScanningTime, controls);
                case CurveType.B_Spline:
                    throw new NotImplementedException();
            }

            throw new InvalidProgramException();
        }

        float GetScanningLinearValue(float t, ControlPoint[] controls)
        {
            var p0 = controls.Reverse().FirstOrDefault(arg => arg.NormalizedTime <= t);
            var p1 = controls.FirstOrDefault(arg => arg.NormalizedTime > t);

            float t0 = p0.NormalizedTime;
            float t1 = p1 != null ? p1.NormalizedTime : p0.NormalizedTime;
            float dt = t1 - t0;
            if (dt == 0)
            {
                return p0.Value;
            }

            float v0 = p0.Value;
            float v1 = p1.Value;
            float st = (t - t0) / dt;

            return (v1 - v0) * st + v0;
        }

        float GetScanningCatmullRomValue(float t, ControlPoint[] controls)
        {
            if (t == 0)
            {
                return controls.First().Value;
            }
            if (t == 1.0f)
            {
                return controls.Last().Value;
            }

            var p0 = controls.Reverse().FirstOrDefault(arg => arg.NormalizedTime <= t);
            var p1 = controls.FirstOrDefault(arg => arg.NormalizedTime > t);

            var t0 = p0.NormalizedTime;
            var t1 = p1 != null ? p1.NormalizedTime : p0.NormalizedTime;
            var dt = t1 - t0;
            if (dt == 0)
            {
                return p0.Value;
            }
            var seg_t = (t - t0) / dt;

            var values = controls.Select(arg => arg.Value).ToArray();
            var value = CatmullRomSpline.ComputeSingle(controls.IndexOf(p0), seg_t, values);

            if (IsClamped == false)
            {
                return value;
            }

            return Math.Min(Math.Max(value, _Editor.MinValue), _Editor.MaxValue);
        }

        void ControlPoint_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var cp = sender as ControlPoint;

            _DraggingControlPoint = cp;
            _DraggingControlPoint.Capture(e.GetPosition(cp));

            UpdateDraggingControlPosition(e);
        }
    }

    /// <summary>
    /// CurveEditorWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class CurveEditorWindow : Window
    {
        Controls.CurveEditor Editor { get; } = null;

        public CurveEditorWindow(Controls.CurveEditor editor)
        {
            Editor = editor;

            InitializeComponent();

            EditorCanvas.SetEditor(Editor);
            EditorCanvas.IsClamped = Editor.IsClampEnabled;
            EditorCanvas.IsRanged = Editor.IsRangeEnabled;

            switch (Editor.Type)
            {
                case CurveType.Linear:
                    CurveTypeComboBox.SelectedIndex = 0;
                    break;
                case CurveType.CatmullRom:
                    CurveTypeComboBox.SelectedIndex = 1;
                    break;
            }

            ClampCheckBox.IsChecked = Editor.IsClampEnabled;
            RangeCheckBox.IsChecked = Editor.IsRangeEnabled;

            ClampCheckBox.IsEnabled = Editor.IsReadOnlyClampFlag == false;
            RangeCheckBox.IsEnabled = Editor.IsReadOnlyRangeFlag == false;
            CurveTypeComboBox.IsEnabled = Editor.IsReadOnlyType == false;

            CurveTypeComboBox.SelectionChanged += CurveTypeComboBox_SelectionChanged;

            ClampCheckBox.Checked += ClampCheckBox_Checked;
            ClampCheckBox.Unchecked += ClampCheckBox_Unchecked;

            RangeCheckBox.Checked += RangeCheckBox_Checked;
            RangeCheckBox.Unchecked += RangeCheckBox_Unchecked;

            var cpTbl = Editor.ItemsSource?.OfType<ControlValue>().ToArray();

            if (cpTbl != null)
            {
                EditorCanvas.AddControlPoints(false, cpTbl);
            }
        }

        public void ItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            if (oldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= CollectionChanged;
            }

            if (newValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += CollectionChanged;
            }

            EditorCanvas.ClearControlPoints();

            if (newValue != null)
            {
                EditorCanvas.AddControlPoints(false, newValue.OfType<ControlValue>().ToArray());
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            EditorCanvas.MoveScanning(e);

            EditorCanvas.MoveDraggingControlPosition(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            EditorCanvas.ReleaseDraggingControlPoint();
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonDown(e);

            EditorCanvas.BeginScanning(e);
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonUp(e);

            EditorCanvas.EndScanning(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            CurveTypeComboBox.SelectionChanged -= CurveTypeComboBox_SelectionChanged;

            RangeCheckBox.Unchecked -= RangeCheckBox_Unchecked;
            RangeCheckBox.Checked -= RangeCheckBox_Checked;

            ClampCheckBox.Unchecked -= ClampCheckBox_Unchecked;
            ClampCheckBox.Checked -= ClampCheckBox_Checked;
        }

        void CurveTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedContent = (e.AddedItems[0] as ComboBoxItem).Content;

            switch (selectedContent)
            {
                case nameof(CurveType.Linear):
                    Editor.Type = CurveType.Linear;
                    break;
                case nameof(CurveType.CatmullRom):
                    Editor.Type = CurveType.CatmullRom;
                    break;
            }

            EditorCanvas.InvalidateVisual();
        }

        void ClampCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            EditorCanvas.IsClamped = true;
            Editor.IsClampEnabled = true;
        }

        void ClampCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            EditorCanvas.IsClamped = false;
            Editor.IsClampEnabled = false;
        }

        void RangeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            EditorCanvas.IsRanged = true;
            Editor.IsRangeEnabled = true;
        }

        void RangeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            EditorCanvas.IsRanged = false;
            Editor.IsRangeEnabled = false;
        }

        void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    EditorCanvas.ClearControlPoints();
                    break;
                case NotifyCollectionChangedAction.Add:
                    EditorCanvas.InsertControlPoints(false, e.NewItems.OfType<ControlValue>().ToArray());
                    break;
                case NotifyCollectionChangedAction.Remove:
                    EditorCanvas.RemoveControlPoints(e.OldItems.OfType<ControlValue>().ToArray());
                    break;
            }
        }
    }
}
