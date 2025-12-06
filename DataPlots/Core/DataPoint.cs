namespace DataPlots.Core
{
    public class DataPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public object? Tag { get; set; }
        public DataPoint(double x, double y, object? tag = null)
        {
            X = x;
            Y = y;
            Tag = tag;
        }
    }
}