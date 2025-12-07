using DataPlots.Models;
using System.Windows;

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
            plotView.Model = new RandomDataPlot();
        }
    }
}