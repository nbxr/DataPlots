using System.Diagnostics.CodeAnalysis;

namespace DataPlots.Core
{
    public struct RectD
    {
        public static readonly RectD Empty = new RectD(0.0d, 0.0d, 0.0d, 0.0d);

        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public readonly double Left
        {
            get { return X; }
        }

        public readonly double Right
        {
            get { return X + Width; }
        }

        public readonly double Top
        {
            get { return Y; }
        }

        public readonly double Bottom
        {
            get { return Y + Height; }
        }

        public RectD(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool Contains(double x, double y)
        {
            return (x >= Left) && (x <= Right) && (y >= Top) && (y <= Bottom);
        }

        public static RectD Normalized(PointD p1, PointD p2)
        {
            double x = Math.Min(p1.X, p2.X);
            double y = Math.Min(p1.Y, p2.Y);
            double width = Math.Abs(p1.X - p2.X);
            double height = Math.Abs(p1.Y - p2.Y);
            return new RectD(x, y, width, height);
        }

        private static bool DEquals(double a, double b)
        {
            if (Math.Abs(a - b) < double.Epsilon)
                return true;
            else
                return false;
        }

        public static bool operator ==(RectD a, RectD b)
        {
            return DEquals(a.X, b.X) && DEquals(a.Y, b.Y) && DEquals(a.Width, b.Width) && DEquals(a.Height, b.Height);
        }

        public static bool operator !=(RectD a, RectD b)
        {
            return !(a == b);
        }

        public override readonly bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is RectD other && this == other;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(X, Y, Width, Height);
        }
    }
}