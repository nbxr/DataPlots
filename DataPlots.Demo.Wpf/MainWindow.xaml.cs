using DataPlots.Core;
using DataPlots.Series;
using DataPlots.Wpf.Extensions;
using DataPlots.Wpf.Plots;
using System.Windows;
using System.Windows.Media;

namespace DataPlots.Demo.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var model = new PlotModel();
            var scatter = new ScatterSeries()
            {
                Title = "Random points",
                Fill = Brushes.Crimson.Color.ToDrawingColor(),
                Stroke = Brushes.DarkRed.Color.ToDrawingColor(),
                PointSize = 8.0d
            };

            var rnd = new Random(0);
            for (int i = 0;i < 5000; i++)
            {
                double x = rnd.NextDouble() * 100.0d;
                double y = rnd.NextDouble() * 100.0d;
                scatter.Points.Add(new DataPoint(x, y, $"Scatter Point {i}"));
            }

            model.Series.Add(scatter);

            var line = new LineSeries() { Stroke = Brushes.MediumBlue.Color.ToDrawingColor() };
            for (int i = 0; i < 1000; i++) 
            {
                double x = i / 10.0d;
                double y = Math.Sin(x) * 30.0d + 50.0d;
                line.Points.Add(new DataPoint(x, y, $"Line Point {i}"));
            }

            model.Series.Add(line);
            
            plotView.Model = model;
        }
    }
}