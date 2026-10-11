using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using SelectPdf.Universal;
using SelectPdf.Universal.Drawing;

#if WKHTMLTOPDFDOTNET
namespace WkHtmlToPdfDotNet.Internal
#else
namespace DinkToPdf.Internal
#endif
{
    /// <summary>Renders a wkhtmltopdf-wrapper document with the SelectPdf Community Edition (one conversion per document).</summary>
    internal sealed class SelectPdfRenderer
    {
        readonly Action<string> warn;

        public SelectPdfRenderer(Action<string> warn) { this.warn = warn ?? (_ => { }); }

        public byte[] Render(IDocument document, Action<int, int, string> phase)
        {
            GlobalSettings global = GetGlobalSettings(document);
            List<ObjectSettings> objects = document.GetObjects().Where(x => x != null).Select(ToObjectSettings).ToList();

            DocumentLayout layout = DocumentLayout.Create(objects);
            foreach (string w in layout.Warnings) warn(w);

            ConversionPlan plan = ConversionPlan.Create(global, layout.Primary);
            if (layout.CombinedHtml != null) { plan.Url = null; plan.Html = layout.CombinedHtml; }
            foreach (string w in plan.Warnings) warn(w);

            phase(0, 2, "Converting");
            HtmlToPdf converter = CreateConverter(plan, layout);
            SelectPdfCompat.Configure?.Invoke(converter, layout.Primary);

            PdfDocument doc = plan.Url != null ? converter.ConvertUrl(ToUrl(plan.Url)) : converter.ConvertHtmlString(plan.Html);
            try
            {
                CheckPageLimit(converter.ConversionResult.PdfPageCount);

                phase(1, 2, "Saving");
                if (!string.IsNullOrEmpty(global.DocumentTitle))
                    doc.DocumentInformation.Title = global.DocumentTitle;
                byte[] bytes = doc.Save();

                if (!string.IsNullOrEmpty(global.Out))
                {
                    // as in the wkhtmltopdf wrappers: with Out set, the PDF goes to that file and the array is empty
                    File.WriteAllBytes(global.Out, bytes);
                    return new byte[0];
                }
                return bytes;
            }
            finally
            {
                doc.Close();
            }
        }

        void CheckPageLimit(int pageCount)
        {
            if (pageCount < SelectPdfCompat.CommunityPageLimit) return;
            string message = $"The output reached the SelectPdf Community Edition limit of {SelectPdfCompat.CommunityPageLimit} pages; " +
                             "content past that page was not rendered. SelectPdf.Universal (licensed) has no page limit: https://selectpdf.com/pdf-library-cross-platform/";
            if (SelectPdfCompat.FailOnPageLimit) throw new InvalidOperationException(message);
            warn(message);
        }

        static HtmlToPdf CreateConverter(ConversionPlan plan, DocumentLayout layout)
        {
            var converter = new HtmlToPdf();
            HtmlToPdfOptions o = converter.Options;

            o.PdfPageSize = PdfPageSize.Custom;
            o.PdfPageCustomSize = new SizeF((float)plan.PageWidthPt, (float)plan.PageHeightPt);
            o.PdfPageOrientation = PdfPageOrientation.Portrait; // size already oriented
            o.MarginTop = plan.MarginTop;
            o.MarginBottom = plan.MarginBottom;
            o.MarginLeft = plan.MarginLeft;
            o.MarginRight = plan.MarginRight;

            if (plan.Grayscale) o.ColorSpace = PdfColorSpace.GrayScale;
            if (plan.PrintMediaType == true) o.CssMediaType = HtmlToPdfCssMediaType.Print;
            if (plan.JavaScript.HasValue) o.JavaScriptEnabled = plan.JavaScript.Value;
            if (plan.Background.HasValue) o.DrawBackground = plan.Background.Value;
            if (plan.ExternalLinks.HasValue) o.ExternalLinksEnabled = plan.ExternalLinks.Value;
            if (plan.InternalLinks.HasValue) o.InternalLinksEnabled = plan.InternalLinks.Value;
            if (plan.DenyLocalFileAccess.HasValue) o.DenyLocalFileAccess = plan.DenyLocalFileAccess.Value;
            if (plan.WebPageWidth.HasValue) o.WebPageWidth = plan.WebPageWidth.Value;
            if (plan.WebPageHeight.HasValue) o.WebPageHeight = plan.WebPageHeight.Value;
            if (plan.ConversionDelaySeconds.HasValue) o.ConversionDelay = plan.ConversionDelaySeconds.Value;
            if (plan.JpegCompressionLevel.HasValue)
            {
                o.JpegCompressionEnabled = true;
                o.JpegCompressionLevel = plan.JpegCompressionLevel.Value;
            }
            if (plan.UserStyleSheetCss != null) o.CustomCSS = plan.UserStyleSheetCss;
            if (plan.OutlineDepth.HasValue)
            {
                o.PdfBookmarkOptions.CssSelectors = Enumerable.Range(1, plan.OutlineDepth.Value).Select(n => "h" + n).ToArray();
                o.PdfBookmarkOptions.HierarchicalBookmarks = true;
            }

            if (plan.Username != null) o.Authentication.Username = plan.Username;
            if (plan.Password != null) o.Authentication.Password = plan.Password;
            foreach (var kv in plan.HttpHeaders) o.HttpHeaders.Add(kv.Key, kv.Value);
            foreach (var kv in plan.Cookies) o.HttpCookies.Add(kv.Key, kv.Value);
            foreach (var kv in plan.PostFields) o.HttpPostParameters.Add(kv.Key, kv.Value);
            if (plan.Proxy != null)
            {
                o.ProxyOptions.Type = plan.Proxy.IsSocks5 ? NetworkProxyType.Socks5 : NetworkProxyType.Http;
                o.ProxyOptions.HostName = plan.Proxy.Host;
                o.ProxyOptions.PortNumber = plan.Proxy.Port;
                if (plan.Proxy.Username != null) o.ProxyOptions.Username = plan.Proxy.Username;
                if (plan.Proxy.Password != null) o.ProxyOptions.Password = plan.Proxy.Password;
            }

            if (plan.Header != null)
            {
                o.DisplayHeader = true;
                converter.Header.Height = plan.Header.HeightPt;
                converter.Header.FirstPageNumber = plan.FirstPageNumber;
                if (layout.CoverFirst) converter.Header.DisplayOnFirstPage = false;
                foreach (PdfSectionElement e in BuildSection(plan.Header)) converter.Header.Add(e);
            }
            if (plan.Footer != null)
            {
                o.DisplayFooter = true;
                converter.Footer.Height = plan.Footer.HeightPt;
                converter.Footer.FirstPageNumber = plan.FirstPageNumber;
                if (layout.CoverFirst) converter.Footer.DisplayOnFirstPage = false;
                foreach (PdfSectionElement e in BuildSection(plan.Footer)) converter.Footer.Add(e);
            }
            return converter;
        }

        static IEnumerable<PdfSectionElement> BuildSection(ConversionPlan.SectionPlan s)
        {
            if (s.HtmlUrl != null)
            {
                yield return new PdfHtmlSection(0, 0, ToUrl(s.HtmlUrl));
                yield break;
            }
            if (s.Line)
            {
                const string line = "<div style=\"border-top:2px solid #000;margin:0;padding:0\"></div>";
                yield return new PdfHtmlSection(0, s.LineY, s.WidthPt, line, "");
            }
            if (!s.HasText) yield break;

            PdfFont font = CreateFont(s.FontName, s.FontSize);
            if (!string.IsNullOrEmpty(s.Left)) yield return Text(s, s.Left, font, PdfTextHorizontalAlign.Left);
            if (!string.IsNullOrEmpty(s.Center)) yield return Text(s, s.Center, font, PdfTextHorizontalAlign.Center);
            if (!string.IsNullOrEmpty(s.Right)) yield return Text(s, s.Right, font, PdfTextHorizontalAlign.Right);
        }

        static PdfTextSection Text(ConversionPlan.SectionPlan s, string text, PdfFont font, PdfTextHorizontalAlign align)
        {
            var t = new PdfTextSection(0, s.TextY, s.WidthPt, text, font);
            t.HorizontalAlign = align;
            return t;
        }

        /// <summary>
        /// SelectPdf does not substitute missing font families, so fall back to the bundled
        /// Liberation fonts (Arial/Times/Courier metrics), then to standard Helvetica.
        /// </summary>
        internal static PdfFont CreateFont(string family, float size)
        {
            foreach (string name in FontCandidates(family))
            {
                try { return PdfFont.CreateFromSystemFont(name, size); }
                catch (PdfDocumentException) { }
            }
            return PdfFont.CreateStandardFont(PdfStandardFont.Helvetica, size);
        }

        internal static IEnumerable<string> FontCandidates(string family)
        {
            if (!string.IsNullOrWhiteSpace(family)) yield return family;
            string f = (family ?? "").ToLowerInvariant();
            if (f.Contains("times") || (f.Contains("serif") && !f.Contains("sans"))) yield return "Liberation Serif";
            else if (f.Contains("courier") || f.Contains("mono")) yield return "Liberation Mono";
            yield return "Liberation Sans";
        }

        /// <summary>wkhtmltopdf accepts local paths in Page and HtmUrl; turn them into file:// URLs.</summary>
        internal static string ToUrl(string pageOrPath)
        {
            if (Uri.TryCreate(pageOrPath, UriKind.Absolute, out Uri uri) && !uri.IsFile)
                return pageOrPath;
            if (File.Exists(pageOrPath)) return new Uri(Path.GetFullPath(pageOrPath)).AbsoluteUri;
            return pageOrPath;
        }

        static GlobalSettings GetGlobalSettings(IDocument document)
        {
            if (document is HtmlToPdfDocument d) return d.GlobalSettings ?? new GlobalSettings();
            // custom IDocument implementations: find a GlobalSettings property, as DinkToPdf's reflection did
            var prop = document.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(p => typeof(GlobalSettings).IsAssignableFrom(p.PropertyType) && p.GetIndexParameters().Length == 0);
            return (prop?.GetValue(document) as GlobalSettings) ?? new GlobalSettings();
        }

        static ObjectSettings ToObjectSettings(IObject obj)
        {
            if (obj is ObjectSettings s) return s;
            // custom IObject: DinkToPdf sent GetContent() bytes to wkhtmltopdf as the page HTML
            return new ObjectSettings { HtmlContent = Encoding.UTF8.GetString(obj.GetContent() ?? new byte[0]) };
        }
    }
}
