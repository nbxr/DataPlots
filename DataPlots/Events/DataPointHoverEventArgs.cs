namespace DataPlots.Events
{
    public class DataPointHoverEventArgs : EventArgs
    {
        public ISeries? Series { get; }
        public int PointIndex { get; }
        public DataPoint? Point { get; }
        public DataPointHoverEventArgs(ISeries? series, int pointIndex, DataPoint? point)
        {
            Series = series;
            PointIndex = pointIndex;
            Point = point;
        }
    }
}
