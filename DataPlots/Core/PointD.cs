namespace DataPlots.Core
{
    public struct PointD
    {
        public static readonly PointD Empty = new PointD(0.0d, 0.0d);
        public double X { get; set; }
        public double Y { get; set; }
        public PointD(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double DistanceTo(PointD other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static PointD operator +(PointD p1, PointD p2)
        {
            return new PointD(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static PointD operator -(PointD p1, PointD p2)
        {
            return new PointD(p1.X - p2.X, p1.Y - p2.Y);
        }
    }
}