using DataPlots.Models;
using DataPlots.Wpf.Extensions;
using System.ComponentModel;
using System.Data;
using System.Windows.Shapes;

namespace DataPlots.Wpf.Plots
{
    public class PlotView : Canvas
    {
        #region Events
        public event EventHandler<DataPointHoverEventArgs>? DataPointHoverChanged;
        public event EventHandler<DataPointSelectedEventArgs>? DataPointSelected;
        #endregion Events
        #region Fields
        private readonly Rectangle _selectionRect;
        private readonly Image _plotImage;
        private readonly ToolTip _toolTip;
        private WriteableBitmap _bitmap;
        private IPlotTransform _transform = IPlotTransform.Empty;
        private RectD _currentView = RectD.Empty;
        private RectD _innerRenderRect = RectD.Empty;
        private ThicknessD _plotPadding = new ThicknessD(60, 50, 60, 50);
        private Thickness _effectivePadding = new Thickness(60, 50, 60, 50);
        private ISeries? _hoveredSeries;
        private int _hoveredIndex = -1;
        private Point _boxStart;
        private Rect _boxOverlay;
        private Point _lastMousePos;
        private bool _isBoxZooming;
        private bool _isPanning;
        // Label caching
        private List<TextBlock> inactiveLabels = new List<TextBlock>();
        private List<TextBlock> activeLabels = new List<TextBlock>();
        #endregion Fields
        #region Dependency Properties
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register(nameof(Model),
                typeof(IPlotModel),
                typeof(PlotView),
                new PropertyMetadata(null, OnModelChanged));

        public static readonly DependencyProperty ZoomModeProperty =
            DependencyProperty.Register(
                nameof(ZoomMode),
                typeof(ZoomMode),
                typeof(PlotView),
                new PropertyMetadata(ZoomMode.XY, OnZoomModeChanged));

        // Using a DependencyProperty as the backing store for PlotPadding.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PlotPaddingProperty =
            DependencyProperty.Register(nameof(PlotPadding),
                typeof(Thickness),
                typeof(PlotView),
                new PropertyMetadata(new Thickness(60.0d, 50.0d, 60.0d, 50.0d),
                OnPlotPaddingChanged));

        private static void OnPlotPaddingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlotView view)
            {
                view.InvalidatePlot(); // Re-render when padding changes
                view._plotImage.Margin = (Thickness)e.NewValue;
            }
        }

        // Using a DependencyProperty as the backing store for PlotBackgroundColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PlotBackgroundColorProperty =
            DependencyProperty.Register(nameof(PlotBackgroundColor),
                typeof(SolidColorBrush),
                typeof(PlotView),
                new PropertyMetadata(Colors.White.ToSolidColorBrush()));

        // Using a DependencyProperty as the backing store for TooltipBackgroundColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TooltipBackgroundColorProperty =
            DependencyProperty.Register(nameof(TooltipBackgroundColor),
                typeof(Brush),
                typeof(PlotView),
                new PropertyMetadata(Colors.Black.ToSolidColorBrush()));

        // Using a DependencyProperty as the backing store for TooltipForegroundColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TooltipForegroundColorProperty =
            DependencyProperty.Register(nameof(TooltipForegroundColor),
                typeof(Brush),
                typeof(PlotView),
                new PropertyMetadata(Colors.White.ToSolidColorBrush()));

        // Using a DependencyProperty as the backing store for GridColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GridColorProperty =
            DependencyProperty.Register(nameof(GridColor),
                typeof(Color),
                typeof(PlotView),
                new PropertyMetadata(Colors.Gray));

        public SolidColorBrush PlotBackgroundColor
        {
            get { return (SolidColorBrush)GetValue(PlotBackgroundColorProperty); }
            set { SetValue(PlotBackgroundColorProperty, value); }
        }

        public Thickness PlotPadding
        {
            get { return (Thickness)GetValue(PlotPaddingProperty); }
            set { SetValue(PlotPaddingProperty, value); }
        }

        public Brush TooltipBackgroundColor
        {
            get { return (Brush)GetValue(TooltipBackgroundColorProperty); }
            set { SetValue(TooltipBackgroundColorProperty, value); }
        }

        public Brush TooltipForegroundColor
        {
            get { return (Brush)GetValue(TooltipForegroundColorProperty); }
            set { SetValue(TooltipForegroundColorProperty, value); }
        }

        public Color GridColor
        {
            get { return (Color)GetValue(GridColorProperty); }
            set { SetValue(GridColorProperty, value); }
        }

        public Color BorderColor
        {
            get { return (Color)GetValue(BorderColorProperty); }
            set { SetValue(BorderColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BorderColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BorderColorProperty =
            DependencyProperty.Register(nameof(BorderColor),
                typeof(Color),
                typeof(PlotView),
                new PropertyMetadata(Colors.Black));

        public Color FontColor
        {
            get { return (Color)GetValue(FontColorProperty); }
            set { SetValue(FontColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FontColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FontColorProperty =
            DependencyProperty.Register(nameof(FontColor),
                typeof(Color),
                typeof(PlotView),
                new PropertyMetadata(Colors.Black));

        public double AxisTitleFontSize
        {
            get { return (double)GetValue(AxisTitleFontSizeProperty); }
            set { SetValue(AxisTitleFontSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AxisTitleFontSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AxisTitleFontSizeProperty =
            DependencyProperty.Register(nameof(AxisTitleFontSize),
                typeof(double),
                typeof(PlotView),
                new PropertyMetadata(14.0d));

        public double TickLabelFontSize
        {
            get { return (double)GetValue(TickLabelFontSizeProperty); }
            set { SetValue(TickLabelFontSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TickLabelFontSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TickLabelFontSizeProperty =
            DependencyProperty.Register(nameof(TickLabelFontSize),
                typeof(double),
                typeof(PlotView),
                new PropertyMetadata(12.0d));
        #endregion Dependency Properties
        #region Properties
        public ZoomMode ZoomMode
        {
            get { return (ZoomMode)GetValue(ZoomModeProperty); }
            set { SetValue(ZoomModeProperty, value); }
        }

        public IPlotModel Model
        {
            get
            {
                if (GetValue(ModelProperty) is IPlotModel model)
                    return model;
                else
                    return IPlotModel.Empty;
            }

            set
            {
                if (GetValue(ModelProperty) is IPlotModel model)
                    model.PropertyChanged -= OnModelPropertyChanged;
                SetValue(ModelProperty, value);
                value.PropertyChanged += OnModelPropertyChanged;
                _currentView = RectD.Empty;
                InvalidatePlot();
            }
        }
        #endregion Properties
        #region Constructor
        public PlotView()
        {
            _bitmap = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Pbgra32, null);
            SnapsToDevicePixels = true;

            _toolTip = new ToolTip()
            {
                Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse,
                HasDropShadow = true,
                Padding = new Thickness(6.0d)
            };

            ToolTipService.SetInitialShowDelay(this, 0);
            ToolTip = _toolTip;

            _selectionRect = new Rectangle()
            {
                Stroke = Brushes.DodgerBlue,
                StrokeThickness = 2.0d,
                StrokeDashArray = new DoubleCollection() { 4, 2 },
                Fill = Colors.DodgerBlue.ToSolidColorBrush(),
                Opacity = 0.4d,
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };

            Children.Add(_selectionRect);

            _plotImage = new Image
            {
                Source = _bitmap,
                Stretch = Stretch.None,
                IsHitTestVisible = false
            };

            Children.Add(_plotImage);
            SetZIndex(_plotImage, -100);

            UpdateBrushes();

            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            MouseMove += OnMouseMove;
            MouseWheel += OnMouseWheel;
            MouseLeave += OnMouseLeave;
            SizeChanged += (_, __) => InvalidatePlot();
            Loaded += (_, __) => InvalidatePlot();
        }

        private void UpdateBrushes()
        {
            _toolTip.Background = TooltipBackgroundColor;
            _toolTip.Foreground = TooltipForegroundColor;
            Background = PlotBackgroundColor;
        }
        #endregion Constructor
        #region Methods

        #endregion Methods
        #region Private Methods
        private void InvalidatePlot()
        {
            if (ActualWidth < 10 || ActualHeight < 10) return;

            if (_currentView == RectD.Empty)
                _currentView = Model.CalculateDataRect();

            var padding = PlotPadding;

            int innerWidth = (int)Math.Max(1, (ActualWidth - padding.Left - padding.Right));
            int innerHeight = (int)Math.Max(1, (ActualHeight - padding.Top - padding.Bottom));

            if (_bitmap.PixelWidth != innerWidth || _bitmap.PixelHeight != innerHeight)
            {
                _bitmap = new WriteableBitmap(innerWidth, innerHeight, 96, 96, PixelFormats.Pbgra32, null);
                _plotImage.Source = _bitmap;
            }

            _plotImage.Margin = new Thickness(padding.Left, padding.Top, padding.Right, padding.Bottom);
            _plotImage.Width = innerWidth;
            _plotImage.Height = innerHeight;

            Render();
            InvalidateVisual();
        }

        private void Render()
        {
            var dataRect = _currentView;
            if (dataRect.Width <= 0) dataRect.Width = 1;
            if (dataRect.Height <= 0) dataRect.Height = 1;

            var userPadding = PlotPadding;
            double extraLeft = 0.0d;
            double extraRight = 0.0d;
            double maxLabelExtent = 0.0d;

            MeasureExtraPadding(ref extraLeft, ref extraRight, ref maxLabelExtent, dataRect);

            _effectivePadding = new Thickness(
                Math.Max(userPadding.Left, extraLeft),
                userPadding.Top,
                Math.Max(userPadding.Right, extraRight),
                userPadding.Bottom);

            int innerWidth = (int)Math.Max(1.0d, ActualWidth - _effectivePadding.Left - _effectivePadding.Right);
            int innerHeight = (int)Math.Max(1.0d, ActualHeight - _effectivePadding.Top - _effectivePadding.Bottom);

            // Resize bitmap if needed
            if (_bitmap.PixelWidth != innerWidth || _bitmap.PixelHeight != innerHeight)
            {
                _bitmap = new WriteableBitmap(innerWidth, innerHeight, 96, 96, PixelFormats.Pbgra32, null);
                _plotImage.Source = _bitmap;
            }

            _plotImage.Margin = new Thickness(_effectivePadding.Left, _effectivePadding.Top,
                _effectivePadding.Right, _effectivePadding.Bottom);
            _plotImage.Width = innerWidth;
            _plotImage.Height = innerHeight;

            var renderRect = new RectD(0.0d, 0.0d, innerWidth, innerHeight);

            _innerRenderRect = new RectD(
                _effectivePadding.Left,
                _effectivePadding.Top,
                innerWidth,
                innerHeight);

            _transform = new PlotTransform(dataRect, renderRect);

            // Clear old labels and add new ones
            HideOldLabels();

            _bitmap.Clear(PlotBackgroundColor.Color);

            DrawGridLines(renderRect, dataRect);
            DrawPlotContent(renderRect, dataRect);
            DrawAxesAndLabels(renderRect, dataRect);
        }

        private void MeasureExtraPadding(ref double extraLeft, ref double extraRight, ref double maxLabelExtent, RectD dataRect)
        {
            double[] ticks = Array.Empty<double>();
            string[] labels = Array.Empty<string>();
            foreach (Axis axis in Model.Axes)
            {
                if (!axis.IsVisible) continue;

                if (!string.IsNullOrEmpty(axis.Title))
                {
                    if (axis.Position == AxisPosition.Left)
                    {
                        (ticks, labels) = TickGenerator.Generate(dataRect.Y, dataRect.Bottom);
                    }
                    else if (axis.Position == AxisPosition.Right)
                    {
                        (ticks, labels) = TickGenerator.Generate(dataRect.Y, dataRect.Bottom);
                    }

                    if (axis.Position == AxisPosition.Left || axis.Position == AxisPosition.Right)
                    {
                        // 1. Measure all tick labels for this axis
                        foreach (string label in labels)
                        {
                            if (string.IsNullOrEmpty(label)) continue;
                            Size size = label.MeasureText(TickLabelFontSize);
                            maxLabelExtent = Math.Max(maxLabelExtent, size.Width);
                        }

                        // 2. Measure title (rotated → height becomes the horizontal extent)
                        if (!string.IsNullOrEmpty(axis.Title))
                        {
                            Size titleSize = axis.Title.MeasureText(AxisTitleFontSize);
                            maxLabelExtent = Math.Max(maxLabelExtent, titleSize.Height); // rotated!
                        }

                        // 3. Add some breathing room (10–15 px is standard)
                        double required = maxLabelExtent + 15; // 15 = tick length + gap

                        if (axis.Position == AxisPosition.Left)
                            extraLeft = Math.Max(extraLeft, required);
                        else if (axis.Position == AxisPosition.Right)
                            extraRight = Math.Max(extraRight, required);
                    }
                }
            }
        }

        private void HideOldLabels()
        {
            foreach (TextBlock tb in activeLabels)
            {
                tb.Visibility = Visibility.Collapsed;
                inactiveLabels.Add(tb);
            }
            activeLabels.Clear();
        }

        private void DrawGridLines(RectD renderRect, RectD dataRect)
        {
            var gridColor = GridColor; // Light gray

            // Vertical grid lines (X ticks)
            var (xTicks, _) = TickGenerator.Generate(dataRect.X, dataRect.Right);
            foreach (double tick in xTicks)
            {
                double x = _transform.DataToScreen(new PointD(tick, 0)).X;
                _bitmap.DrawLine(x, renderRect.Top, x, renderRect.Bottom, gridColor);
            }

            // Horizontal grid lines (Y ticks)
            var (yTicks, _) = TickGenerator.Generate(dataRect.Y, dataRect.Bottom);
            foreach (double tick in yTicks)
            {
                double y = _transform.DataToScreen(new PointD(0, tick)).Y;
                _bitmap.DrawLine(renderRect.Left, y, renderRect.Right, y, gridColor);
            }
        }

        private void DrawPlotContent(RectD renderRect, RectD dataRect)
        {
            // Series
            foreach (var series in Model!.Series.Where(s => s.IsVisible))
            {
                if (series is LineSeries line)
                {
                    var color = line.Stroke.ToMediaColor();
                    for (int i = 1; i < line.Points.Count; i++)
                    {
                        var p0 = _transform!.DataToScreen(new PointD(line.Points[i - 1].X, line.Points[i - 1].Y));
                        var p1 = _transform!.DataToScreen(new PointD(line.Points[i].X, line.Points[i].Y));
                        _bitmap.DrawLine(p0.X, p0.Y, p1.X, p1.Y, color, line.Thickness);
                    }
                }
                else if (series is ScatterSeries scatter)
                {
                    var color = scatter.Fill.ToMediaColor();
                    var selected = Colors.DodgerBlue;
                    foreach (var pt in scatter.Points)
                    {
                        var sp = _transform!.DataToScreen(new PointD(pt.X, pt.Y));
                        _bitmap.FillCircle(sp.X, sp.Y, scatter.PointSize, pt.Selected ? selected : color, BorderColor, 0.5d);
                    }
                }
            }
        }

        private void DrawAxesAndLabels(RectD renderRect, RectD dataRect)
        {
            // Titles
            foreach (Axis axis in Model.Axes)
            {
                DrawAxisTicks(renderRect, dataRect, axis);
                DrawAxisTitle(renderRect, axis);
            }
        }

        private void DrawAxisTicks(RectD renderRect, RectD dataRect, Axis axis)
        {
            switch (axis.Position)
            {
                case AxisPosition.Bottom:
                    // X axis
                    var (xbTicks, xbLabels) = TickGenerator.Generate(dataRect.X, dataRect.Right);
                    for (int i = 0; i < xbTicks.Length; i++)
                        DrawXTick(renderRect, dataRect, xbTicks[i], axis.TickLength, xbLabels[i], false);
                    break;
                case AxisPosition.Left:
                    // Y axis
                    var (ylTicks, ylLabels) = TickGenerator.Generate(dataRect.Y, dataRect.Bottom);
                    for (int i = 0; i < ylTicks.Length; i++)
                        DrawYTick(renderRect, dataRect, ylTicks[i], axis.TickLength, ylLabels[i], true);
                    break;
                case AxisPosition.Top:
                    // X axis
                    var (xtTicks, xtLabels) = TickGenerator.Generate(dataRect.X, dataRect.Right);
                    for (int i = 0; i < xtTicks.Length; i++)
                        DrawXTick(renderRect, dataRect, xtTicks[i], axis.TickLength, xtLabels[i], true);
                    break;
                case AxisPosition.Right:
                    // Y axis
                    var (yrTicks, yrLabels) = TickGenerator.Generate(dataRect.Y, dataRect.Bottom);
                    for (int i = 0; i < yrTicks.Length; i++)
                        DrawYTick(renderRect, dataRect, yrTicks[i], axis.TickLength, yrLabels[i], false);
                    break;
            }
        }

        private void DrawXTick(RectD renderRect, RectD dataRect, double tick, double length, string label, bool isTop)
        {
            PointD sp = _transform.DataToScreen(new PointD(tick, dataRect.Y));

            double screenX = sp.X;
            double controlX = PlotPadding.Left + screenX;

            double edgeY = isTop ? PlotPadding.Top : (ActualHeight - PlotPadding.Bottom);
            double tickDir = isTop ? -1.0d : +1.0d;

            //double y0 = edgeY;
            //double y1 = edgeY + tickDir * length;
            //_bitmap.DrawLine(sp.X, y0, sp.X, y1, BorderColor);

            double labelY = edgeY + tickDir * (length + 8);
            AddLabel(label, controlX, labelY, FontColor, TickLabelFontSize, centerX: true, centerY: true);
        }

        private void DrawYTick(RectD renderRect, RectD dataRect, double tick, double length, string label, bool isLeft)
        {
            PointD sp = _transform.DataToScreen(new PointD(dataRect.X, tick));

            double screenY = sp.Y;
            double controlY = PlotPadding.Top + screenY;

            double edgeX = isLeft ? PlotPadding.Left : (ActualWidth - PlotPadding.Right);
            double tickDir = isLeft ? -1.0d : +1.0d;

            //double x0 = edgeX;
            //double x1 = edgeX + tickDir * length;
            //_bitmap.DrawLine(x0, sp.Y, x1, sp.Y, BorderColor);

            // Measure label width
            var labelWidth = !isLeft ? 0.0d : label.MeasureText(TickLabelFontSize).Width;
            // Dynamic offset: always fully visible, never clipped
            double labelX = edgeX + tickDir * (labelWidth + 12.0d); // 12px padding from axis
            AddLabel(label, labelX, controlY, FontColor, TickLabelFontSize, centerX: false, centerY: true);
        }

        private void DrawAxisTitle(RectD renderRect, Axis axis)
        {
            if (string.IsNullOrEmpty(axis.Title))
                return;

            double x, y;
            double angle = 0.0d;
            bool cx = true;
            bool cy = true;

            Thickness padding = PlotPadding;
            switch (axis.Position)
            {
                case AxisPosition.Bottom:
                    x = _innerRenderRect.Left + renderRect.Width * 0.5d;
                    y = _innerRenderRect.Bottom + padding.Bottom * 0.6d;
                    break;
                case AxisPosition.Top:
                    x = _innerRenderRect.Left + renderRect.Width * 0.5d;
                    y = _innerRenderRect.Top - padding.Top * 0.4d;
                    break;
                case AxisPosition.Left:
                    x = axis.Title!.MeasureText(AxisTitleFontSize).Height * -0.3;
                    y = _innerRenderRect.Top + renderRect.Height * 0.5d;
                    cx = false;
                    angle = -90.0d;
                    break;
                case AxisPosition.Right:
                default:
                    x = ActualWidth - _effectivePadding.Right * 0.35d;
                    y = _innerRenderRect.Top + renderRect.Height * 0.5d;
                    angle = +90.0d;
                    break;
            }
            AddLabel(axis.Title!, x, y, FontColor, AxisTitleFontSize, angle, cx, cy);
        }

        private void ZoomToFit()
        {
            _currentView = RectD.Empty;
            InvalidatePlot();
        }

        private void UpdateHover(ISeries? series, int index, DataPoint? point)
        {
            if (_hoveredSeries == series && _hoveredIndex == index)
                return;
            _hoveredSeries = series;
            _hoveredIndex = index;

            if (series != null && point != null)
            {
                if (point.Tag == null)
                    _toolTip.Content = $"{series.Title}\nX: {point.X:F2}\nY: {point.Y:F2}";
                else
                    _toolTip.Content = $"{series.Title}\nX: {point.X:F2}\nY: {point.Y:F2}\n{point.Tag}";
                _toolTip.IsOpen = true;
            }
            else
            {
                _toolTip.IsOpen = false;
            }

            DataPointHoverChanged?.Invoke(this, new DataPointHoverEventArgs(series, index, point));
        }

        private bool Zooms()
        {
            return ZoomMode != ZoomMode.None;
        }

        private bool ZoomsX()
        {
            return ZoomMode == ZoomMode.XOnly || ZoomMode == ZoomMode.XY;
        }

        private bool ZoomsY()
        {
            return ZoomMode == ZoomMode.YOnly || ZoomMode == ZoomMode.XY;
        }

        public void AddLabel(string text, double x, double y,
            Color color, double fontSize = 12, double angle = 0, bool centerX = true, bool centerY = true)
        {
            (bool add, TextBlock tb) = GetLabel();
            tb.Text = text;
            tb.Foreground = new SolidColorBrush(color);
            tb.FontSize = fontSize;
            tb.RenderTransformOrigin = new Point(0.5d, 0.5d);
            tb.IsHitTestVisible = false;
            tb.Visibility = Visibility.Visible;
            tb.RenderTransform = new RotateTransform(angle);

            if (centerX || centerY)
            {
                tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                tb.Arrange(new Rect(tb.DesiredSize));
            }

            double left = centerX ? x - tb.ActualWidth / 2.0d : x;
            double top = centerY ? y - tb.ActualHeight / 2.0d : y;

            SetLeft(tb, left);
            SetTop(tb, top);

            if (add)
                Children.Add(tb);
        }

        private (bool add, TextBlock) GetLabel()
        {
            if (inactiveLabels.Count > 0)
            {
                TextBlock label = inactiveLabels[^1];
                inactiveLabels.RemoveAt(inactiveLabels.Count - 1);
                activeLabels.Add(label);
                return (false, label);
            }
            else
            {
                TextBlock label = new TextBlock();
                activeLabels.Add(label);
                return (true, label);
            }
        }
        #endregion Methods
        #region Event Handlers
        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PlotView view = (PlotView)d;
            view._currentView = RectD.Empty;
            view.InvalidatePlot();
            view.UpdateBrushes();
        }

        private static void OnZoomModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PlotView plotView = (PlotView)d;
            // Optional: Invalidate rendering and reset zoom if switching to None
            if (plotView.ZoomMode == ZoomMode.None)
            {
                // Reset transform to full view
                plotView.ZoomToFit();
            }
            plotView.InvalidateVisual(); // Trigger re-render if axes need updating
        }
        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ZoomMode == ZoomMode.None) return;

            double factor = e.Delta > 0 ? 0.9 : 1.1;
            Point mousePoint = e.GetPosition(this);
            PointD mouseData = _transform.ScreenToData(new PointD(mousePoint.X, mousePoint.Y));

            double factorX = ZoomsX() ? factor : 1.0d;
            double factorY = ZoomsY() ? factor : 1.0d;
            double newW = _currentView.Width * factorX;
            double newH = _currentView.Height * factorY;
            double newX = mouseData.X - (mouseData.X - _currentView.X) * factorX;
            double newY = mouseData.Y - (mouseData.Y - _currentView.Y) * factorY;

            _currentView = new RectD(newX, newY, newW, newH);
            InvalidatePlot();
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ZoomToFit();
                return;
            }

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && Zooms())
            {
                _isBoxZooming = true;
                _boxOverlay.Width = 1.0d;
                _boxOverlay.Height = 1.0d;
                _boxStart = e.GetPosition(this);
                _selectionRect.Width = _selectionRect.Height = 0;
                SetLeft(_selectionRect, _boxStart.X);
                SetTop(_selectionRect, _boxStart.Y);
                _selectionRect.Visibility = Visibility.Visible;
                CaptureMouse();
                e.Handled = true;
            }
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                if (_hoveredSeries != null && _hoveredIndex > -1)
                {
                    DataPoint point = _hoveredSeries.Points[_hoveredIndex];
                    point.Selected = !point.Selected;
                    CaptureMouse();
                    e.Handled = true;
                    InvalidatePlot();
                    DataPointSelected?.Invoke(this,
                        new DataPointSelectedEventArgs(_hoveredSeries, _hoveredIndex, point, Core.MouseButton.Left));
                }
            }
            else
            {
                _isPanning = true;
                _lastMousePos = e.GetPosition(this);
            }

            base.OnMouseLeftButtonDown(e);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point pos = e.GetPosition(this);

            // Box zoom rubber band
            if (_isBoxZooming)
            {
                double left = Math.Min(_boxStart.X, pos.X);
                double top = Math.Min(_boxStart.Y, pos.Y);
                double width = Math.Abs(pos.X - _boxStart.X);
                double height = Math.Abs(pos.Y - _boxStart.Y);

                _boxOverlay = new Rect(left, top, width, height);

                // set left and top again to account for dragging to left
                SetLeft(_selectionRect, left);
                SetTop(_selectionRect, top);
                _selectionRect.Width = width;
                _selectionRect.Height = height;
                _selectionRect.Visibility = Visibility.Visible;
            }
            // Panning
            else if (_isPanning)
            {
                Vector delta = _lastMousePos - pos;
                double dx = 0.0d;
                double dy = 0.0d;

                if (ZoomsX())
                    dx = delta.X * _currentView.Width / _innerRenderRect.Width;

                if (ZoomsY())
                    dy = -delta.Y * _currentView.Height / _innerRenderRect.Height;

                _currentView = new RectD(
                    _currentView.X + dx,
                    _currentView.Y + dy,
                    _currentView.Width,
                    _currentView.Height);

                _lastMousePos = pos;
                InvalidatePlot();
            }
            // Hovering
            else
            {
                ISeries? bestSeries = null;
                int bestIndex = -1;
                if (pos.X >= PlotPadding.Left && pos.X < ActualWidth - PlotPadding.Right &&
                    pos.Y >= PlotPadding.Top && pos.Y < ActualHeight - PlotPadding.Bottom)
                {
                    double bestDist = double.MaxValue;
                    //PointD posD = pos.ToPointD();
                    PointD localPos = new PointD(
                        pos.X - PlotPadding.Left,
                        pos.Y - PlotPadding.Top);

                    foreach (ISeries series in Model.Series.Where(s => s.IsVisible))
                    {
                        var hit = series.GetNearestPoint(localPos, _transform, 12.0d);
                        if (hit.HasValue && hit.Value.DistancePixels < bestDist)
                        {
                            bestDist = hit.Value.DistancePixels;
                            bestSeries = series;
                            bestIndex = hit.Value.PointIndex;
                        }
                    }
                }

                if (bestSeries != null && bestIndex >= 0)
                {
                    var point = bestSeries.Points[bestIndex];
                    UpdateHover(bestSeries, bestIndex, point);
                }
                else
                {
                    UpdateHover(null, -1, null);
                }
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isBoxZooming)
            {
                _isBoxZooming = false;
                _selectionRect.Visibility = Visibility.Collapsed;
                ReleaseMouseCapture();

                if (_boxOverlay.Width < 10.0d || _boxOverlay.Height < 10.0d)
                    return; // ignore tiny clicks

                PointD p1 = _transform.ScreenToData(new PointD(
                    _boxOverlay.Left - _innerRenderRect.Left,
                    _boxOverlay.Top - _innerRenderRect.Top));

                PointD p2 = _transform.ScreenToData(new PointD(
                    _boxOverlay.Right - _innerRenderRect.Left,
                    _boxOverlay.Bottom - _innerRenderRect.Top));

                RectD updatedView = RectD.Normalized(p1, p2);

                if (!ZoomsX())
                {
                    updatedView.X = _currentView.X;
                    updatedView.Width = _currentView.Width;
                }

                if (!ZoomsY())
                {
                    updatedView.Y = _currentView.Y;
                    updatedView.Height = _currentView.Height;
                }

                _currentView = updatedView;
                InvalidatePlot();
            }
            else if (_isPanning)
            {
                _isPanning = false;
                ReleaseMouseCapture();
            }
            base.OnMouseLeftButtonUp(e);
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            UpdateHover(null, -1, null);
            if (_isPanning)
            {
                _isPanning = false;
                ReleaseMouseCapture();
            }
        }

        private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            InvalidatePlot();
        }
        #endregion Private Methods
    }
}
