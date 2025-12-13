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
        /// <summary>
        /// Event that indicates that a different data point 
        /// is being hovered over in the plot
        /// </summary>
        public event EventHandler<DataPointHoverEventArgs>? DataPointHoverChanged;

        /// <summary>
        /// Event that fires when a plot dat point is clicked on
        /// </summary>
        public event EventHandler<DataPointSelectedEventArgs>? DataPointSelected;
        #endregion Events
        #region Static Fields
        /// <summary>
        /// Empty value used as a placeholder before actual rendering to
        /// prevent every new <see cref="PlotView"/> initializing an 
        /// unused <see cref="WriteableBitmap"/>.
        /// </summary>
        private static WriteableBitmap _emptyBitmap = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Pbgra32, null);
        #endregion Static Fields
        #region Fields
        /// <summary>
        /// Defines the data coordinate range currently displayed by the plot. It 
        /// sets the minimum and maximum X/Y values that are visible (the axis 
        /// limits). It is the source rectangle used by the 
        /// <see cref="_dataToScreenTransform"/> to determine how to translate 
        /// data values to screen pixels. This value changes only upon zooming, 
        /// panning, or a call to <see cref="ZoomToFit"/>.
        /// </summary>
        private RectD _dataViewport = RectD.Empty;

        /// <summary>
        /// Defines the final bounding box in Canvas coordinates for the inner 
        /// plotting area where the data is actually drawn. Its origin (X, Y) 
        /// is the offset from the top-left of the main PlotView control (which 
        /// is equal to <see cref="_effectiveMargin"/>.Left/Top), and its size (Width, Height) 
        /// is equal to the pixel dimensions (<see cref="_plotPixels"/>.Width/Height). 
        /// It is used to position axes and mouse interaction checks correctly 
        /// relative to the entire control.
        /// </summary>
        private RectD _innerRenderRect = RectD.Empty;

        /// <summary>
        /// Size of tick marks in plot.
        /// </summary>
        private PointD _tickLength = PointD.Empty;

        /// <summary>
        /// Size of the plot area in pixels (excluding margins).
        /// </summary>
        private SizeI _plotPixels = SizeI.Empty;

        /// <summary>
        /// Last mouse position during panning operations.
        /// </summary>
        private Point _lastMousePanPoint = new Point(0.0d, 0.0d);

        /// <summary>
        /// Starting point of the selection rectangle during box-zoom operations.
        /// </summary>
        private Point _selectionRectangleStartPoint = new Point(0.0d, 0.0d);

        /// <summary>
        /// Size of the selection rectangle during box-zoom operations,
        /// </summary>
        private Rect _selectionRectangleArea = new Rect(0.0d, 0.0d, 1.0d, 1.0d);

        /// <summary>
        /// Margin used for layout and rendering, adjusted dynamically 
        /// based on axis titles and labels.
        /// </summary>
        private Thickness _effectiveMargin = new Thickness(60, 50, 60, 50);

        /// <summary>
        /// ISeries currently being hovered over, or null if none.
        /// </summary>
        private ISeries? _hoveredSeries = null;

        /// <summary>
        /// Data point index currently being hovered over, or -1 if none.
        /// </summary>
        private int _hoveredIndex = -1;

        /// <summary>
        /// Tracks whether the user is currently box-zooming the plot.
        /// </summary>
        private bool _isBoxZooming = false;

        /// <summary>
        /// Tracks whether the user is currently panning the plot.
        /// </summary>
        private bool _isPanning = false;

        /// <summary>
        /// Bitmap used for rendering the plot.
        /// </summary>
        private WriteableBitmap _bitmap = _emptyBitmap;

        /// <summary>
        /// Translates coordinates from data space to screen space.
        /// </summary>
        private IPlotTransform _dataToScreenTransform = IPlotTransform.Empty;

        /// <summary>
        /// Rectangle used to display the zoom selection box.
        /// </summary>
        private readonly Rectangle _selectionRect;

        /// <summary>
        /// Image control that displays the plot bitmap.
        /// </summary>
        private readonly Image _plotImage;

        /// <summary>
        /// ToolTip used for displaying additional information on the plot.
        /// </summary>
        private readonly ToolTip _plotToolTip;

        /// <summary>
        /// Cache of <see cref="TextBlock"/> labels to use for dynamically
        /// created labels.
        /// </summary>
        private readonly List<TextBlock> _inactiveLabels = new List<TextBlock>();

        /// <summary>
        /// List of <see cref="TextBlock"/> that are currently used as labels.
        /// </summary>
        private readonly List<TextBlock> _activeLabels = new List<TextBlock>();

        /// <summary>
        /// Cache of label size measurements
        /// </summary>
        private readonly Dictionary<(string text, double fontSize), Size> _textSizeCache =
            new Dictionary<(string text, double fontSize), Size>();

        /// <summary>
        /// Cache of ticks for rendering
        /// </summary>
        private readonly Dictionary<AxisPosition, (double[] ticks, string[] labels)> _ticksCache =
            new Dictionary<AxisPosition, (double[] ticks, string[] labels)>();
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

        // Using a DependencyProperty as the backing store for BorderColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BorderColorProperty =
            DependencyProperty.Register(nameof(BorderColor),
                typeof(Color),
                typeof(PlotView),
                new PropertyMetadata(Colors.Black));

        // Using a DependencyProperty as the backing store for FontColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FontColorProperty =
            DependencyProperty.Register(nameof(FontColor),
                typeof(Color),
                typeof(PlotView),
                new PropertyMetadata(Colors.Black));

        // Using a DependencyProperty as the backing store for AxisTitleFontSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AxisTitleFontSizeProperty =
            DependencyProperty.Register(nameof(AxisTitleFontSize),
                typeof(double),
                typeof(PlotView),
                new PropertyMetadata(14.0d));

        // Using a DependencyProperty as the backing store for TickLabelFontSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TickLabelFontSizeProperty =
            DependencyProperty.Register(nameof(TickLabelFontSize),
                typeof(double),
                typeof(PlotView),
                new PropertyMetadata(12.0d));

        public static readonly DependencyProperty DataAreaMarginProperty =
            DependencyProperty.Register(
                nameof(DataAreaMargin),
                typeof(double),
                typeof(PlotView),
                new PropertyMetadata(0.05, OnDataAreaMarginChanged)); // 5% default

        // Using a DependencyProperty as the backing store for GridStepSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GridStepSizeProperty =
            DependencyProperty.Register(
                nameof(GridStepSize),
                typeof(int),
                typeof(PlotView),
                new PropertyMetadata(8));
        #endregion Dependency Properties
        #region Properties
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
                SetValue(ModelProperty, value);
                _dataViewport = RectD.Empty;
                _textSizeCache.Clear();
                InvalidatePlot();
            }
        }

        public ZoomMode ZoomMode
        {
            get { return (ZoomMode)GetValue(ZoomModeProperty); }
            set { SetValue(ZoomModeProperty, value); }
        }

        public Thickness PlotPadding
        {
            get { return (Thickness)GetValue(PlotPaddingProperty); }
            set { SetValue(PlotPaddingProperty, value); }
        }

        public SolidColorBrush PlotBackgroundColor
        {
            get { return (SolidColorBrush)GetValue(PlotBackgroundColorProperty); }
            set { SetValue(PlotBackgroundColorProperty, value); }
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

        public Color FontColor
        {
            get { return (Color)GetValue(FontColorProperty); }
            set { SetValue(FontColorProperty, value); }
        }

        public double AxisTitleFontSize
        {
            get { return (double)GetValue(AxisTitleFontSizeProperty); }
            set { SetValue(AxisTitleFontSizeProperty, value); }
        }

        public double TickLabelFontSize
        {
            get { return (double)GetValue(TickLabelFontSizeProperty); }
            set { SetValue(TickLabelFontSizeProperty, value); }
        }

        public double DataAreaMargin
        {
            get { return (double)GetValue(DataAreaMarginProperty); }
            set { SetValue(DataAreaMarginProperty, value); }
        }

        public int GridStepSize
        {
            get { return (int)GetValue(GridStepSizeProperty); }
            set { SetValue(GridStepSizeProperty, value); }
        }
        #endregion Properties
        #region Constructor
        public PlotView()
        {
            _bitmap = _emptyBitmap;
            SnapsToDevicePixels = true;

            _plotImage = new Image()
            {
                Source = _bitmap,
                Stretch = Stretch.None,
                IsHitTestVisible = false
            };

            _plotToolTip = new ToolTip()
            {
                Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse,
                HasDropShadow = true,
                Padding = new Thickness(6.0d)
            };

            ToolTipService.SetInitialShowDelay(_plotImage, 0);
            _plotImage.ToolTip = _plotToolTip;

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
            Children.Add(_plotImage);
            SetZIndex(_plotImage, -100);
            UpdateBrushes();
            SizeChanged += OnSizeChanged;
            Loaded += OnLoaded;
        }
        #endregion Constructor
        #region Methods - Private
        private void InvalidatePlot()
        {
            if (ActualWidth < 10 || ActualHeight < 10)
                return;

            if (_dataViewport == RectD.Empty)
            {
                RectD tightRect = Model.CalculateDataRect();
                if (tightRect.IsEmpty())
                    _dataViewport = new RectD(0, 0, 100, 100); // fallback
                else
                {
                    double m = DataAreaMargin;
                    if (m > 0)
                    {
                        double dx = tightRect.Width * m;
                        double dy = tightRect.Height * m;
                        _tickLength = new PointD(dx, dy);

                        _dataViewport = new RectD(
                            tightRect.X - dx,
                            tightRect.Y - dy,
                            tightRect.Width + 2 * dx,
                            tightRect.Height + 2 * dy);
                    }
                    else
                    {
                        _dataViewport = tightRect;
                    }
                }
            }

            _plotPixels = CalculateBitmapSize();
            UpdateBitmap(_plotPixels);

            _plotImage.Margin = _effectiveMargin;
            _plotImage.Width = _plotPixels.Width;
            _plotImage.Height = _plotPixels.Height;

            _ticksCache.Clear();

            Render();
            InvalidateVisual();
        }

        private void Render()
        {
            if (_dataViewport.Width <= 0) _dataViewport.Width = 1;
            if (_dataViewport.Height <= 0) _dataViewport.Height = 1;

            double extraLeft = 0.0d;
            double extraRight = 0.0d;

            // This runs the measurement logic (including DrawAxisTitle logic) to get the raw required space.
            MeasureExtraPadding(ref extraLeft, ref extraRight, _dataViewport);

            // Calculate the raw required padding based on MeasureExtraPadding results and base PlotPadding.
            Thickness requiredRawPadding = new Thickness(
                Math.Max(PlotPadding.Left, extraLeft),
                PlotPadding.Top,
                Math.Max(PlotPadding.Right, extraRight),
                PlotPadding.Bottom);

            // Get the final, stable, growth/shrink-managed padding value.
            Thickness newEffectivePadding = CalculateRequiredEffectivePadding(requiredRawPadding);

            // Get the current dynamic padding from the internal field.
            Thickness currentDynamicPadding = this._effectiveMargin;

            // Compare the new stable value against the current value being used (using direct compare).
            if (newEffectivePadding.Left != currentDynamicPadding.Left ||
                newEffectivePadding.Right != currentDynamicPadding.Right ||
                newEffectivePadding.Top != currentDynamicPadding.Top ||
                newEffectivePadding.Bottom != currentDynamicPadding.Bottom)
            {
                // Change needed: Update the internal field to the new stable value.
                this._effectiveMargin = newEffectivePadding;

                // This triggers a new render cycle via InvalidatePlot() which uses the UPDATED _effectiveLayoutPadding
                InvalidatePlot();

                // Stop the current render cycle. The new cycle will execute the full drawing.
                return;
            }

            // The effective padding is stable, so update the local _stablePadding field for drawing logic 
            // (e.g., DrawAxisTitle uses this field for centering).
            RectD renderRect = new RectD(0.0d, 0.0d, _plotPixels.Width, _plotPixels.Height);

            _innerRenderRect = new RectD(
                _effectiveMargin.Left,
                _effectiveMargin.Top,
                _plotPixels.Width,
                _plotPixels.Height);

            _dataToScreenTransform = new PlotTransform(_dataViewport, renderRect);

            // Clear old labels and add new ones
            HideOldLabels();

            _bitmap.Clear(PlotBackgroundColor.Color);

            DrawGridLines(renderRect, _dataViewport);
            DrawPlotContent();
            DrawBorder();
            DrawAxesAndLabels(renderRect, _dataViewport);
        }

        private void DrawBorder()
        {
            if (BorderColor.A == 0)
                return;

            _bitmap.DrawRectangle(0, 0, _plotPixels.Width - 1, _plotPixels.Height - 1, BorderColor);
        }

        private SizeI CalculateBitmapSize()
        {
            int innerWidth = (int)Math.Max(1.0d, (ActualWidth - _effectiveMargin.Left - _effectiveMargin.Right));
            int innerHeight = (int)Math.Max(1.0d, (ActualHeight - _effectiveMargin.Top - _effectiveMargin.Bottom));
            SizeI innerSize = new SizeI(innerWidth, innerHeight);
            return innerSize;
        }

        private void UpdateBitmap(SizeI size)
        {
            if (_bitmap.PixelWidth != size.Width || _bitmap.PixelHeight != size.Height)
            {
                _bitmap = new WriteableBitmap(size.Width, size.Height, 96, 96, PixelFormats.Pbgra32, null);
                _plotImage.Source = _bitmap;
            }
        }

        private Thickness CalculateRequiredEffectivePadding(Thickness requiredPadding)
        {
            // The base minimum padding defined by the user's PlotPadding property.
            Thickness baseMinPadding = this.PlotPadding;

            // The current dynamic padding being used by the layout system.
            Thickness currentDynamicPadding = this._effectiveMargin;

            // --- LEFT PADDING ---
            double newStableLeft;
            if (requiredPadding.Left > currentDynamicPadding.Left)
            {
                // PADDING NEEDS TO GROW: Use Math.Ceiling to stabilize growth and prevent oscillation.
                newStableLeft = Math.Ceiling(requiredPadding.Left);
            }
            else if (requiredPadding.Left < baseMinPadding.Left)
            {
                // PADDING CAN SHRINK BELOW CURRENT DYNAMIC VALUE: Shrink back to the PlotPadding floor.
                newStableLeft = baseMinPadding.Left;
            }
            else
            {
                // PADDING REMAINS THE SAME OR SHRINKS SLIGHTLY ABOVE THE FLOOR.
                newStableLeft = Math.Floor(currentDynamicPadding.Left);
                if (newStableLeft < requiredPadding.Left)
                    newStableLeft = requiredPadding.Left;
            }

            // --- RIGHT PADDING ---
            double newStableRight;
            if (requiredPadding.Right > currentDynamicPadding.Right)
            {
                newStableRight = Math.Ceiling(requiredPadding.Right);
            }
            else if (requiredPadding.Right < baseMinPadding.Right)
            {
                newStableRight = baseMinPadding.Right;
            }
            else
            {
                newStableRight = Math.Floor(currentDynamicPadding.Right);
                if (newStableRight < requiredPadding.Right)
                    newStableRight = requiredPadding.Right;
            }

            // --- TOP PADDING ---
            double newStableTop;
            if (requiredPadding.Top > currentDynamicPadding.Top)
            {
                newStableTop = Math.Ceiling(requiredPadding.Top);
            }
            else if (requiredPadding.Top < baseMinPadding.Top)
            {
                newStableTop = baseMinPadding.Top;
            }
            else
            {
                newStableTop = Math.Floor(currentDynamicPadding.Top);
                if (newStableTop < requiredPadding.Top)
                    newStableTop = requiredPadding.Top;
            }

            // --- BOTTOM PADDING ---
            double newStableBottom;
            if (requiredPadding.Bottom > currentDynamicPadding.Bottom)
            {
                newStableBottom = Math.Ceiling(requiredPadding.Bottom);
            }
            else if (requiredPadding.Bottom < baseMinPadding.Bottom)
            {
                newStableBottom = baseMinPadding.Bottom;
            }
            else
            {
                newStableBottom = Math.Floor(currentDynamicPadding.Bottom);
                if (newStableBottom < requiredPadding.Bottom)
                    newStableBottom = requiredPadding.Bottom;
            }

            return new Thickness(newStableLeft, newStableTop, newStableRight, newStableBottom);
        }

        private void UpdateBrushes()
        {
            _plotToolTip.Background = TooltipBackgroundColor;
            _plotToolTip.Foreground = TooltipForegroundColor;
        }

        private Size MeasureTextCached(string text, double fontSize)
        {
            if (string.IsNullOrEmpty(text))
                return Size.Empty;

            (string text, double fontSize) key = (text, fontSize);

            if (_textSizeCache.TryGetValue(key, out Size size))
            {
                return size;
            }
            else
            {
                // Fallback to actual measurement
                size = text.MeasureText(fontSize);
                _textSizeCache[key] = size;
                return size;
            }
        }

        private (double[] ticks, string[] labels) GetTicks(AxisPosition position)
        {
            if (!_ticksCache.ContainsKey(position))
            {
                switch (position)
                {
                    case AxisPosition.Top:
                    case AxisPosition.Bottom:
                        _ticksCache.Add(position, TickGenerator.Generate(_dataViewport.X, _dataViewport.Right, GridStepSize));
                        break;
                    case AxisPosition.Left:
                    case AxisPosition.Right:
                    default:
                        _ticksCache.Add(position, TickGenerator.Generate(_dataViewport.Y, _dataViewport.Bottom, GridStepSize));
                        break;
                }
            }

            return _ticksCache[position];
        }

        private void MeasureExtraPadding(ref double extraLeft, ref double extraRight, RectD dataRect)
        {
            double[] ticks;
            string[] labels;

            foreach (Axis axis in Model.Axes)
            {
                if (!axis.IsVisible) continue;

                // NOTE: X-axis (Bottom/Top) logic is omitted here for focus, assuming it's correct
                // to not require extra padding beyond the user's PlotPadding.

                if (axis.Position == AxisPosition.Left || axis.Position == AxisPosition.Right)
                {
                    // 1. Generate ticks/labels (REQUIRED whenever the axis is visible)
                    (ticks, labels) = GetTicks(axis.Position); // TickGenerator.Generate(dataRect.Y, dataRect.Bottom);

                    double requiredExtent = 0.0d;
                    double maxLabelWidth = 0.0d;

                    // 2. Find the width of the longest tick label (horizontal space needed)
                    foreach (string label in labels)
                    {
                        if (string.IsNullOrEmpty(label)) continue;
                        Size size = MeasureTextCached(label, TickLabelFontSize);
                        maxLabelWidth = Math.Max(maxLabelWidth, size.Width);
                    }

                    // 3. Calculate minimum required space for labels + tick mark + gap (8.0d is standard buffer)
                    // _tickLength.X is the horizontal length of the Y-axis tick mark.
                    double requiredForLabels = maxLabelWidth + _tickLength.X + 8.0d;
                    requiredExtent = requiredForLabels;

                    // 4. Account for the Title (rotated, so its height determines its horizontal space)
                    if (!string.IsNullOrEmpty(axis.Title))
                    {
                        Size titleSize = MeasureTextCached(axis.Title, AxisTitleFontSize);
                        double titleExtent = titleSize.Height;

                        // The required padding must be large enough to contain the longest element.
                        // We use Math.Max to ensure we request enough space for the title or the labels.
                        // Add a small 5.0d buffer if the title is longer.
                        requiredExtent = Math.Max(requiredExtent, titleExtent + 5.0d);
                    }

                    if (axis.Position == AxisPosition.Left)
                        extraLeft = Math.Max(extraLeft, requiredExtent);
                    else if (axis.Position == AxisPosition.Right)
                        extraRight = Math.Max(extraRight, requiredExtent);
                }
            }
        }

        private void HideOldLabels()
        {
            foreach (TextBlock tb in _activeLabels)
            {
                tb.Visibility = Visibility.Collapsed;
                _inactiveLabels.Add(tb);
            }
            _activeLabels.Clear();
        }

        private void DrawGridLines(RectD renderRect, RectD dataRect)
        {
            var gridColor = GridColor; // Light gray

            // Vertical grid lines (X ticks)
            var (xTicks, _) = GetTicks(AxisPosition.Bottom);
            foreach (double tick in xTicks)
            {
                double x = _dataToScreenTransform.DataToScreen(new PointD(tick, 0)).X;
                _bitmap.DrawLine(x, renderRect.Top, x, renderRect.Bottom, gridColor);
            }

            // Horizontal grid lines (Y ticks)
            var (yTicks, _) = GetTicks(AxisPosition.Left);
            foreach (double tick in yTicks)
            {
                double y = _dataToScreenTransform.DataToScreen(new PointD(0, tick)).Y;
                _bitmap.DrawLine(renderRect.Left, y, renderRect.Right, y, gridColor);
            }
        }

        private void DrawPlotContent()
        {
            // Series
            foreach (ISeries series in Model.Series.Where(s => s.IsVisible))
            {
                if (series is LineSeries line)
                {
                    Color color = line.Stroke.ToMediaColor();
                    for (int i = 1; i < line.Points.Count; i++)
                    {
                        PointD p0 = _dataToScreenTransform!.DataToScreen(new PointD(line.Points[i - 1].X, line.Points[i - 1].Y));
                        PointD p1 = _dataToScreenTransform!.DataToScreen(new PointD(line.Points[i].X, line.Points[i].Y));
                        _bitmap.DrawLine(p0.X, p0.Y, p1.X, p1.Y, color, line.Thickness);
                    }
                }
                else if (series is ScatterSeries scatter)
                {
                    Color color = scatter.Fill.ToMediaColor();
                    Color selected = Colors.DodgerBlue;
                    foreach (var pt in scatter.Points)
                    {
                        PointD sp = _dataToScreenTransform!.DataToScreen(new PointD(pt.X, pt.Y));
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
                DrawAxisTicks(dataRect, axis);
                DrawAxisTitle(renderRect, axis);
            }
        }

        private void DrawAxisTicks(RectD dataRect, Axis axis)
        {
            var (ticks, labels) = GetTicks(axis.Position);
            switch (axis.Position)
            {
                case AxisPosition.Bottom:
                    // X axis
                    for (int i = 0; i < ticks.Length; i++)
                        DrawXTickLabel(dataRect, ticks[i], labels[i], false);
                    break;
                case AxisPosition.Left:
                    // Y axis
                    for (int i = 0; i < ticks.Length; i++)
                        DrawYTickLabel(dataRect, ticks[i], labels[i], true);
                    break;
                case AxisPosition.Top:
                    // X axis                    
                    for (int i = 0; i < ticks.Length; i++)
                        DrawXTickLabel(dataRect, ticks[i], labels[i], true);
                    break;
                case AxisPosition.Right:
                    // Y axis                    
                    for (int i = 0; i < ticks.Length; i++)
                        DrawYTickLabel(dataRect, ticks[i], labels[i], false);
                    break;
            }
        }

        private void DrawXTickLabel(RectD dataRect, double tick, string label, bool isTop)
        {
            PointD sp = _dataToScreenTransform.DataToScreen(new PointD(tick, dataRect.Y));
            double screenX = sp.X;
            double controlX = _effectiveMargin.Left + screenX;
            double edgeY = isTop ? _effectiveMargin.Top : (ActualHeight - _effectiveMargin.Bottom);
            double tickDir = isTop ? -1.0d : +1.0d;
            double length = _tickLength.Y;
            double labelY = edgeY + tickDir * length;
            AddLabel(label, controlX, labelY, FontColor, TickLabelFontSize, centerX: true, centerY: true);
        }

        private void DrawYTickLabel(RectD dataRect, double tick, string label, bool isLeft)
        {
            PointD sp = _dataToScreenTransform.DataToScreen(new PointD(dataRect.X, tick));
            double screenY = sp.Y;
            double controlY = _effectiveMargin.Top + screenY;
            double edgeX = isLeft ? _effectiveMargin.Left : (ActualWidth - _effectiveMargin.Right);
            double tickDir = isLeft ? -1.0d : +1.0d;
            double length = _tickLength.X;
            // Measure label width
            double labelWidth = isLeft ? MeasureTextCached(label, TickLabelFontSize).Width : 0.0d; // right-align labels on left
            double labelX = edgeX + tickDir * (labelWidth + length);
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

            switch (axis.Position)
            {
                case AxisPosition.Bottom:
                    x = _innerRenderRect.Left + renderRect.Width * 0.5d;
                    y = _innerRenderRect.Bottom + _effectiveMargin.Bottom * 0.5d;
                    break;
                case AxisPosition.Top:
                    x = _innerRenderRect.Left + renderRect.Width * 0.5d;
                    y = _effectiveMargin.Top * 0.5d;
                    break;
                case AxisPosition.Left:
                    x = _effectiveMargin.Left * 0.5d;
                    y = _innerRenderRect.Top + renderRect.Height * 0.5d;
                    cx = true;
                    angle = -90.0d;
                    break;
                case AxisPosition.Right:
                default:
                    x = ActualWidth - _effectiveMargin.Right * 0.5d;
                    y = _innerRenderRect.Top + renderRect.Height * 0.5d;
                    angle = +90.0d;
                    break;
            }

            AddLabel(axis.Title!, x, y, FontColor, AxisTitleFontSize, angle, cx, cy);
        }

        private void ZoomToFit()
        {
            _dataViewport = RectD.Empty;
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
                    _plotToolTip.Content = $"{series.Title}\nX: {point.X:F2}\nY: {point.Y:F2}";
                else
                    _plotToolTip.Content = $"{series.Title}\nX: {point.X:F2}\nY: {point.Y:F2}\n{point.Tag}";
                _plotToolTip.IsOpen = true;
            }
            else
            {
                _plotToolTip.IsOpen = false;
            }

            OnDataPointHoverChanged();
        }

        private void OnDataPointHoverChanged()
        {
            DataPointHoverChanged?.Invoke(this, new DataPointHoverEventArgs(_hoveredSeries, _hoveredIndex));
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

        private void AddLabel(string text, double x, double y, Color color,
            double fontSize = 12.0d, double angle = 0.0d,
            bool centerX = true, bool centerY = true)
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
            if (_inactiveLabels.Count > 0)
            {
                TextBlock label = _inactiveLabels[^1];
                _inactiveLabels.RemoveAt(_inactiveLabels.Count - 1);
                _activeLabels.Add(label);
                return (false, label);
            }
            else
            {
                TextBlock label = new TextBlock();
                _activeLabels.Add(label);
                return (true, label);
            }
        }
        #endregion Methods - Private
        #region Methods - PropertyChangedCallbacks
        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PlotView view = (PlotView)d;

            if (e.OldValue is IPlotModel oldModel)
            {
                oldModel.PropertyChanged -= view.Model_PropertyChanged;
            }

            if (e.NewValue is IPlotModel newModel)
            {
                newModel.PropertyChanged += view.Model_PropertyChanged;
            }

            view._dataViewport = RectD.Empty;
            view._textSizeCache.Clear();
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

        private static void OnPlotPaddingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlotView view)
            {
                view.InvalidatePlot(); // Re-render when padding changes
                view._plotImage.Margin = (Thickness)e.NewValue;
            }
        }

        private static void OnDataAreaMarginChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlotView view)
                view.InvalidatePlot();
        }
        #endregion Methods - PropertyChangedCallbacks
        #region Methods - Event Overrides
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (ZoomMode == ZoomMode.None) return;

            double factor = e.Delta > 0 ? 0.9 : 1.1;
            Point mousePoint = e.GetPosition(_plotImage);
            PointD mouseData = _dataToScreenTransform.ScreenToData(new PointD(mousePoint.X, mousePoint.Y));

            double factorX = ZoomsX() ? factor : 1.0d;
            double factorY = ZoomsY() ? factor : 1.0d;
            double newW = _dataViewport.Width * factorX;
            double newH = _dataViewport.Height * factorY;
            double newX = mouseData.X - (mouseData.X - _dataViewport.X) * factorX;
            double newY = mouseData.Y - (mouseData.Y - _dataViewport.Y) * factorY;

            _dataViewport = new RectD(newX, newY, newW, newH);
            InvalidatePlot();

            base.OnMouseWheel(e);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && Zooms())
            {
                _isBoxZooming = true;
                _selectionRectangleArea.Width = 1.0d;
                _selectionRectangleArea.Height = 1.0d;
                _selectionRectangleStartPoint = e.GetPosition(this);
                _selectionRect.Width = _selectionRect.Height = 1.0d;
                SetLeft(_selectionRect, _selectionRectangleStartPoint.X);
                SetTop(_selectionRect, _selectionRectangleStartPoint.Y);
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
            // intentionally checked after keyboard modifiers to
            // prevent responding to accidental double clicks
            else if (e.ClickCount == 2)
            {
                ZoomToFit();
                return;
            }
            else
            {
                _isPanning = true;
                _lastMousePanPoint = e.GetPosition(this);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            Point pos = e.GetPosition(this);

            // Box zoom rubber band
            if (_isBoxZooming)
            {
                double left = Math.Min(_selectionRectangleStartPoint.X, pos.X);
                double top = Math.Min(_selectionRectangleStartPoint.Y, pos.Y);
                double width = Math.Abs(pos.X - _selectionRectangleStartPoint.X);
                double height = Math.Abs(pos.Y - _selectionRectangleStartPoint.Y);

                _selectionRectangleArea = new Rect(left, top, width, height);

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
                Vector delta = _lastMousePanPoint - pos;
                double dx = 0.0d;
                double dy = 0.0d;

                if (ZoomsX())
                    dx = delta.X * _dataViewport.Width / _innerRenderRect.Width;

                if (ZoomsY())
                    dy = -delta.Y * _dataViewport.Height / _innerRenderRect.Height;

                _dataViewport = new RectD(
                    _dataViewport.X + dx,
                    _dataViewport.Y + dy,
                    _dataViewport.Width,
                    _dataViewport.Height);

                _lastMousePanPoint = pos;
                InvalidatePlot();
            }
            // Hovering
            else
            {
                ISeries? bestSeries = null;
                int bestIndex = -1;
                if (pos.X >= _effectiveMargin.Left && pos.X < ActualWidth - _effectiveMargin.Right &&
                    pos.Y >= _effectiveMargin.Top && pos.Y < ActualHeight - _effectiveMargin.Bottom)
                {
                    double bestDist = double.MaxValue;
                    PointD localPos = new PointD(
                        pos.X - _effectiveMargin.Left,
                        pos.Y - _effectiveMargin.Top);

                    foreach (ISeries series in Model.Series.Where(s => s.IsVisible))
                    {
                        var hit = series.GetNearestPoint(localPos, _dataToScreenTransform, 12.0d);
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
                    DataPoint point = bestSeries.Points[bestIndex];
                    UpdateHover(bestSeries, bestIndex, point);
                }
                else
                {
                    UpdateHover(null, -1, null);
                }
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (_isBoxZooming)
            {
                _isBoxZooming = false;
                _selectionRect.Visibility = Visibility.Collapsed;
                ReleaseMouseCapture();

                if (_selectionRectangleArea.Width < 10.0d || _selectionRectangleArea.Height < 10.0d)
                    return; // ignore tiny clicks

                PointD p1 = _dataToScreenTransform.ScreenToData(new PointD(
                    _selectionRectangleArea.Left - _effectiveMargin.Left,
                    _selectionRectangleArea.Top - _effectiveMargin.Top));

                PointD p2 = _dataToScreenTransform.ScreenToData(new PointD(
                    _selectionRectangleArea.Right - _effectiveMargin.Left,
                    _selectionRectangleArea.Bottom - _effectiveMargin.Top));

                RectD updatedView = RectD.Normalized(p1, p2);

                if (!ZoomsX())
                {
                    updatedView.X = _dataViewport.X;
                    updatedView.Width = _dataViewport.Width;
                }

                if (!ZoomsY())
                {
                    updatedView.Y = _dataViewport.Y;
                    updatedView.Height = _dataViewport.Height;
                }

                _dataViewport = updatedView;
                InvalidatePlot();
            }
            else if (_isPanning)
            {
                _isPanning = false;
                ReleaseMouseCapture();
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            UpdateHover(null, -1, null);
            if (_isPanning)
            {
                _isPanning = false;
                ReleaseMouseCapture();
            }
        }
        #endregion Methods - Event Overrides
        #region Event Handlers
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            InvalidatePlot();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            InvalidatePlot();
        }

        private void Model_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            InvalidatePlot();
        }
        #endregion Event Handlers
    }
}
