using System.Drawing;

namespace DataPlots.Series
{
    public class LineSeries : ISeries
    {
        public string Title { get; set; } = "Line Series";
        public bool IsVisible { get; set; } = true;

        public IList<DataPoint> Points { get; } = new List<DataPoint>();
        public double Thickness { get; set; } = 1.5d;
        public Color Stroke { get; set; } = Color.Black;
        public Color Fill { get; set; } = Color.Red;
        public HitTestResult? GetNearestPoint(PointD screenPosition, IPlotTransform transform, double maxDistancePixels = 12)
        {
            // TASK: simple distance-to-segment hit-test will come later - for now, reuse scatter logic on points
            double bestDistance = double.MaxValue;
            int bestIdx = -1;
            for (int i = 0; i < Points.Count; i++)
            {
                PointD sp = transform.DataToScreen(new PointD(Points[i].X, Points[i].Y));
                double d = screenPosition.DistanceTo(sp);
                if (d < maxDistancePixels && d < bestDistance)
                {
                    bestDistance = d;
                    bestIdx = i;
                }
            }
            return bestIdx >= 0 ? new HitTestResult(bestIdx, bestDistance) : null;
        }
    }
}
