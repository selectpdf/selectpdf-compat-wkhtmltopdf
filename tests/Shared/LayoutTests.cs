using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
#if WKHTMLTOPDFDOTNET
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Internal;
#else
using DinkToPdf;
using DinkToPdf.Internal;
#endif

// Engine-free tests: how a document's objects become one Community Edition conversion.
public class LayoutTests
{
    static ObjectSettings Html(string html) => new ObjectSettings { HtmlContent = html };

    [Fact]
    public void Single_object_is_converted_on_its_own()
    {
        var o = new ObjectSettings { Page = "https://example.com" };
        var l = DocumentLayout.Create(new List<ObjectSettings> { o });
        Assert.Same(o, l.Primary);
        Assert.Null(l.CombinedHtml);
        Assert.Empty(l.Warnings);
    }

    [Fact]
    public void Html_objects_are_joined_with_page_breaks()
    {
        var a = Html("<h1>A</h1>"); var b = Html("<h1>B</h1>");
        var l = DocumentLayout.Create(new List<ObjectSettings> { a, b });
        Assert.Same(a, l.Primary);
        Assert.Equal("<h1>A</h1>" + DocumentLayout.PageBreak + "<h1>B</h1>", l.CombinedHtml);
        Assert.False(l.CoverFirst);
        Assert.Empty(l.Warnings);
    }

    [Fact]
    public void Several_objects_with_a_url_cannot_be_merged_by_the_free_edition()
    {
        var e = Assert.Throws<NotSupportedException>(() => DocumentLayout.Create(new List<ObjectSettings>
            { Html("<p>x</p>"), new ObjectSettings { Page = "https://example.com" } }));
        Assert.Contains("SelectPdf.Universal", e.Message);
    }

    [Fact]
    public void Different_headers_warn_that_the_first_objects_settings_apply()
    {
        var a = Html("a"); a.HeaderSettings = new HeaderSettings { Right = "[page]" };
        var b = Html("b"); b.HeaderSettings = new HeaderSettings { Left = "other" };
        var l = DocumentLayout.Create(new List<ObjectSettings> { a, b });
        Assert.Contains(l.Warnings, w => w.Contains("header/footer"));
    }

#if WKHTMLTOPDFDOTNET
    [Fact]
    public void Cover_goes_first_without_header_and_does_not_drive_settings()
    {
        var body = Html("<p>body</p>"); body.FooterSettings = new FooterSettings { Center = "[page]" };
        var cover = new CoverSettings { HtmlContent = "<h1>Cover</h1>" };
        var l = DocumentLayout.Create(new List<ObjectSettings> { body, cover });
        Assert.True(l.CoverFirst);
        Assert.Same(body, l.Primary);
        Assert.StartsWith("<h1>Cover</h1>", l.CombinedHtml);
        Assert.Contains(l.Warnings, w => w.StartsWith("CoverSettings"));
    }

    [Fact]
    public void Table_of_contents_is_skipped_with_a_warning()
    {
        var l = DocumentLayout.Create(new List<ObjectSettings> { new TableOfContentsSettings(), Html("<h1>x</h1>") });
        Assert.Null(l.CombinedHtml);
        Assert.Contains(l.Warnings, w => w.StartsWith("TableOfContentsSettings"));
        Assert.Throws<NotSupportedException>(() => DocumentLayout.Create(new List<ObjectSettings> { new TableOfContentsSettings() }));
    }

    [Fact]
    public void Post_fields_map()
    {
        var o = new ObjectSettings { Page = "https://example.com/form" };
        o.LoadSettings.Post = new Dictionary<string, string> { ["q"] = "pdf" };
        var p = ConversionPlan.Create(new GlobalSettings(), o);
        Assert.Equal("pdf", p.PostFields["q"]);
    }

    [Fact]
    public void Content_honours_Encoding()
    {
        var o = new ObjectSettings { HtmlContent = "é", Encoding = Encoding.Unicode };
        Assert.Equal(Encoding.Unicode.GetBytes("é"), o.GetContent());
        Assert.Equal(Encoding.UTF8.GetBytes("é"), new ObjectSettings { HtmlContent = "é" }.GetContent());
    }

    [Fact]
    public void Converter_is_disposable()
    {
        using (WkHtmlToPdfDotNet.Contracts.IConverter c = new SynchronizedConverter(new PdfTools())) { }
    }
#endif
}
