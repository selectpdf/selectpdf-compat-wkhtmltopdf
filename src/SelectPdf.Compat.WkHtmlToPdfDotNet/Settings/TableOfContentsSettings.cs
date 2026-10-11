namespace WkHtmlToPdfDotNet
{
    /// <summary>
    /// wkhtmltopdf's generated table of contents. Kept so existing code compiles; SelectPdf does not
    /// generate a TOC page, so this object is skipped with a Warning. The PDF gets bookmarks instead
    /// when GlobalSettings.Outline is true.
    /// </summary>
    public class TableOfContentsSettings : ObjectSettings
    {
        public bool IsTableOfContent { get; set; }
        public string CaptionText { get; set; }
        public bool UseDottedLines { get; set; }

        public TableOfContentsSettings()
        {
            IsTableOfContent = true;
        }
    }
}
