namespace DataPlots.Core
{
    public interface IPlotTransform
    {
        public static readonly IPlotTransform Empty = new EmptyPlotTransform();
        PointD DataToScreen(PointD dataPoint);
        PointD ScreenToData(PointD screenPoint);

        private sealed class EmptyPlotTransform : IPlotTransform
        {
            public PointD DataToScreen(PointD dataPoint)
            {
                return PointD.Empty;

            }

            public PointD ScreenToData(PointD screenPoint)
            {
                return PointD.Empty;
            }
        }
    }
}
