namespace DataPlots.Wpf.Extensions
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
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = false
            };

            if (Math.Abs(angle) > 0.01)
                tb.RenderTransform = new RotateTransform(angle);

            tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            tb.Arrange(new Rect(tb.DesiredSize));

            double left = centerX ? x - tb.ActualWidth / 2 : x;
            double top = centerY ? y - tb.ActualHeight / 2 : y;

            Canvas.SetLeft(tb, left);
            Canvas.SetTop(tb, top);

            canvas.Children.Add(tb);
        }
    }
}
