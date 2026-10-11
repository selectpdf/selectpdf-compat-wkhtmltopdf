using System;
using System.Collections.Generic;
using System.IO;

namespace DinkToPdf.Internal
{
    /// <summary>
    /// Engine-independent translation of DinkToPdf settings into SelectPdf terms (points, flags, sections).
    /// Kept free of SelectPdf types so the mapping is unit-testable without the Chromium engine.
    /// </summary>
    internal sealed class ConversionPlan
    {
        // wkhtmltopdf defaults
        public const double DefaultMarginMm = 10;
        public const int DefaultHeaderFontSize = 12;
        public const string DefaultHeaderFontName = "Arial";

        public double PageWidthPt, PageHeightPt;           // final page size; landscape already applied
        public int MarginTop, MarginBottom, MarginLeft, MarginRight; // points, after header/footer space is taken out
        public SectionPlan Header, Footer;                 // null when not displayed

        public string Url;                                 // ObjectSettings.Page
        public string Html;                                // ObjectSettings.HtmlContent (when Url is null)

        public bool Grayscale;
        public bool? PrintMediaType, JavaScript, Background, ExternalLinks, InternalLinks, DenyLocalFileAccess;
        public int? WebPageWidth, WebPageHeight;
        public int? ConversionDelaySeconds;
        public int? JpegCompressionLevel;
        public int FirstPageNumber = 1;
        public int? OutlineDepth;                          // null = no bookmarks
        public string UserStyleSheetCss;
        public string Username, Password;
        public ProxySpec Proxy;
        public Dictionary<string, string> HttpHeaders = new Dictionary<string, string>();
        public Dictionary<string, string> Cookies = new Dictionary<string, string>();

        public readonly List<string> Warnings = new List<string>();

        public sealed class SectionPlan
        {
            public string Left, Center, Right;             // SelectPdf variables already substituted
            public string HtmlUrl;                         // HeaderSettings.HtmUrl (replaces text parts)
            public string FontName;
            public float FontSize;
            public bool Line;
            public float HeightPt;                         // total section height incl. spacing
            public float TextY, LineY;                     // offsets inside the section
            public float WidthPt;                          // content width
            public bool HasText => !string.IsNullOrEmpty(Left) || !string.IsNullOrEmpty(Center) || !string.IsNullOrEmpty(Right);
        }

        public static ConversionPlan Create(GlobalSettings g, ObjectSettings o)
        {
            g = g ?? new GlobalSettings();
            var p = new ConversionPlan();
            if (o != null)
            {
                p.Url = string.IsNullOrWhiteSpace(o.Page) ? null : o.Page;
                p.Html = p.Url == null ? (o.HtmlContent ?? "") : null;
            }

            // ---- page size and orientation (wkhtmltopdf default: A4 portrait)
            PechkinPaperSize size = g.PaperSize ?? (PechkinPaperSize)PaperKind.A4;
            double w = Units.ParseToPoints(size.Width), h = Units.ParseToPoints(size.Height);
            // wkhtmltopdf turns the sheet for an explicit orientation; with none set, the size is used as given
            if ((g.Orientation == Orientation.Landscape && w < h) || (g.Orientation == Orientation.Portrait && w > h))
            { var t = w; w = h; h = t; }
            p.PageWidthPt = w; p.PageHeightPt = h;

            // ---- margins (wkhtmltopdf default 10mm on each side)
            var m = g.Margins ?? new MarginSettings();
            double Margin(double? v) => v.HasValue ? Units.ToPoints(v.Value, m.Unit) : Units.MmToPoints(DefaultMarginMm);
            double top = Margin(m.Top), bottom = Margin(m.Bottom), left = Margin(m.Left), right = Margin(m.Right);
            float contentWidth = (float)Math.Max(0, w - left - right);

            p.FirstPageNumber = 1 + (g.PageOffset ?? 0);
            var ctx = new Placeholders.Context
            {
                PageOffset = g.PageOffset ?? 0,
                DocumentTitle = g.DocumentTitle,
                WebPage = o?.Page,
            };

            // ---- header / footer: wkhtmltopdf draws them inside the margins, so the space
            // SelectPdf reserves for them is taken out of the margin to keep the content where it was.
            p.Header = BuildSection(o?.HeaderSettings, true, top, contentWidth, ctx, p.Warnings);
            p.Footer = BuildSection(o?.FooterSettings, false, bottom, contentWidth, ctx, p.Warnings);
            if (p.Header != null) top = Math.Max(0, top - p.Header.HeightPt);
            if (p.Footer != null) bottom = Math.Max(0, bottom - p.Footer.HeightPt);

            p.MarginTop = (int)Math.Round(top);
            p.MarginBottom = (int)Math.Round(bottom);
            p.MarginLeft = (int)Math.Round(left);
            p.MarginRight = (int)Math.Round(right);

            // ---- global options
            p.Grayscale = g.ColorMode == ColorMode.Grayscale;
            if (g.ImageQuality.HasValue)
                p.JpegCompressionLevel = Clamp(100 - g.ImageQuality.Value, 0, 100);
            if (g.Outline == true)
                p.OutlineDepth = Clamp(g.OutlineDepth ?? 4, 1, 6);
            if (!string.IsNullOrWhiteSpace(g.ViewportSize))
            {
                var parts = g.ViewportSize.ToLowerInvariant().Split('x');
                if (parts.Length == 2 && int.TryParse(parts[0], out int vw) && int.TryParse(parts[1], out int vh))
                { p.WebPageWidth = vw; p.WebPageHeight = vh; }
                else p.Warnings.Add($"GlobalSettings.ViewportSize '{g.ViewportSize}' is not WIDTHxHEIGHT; ignored.");
            }
            Unsupported(p, g.Copies.HasValue && g.Copies != 1, "GlobalSettings.Copies");
            Unsupported(p, g.Collate.HasValue, "GlobalSettings.Collate");
            Unsupported(p, g.DPI.HasValue, "GlobalSettings.DPI (output is vector; use WebPageWidth via Configure for layout width)");
            Unsupported(p, g.ImageDPI.HasValue, "GlobalSettings.ImageDPI");
            Unsupported(p, g.UseCompression == false, "GlobalSettings.UseCompression=false");
            Unsupported(p, !string.IsNullOrEmpty(g.DumpOutline), "GlobalSettings.DumpOutline");
            Unsupported(p, !string.IsNullOrEmpty(g.CookieJar), "GlobalSettings.CookieJar");

            if (o == null) return p;

            // ---- object options
            p.ExternalLinks = o.UseExternalLinks;
            p.InternalLinks = o.UseLocalLinks;
            Unsupported(p, o.ProduceForms == true, "ObjectSettings.ProduceForms");

            var web = o.WebSettings ?? new WebSettings();
            p.Background = web.Background;
            p.JavaScript = web.EnableJavascript;
            p.PrintMediaType = web.PrintMediaType;
            if (!string.IsNullOrWhiteSpace(web.UserStyleSheet))
            {
                string path = web.UserStyleSheet.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
                    ? new Uri(web.UserStyleSheet).LocalPath : web.UserStyleSheet;
                if (File.Exists(path)) p.UserStyleSheetCss = File.ReadAllText(path);
                else p.Warnings.Add($"WebSettings.UserStyleSheet '{web.UserStyleSheet}' is not a local file; ignored.");
            }
            Unsupported(p, web.LoadImages == false, "WebSettings.LoadImages=false");
            Unsupported(p, web.MinimumFontSize.HasValue, "WebSettings.MinimumFontSize");
            Unsupported(p, web.EnableIntelligentShrinking == false, "WebSettings.EnableIntelligentShrinking=false");

            var load = o.LoadSettings ?? new LoadSettings();
            p.Username = load.Username;
            p.Password = load.Password;
            if (load.JSDelay.HasValue && load.JSDelay.Value > 0)
                p.ConversionDelaySeconds = (int)Math.Ceiling(load.JSDelay.Value / 1000.0);
            p.DenyLocalFileAccess = load.BlockLocalFileAccess;
            if (!string.IsNullOrWhiteSpace(load.Proxy))
            {
                try { p.Proxy = ProxySpec.Parse(load.Proxy); }
                catch (FormatException e) { p.Warnings.Add("LoadSettings.Proxy: " + e.Message); }
            }
            if (load.CustomHeaders != null)
                foreach (var kv in load.CustomHeaders) if (kv.Key != null && kv.Value != null) p.HttpHeaders[kv.Key] = kv.Value;
            if (load.Cookies != null)
                foreach (var kv in load.Cookies) if (kv.Key != null && kv.Value != null) p.Cookies[kv.Key] = kv.Value;
            Unsupported(p, load.ZoomFactor.HasValue && load.ZoomFactor != 1, "LoadSettings.ZoomFactor");

            return p;
        }

        static SectionPlan BuildSection(object settings, bool isHeader, double marginPt, float contentWidth,
                                        Placeholders.Context ctx, List<string> warnings)
        {
            string left, center, right, url, fontName; int? fontSize; bool? line; double? spacing;
            if (settings is HeaderSettings hs)
            { left = hs.Left; center = hs.Center; right = hs.Right; url = hs.HtmUrl; fontName = hs.FontName; fontSize = hs.FontSize; line = hs.Line; spacing = hs.Spacing; }
            else if (settings is FooterSettings fs)
            { left = fs.Left; center = fs.Center; right = fs.Right; url = fs.HtmUrl; fontName = fs.FontName; fontSize = fs.FontSize; line = fs.Line; spacing = fs.Spacing; }
            else return null;

            string name = isHeader ? "HeaderSettings" : "FooterSettings";
            var s = new SectionPlan { WidthPt = contentWidth };
            float spacingPt = (float)Units.MmToPoints(spacing ?? 0);   // wkhtmltopdf spacing is in mm

            if (!string.IsNullOrWhiteSpace(url))
            {
                // An HTML header/footer fills the whole margin in wkhtmltopdf.
                s.HtmlUrl = url;
                s.HeightPt = (float)marginPt;
                warnings.Add($"{name}.HtmUrl: wkhtmltopdf's ?page=&topage= query parameters are not passed; page numbers in the HTML will not update per page.");
                return s;
            }

            void Warn(string v) => warnings.Add($"{name}: variable {v} is not supported; replaced with an empty string.");
            s.Left = Placeholders.Translate(left, ctx, Warn);
            s.Center = Placeholders.Translate(center, ctx, Warn);
            s.Right = Placeholders.Translate(right, ctx, Warn);
            if (!s.HasText && line != true) return null;

            s.FontName = string.IsNullOrWhiteSpace(fontName) ? DefaultHeaderFontName : fontName;
            s.FontSize = fontSize ?? DefaultHeaderFontSize;
            s.Line = line == true;

            float textHeight = s.HasText ? s.FontSize * 1.2f : 0;
            float lineHeight = s.Line ? 2f : 0;
            s.HeightPt = textHeight + lineHeight + spacingPt;
            if (isHeader)
            {   // text, line, spacing, then content
                s.TextY = 0;
                s.LineY = textHeight;
            }
            else
            {   // content, spacing, line, text
                s.LineY = spacingPt;
                s.TextY = spacingPt + lineHeight;
            }
            return s;
        }

        static void Unsupported(ConversionPlan p, bool isSet, string what)
        {
            if (isSet) p.Warnings.Add(what + " has no SelectPdf equivalent and is ignored.");
        }

        static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
    }
}
