using System.Drawing;

namespace DataPlots.Core
{
    public class Axis
    {
        public AxisPosition Position { get; set; } = AxisPosition.Bottom;
        public string? Title { get; set; }
        public bool IsVisiblity { get; set; } = true;
        public Color Color { get; set; } = Color.Black;
        public double TickLenght { get; set; } = 6.0d;
        public double TitleOffset { get; set; } = 30.0d;
    }
}
