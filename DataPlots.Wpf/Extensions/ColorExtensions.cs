namespace DataPlots.Wpf.Extensions
{
    public static class ColorExtensions
    {
        public static System.Windows.Media.Color ToMediaColor(this System.Drawing.Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static System.Drawing.Color ToDrawingColor(this System.Windows.Media.Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static SolidColorBrush ToSolidColorBrush(this System.Drawing.Color color)
        {
            return new SolidColorBrush(color.ToMediaColor());
        }

        public static SolidColorBrush ToSolidColorBrush(this System.Windows.Media.Color color)
        {
            return new SolidColorBrush(color);
        }
    }
}
