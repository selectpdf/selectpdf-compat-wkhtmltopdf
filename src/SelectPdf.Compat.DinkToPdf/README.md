# SelectPdf.Compat.DinkToPdf

A free drop-in replacement for [DinkToPdf](https://github.com/rdvojmoc/DinkToPdf). It has the same API (`IConverter`, `SynchronizedConverter`, `HtmlToPdfDocument`, `GlobalSettings`, `ObjectSettings`), but Chromium does the rendering, through the free [SelectPdf Community Edition](https://www.nuget.org/packages/SelectPdf.HtmlToPdf.Universal), instead of wkhtmltopdf.

- **No code changes.** The namespaces and types are DinkToPdf's.
- **No native wkhtmltopdf files**. You can delete `libwkhtmltox.dll`, `.so` and `.dylib`.
- **Runs on** Windows x64/x86, Linux x64/ARM64, macOS on Apple Silicon, and Docker.
- **Renders modern HTML and CSS** (flexbox, grid, web fonts, current JavaScript) through Chromium instead of the archived Qt WebKit.
- **Free for commercial use**, up to **5 pages per document**. See [Limits](#limits).

This package is not affiliated with DinkToPdf.

## Migrate

```bash
dotnet remove package DinkToPdf
dotnet add package SelectPdf.Compat.DinkToPdf
# the engine for each platform you run on (they install side by side):
dotnet add package SelectPdf.Universal.Native.linux-x64     # or linux-arm64, win-x64, win-x86, osx-arm64
```

Delete the `libwkhtmltox.*` files from your project and the `CustomAssemblyLoadContext` code that loaded them. Your code stays as it is:

```csharp
using DinkToPdf;
using DinkToPdf.Contracts;

services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

byte[] pdf = converter.Convert(new HtmlToPdfDocument
{
    GlobalSettings = { PaperSize = PaperKind.A4, Orientation = Orientation.Portrait, Margins = new MarginSettings { Top = 10 } },
    Objects = { new ObjectSettings { HtmlContent = html, FooterSettings = { Center = "Page [page] of [toPage]" } } }
});
```

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "YourApp.dll"]
```

The native package ships the engine, so the image needs no `apt-get` lines. On Linux, glibc 2.30 or later is required, which the official .NET images have.

## Limits

The Community Edition renders **up to 5 pages per document**. Pages past the fifth are not rendered. When output reaches 5 pages, the converter raises its `Warning` event saying so. To make that an error instead:

```csharp
SelectPdfCompat.FailOnPageLimit = true;   // empty result + Error event (or an exception with ThrowOnError)
```

Several objects in one document are joined into one HTML document with page breaks, so page numbering runs continuously across them. They must all use `HtmlContent`. A document with several objects where any of them is a URL (`Page`) can't be built by the free edition, which cannot merge PDFs, so it fails with a clear error.

For longer documents or URL merging, use [SelectPdf.Universal](https://www.nuget.org/packages/SelectPdf.Universal) (licensed, no page limit). See the [migration guide](https://selectpdf.com/pdf-library/html/Migrating-To-SelectPdf-Universal.htm).

## How settings map

| DinkToPdf | SelectPdf |
|---|---|
| `PaperSize` (any `PaperKind` or `PechkinPaperSize`) and `Orientation` | Exact page size in points |
| `Margins` (mm, cm or in) | Margins in points. Defaults to 10 mm on each side, as in wkhtmltopdf |
| `HeaderSettings` / `FooterSettings`: `Left`/`Center`/`Right`, `FontName`, `FontSize`, `Line`, `Spacing` | Text sections placed inside the margin, as wkhtmltopdf placed them |
| `[page]`, `[topage]`, `[frompage]`, `[date]`, `[isodate]`, `[time]`, `[title]`, `[doctitle]`, `[webpage]` | `{page_number}`, `{total_pages}`, or the value itself (case-insensitive, so `[toPage]` works) |
| `HtmUrl` | HTML header/footer section |
| `ObjectSettings.Page` (URL or local file) / `HtmlContent` | `ConvertUrl` / `ConvertHtmlString` |
| Several `Objects` (HTML) | One PDF, page breaks between objects |
| `ColorMode.Grayscale` | Grayscale color space |
| `DocumentTitle`, `Outline`/`OutlineDepth`, `ImageQuality`, `PageOffset`, `ViewportSize`, `Out` | Title, h1..hN bookmarks, JPEG level, first page number, browser viewport, file output (returns an empty array, as before) |
| `WebSettings`: `PrintMediaType`, `EnableJavascript`, `Background`, `UserStyleSheet` | CSS media type, JavaScript, backgrounds, custom CSS |
| `LoadSettings`: `Username`/`Password`, `JSDelay`, `Proxy`, `CustomHeaders`, `Cookies`, `BlockLocalFileAccess` | HTTP auth, conversion delay (rounded up to whole seconds), proxy, headers, cookies, local file access |
| Events: `PhaseChanged`, `ProgressChanged`, `Finished`, `Error`, `Warning` | Raised at the same points |

Settings with no equivalent, such as `Copies`, `DPI`, `MinimumFontSize` and `ZoomFactor`, are ignored. Each one raises the `Warning` event so you can see it.

## What behaves differently

- **Engine.** Chromium lays pages out like a current browser. Pages tuned around WebKit quirks may paginate differently. Use CSS `break-before`, `break-after` and `break-inside: avoid` to control page breaks.
- **Page width.** wkhtmltopdf's "intelligent shrinking" fitted wide pages to the paper. SelectPdf renders at a 1024 px browser width and scales to the page. Set `GlobalSettings.ViewportSize = "1280x1024"` to change the width.
- **HTML header/footer URLs.** wkhtmltopdf passed `?page=&topage=` to the header page. That isn't done here, so use text headers for page numbers.
- **Several objects.** The first content object's header, footer and web settings apply to the whole document.
- **Concurrency.** `SynchronizedConverter` no longer queues every conversion on one thread. SelectPdf converts in parallel, so a singleton converter serves concurrent requests.
- **Failures.** A failed conversion returns an empty array and raises `Error`, as before. Set `SelectPdfCompat.ThrowOnError = true` to throw instead.

## Other SelectPdf options

`SelectPdfCompat.Configure` receives the `SelectPdf.Universal.HtmlToPdf` converter after your settings are applied, so you can set any Community Edition option:

```csharp
SelectPdfCompat.Configure = (converter, obj) => converter.Options.ConversionDelay = 2;
```

## License

This package is MIT licensed. Its public API reproduces DinkToPdf's (MIT, © 2017 Rok Dvojmoc); see `THIRD-PARTY-DinkToPdf-LICENSE.txt`. Rendering uses the free SelectPdf Community Edition, © Outside Software SRL.
