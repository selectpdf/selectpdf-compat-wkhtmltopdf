using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if WKHTMLTOPDFDOTNET
namespace WkHtmlToPdfDotNet.Internal
#else
namespace DinkToPdf.Internal
#endif
{
    /// <summary>
    /// Turns the document's objects into a single SelectPdf conversion. The Community Edition cannot
    /// append PDFs, so several HTML objects are joined into one HTML document with page breaks between
    /// them, which also keeps page numbering continuous, as wkhtmltopdf did.
    /// </summary>
    internal sealed class DocumentLayout
    {
        public const string PageBreak = "<div style=\"break-before:page;page-break-before:always\"></div>";

        public ObjectSettings Primary;      // its header/footer/web/load settings drive the conversion
        public string CombinedHtml;         // null when Primary is converted on its own
        public bool CoverFirst;             // hide header/footer on the first page
        public readonly List<string> Warnings = new List<string>();

        public static DocumentLayout Create(IList<ObjectSettings> objects)
        {
            var layout = new DocumentLayout();
            var content = new List<ObjectSettings>();
            foreach (var o in objects)
            {
                if (IsTableOfContents(o))
                    layout.Warnings.Add("TableOfContentsSettings: SelectPdf does not generate a table-of-contents page; skipped. Set GlobalSettings.Outline = true for PDF bookmarks.");
                else content.Add(o);
            }
            if (content.Count == 0)
                throw new NotSupportedException("The document has only a table of contents and no content objects.");

            int cover = content.FindIndex(IsCover);
            if (cover > 0)
            {
                // wkhtmltopdf placed the cover where it was listed; a cover is almost always first, so move it there
                var c = content[cover]; content.RemoveAt(cover); content.Insert(0, c);
                layout.Warnings.Add("CoverSettings was not the first object; it is rendered as the first page.");
            }
            layout.CoverFirst = cover >= 0 && content.Count > 1;
            layout.Primary = content.FirstOrDefault(o => !IsCover(o)) ?? content[0];

            if (content.Count == 1) return layout;

            if (content.Any(o => !string.IsNullOrWhiteSpace(o.Page)))
                throw new NotSupportedException(
                    "This document has several objects and at least one is a URL (ObjectSettings.Page). " +
                    "The free SelectPdf Community Edition renders one source per PDF and cannot merge PDFs. " +
                    "Use HtmlContent for every object, or use SelectPdf.Universal (licensed), which merges documents.");

            if (content.Where(o => !IsCover(o)).Select(HeaderFooterKey).Distinct().Count() > 1)
                layout.Warnings.Add("Objects have different header/footer settings; the first content object's settings are used for the whole document.");

            var sb = new StringBuilder();
            for (int i = 0; i < content.Count; i++)
            {
                if (i > 0) sb.Append(PageBreak);
                sb.Append(content[i].HtmlContent ?? "");
            }
            layout.CombinedHtml = sb.ToString();
            return layout;
        }

        static string HeaderFooterKey(ObjectSettings o)
        {
            var h = o.HeaderSettings; var f = o.FooterSettings;
            return string.Join("|", h?.Left, h?.Center, h?.Right, h?.Line, h?.FontSize, h?.FontName, h?.Spacing,
                                    f?.Left, f?.Center, f?.Right, f?.Line, f?.FontSize, f?.FontName, f?.Spacing,
                                    HeaderHtmlUrl(h), FooterHtmlUrl(f));
        }

#if WKHTMLTOPDFDOTNET
        internal static bool IsCover(ObjectSettings o) => o is CoverSettings c && c.IsCover;
        internal static bool IsTableOfContents(ObjectSettings o) => o is TableOfContentsSettings t && t.IsTableOfContent;
        internal static string HeaderHtmlUrl(HeaderSettings h) => h?.HtmlUrl;
        internal static string FooterHtmlUrl(FooterSettings f) => f?.HtmlUrl;
#else
        internal static bool IsCover(ObjectSettings o) => false;
        internal static bool IsTableOfContents(ObjectSettings o) => false;
        internal static string HeaderHtmlUrl(HeaderSettings h) => h?.HtmUrl;
        internal static string FooterHtmlUrl(FooterSettings f) => f?.HtmUrl;
#endif
    }
}
