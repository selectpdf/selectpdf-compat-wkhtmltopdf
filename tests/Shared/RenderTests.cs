using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
#if WKHTMLTOPDFDOTNET
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;
using WkHtmlToPdfDotNet.Internal;
#else
using DinkToPdf;
using DinkToPdf.Contracts;
using DinkToPdf.Internal;
#endif

// End-to-end tests through the Chromium engine. They run DinkToPdf's own README samples unchanged.
// Rendering uses the free SelectPdf Community Edition (5 pages per document), or SelectPdf.Universal
// in the .Licensed test projects.
[Collection("render")]
public class RenderTests
{
    static bool IsPdf(byte[] b) => b.Length > 1000 && Encoding.ASCII.GetString(b, 0, 5) == "%PDF-";

    static HtmlToPdfDocument ReadmeDocument() => new HtmlToPdfDocument
    {
        GlobalSettings = { ColorMode = ColorMode.Color, Orientation = Orientation.Landscape, PaperSize = PaperKind.A4Plus },
        Objects =
        {
            new ObjectSettings
            {
                PagesCount = true,
                HtmlContent = "<h1>Hello</h1><p>Lorem ipsum dolor sit amet, consectetur adipiscing elit.</p>",
                WebSettings = { DefaultEncoding = "utf-8" },
                HeaderSettings = { FontSize = 9, Right = "Page [page] of [toPage]", Line = true, Spacing = 2.812 }
            }
        }
    };

    [Fact]
    public void Readme_sample_converts_and_raises_events()
    {
        var converter = new BasicConverter(new PdfTools());
        var errors = new List<string>();
        var phases = new List<string>();
        bool? success = null;
        converter.Error += (s, e) => errors.Add(e.Message);
        converter.PhaseChanged += (s, e) => phases.Add(e.Description);
        converter.Finished += (s, e) => success = e.Success;

        byte[] pdf = converter.Convert(ReadmeDocument());

        Assert.Empty(errors);
        Assert.True(success);
        Assert.True(IsPdf(pdf));
        Assert.NotEmpty(phases);
    }

    [Fact]
    public void Out_writes_the_file_and_returns_empty_like_DinkToPdf()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf");
        try
        {
            var doc = ReadmeDocument();
            doc.GlobalSettings.Out = path;
            byte[] result = new BasicConverter(new PdfTools()).Convert(doc);
            Assert.Empty(result);
            Assert.True(IsPdf(File.ReadAllBytes(path)));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Multiple_objects_are_merged()
    {
        var doc = new HtmlToPdfDocument
        {
            GlobalSettings = { PaperSize = PaperKind.A4, DocumentTitle = "Merged" },
            Objects =
            {
                new ObjectSettings { HtmlContent = "<h1>One</h1>" },
                new ObjectSettings { HtmlContent = "<h1>Two</h1>" },
            }
        };
        byte[] single = new BasicConverter(new PdfTools()).Convert(new HtmlToPdfDocument { Objects = { new ObjectSettings { HtmlContent = "<h1>One</h1>" } } });
        byte[] merged = new BasicConverter(new PdfTools()).Convert(doc);
        Assert.True(IsPdf(merged));
        Assert.True(merged.Length > single.Length);
    }

    [Fact]
    public void Singleton_converter_handles_parallel_requests()
    {
        // the usual ASP.NET Core registration: one SynchronizedConverter shared by all requests
        IConverter converter = new SynchronizedConverter(new PdfTools());
        byte[][] results = Task.WhenAll(Enumerable.Range(0, 4).Select(i => Task.Run(() =>
            converter.Convert(new HtmlToPdfDocument
            {
                Objects = { new ObjectSettings { HtmlContent = $"<h1>Request {i}</h1>" } }
            })))).GetAwaiter().GetResult();
        Assert.All(results, r => Assert.True(IsPdf(r)));
    }

    [Fact]
    public void Configure_hook_sees_the_object()
    {
        IObject seen = null;
        SelectPdfCompat.Configure = (c, o) => seen = o;
        try
        {
            var doc = new HtmlToPdfDocument { Objects = { new ObjectSettings { HtmlContent = "<p>x</p>" } } };
            new BasicConverter(new PdfTools()).Convert(doc);
            Assert.Same(doc.Objects[0], seen);
        }
        finally { SelectPdfCompat.Configure = null; }
    }
    static HtmlToPdfDocument Pages(int count)
    {
        var html = string.Join("", Enumerable.Range(1, count).Select(i =>
            $"<h1 style=\"{(i < count ? "break-after:page" : "")}\">Page {i}</h1>"));
        return new HtmlToPdfDocument { Objects = { new ObjectSettings { HtmlContent = html } } };
    }

#if SELECTPDF_LICENSED
    static int PageCount(byte[] pdf)
    {
        var doc = new SelectPdf.Universal.PdfDocument(new MemoryStream(pdf));
        try { return doc.Pages.Count; } finally { doc.Close(); }
    }

    [Fact]
    public void Licensed_package_has_no_page_limit()
    {
        var converter = new BasicConverter(new PdfTools());
        var warnings = new List<string>();
        converter.Warning += (s, e) => warnings.Add(e.Message);
        byte[] pdf = converter.Convert(Pages(10));
        Assert.Equal(10, PageCount(pdf));
        Assert.DoesNotContain(warnings, w => w.Contains("Community Edition"));
    }

    [Fact]
    public void Licensed_package_appends_several_objects_with_a_url()
    {
        string page = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".html");
        File.WriteAllText(page, "<h1>From a file</h1>");
        try
        {
            var doc = new HtmlToPdfDocument
            {
                Objects =
                {
                    new ObjectSettings { HtmlContent = "<h1>One</h1>" },
                    new ObjectSettings { Page = page },
                }
            };
            Assert.Equal(2, PageCount(new BasicConverter(new PdfTools()).Convert(doc)));
        }
        finally { File.Delete(page); }
    }
#else
    [Fact]
    public void A_document_over_the_page_limit_throws_by_default()
    {
        var converter = new BasicConverter(new PdfTools());
        var errors = new List<string>();
        converter.Error += (s, e) => errors.Add(e.Message);
        var e = Assert.ThrowsAny<InvalidOperationException>(() => converter.Convert(Pages(10)));
        Assert.Contains("10 pages", e.Message);
        Assert.Contains(errors, m => m.Contains("10 pages"));
    }

    [Fact]
    public void Page_limit_can_return_the_first_pages_with_a_warning()
    {
        SelectPdfCompat.FailOnPageLimit = false;
        try
        {
            var converter = new BasicConverter(new PdfTools());
            var warnings = new List<string>();
            converter.Warning += (s, e) => warnings.Add(e.Message);
            byte[] pdf = converter.Convert(Pages(10));
            Assert.True(IsPdf(pdf));
            Assert.Contains(warnings, w => w.Contains("10 pages") && w.Contains("first 5 pages"));
        }
        finally { SelectPdfCompat.FailOnPageLimit = true; }
    }

    [Fact]
    public void A_document_of_exactly_five_pages_is_not_cut()
    {
        var converter = new BasicConverter(new PdfTools());
        var warnings = new List<string>();
        converter.Warning += (s, e) => warnings.Add(e.Message);
        byte[] pdf = converter.Convert(Pages(5));
        Assert.True(IsPdf(pdf));
        Assert.DoesNotContain(warnings, w => w.Contains("Community Edition"));
    }
#endif

    [Fact]
    public void Short_document_has_no_page_limit_warning()
    {
        var converter = new BasicConverter(new PdfTools());
        var warnings = new List<string>();
        converter.Warning += (s, e) => warnings.Add(e.Message);
        converter.Convert(ReadmeDocument());
        Assert.DoesNotContain(warnings, w => w.Contains("Community Edition"));
    }
}

[CollectionDefinition("render", DisableParallelization = true)]
public class RenderCollection { }
