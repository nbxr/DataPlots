using DataPlots.Models;
using DataPlots.Wpf.Extensions;
using DataPlots.Wpf.Utilities;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using static DataPlots.Wpf.Utilities.CanvasUtilities;

namespace DataPlots.Wpf.Plots
{
    public class PlotView : Canvas
    {
        #region Events
        public event EventHandler<DataPointHoverEventArgs>? DataPointHoverChanged;
        public event EventHandler<DataPointSelectedEventArgs>? DataPointSelected;
        #endregion Events
        #region Fields
        private readonly Rectangle _boxRect;
        private readonly Image _plotImage;
        private readonly ToolTip _toolTip;
        private WriteableBitmap _bitmap;
        private IPlotTransform _transform = IPlotTransform.Empty;
        private RectD _currentView = RectD.Empty;
        private RectD _lastRenderRect = RectD.Empty;
        private ThicknessD _plotPadding = new ThicknessD(60, 30, 30, 50);
        private ISeries? _hoveredSeries;
        private int _hoveredIndex = -1;
        private Point _boxStart;
        private Rect _boxOverlay;
        private Point _lastMousePos;
        private bool _isBoxZooming;
        private bool _isPanning;
        #endregion Fields
        #region Dependency Properties
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register(nameof(Model), typeof(PlotModel), typeof(PlotView),
                new PropertyMetadata(null, (d, e) =>
                {
                    var view = (PlotView)d;
                    view._currentView = RectD.Empty;
                    view.InvalidatePlot();
                }));

        public static readonly DependencyProperty ZoomModeProperty =
            DependencyProperty.Register(
                nameof(ZoomMode),
                typeof(ZoomMode),
                typeof(PlotView),
                new PropertyMetadata(ZoomMode.XY, OnZoomModeChanged));
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
                SetValue(ModelProperty, value);
                _currentView = RectD.Empty;
                InvalidatePlot();
            }
        }
        #endregion Properties
        #region Constructor
        public PlotView()
        {
            _bitmap = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Pbgra32, null);
            Background = Brushes.WhiteSmoke;
            SnapsToDevicePixels = true;

            _toolTip = new ToolTip()
            {
                Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse,
                HasDropShadow = true,
                Background = Brushes.Black,
                Foreground = Brushes.White,
                Padding = new Thickness(6.0d)
            };

            ToolTipService.SetInitialShowDelay(this, 0);
            ToolTip = _toolTip;

            _boxRect = new Rectangle()
            {
                Stroke = Brushes.DodgerBlue,
                StrokeThickness = 2.0d,
                StrokeDashArray = new DoubleCollection() { 4, 2 },
                Fill = Colors.DodgerBlue.ToSolidColorBrush(),
                Opacity = 0.4d,
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };

            Children.Add(_boxRect);

            _plotImage = new Image
            {
                Source = _bitmap,
                Stretch = Stretch.None,
                IsHitTestVisible = false
            };

            Children.Add(_plotImage);
            Canvas.SetZIndex(_plotImage, -100);

            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            MouseMove += OnMouseMove;
            MouseWheel += OnMouseWheel;
            MouseLeave += (_, __) => UpdateHover(null, -1, null);
            SizeChanged += (_, __) => InvalidatePlot();
            Loaded += (_, __) => InvalidatePlot();
        }
        #endregion Constructor
        #region Methods
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
        #endregion Methods
        #region Private Methods
        private void InvalidatePlot()
        {
            if (ActualWidth < 10 || ActualHeight < 10) return;

            if (_currentView == RectD.Empty)
                _currentView = Model.CalculateDataRect();

            int w = (int)ActualWidth;
            int h = (int)ActualHeight;

            if (_bitmap.PixelWidth != w || _bitmap.PixelHeight != h)
            {
                _bitmap = new WriteableBitmap(w, h, 96, 96, PixelFormats.Pbgra32, null);
                _plotImage.Source = _bitmap;
            }

            Render();
            InvalidateVisual();
        }

        private void Render()
        {
            var dataRect = _currentView;
            if (dataRect.Width <= 0) dataRect.Width = 1;
            if (dataRect.Height <= 0) dataRect.Height = 1;

            var renderRect = new RectD(
                _plotPadding.Left, _plotPadding.Top,
                _bitmap.PixelWidth - _plotPadding.Left - _plotPadding.Right,
                _bitmap.PixelHeight - _plotPadding.Top - _plotPadding.Bottom);

            _lastRenderRect = renderRect;
            _transform = new PlotTransform(dataRect, renderRect);

            // Clear old labels and add new ones
            var oldLabels = Children.OfType<TextBlock>().ToList();
            foreach (var tb in oldLabels) Children.Remove(tb);

            _bitmap.Clear(Colors.WhiteSmoke);
            DrawGridLines(renderRect, dataRect);
            DrawPlotContent(renderRect, dataRect);
            DrawAxesAndLabels(renderRect, dataRect);
        }

        private void DrawGridLines(RectD renderRect, RectD dataRect)
        {
            var gridColor = Color.FromArgb(40, 0, 0, 0); // Light gray

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
                        _bitmap.FillCircle(sp.X, sp.Y, scatter.PointSize, pt.Selected ? selected : color, Colors.Black, 0.5d);
                    }
                }
            }
        }

        private void DrawAxesAndLabels(RectD renderRect, RectD dataRect)
        {
            var axisColor = Colors.Black;

            // X axis
            var (xTicks, xLabels) = TickGenerator.Generate(dataRect.X, dataRect.Right);
            for (int i = 0; i < xTicks.Length; i++)
            {
                var sp = _transform.DataToScreen(new PointD(xTicks[i], dataRect.Y));
                _bitmap.DrawLine(sp.X, renderRect.Bottom, sp.X, renderRect.Bottom - 6, axisColor);
                AddLabel(this, xLabels[i], sp.X, renderRect.Bottom + 8, Colors.Black, centerX: true, centerY: true);
            }

            // Y axis
            var (yTicks, yLabels) = TickGenerator.Generate(dataRect.Y, dataRect.Bottom);
            for (int i = 0; i < yTicks.Length; i++)
            {
                var sp = _transform.DataToScreen(new PointD(dataRect.X, yTicks[i]));
                _bitmap.DrawLine(renderRect.Left, sp.Y, renderRect.Left + 6, sp.Y, axisColor);

                // Measure label width
                var labelWidth = yLabels[i].MeasureText(12.0d).Width;
                // Dynamic offset: always fully visible, never clipped
                double labelX = renderRect.Left - labelWidth - 12.0d; // 12px padding from axis
                AddLabel(this, yLabels[i], labelX, sp.Y, Colors.Black, centerX: false, centerY: true);
            }

            // Titles
            var xAxis = Model.Axes.First(a => a.Position == AxisPosition.Bottom);
            AddLabel(this,
                xAxis.Title!,
                renderRect.Left + renderRect.Width / 2,
                renderRect.Bottom + _plotPadding.Bottom * 0.6d,
                Colors.Black,
                14);

            var yAxis = Model.Axes.First(a => a.Position == AxisPosition.Left);
            AddLabel(
                this,
                yAxis.Title!,
                _plotPadding.Left * 0.4d,
                renderRect.Top + renderRect.Height / 2.0d,
                Colors.Black,
                14.0d,
                -90.0d,
                centerX: true,
                centerY: true);
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
                _boxRect.Width = _boxRect.Height = 0;
                Canvas.SetLeft(_boxRect, _boxStart.X);
                Canvas.SetTop(_boxRect, _boxStart.Y);
                _boxRect.Visibility = Visibility.Visible;
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
            var pos = e.GetPosition(this);

            // Box zoom rubber band
            if (_isBoxZooming)
            {
                var current = pos;

                double left = Math.Min(_boxStart.X, current.X);
                double top = Math.Min(_boxStart.Y, current.Y);
                double width = Math.Abs(current.X - _boxStart.X);
                double height = Math.Abs(current.Y - _boxStart.Y);

                _boxOverlay = new Rect(left, top, width, height);

                Canvas.SetLeft(_boxRect, left);
                Canvas.SetTop(_boxRect, top);
                _boxRect.Width = width;
                _boxRect.Height = height;
                _boxRect.Visibility = Visibility.Visible;
            }
            // Panning
            else if (_isPanning)
            {
                Vector delta = _lastMousePos - pos;
                double dx = 0.0d;
                double dy = 0.0d;

                if (ZoomsX())
                    dx = delta.X * _currentView.Width / _lastRenderRect.Width;

                if (ZoomsY())
                    dy = -delta.Y * _currentView.Height / _lastRenderRect.Height;

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
                var posD = new PointD(pos.X, pos.Y);
                ISeries? bestSeries = null;
                int bestIndex = -1;
                double bestDist = double.MaxValue;
                foreach (var series in Model.Series.Where(s => s.IsVisible))
                {
                    var hit = series.GetNearestPoint(posD, _transform, 12.0d);
                    if (hit.HasValue && hit.Value.DistancePixels < bestDist)
                    {
                        bestDist = hit.Value.DistancePixels;
                        bestSeries = series;
                        bestIndex = hit.Value.PointIndex;
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

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isBoxZooming)
            {
                _isBoxZooming = false;
                _boxRect.Visibility = Visibility.Collapsed;
                ReleaseMouseCapture();

                if (_boxOverlay.Width < 10.0d || _boxOverlay.Height < 10.0d)
                    return; // ignore tiny clicks

                PointD p1 = _transform.ScreenToData(new PointD(_boxOverlay.Left, _boxOverlay.Top));
                PointD p2 = _transform.ScreenToData(new PointD(_boxOverlay.Right, _boxOverlay.Bottom));
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
        #endregion Private Methods
    }
}
