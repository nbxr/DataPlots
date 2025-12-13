namespace DataPlots.Core
{
    public static class TickGenerator
    {
        private static (double[] ticks, string[] labels) emptyResult = new(Array.Empty<double>(), Array.Empty<string>());
        
        public static (double[] ticks, string[] labels) Generate(double min, double max, int maxCount = 8)
        {
            if (max <= min)
                return emptyResult;

            double range = max - min;
            double roughtStep = range / maxCount;
            double step = Math.Pow(10, Math.Floor(Math.Log10(roughtStep)));

            if (roughtStep / step > 5)
                step *= 5;
            else if (roughtStep / step > 2)
                step *= 2;

            double first = Math.Ceiling(min / step) * step;
            List<double> ticks = new List<double>();
            List<string> labels = new List<string>();

            for (double t = first; t <= max + step / 2; t += step)
            {
                if (t >= min)
                {
                    ticks.Add(t);
                    labels.Add(t % 1 == 0.0 ? t.ToString("F0") : t.ToString("G6"));
                }
            }

            return (ticks.ToArray(), labels.ToArray());
        }
    }
}