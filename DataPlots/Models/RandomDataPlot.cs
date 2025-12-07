using System.Drawing;

namespace DataPlots.Models
{
    public class RandomDataPlot : IPlotModel
    {
        public IList<ISeries> Series { get; } = new List<ISeries>();

        public IList<Axis> Axes { get; } = new List<Axis>() {
            new Axis(){ Position = AxisPosition.Bottom , Title = "X Axis"},
            new Axis(){ Position = AxisPosition.Left , Title = "Y Axis"}
        };

        public ZoomMode ZoomMode { get; } = ZoomMode.XY;

        public RandomDataPlot()
        {
            ScatterSeries scatter = new ScatterSeries()
            {
                Title = "Random points",
                Fill = Color.Crimson,
                Stroke = Color.DarkRed,
                PointSize = 8.0d
            };

            Random rnd = new Random(0);
            for (int i = 0; i < 5000; i++)
            {
                double x = rnd.NextDouble() * 100.0d;
                double y = rnd.NextDouble() * 100.0d;
                scatter.Points.Add(new DataPoint(x, y, $"Scatter Point {i}"));
            }

            Series.Add(scatter);

            LineSeries line = new LineSeries() { Stroke = Color.MediumBlue };
            for (int i = 0; i < 1000; i++)
            {
                double x = i / 10.0d;
                double y = Math.Sin(x) * 30.0d + 50.0d;
                line.Points.Add(new DataPoint(x, y, $"Line Point {i}"));
            }

            Series.Add(line);
        }

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