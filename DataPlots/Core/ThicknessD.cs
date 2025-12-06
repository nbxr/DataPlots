namespace DataPlots.Core
{
    public struct ThicknessD
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Right { get; set; }
        public double Bottom { get; set; }
        public ThicknessD(double uniform) : this(uniform, uniform, uniform, uniform) { }
        public ThicknessD(double left, double top, double right, double bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
        public override string ToString()
        {
            return $"{Left}, {Top}, {Right}, {Bottom}";
        }
    }
}
