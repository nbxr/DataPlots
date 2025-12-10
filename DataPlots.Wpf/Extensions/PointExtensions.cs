namespace DataPlots.Wpf.Extensions
{
    public static class PointExtensions
    {
        public static PointD ToPointD(this Point point)
        {
            return new PointD(point.X, point.Y);
        }
    }
}
