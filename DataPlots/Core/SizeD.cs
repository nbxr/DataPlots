namespace DataPlots.Core
{
    public struct SizeD
    {
        public static readonly SizeD Empty = new SizeD(0.0d, 0.0d);
        public double Width { get; set; }
        public double Height { get; set; }
        public SizeD(double width, double height)
        {
            Width = width;
            Height = height;
        }
    }
}