using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPlots.Events
{
    public class DataPointSelectedEventArgs : EventArgs
    {
        public ISeries? Series { get; }
        public int PointIndex { get; }
        public DataPoint? Point { get; }
        public MouseButton Button { get; }
        public DataPointSelectedEventArgs(ISeries? series, int pointIndex, DataPoint? point, MouseButton button)
        {
            Series = series;
            PointIndex = pointIndex;
            Point = point;
            Button = button;
        }
    }
}
