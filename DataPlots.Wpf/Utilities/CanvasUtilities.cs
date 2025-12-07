namespace DataPlots.Wpf.Utilities
{
    public static class CanvasUtilities
    {
        public static void AddLabel(Canvas canvas, string text, double x, double y, 
            Color color, double fontSize = 12, double angle = 0, bool centerX = true, bool centerY = true)
        {
            TextBlock tb = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(color),
                FontSize = fontSize,
                RenderTransformOrigin = new Point(0.5d, 0.5d),
                IsHitTestVisible = false
            };

            if (Math.Abs(angle) > 0.01d)
                tb.RenderTransform = new RotateTransform(angle);

            if (centerX || centerY)
            {
                tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                tb.Arrange(new Rect(tb.DesiredSize));
            }

            double left = centerX ? x - tb.ActualWidth / 2.0d : x;
            double top = centerY ? y - tb.ActualHeight / 2.0d : y;

            Canvas.SetLeft(tb, left);
            Canvas.SetTop(tb, top);

            canvas.Children.Add(tb);
        }
    }
}
