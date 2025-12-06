namespace DataPlots.Core
{
    public class PlotTransform : IPlotTransform
    {
        private readonly RectD dataRect;
        private readonly RectD renderRect;

        public PlotTransform(RectD dataRect, RectD renderRect)
        {
            this.dataRect = dataRect;
            this.renderRect = renderRect;
        }

        public PointD DataToScreen(PointD dataPoint)
        {
            double x = renderRect.X + (dataPoint.X - dataRect.X) * renderRect.Width / dataRect.Width;
            double y = renderRect.Bottom - (dataPoint.Y - dataRect.Y) * renderRect.Height / dataRect.Height;
            return new PointD(x, y);
        }

        public PointD ScreenToData(PointD screenPoint)
        {
            double x = dataRect.X + (screenPoint.X - renderRect.X) * dataRect.Width / renderRect.Width;
            double y = dataRect.Y + (renderRect.Bottom - screenPoint.Y) * dataRect.Height / renderRect.Height;
            return new PointD(x, y);
        }
    }
}
