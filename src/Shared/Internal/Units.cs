using System;
using System.Globalization;
using System.Text.RegularExpressions;

#if WKHTMLTOPDFDOTNET
namespace WkHtmlToPdfDotNet.Internal
#else
namespace DinkToPdf.Internal
#endif
{
    /// <summary>Converts wkhtmltopdf lengths ("210mm", "8.5in", "2cm", "96px", "12pt") to PDF points.</summary>
    internal static class Units
    {
        public const double PointsPerInch = 72.0;
        public const double MmPerInch = 25.4;

        static readonly Regex LengthRe = new Regex(@"^\s*([0-9]*\.?[0-9]+)\s*(mm|cm|in|pt|px)?\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static double MmToPoints(double mm) => mm * PointsPerInch / MmPerInch;

        public static double ToPoints(double value, Unit unit)
        {
            switch (unit)
            {
                case Unit.Millimeters: return value * PointsPerInch / MmPerInch;
                case Unit.Centimeters: return value * 10 * PointsPerInch / MmPerInch;
                default: return value * PointsPerInch; // Unit.Inches
            }
        }

        /// <summary>Parses a wkhtmltopdf length. A bare number is millimetres, as in wkhtmltopdf.</summary>
        public static double ParseToPoints(string length)
        {
            if (string.IsNullOrWhiteSpace(length))
                throw new ArgumentException("Empty length.", nameof(length));
            var m = LengthRe.Match(length);
            if (!m.Success)
                throw new FormatException($"Unrecognized length '{length}'. Use mm, cm, in, pt or px.");
            double v = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            switch (m.Groups[2].Value.ToLowerInvariant())
            {
                case "cm": return v * 10 * PointsPerInch / MmPerInch;
                case "in": return v * PointsPerInch;
                case "pt": return v;
                case "px": return v * 0.75; // CSS px at 96 dpi
                default: return v * PointsPerInch / MmPerInch; // mm, or no unit
            }
        }
    }
}
