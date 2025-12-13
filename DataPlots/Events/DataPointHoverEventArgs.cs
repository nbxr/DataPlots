namespace DataPlots.Events
{
    public class DataPointHoverEventArgs : EventArgs
    {
        public ISeries? Series { get; }
        public int PointIndex { get; }
        public DataPoint? Point { get; }
        public DataPointHoverEventArgs(ISeries? series, int pointIndex)
        {
            Series = series;
            PointIndex = pointIndex;
            if (series != null && pointIndex > -1 && pointIndex < series.Points.Count)
                Point = series.Points[pointIndex];
            else
                Point = null;
        }
    }
}
