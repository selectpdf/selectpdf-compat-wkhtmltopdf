# SelectPdf.Compat.DinkToPdf

A drop-in replacement for [DinkToPdf](https://github.com/rdvojmoc/DinkToPdf). It has the same API (`IConverter`, `SynchronizedConverter`, `HtmlToPdfDocument`, `GlobalSettings`, `ObjectSettings`), but SelectPdf.Universal's Chromium engine does the rendering instead of wkhtmltopdf.

- **No code changes.** The namespaces and types are DinkToPdf's.
- **No native files to copy.** You can delete `libwkhtmltox.dll`, `.so` and `.dylib`.
- **Runs on** Windows x64/x86, Linux x64/ARM64, macOS on Apple Silicon, and Docker, with nothing to `apt-get`.
- **Renders modern HTML and CSS** (flexbox, grid, web fonts, current JavaScript) through Chromium instead of the archived Qt WebKit.

This package is not affiliated with the DinkToPdf project.

## Migrate

```bash
dotnet remove package DinkToPdf
dotnet add package SelectPdf.Compat.DinkToPdf
# the engine for each platform you run on (side by side is fine):
dotnet add package SelectPdf.Universal.Native.linux-x64     # or linux-arm64, win-x64, win-x86, osx-arm64
```

Delete the `libwkhtmltox.*` files from your project and the `CustomAssemblyLoadContext` code that loaded them. Your DinkToPdf code stays as it is:

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

Set your SelectPdf license key once at startup. Without one, output is in evaluation mode.

```csharp
DinkToPdfCompat.LicenseKey = "your-key";
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

The native package ships the engine, so the image needs no extra system packages. On Linux, glibc 2.30 or later is required, which the official .NET images have.

## How settings map

| DinkToPdf | SelectPdf |
|---|---|
| `PaperSize` (any `PaperKind` or `PechkinPaperSize`) and `Orientation` | Exact page size in points |
| `Margins` (mm, cm or in) | Margins in points. Defaults to 10 mm on each side, as in wkhtmltopdf |
| `HeaderSettings` / `FooterSettings`: `Left`/`Center`/`Right`, `FontName`, `FontSize`, `Line`, `Spacing` | Text sections placed inside the margin, as wkhtmltopdf placed them |
| `[page]`, `[topage]`, `[frompage]`, `[date]`, `[isodate]`, `[time]`, `[title]`, `[doctitle]`, `[webpage]` | `{page_number}`, `{total_pages}`, or the value itself (case-insensitive, so `[toPage]` works) |
| `HtmUrl` | HTML header/footer section |
| `ObjectSettings.Page` (URL or local file) / `HtmlContent` | `ConvertUrl` / `ConvertHtmlString` |
| Several `Objects` | One merged PDF |
| `ColorMode.Grayscale` | Grayscale color space |
| `DocumentTitle`, `Outline`/`OutlineDepth`, `ImageQuality`, `PageOffset`, `ViewportSize`, `Out` | Title, h1..hN bookmarks, JPEG level, first page number, browser viewport, file output (returns an empty array, as DinkToPdf did) |
| `WebSettings`: `PrintMediaType`, `EnableJavascript`, `Background`, `UserStyleSheet` | CSS media type, JavaScript, backgrounds, custom CSS |
| `LoadSettings`: `Username`/`Password`, `JSDelay`, `Proxy`, `CustomHeaders`, `Cookies`, `BlockLocalFileAccess` | HTTP auth, conversion delay (rounded up to whole seconds), proxy, headers, cookies, local file access |
| Events: `PhaseChanged`, `ProgressChanged`, `Finished`, `Error`, `Warning` | Raised at the same points |

Settings with no equivalent, such as `Copies`, `DPI`, `MinimumFontSize` and `ZoomFactor`, are ignored. Each one raises the `Warning` event so you can see it.

## What behaves differently

- **Engine.** Chromium lays pages out like a current browser. Pages tuned around WebKit quirks may paginate differently. Use CSS `break-before`, `break-after` and `break-inside: avoid` to control page breaks.
- **Page width.** wkhtmltopdf's "intelligent shrinking" fitted wide pages to the paper. SelectPdf renders at a 1024 px browser width and scales to the page. Set `GlobalSettings.ViewportSize = "1280x1024"` to change the width.
- **`HtmUrl` headers.** wkhtmltopdf passed `?page=&topage=` to the header page. That isn't done here, so use text headers for page numbers.
- **Multiple objects.** Page numbers restart for each object.
- **Concurrency.** `SynchronizedConverter` no longer queues every conversion on one thread. SelectPdf converts in parallel, so a singleton converter serves concurrent requests.
- **Failures.** As with DinkToPdf, a failed conversion returns an empty array and raises `Error`. Set `DinkToPdfCompat.ThrowOnError = true` to throw the exception instead.

## SelectPdf options DinkToPdf doesn't have

Use the `Configure` hook. For example, to produce tagged, accessible PDFs:

```csharp
DinkToPdfCompat.Configure = (converter, obj) =>
{
    converter.Options.Tagged = true;
    converter.Options.Language = "en-US";
};
```

The hook gets the `SelectPdf.Universal.HtmlToPdf` converter after the DinkToPdf settings are applied, so every SelectPdf option is available. For new code, use [SelectPdf.Universal](https://www.nuget.org/packages/SelectPdf.Universal) directly. Docs: https://selectpdf.com/pdf-library/

## License

This package is MIT licensed. Its public API reproduces DinkToPdf's (MIT, © 2017 Rok Dvojmoc); see `THIRD-PARTY-DinkToPdf-LICENSE.txt`. Rendering requires SelectPdf.Universal, which is licensed separately: https://selectpdf.com/pricing/
