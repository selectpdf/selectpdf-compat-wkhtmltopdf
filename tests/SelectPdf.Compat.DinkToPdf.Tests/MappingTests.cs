using System;
using System.Collections.Generic;
using System.Linq;
using DinkToPdf;
using DinkToPdf.Contracts;
using DinkToPdf.Internal;
using Xunit;

// Engine-free tests: how DinkToPdf settings translate to SelectPdf values.
public class MappingTests
{
    const double MmPt = 72 / 25.4;

    static ObjectSettings Obj(Action<ObjectSettings> setup = null)
    {
        var o = new ObjectSettings { HtmlContent = "<p>x</p>" };
        setup?.Invoke(o);
        return o;
    }

    [Theory]
    [InlineData("8.5in", 612)]
    [InlineData("210mm", 595.2756)]
    [InlineData("2cm", 56.6929)]
    [InlineData("96px", 72)]
    [InlineData("12pt", 12)]
    [InlineData("10", 28.3465)]   // bare number = mm, as in wkhtmltopdf
    public void Units_parse(string input, double points)
        => Assert.Equal(points, Units.ParseToPoints(input), 3);

    [Fact]
    public void Units_reject_garbage() => Assert.Throws<FormatException>(() => Units.ParseToPoints("wide"));

    [Fact]
    public void Defaults_match_wkhtmltopdf_A4_portrait_10mm_margins()
    {
        var p = ConversionPlan.Create(new GlobalSettings(), Obj());
        Assert.Equal(595.2756, p.PageWidthPt, 3);
        Assert.Equal(841.8898, p.PageHeightPt, 3);
        Assert.Equal(28, p.MarginTop);
        Assert.Equal(28, p.MarginLeft);
        Assert.Null(p.Header);
        Assert.Null(p.Footer);
        Assert.Empty(p.Warnings);
    }

    [Fact]
    public void Readme_example_A4Plus_landscape_with_header()
    {
        // DinkToPdf README sample, unchanged
        var g = new GlobalSettings { ColorMode = ColorMode.Color, Orientation = Orientation.Landscape, PaperSize = PaperKind.A4Plus };
        var o = new ObjectSettings
        {
            PagesCount = true,
            HtmlContent = "Lorem ipsum",
            WebSettings = { DefaultEncoding = "utf-8" },
            HeaderSettings = { FontSize = 9, Right = "Page [page] of [toPage]", Line = true, Spacing = 2.812 }
        };
        var p = ConversionPlan.Create(g, o);

        Assert.Equal(330 * MmPt, p.PageWidthPt, 3);   // A4Plus is 210 x 330 mm, turned to landscape
        Assert.Equal(210 * MmPt, p.PageHeightPt, 3);
        Assert.False(p.Grayscale);

        Assert.NotNull(p.Header);
        Assert.Equal("Page {page_number} of {total_pages}", p.Header.Right);
        Assert.True(p.Header.Line);
        Assert.Equal(9f, p.Header.FontSize);
        float expectedHeight = 9 * 1.2f + 2 + (float)(2.812 * MmPt);
        Assert.Equal(expectedHeight, p.Header.HeightPt, 2);
        // header space comes out of the 10mm margin, so content starts where wkhtmltopdf put it
        Assert.Equal((int)Math.Round(10 * MmPt - expectedHeight), p.MarginTop);
        Assert.Equal(p.Header.HeightPt - (float)(2.812 * MmPt), p.Header.LineY + 2, 2);
        Assert.Empty(p.Warnings);
    }

    [Fact]
    public void Orientation_unset_keeps_size_as_given_and_portrait_turns_it()
    {
        var asGiven = ConversionPlan.Create(new GlobalSettings { PaperSize = PaperKind.Ledger }, Obj()); // 17 x 11 in
        Assert.Equal(17 * 72, asGiven.PageWidthPt, 3);
        var portrait = ConversionPlan.Create(new GlobalSettings { PaperSize = PaperKind.Ledger, Orientation = Orientation.Portrait }, Obj());
        Assert.Equal(11 * 72, portrait.PageWidthPt, 3);
    }

    [Fact]
    public void Margins_respect_unit()
    {
        var g = new GlobalSettings { Margins = new MarginSettings { Unit = Unit.Inches, Top = 1, Left = 0.5 } };
        var p = ConversionPlan.Create(g, Obj());
        Assert.Equal(72, p.MarginTop);
        Assert.Equal(36, p.MarginLeft);
        Assert.Equal(28, p.MarginBottom); // unset side keeps the 10mm default
    }

    [Fact]
    public void Footer_layout_spacing_then_line_then_text()
    {
        var p = ConversionPlan.Create(new GlobalSettings(), Obj(o =>
            o.FooterSettings = new FooterSettings { Center = "[page]/[topage]", Line = true, Spacing = 5 }));
        float spacing = (float)(5 * MmPt);
        Assert.Equal("{page_number}/{total_pages}", p.Footer.Center);
        Assert.Equal(spacing, p.Footer.LineY, 3);
        Assert.Equal(spacing + 2, p.Footer.TextY, 3);
        Assert.Equal(12 * 1.2f + 2 + spacing, p.Footer.HeightPt, 2); // default font size 12
        Assert.Equal("Arial", p.Footer.FontName);
    }

    [Fact]
    public void Header_space_never_makes_margin_negative()
    {
        var g = new GlobalSettings { Margins = new MarginSettings { Top = 2 } };
        var p = ConversionPlan.Create(g, Obj(o => o.HeaderSettings = new HeaderSettings { Left = "Title", FontSize = 20 }));
        Assert.Equal(0, p.MarginTop);
    }

    [Fact]
    public void Html_header_fills_the_margin_and_warns_about_page_variables()
    {
        var g = new GlobalSettings { Margins = new MarginSettings { Top = 25 } };
        var p = ConversionPlan.Create(g, Obj(o => o.HeaderSettings = new HeaderSettings { HtmUrl = "https://example.com/h.html" }));
        Assert.Equal("https://example.com/h.html", p.Header.HtmlUrl);
        Assert.Equal(25 * MmPt, p.Header.HeightPt, 2);
        Assert.Equal(0, p.MarginTop);
        Assert.Contains(p.Warnings, w => w.Contains("HtmUrl"));
    }

    [Fact]
    public void Header_with_no_text_and_no_line_is_not_displayed()
    {
        var p = ConversionPlan.Create(new GlobalSettings(), Obj(o => o.HeaderSettings = new HeaderSettings { FontSize = 10 }));
        Assert.Null(p.Header);
        var lineOnly = ConversionPlan.Create(new GlobalSettings(), Obj(o => o.HeaderSettings = new HeaderSettings { Line = true }));
        Assert.NotNull(lineOnly.Header);
        Assert.False(lineOnly.Header.HasText);
    }

    [Fact]
    public void Placeholders_translate()
    {
        var warnings = new List<string>();
        var ctx = new Placeholders.Context { PageOffset = 4, DocumentTitle = "Invoice", WebPage = "https://x.test", Now = new DateTime(2026, 10, 11, 9, 5, 0) };
        string r = Placeholders.Translate("[frompage] [Page] [TOPAGE] [doctitle] [webpage] [isodate] [section] [other]", ctx, warnings.Add);
        Assert.Equal("5 {page_number} {total_pages} Invoice https://x.test 2026-10-11  [other]", r);
        Assert.Equal(new[] { "[section]" }, warnings);
    }

    [Fact]
    public void Page_offset_sets_first_page_number()
        => Assert.Equal(3, ConversionPlan.Create(new GlobalSettings { PageOffset = 2 }, Obj()).FirstPageNumber);

    [Fact]
    public void Global_options_map()
    {
        var g = new GlobalSettings
        {
            ColorMode = ColorMode.Grayscale, ImageQuality = 94, Outline = true, ViewportSize = "1280x1024", Copies = 2
        };
        var p = ConversionPlan.Create(g, Obj());
        Assert.True(p.Grayscale);
        Assert.Equal(6, p.JpegCompressionLevel);
        Assert.Equal(4, p.OutlineDepth);
        Assert.Equal(1280, p.WebPageWidth);
        Assert.Equal(1024, p.WebPageHeight);
        Assert.Contains(p.Warnings, w => w.StartsWith("GlobalSettings.Copies"));
    }

    [Fact]
    public void Object_options_map()
    {
        var o = Obj(x =>
        {
            x.Page = "https://example.com";
            x.UseExternalLinks = false;
            x.WebSettings = new WebSettings { PrintMediaType = true, EnableJavascript = false, Background = false };
            x.LoadSettings = new LoadSettings
            {
                Username = "u", Password = "p", JSDelay = 2500, BlockLocalFileAccess = true,
                Proxy = "http://pu:p%40ss@proxy.local:3128",
                CustomHeaders = new Dictionary<string, string> { ["X-Api"] = "1" },
                Cookies = new Dictionary<string, string> { ["sid"] = "abc" },
            };
        });
        var p = ConversionPlan.Create(new GlobalSettings(), o);
        Assert.Equal("https://example.com", p.Url);
        Assert.Null(p.Html);
        Assert.False(p.ExternalLinks);
        Assert.True(p.PrintMediaType);
        Assert.False(p.JavaScript);
        Assert.False(p.Background);
        Assert.Equal(("u", "p"), (p.Username, p.Password));
        Assert.Equal(3, p.ConversionDelaySeconds); // 2500 ms rounds up to whole seconds
        Assert.True(p.DenyLocalFileAccess);
        Assert.Equal(("proxy.local", 3128, "pu", "p@ss", false), (p.Proxy.Host, p.Proxy.Port, p.Proxy.Username, p.Proxy.Password, p.Proxy.IsSocks5));
        Assert.Equal("1", p.HttpHeaders["X-Api"]);
        Assert.Equal("abc", p.Cookies["sid"]);
        Assert.Empty(p.Warnings);
    }

    [Theory]
    [InlineData("socks5://10.0.0.1", "10.0.0.1", 1080, true)]
    [InlineData("proxy:8888", "proxy", 8888, false)]
    [InlineData("http://proxy", "proxy", 8080, false)]
    public void Proxy_parse(string input, string host, int port, bool socks)
    {
        var s = ProxySpec.Parse(input);
        Assert.Equal((host, port, socks), (s.Host, s.Port, s.IsSocks5));
    }

    [Fact]
    public void Bad_proxy_is_a_warning_not_a_failure()
    {
        var p = ConversionPlan.Create(new GlobalSettings(), Obj(o => o.LoadSettings = new LoadSettings { Proxy = "ftp://x" }));
        Assert.Null(p.Proxy);
        Assert.Contains(p.Warnings, w => w.StartsWith("LoadSettings.Proxy"));
    }

    [Fact]
    public void Font_fallbacks_end_at_bundled_Liberation_fonts()
    {
        Assert.Equal(new[] { "Arial", "Liberation Sans" }, SelectPdfRenderer.FontCandidates("Arial"));
        Assert.Equal(new[] { "Times New Roman", "Liberation Serif", "Liberation Sans" }, SelectPdfRenderer.FontCandidates("Times New Roman"));
        Assert.Equal(new[] { "Courier New", "Liberation Mono", "Liberation Sans" }, SelectPdfRenderer.FontCandidates("Courier New"));
    }

    [Fact]
    public void Local_paths_become_file_urls()
    {
        string path = System.IO.Path.GetTempFileName();
        try
        {
            Assert.StartsWith("file://", SelectPdfRenderer.ToUrl(path));
            Assert.Equal("https://example.com/a", SelectPdfRenderer.ToUrl("https://example.com/a"));
        }
        finally { System.IO.File.Delete(path); }
    }

    [Fact]
    public void PdfTools_has_no_native_dependency()
    {
        using var tools = new PdfTools();
        tools.Load();
        Assert.True(tools.IsLoaded);
        Assert.Contains("SelectPdf", tools.GetLibraryVersion());
        Assert.Throws<NotSupportedException>(() => tools.CreateGlobalSettings());
    }

    [Fact]
    public void Document_without_objects_throws_like_DinkToPdf()
    {
        IConverter c = new BasicConverter(new PdfTools());
        Assert.Throws<ArgumentException>(() => c.Convert(new HtmlToPdfDocument()));
    }
}
