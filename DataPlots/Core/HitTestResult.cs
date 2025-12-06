namespace DataPlots.Core
{
    public readonly struct HitTestResult
    {
        public int PointIndex { get; }
        public double DistancePixels { get; }
        public HitTestResult(int pointIndex, double distancePixels)
        {
            PointIndex = pointIndex;
            DistancePixels = distancePixels;
        }
    }
}
