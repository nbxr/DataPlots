using DataPlots.Core;

namespace DataPlots.Series
{
    public interface ISeries
    {
        string Title { get; set; }
        bool IsVisible { get; set; }
        IList<DataPoint> Points { get; }
        HitTestResult? GetNearestPoint(PointD screenPosition, IPlotTransform transform, double maxDistancePixels = 12.0d);
    }
}
