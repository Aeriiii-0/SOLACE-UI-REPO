using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SOLUM_UI.Converters
{
    /// <summary>
    /// Converts a percentage value (0-100) to a Path geometry for donut chart arcs
    /// </summary>
    public class PercentageToArcConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 1 || !(values[0] is double percentage))
                return Geometry.Empty;

            // Get start angle from parameter (0, 120, 240 for 3 segments)
            double startAngle = 0;
            if (parameter is string paramStr && double.TryParse(paramStr, out double paramAngle))
                startAngle = paramAngle;

            // Donut dimensions
            double radius = 60;
            double innerRadius = 45;
            double sweepAngle = (percentage / 100.0) * 360.0;

            // Convert angles to radians
            double startRad = (startAngle - 90) * Math.PI / 180;
            double sweepRad = sweepAngle * Math.PI / 180;

            // Calculate points
            double x1 = 70 + radius * Math.Cos(startRad);
            double y1 = 70 + radius * Math.Sin(startRad);

            double x2 = 70 + radius * Math.Cos(startRad + sweepRad);
            double y2 = 70 + radius * Math.Sin(startRad + sweepRad);

            double x3 = 70 + innerRadius * Math.Cos(startRad + sweepRad);
            double y3 = 70 + innerRadius * Math.Sin(startRad + sweepRad);

            double x4 = 70 + innerRadius * Math.Cos(startRad);
            double y4 = 70 + innerRadius * Math.Sin(startRad);

            // Create path geometry
            PathGeometry pathGeometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = new System.Windows.Point(x1, y1);
            pathFigure.IsClosed = true;

            // Outer arc
            ArcSegment arcSegment1 = new ArcSegment();
            arcSegment1.Point = new System.Windows.Point(x2, y2);
            arcSegment1.Size = new System.Windows.Size(radius, radius);
            arcSegment1.IsLargeArc = sweepAngle > 180;
            arcSegment1.SweepDirection = SweepDirection.Clockwise;
            pathFigure.Segments.Add(arcSegment1);

            // Line to inner arc
            LineSegment lineSegment1 = new LineSegment();
            lineSegment1.Point = new System.Windows.Point(x3, y3);
            pathFigure.Segments.Add(lineSegment1);

            // Inner arc (reverse direction)
            ArcSegment arcSegment2 = new ArcSegment();
            arcSegment2.Point = new System.Windows.Point(x4, y4);
            arcSegment2.Size = new System.Windows.Size(innerRadius, innerRadius);
            arcSegment2.IsLargeArc = sweepAngle > 180;
            arcSegment2.SweepDirection = SweepDirection.Counterclockwise;
            pathFigure.Segments.Add(arcSegment2);

            pathGeometry.Figures.Add(pathFigure);
            return pathGeometry;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts percentage to opacity for visual indication
    /// </summary>
    public class PercentageToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double percentage))
                return 1.0;

            return percentage > 0 ? 1.0 : 0.3;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
