using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DinkToPdf.Internal
{
    /// <summary>
    /// Translates wkhtmltopdf header/footer variables to SelectPdf text-section variables.
    /// Matching is case-insensitive because DinkToPdf's README uses "[toPage]".
    /// </summary>
    internal static class Placeholders
    {
        static readonly Regex VarRe = new Regex(@"\[(\w+)\]", RegexOptions.CultureInvariant);

        public sealed class Context
        {
            public int PageOffset;          // GlobalSettings.PageOffset
            public string DocumentTitle;    // GlobalSettings.DocumentTitle
            public string WebPage;          // ObjectSettings.Page
            public DateTime Now = DateTime.Now;
        }

        /// <param name="unsupported">Called with each variable that has no SelectPdf equivalent (replaced by "").</param>
        public static string Translate(string text, Context ctx, Action<string> unsupported)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return VarRe.Replace(text, m =>
            {
                switch (m.Groups[1].Value.ToLowerInvariant())
                {
                    case "page":
                    case "sitepage":
                        return "{page_number}";
                    case "topage":
                    case "sitepages":
                        return "{total_pages}";
                    case "frompage":
                        return (1 + ctx.PageOffset).ToString(CultureInfo.InvariantCulture);
                    case "date":
                        return ctx.Now.ToString("d", CultureInfo.CurrentCulture);
                    case "isodate":
                        return ctx.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    case "time":
                        return ctx.Now.ToString("T", CultureInfo.CurrentCulture);
                    case "title":
                    case "doctitle":
                        return ctx.DocumentTitle ?? "";
                    case "webpage":
                        return ctx.WebPage ?? "";
                    case "section":
                    case "subsection":
                        unsupported?.Invoke(m.Value);
                        return "";
                    default:
                        return m.Value; // not a wkhtmltopdf variable: keep literally
                }
            });
        }
    }
}
