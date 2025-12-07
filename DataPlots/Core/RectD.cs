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
    }
}