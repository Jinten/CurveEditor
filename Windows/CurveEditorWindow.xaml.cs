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

    class ControlPoint : UIElement
    {
        internal const float Size = 9;
        internal const float HalfSize = Size * 0.5f;
        internal static Size FullSize { get; } = new Size(Size, Size);
        internal static Vector RenderOffset { get; } = new Vector(-Math.Ceiling(HalfSize), -Math.Ceiling(HalfSize));
        internal static Vector ComputeOffset { get; } = new Vector(Math.Ceiling(HalfSize), Math.Ceiling(HalfSize));

        double LimitedYPos => _ActualAreaSize.Height - Math.Ceiling(HalfSize);
        double ControlAreaWidth => _ActualAreaSize.Width - FullSize.Width - 1;
        double ControlAreaHeight => _ActualAreaSize.Height - FullSize.Height - 1;

        internal double PositionX => Translate.X;
        internal double PositionY => Translate.Y;

        internal ControlValue Value { get; } = null;

        bool _IsCapturing = false;

        Pen _CapturingPen = null;
        Brush _CapturingBrush = null;

        Pen _DefaultPen = null;
        Brush _DefaultBrush = null;

        float _Delta = 0.0f;
        Size _ActualAreaSize = new Size();

        Vector _CapturedLocalPosition = new Vector(0, 0);

        FontRenderer ValueText { get; } = new FontRenderer(Brushes.Black);

        TranslateTransform Translate { get; } = new TranslateTransform(0, 0);

        public ControlPoint(ControlValue value, float delta, float maxWidth, float maxHeight)
        {
            Value = value;

            _Delta = delta;
            _ActualAreaSize = new Size(maxWidth, maxHeight);

            Placement();

            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(Translate);

            RenderTransform = transformGroup;
            RenderTransformOrigin = new Point(0.5, 0.5);
        }

        public void Capture(Point capturedPosition)
        {
            _CapturedLocalPosition.X = capturedPosition.X;
            _CapturedLocalPosition.Y = capturedPosition.Y;
            _IsCapturing = true;
        }

        public void Release()
        {
            _CapturedLocalPosition.X = 0;
            _CapturedLocalPosition.Y = 0;
            _IsCapturing = false;
        }

        public void Update(float delta, Size actualSize)
        {
            _Delta = delta;
            _ActualAreaSize = actualSize;

            Placement();
        }

        public void UpdatePosition(Point mousePosition, double minX, double maxX)
        {
            Translate.X = MathUtility.Clamp(mousePosition.X - _CapturedLocalPosition.X, minX, maxX);
            Translate.Y = MathUtility.Clamp(mousePosition.Y - _CapturedLocalPosition.Y, ComputeOffset.Y, LimitedYPos);

            var v = GetControlValue(new Point(Translate.X, Translate.Y), _ActualAreaSize, _Delta);
            Value.Value = v.Value;
            Value.NormalizedTime = v.NormalizedTime;

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
            dc.DrawRectangle(GetBrush(), GetPen(), new Rect(RenderOffset.ToPoint(), FullSize));

            var text = string.Format("(v={0:F2},t={1:F2})", Value.Value, Value.NormalizedTime);
            ValueText.Render(dc, text, new Point(0, 4));
        }

        void Placement()
        {
            var x = Value.NormalizedTime * ControlAreaWidth;
            var y = ControlAreaHeight - (Value.Value / _Delta * ControlAreaHeight);
            Translate.X = MathUtility.Clamp(x, 0, _ActualAreaSize.Width) + ComputeOffset.X;
            Translate.Y = MathUtility.Clamp(y, 0, _ActualAreaSize.Height) + ComputeOffset.Y;

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

        Controls.CurveEditor _Editor = null;

        Pen _GridLinePen = null;
        Pen _OuterBorderLinePen = null;
        Pen _InnerBorderLinePen = null;

        bool _IsScanning = false;
        float _ScanningTime = 0.0f;
        float _ScanningValue = 0.0f;
        Point _ScanningPoint = new Point();
        ControlPoint _DraggingControlPoint = null;

        float DeltaValue => _Editor.MaxValue - _Editor.MinValue;

        FontRenderer MinValueFont = new FontRenderer(Brushes.Black);
        FontRenderer MaxValueFont = new FontRenderer(Brushes.Black);
        FontRenderer ScanningValueFont = new FontRenderer(Brushes.Red);


        internal void SetEditor(Controls.CurveEditor editor)
        {
            _Editor = editor;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            foreach (var cp in Children.OfType<ControlPoint>())
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

        public void AddControlPoints(ControlValue[] values)
        {
            foreach (var value in values)
            {
                // EndX/Y is not still initialized when construct control in the first time.
                // So need to be not minus value.
                var w = (float)Math.Max(0, ControlAreaEndX);
                var h = (float)Math.Max(0, ControlAreaEndY);
                var cp = new ControlPoint(value, DeltaValue, w, h);
                cp.MouseLeftButtonDown += ControlPoint_MouseLeftButtonDown;

                Children.Add(cp);
            }

            InvalidateVisual();
        }

        public void InsertControlPoints(int startIndex, ControlValue[] cpTbl)
        {
            foreach (var item in cpTbl)
            {
                var cp = new ControlPoint(item, DeltaValue, (float)ActualWidth, (float)ActualHeight);
                cp.MouseLeftButtonDown += ControlPoint_MouseLeftButtonDown;

                Children.Insert(startIndex, cp);
            }
        }

        public void RemoveControlPoints(int startIndex, ControlValue[] cpTbl)
        {
            for (int i = 0; i < cpTbl.Length; i++)
            {
                var cp = Children[startIndex];
                cp.MouseLeftButtonDown -= ControlPoint_MouseLeftButtonDown;

                Children.RemoveAt(startIndex);
            }
        }

        public void ClearControlPoints()
        {
            foreach (var child in Children)
            {
                (child as UIElement).MouseLeftButtonDown -= ControlPoint_MouseLeftButtonDown;
            }
            Children.Clear();
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

            // render curve
            if (Children.Count > 0)
            {
                var geometry = new StreamGeometry();
                var controlPoints = Children.OfType<ControlPoint>().ToArray();

                switch (_Editor.Type)
                {
                    case CurveType.Linear:
                        using (var ctx = geometry.Open())
                        {
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
                        break;
                    case CurveType.CatmullRom:
                        using (var ctx = geometry.Open())
                        {
                            var firstPoint = controlPoints[0];
                            ctx.BeginFigure(new Point(firstPoint.PositionX, firstPoint.PositionY), false, false);

                            // convert position in area unit.
                            var sz = ControlPoint.GetControlAreaSize(new Size(ActualWidth, ActualHeight));
                            var controlValues = controlPoints.Select(arg => arg.Value.Value).ToArray();

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
                                var t0 = controlPoints[i + 0].Value.NormalizedTime;
                                var t1 = controlPoints[i + 1].Value.NormalizedTime;
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

                dc.DrawGeometry(null, new Pen(Brushes.Pink, 1), geometry);
            }

            if (_IsScanning)
            {
                var sz = ControlPoint.GetControlAreaSize(new Size(ActualWidth, ActualHeight));

                // Subtract ControlPoint.HalfSize is for adjusting to render layout.
                // _ScanningValue / _Owner.MaxValue is normalize vertical value line.
                // Multiply sz.Height for stretching control area vertical and subtract from sz.Height for inverting Y axis.
                var pos = new Point(_ScanningTime * sz.Width, sz.Height - (_ScanningValue / _Editor.MaxValue * sz.Height));
                pos.X += ControlPoint.ComputeOffset.X;
                pos.Y += ControlPoint.ComputeOffset.Y;

                dc.DrawLine(new Pen(Brushes.Red, 1), new Point(pos.X, 0), new Point(pos.X, ActualHeight));
                dc.DrawRectangle(Brushes.DarkRed, null, new Rect(pos + ControlPoint.RenderOffset, ControlPoint.FullSize));

                var text = string.Format("(v={0:F2},t={1:F2})", _ScanningValue, _ScanningTime);
                ScanningValueFont.Render(dc, text, new Point(_ScanningPoint.X + 2, pos.Y + ControlPoint.FullSize.Height));
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

        void UpdateClampEnabled(bool enable)
        {
            _IsClamped = enable;

            InvalidateVisual();
        }

        void UpdateDraggingControlPosition(MouseEventArgs e)
        {
            var controls = Children.OfType<ControlPoint>().ToArray();

            int index = controls.IndexOf(_DraggingControlPoint);
            var min_x = index > 0 ? controls[index - 1].PositionX : ControlAreaStartX;
            var max_x = index < controls.Length - 1 ? controls[index + 1].PositionX : ControlAreaEndX;

            var pos = e.GetPosition(this);
            _DraggingControlPoint?.UpdatePosition(pos, min_x, max_x);

            InvalidateVisual();
        }

        void UpdateScanningValue(MouseEventArgs e)
        {
            var controls = Children.OfType<ControlPoint>().ToArray();
            if (controls.Length > 0)
            {
                var p0 = controls.First();
                var p1 = controls.Last();

                _ScanningPoint = e.GetPosition(this);
                _ScanningPoint.X = MathUtility.Clamp(_ScanningPoint.X, p0.PositionX, p1.PositionX);

                _ScanningTime = (float)((_ScanningPoint.X - Math.Ceiling(ControlPoint.HalfSize)) / (ControlAreaEndX - ControlAreaStartX));

                switch (_Editor.Type)
                {
                    case CurveType.Linear:
                        _ScanningValue = GetScanningLinearValue(_ScanningTime);
                        break;
                    case CurveType.CatmullRom:
                        _ScanningValue = GetScanningCatmullRomValue(_ScanningTime);
                        break;
                    case CurveType.B_Spline:
                        throw new NotImplementedException();
                }
            }
        }

        float GetScanningLinearValue(float t)
        {
            var controls = Children.OfType<ControlPoint>().ToArray();
            var p0 = controls.Reverse().FirstOrDefault(arg => arg.Value.NormalizedTime <= t);
            var p1 = controls.FirstOrDefault(arg => arg.Value.NormalizedTime > t);

            float t0 = p0.Value.NormalizedTime;
            float t1 = p1 != null ? p1.Value.NormalizedTime : p0.Value.NormalizedTime;
            float dt = t1 - t0;
            if (dt == 0)
            {
                return p0.Value.Value;
            }

            float v0 = p0.Value.Value;
            float v1 = p1.Value.Value;
            float st = (t - t0) / dt;

            return (v1 - v0) * st + v0;
        }

        float GetScanningCatmullRomValue(float t)
        {
            var controls = Children.OfType<ControlPoint>().ToArray();

            if(t == 0)
            {
                return controls.First().Value.Value;
            }
            if(t == 1.0f)
            {
                return controls.Last().Value.Value;
            }

            var p0 = controls.Reverse().FirstOrDefault(arg => arg.Value.NormalizedTime <= t);
            var p1 = controls.FirstOrDefault(arg => arg.Value.NormalizedTime > t);

            var t0 = p0.Value.NormalizedTime;
            var t1 = p1 != null ? p1.Value.NormalizedTime : p0.Value.NormalizedTime;
            var dt = t1 - t0;
            if (dt == 0)
            {
                return p0.Value.Value;
            }
            var seg_t = (t - t0) / dt;

            var values = controls.Select(arg => arg.Value.Value).ToArray();
            var value = CatmullRomSpline.ComputeSingle(controls.IndexOf(p0), seg_t, values);

            if(IsClamped == false)
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

            CurveTypeComboBox.IsEnabled = Editor.IsReadOnlyType == false;

            switch(Editor.Type)
            {
                case CurveType.Linear:
                    CurveTypeComboBox.SelectedIndex = 0;
                    break;
                case CurveType.CatmullRom:
                    CurveTypeComboBox.SelectedIndex = 1;
                    break;
            }

            CurveTypeComboBox.SelectionChanged += CurveTypeComboBox_SelectionChanged;

            ClampCheckBox.Checked += ClampCheckBox_Checked;
            ClampCheckBox.Unchecked += ClampCheckBox_Unchecked;

            var cpTbl = Editor.ItemsSource.OfType<ControlValue>().ToArray();

            EditorCanvas.AddControlPoints(cpTbl);
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
                EditorCanvas.AddControlPoints(newValue.OfType<ControlValue>().ToArray());
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
            ClampCheckBox.Unchecked -= ClampCheckBox_Unchecked;
            ClampCheckBox.Checked -= ClampCheckBox_Checked;
        }

        void CurveTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedContent = (e.AddedItems[0] as ComboBoxItem).Content;

            switch(selectedContent)
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
        }

        void ClampCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            EditorCanvas.IsClamped = false;
        }

        void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    EditorCanvas.ClearControlPoints();
                    break;
                case NotifyCollectionChangedAction.Add:
                    EditorCanvas.InsertControlPoints(e.NewStartingIndex, e.NewItems.OfType<ControlValue>().ToArray());
                    break;
                case NotifyCollectionChangedAction.Remove:
                    EditorCanvas.InsertControlPoints(e.OldStartingIndex, e.OldItems.OfType<ControlValue>().ToArray());
                    break;
            }

            EditorCanvas.InvalidateVisual();
        }
    }
}
