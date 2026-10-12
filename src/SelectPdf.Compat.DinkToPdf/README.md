# SelectPdf.Compat.DinkToPdf

A free replacement for [DinkToPdf](https://github.com/rdvojmoc/DinkToPdf) that keeps its API (`IConverter`, `SynchronizedConverter`, `HtmlToPdfDocument`, `GlobalSettings`, `ObjectSettings`). Chromium does the rendering, through the free [SelectPdf Community Edition](https://www.nuget.org/packages/SelectPdf.HtmlToPdf.Universal), instead of wkhtmltopdf.

- **Keeps the DinkToPdf API.** The namespace and types are DinkToPdf's, so existing code builds against this package.
- **No native wkhtmltopdf files.** You can delete `libwkhtmltox.dll`, `.so` and `.dylib`.
- **Runs on** Windows x64/x86, Linux x64/ARM64, macOS on Apple Silicon, and Docker.
- **Renders modern HTML and CSS** (flexbox, grid, web fonts, current JavaScript) through Chromium instead of the archived Qt WebKit.
- **Keeps the size of your content.** CSS sizes are scaled the way wkhtmltopdf's smart shrinking scaled them, and content wider than the page is shrunk to fit.
- **Free for commercial use**, up to **5 pages per document**. See [Limits](#limits).

This package is not affiliated with DinkToPdf.

&nbsp;

## Migrate

```bash
dotnet remove package DinkToPdf
dotnet add package SelectPdf.Compat.DinkToPdf
```

Then add the SelectPdf engine package for the platform the application runs on: `SelectPdf.Universal.Native.<rid>`, where `<rid>` is `win-x64`, `win-x86`, `linux-x64`, `linux-arm64` or `osx-arm64`. Reference one engine package per build, because they all install into the same folder. A project developed on Windows and deployed to a Linux container can pick the package by platform:

```xml
<PropertyGroup>
  <SelectPdfRid>$(RuntimeIdentifier)</SelectPdfRid>
  <SelectPdfRid Condition="'$(SelectPdfRid)' == '' And
      $([MSBuild]::IsOSPlatform('Windows'))">win-x64</SelectPdfRid>
  <SelectPdfRid Condition="'$(SelectPdfRid)' == ''">linux-x64</SelectPdfRid>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="SelectPdf.Universal.Native.$(SelectPdfRid)"
                    Version="26.5.0" />
</ItemGroup>
```

Delete the `libwkhtmltox.*` files from your project and the `CustomAssemblyLoadContext` code that loaded them. Your code stays as it is:

```csharp
using DinkToPdf;
using DinkToPdf.Contracts;

services.AddSingleton(typeof(IConverter),
    new SynchronizedConverter(new PdfTools()));

byte[] pdf = converter.Convert(new HtmlToPdfDocument
{
    GlobalSettings =
    {
        PaperSize = PaperKind.A4,
        Orientation = Orientation.Portrait,
        Margins = new MarginSettings { Top = 10 }
    },
    Objects =
    {
        new ObjectSettings
        {
            HtmlContent = html,
            FooterSettings = { Center = "Page [page] of [toPage]" }
        }
    }
});
```

&nbsp;

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

The engine package bundles the Chromium libraries, so the image needs no `apt-get` lines. On Linux, glibc 2.30 or later is required, which the official .NET images have. Linux images often have no fonts installed; the engine has built-in fonts for Latin scripts, and [SelectPdf.Universal.Fonts](https://www.nuget.org/packages/SelectPdf.Universal.Fonts) adds every other script.

&nbsp;

## Limits

The Community Edition renders **up to 5 pages per document**. A longer document makes `Convert` throw an `InvalidOperationException` that says how many pages the document had, so a cut PDF never reaches your users unnoticed. To accept the first 5 pages instead, with a `Warning` event:

```csharp
SelectPdfCompat.FailOnPageLimit = false;
```

Several objects in one document are joined into one HTML document with page breaks, so page numbering runs continuously across them. They must all use `HtmlContent`: a document with several objects where any of them is a URL (`Page`) needs PDF merging, which the free edition doesn't do, so it fails with a clear error.

For longer documents or URL merging, use [SelectPdf.Compat.DinkToPdf.Licensed](https://www.nuget.org/packages/SelectPdf.Compat.DinkToPdf.Licensed), the same code on the licensed SelectPdf.Universal library.

&nbsp;

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
| `WebSettings.EnableIntelligentShrinking`, `LoadSettings.ZoomFactor` | The scale of the content, as wkhtmltopdf applied it |
| `ColorMode.Grayscale` | Grayscale color space |
| `DocumentTitle`, `Outline`/`OutlineDepth`, `ImageQuality`, `PageOffset`, `ViewportSize`, `Out` | Title, h1..hN bookmarks, JPEG level, first page number, browser viewport, file output (returns an empty array, as before) |
| `WebSettings`: `PrintMediaType`, `EnableJavascript`, `Background`, `UserStyleSheet` | CSS media type, JavaScript, backgrounds, custom CSS |
| `LoadSettings`: `Username`/`Password`, `JSDelay`, `Proxy`, `CustomHeaders`, `Cookies`, `BlockLocalFileAccess` | HTTP auth, conversion delay (rounded up to whole seconds), proxy, headers, cookies, local file access |
| Events: `PhaseChanged`, `ProgressChanged`, `Finished`, `Error`, `Warning` | Raised at the same points |

Settings with no equivalent, such as `Copies`, `DPI` and `MinimumFontSize`, are ignored. Each one raises the `Warning` event so you can see it.

&nbsp;

## What behaves differently

- **Engine.** Chromium lays pages out like a current browser. Layouts that worked around WebKit's missing flexbox and grid support now render as written, and text metrics differ slightly. Use CSS `break-before`, `break-after` and `break-inside: avoid` to control page breaks.
- **HTML header/footer URLs.** wkhtmltopdf passed `?page=&topage=` to the `HtmUrl` page. That isn't done here, so use text headers for page numbers.
- **Wide content without smart shrinking.** With `EnableIntelligentShrinking = false`, content wider than the page is shrunk to fit instead of being cut at the right edge.
- **Several objects.** The first content object's header, footer and web settings apply to the whole document.
- **Concurrency.** `SynchronizedConverter` no longer queues every conversion on one thread. SelectPdf converts in parallel, so a singleton converter serves concurrent requests.
- **Failures.** A failed conversion returns an empty array and raises `Error`, as before. Set `SelectPdfCompat.ThrowOnError = true` to throw instead.

&nbsp;

## Other SelectPdf options

`SelectPdfCompat.Configure` receives the `SelectPdf.Universal.HtmlToPdf` converter after your settings are applied, so you can set any other SelectPdf option:

```csharp
SelectPdfCompat.Configure = (converter, obj) =>
    converter.Options.ConversionDelay = 2;
```

&nbsp;

## License

This package is MIT licensed. Its public API reproduces DinkToPdf's (MIT, © 2017 Rok Dvojmoc); see `THIRD-PARTY-DinkToPdf-LICENSE.txt`. Rendering uses the free SelectPdf Community Edition, © Outside Software SRL.
