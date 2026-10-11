using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DinkToPdf;
using DinkToPdf.Contracts;
using Xunit;

// End-to-end tests through the Chromium engine. They run DinkToPdf's own README samples unchanged.
// Without a license key SelectPdf runs in evaluation mode, which is fine for these checks.
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
        DinkToPdfCompat.Configure = (c, o) => seen = o;
        try
        {
            var doc = new HtmlToPdfDocument { Objects = { new ObjectSettings { HtmlContent = "<p>x</p>" } } };
            new BasicConverter(new PdfTools()).Convert(doc);
            Assert.Same(doc.Objects[0], seen);
        }
        finally { DinkToPdfCompat.Configure = null; }
    }
}

[CollectionDefinition("render", DisableParallelization = true)]
public class RenderCollection { }
