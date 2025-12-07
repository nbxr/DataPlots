using System.Globalization;

namespace DataPlots.Wpf.Extensions
{
    public static class StringExtensions
    {
        public static Size MeasureText(this string text, double fontSize)
        {
            var formattedText = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                fontSize,
                Brushes.Black,
                96);

            return new Size(formattedText.Width, formattedText.Height);
        }
    }
}
