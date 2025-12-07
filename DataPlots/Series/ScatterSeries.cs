using System.Drawing;

namespace DataPlots.Series
{
    public class ScatterSeries : ISeries
    {
        public string Title { get; set; } = "Scatter Series";
        public bool IsVisible { get; set; } = true;

        public IList<DataPoint> Points { get; } = new List<DataPoint>();
        public double PointSize { get; set; } = 7.0d;
        public Color Stroke { get; set; } = Color.Black;
        public Color Fill { get; set; } = Color.Blue;
        public HitTestResult? GetNearestPoint(PointD screenPosition, IPlotTransform transform, double maxDistancePixels = 12.0d)
        {
            double bestDistance = double.MaxValue;
            int bestIdx = -1;
            for (int i = 0; i < Points.Count; i++)
            {
                PointD sp = transform.DataToScreen(new PointD(Points[i].X, Points[i].Y));
                double d = screenPosition.DistanceTo(sp);
                if (d < PointSize && d < bestDistance)
                {
                    bestDistance = d;
                    bestIdx = i;
                }
            }
            return bestIdx >= 0 ? new HitTestResult(bestIdx, bestDistance) : null;
        }
    }
}
