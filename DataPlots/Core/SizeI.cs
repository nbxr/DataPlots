namespace DataPlots.Core
{
    public struct SizeI
    {
        public static readonly SizeI Empty = new SizeI(0, 0);
        public int Width { get; set; }
        public int Height { get; set; }
        public SizeI(int width, int height)
        {
            Width = width;
            Height = height;
        }
        public override string ToString()
        {
            return $"Width: {Width}, Height: {Height}";
        }
    }
}
