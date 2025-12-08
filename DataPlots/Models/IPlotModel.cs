using System.Drawing;

namespace DataPlots.Models
{
    public interface IPlotModel : INotifyPropertyChanged
    {
        public static readonly IPlotModel Empty = new EmptyPlotModel();
        public IList<ISeries> Series { get; }
        public IList<Axis> Axes { get; }
        public RectD CalculateDataRect();
        public ZoomMode ZoomMode { get; }
        private sealed class EmptyPlotModel : IPlotModel
        {
            public IList<ISeries> Series { get; } = Array.Empty<ISeries>();
            public IList<Axis> Axes { get; } = Array.Empty<Axis>();
            public ZoomMode ZoomMode { get; } = ZoomMode.None;
            public event PropertyChangedEventHandler? PropertyChanged;
            public RectD CalculateDataRect()
            {
                return RectD.Empty;
            }
        }
    }
}