using DataPlots.Models;

namespace DataPlots.Wpf.Plots
{
    public class PlotModel : IPlotModel
    {
        public IList<ISeries> Series { get; } = new List<ISeries>();

        public IList<Axis> Axes { get; } = new List<Axis>() {
            new Axis(){ Position = AxisPosition.Bottom , Title = "X Axis"},
            new Axis(){ Position = AxisPosition.Left , Title = "Y Axis"}
        };

        public ZoomMode ZoomMode { get; } = ZoomMode.XY;

        public RectD CalculateDataRect()
        {
            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;
            bool any = false;

            foreach (ISeries s in Series)
            {
                if (!s.IsVisible) continue;
                foreach (DataPoint p in s.Points)
                {
                    any = true;
                    if (p.X < minX) minX = p.X;
                    if (p.X > maxX) maxX = p.X;
                    if (p.Y < minY) minY = p.Y;
                    if (p.Y > maxY) maxY = p.Y;
                }
            }

            if (!any)
                return RectD.Empty;

            return new RectD(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
