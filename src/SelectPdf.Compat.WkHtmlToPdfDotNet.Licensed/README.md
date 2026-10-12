# SelectPdf.Compat.WkHtmlToPdfDotNet.Licensed

The [Haukcode.WkHtmlToPdfDotNet](https://github.com/HakanL/WkHtmlToPdf-DotNet) API (`IConverter`, `SynchronizedConverter`, `HtmlToPdfDocument`, `GlobalSettings`, `ObjectSettings`, `CoverSettings`) on the licensed [SelectPdf.Universal](https://www.nuget.org/packages/SelectPdf.Universal) library. Chromium does the rendering instead of wkhtmltopdf, with no page limit.

It is the same code as the free [SelectPdf.Compat.WkHtmlToPdfDotNet](https://www.nuget.org/packages/SelectPdf.Compat.WkHtmlToPdfDotNet) package, built on SelectPdf.Universal instead of the Community Edition. Moving from the free package to this one means changing the package reference; the code stays the same.

- **Keeps the Haukcode.WkHtmlToPdfDotNet API.** The namespace and types are Haukcode.WkHtmlToPdfDotNet's, so existing code builds against this package.
- **No native wkhtmltopdf files** and no `apt-get install` of wkhtmltopdf's Linux dependencies.
- **Runs on** Windows x64/x86, Linux x64/ARM64, macOS on Apple Silicon, and Docker.
- **Renders modern HTML and CSS** (flexbox, grid, web fonts, current JavaScript) through Chromium instead of the archived Qt WebKit.
- **Keeps the size of your content.** CSS sizes are scaled the way wkhtmltopdf's smart shrinking scaled them, and content wider than the page is shrunk to fit.
- **No page limit**, and several objects that include a URL are merged into one PDF.
- **Needs a SelectPdf license.** Without a license key, the output carries a demo watermark. See [pricing](https://selectpdf.com/pricing/).

This package is not affiliated with Haukcode.WkHtmlToPdfDotNet.

&nbsp;

## Migrate

```bash
dotnet remove package Haukcode.WkHtmlToPdfDotNet
dotnet add package SelectPdf.Compat.WkHtmlToPdfDotNet.Licensed
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

Remove the `apt-get` lines your Dockerfile needed for wkhtmltopdf (libgdiplus, fontconfig and the other libraries in its sample Dockerfile). Your code stays as it is:

```csharp
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;

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

### License key

```csharp
SelectPdf.Universal.GlobalProperties.LicenseKey = "your license key";
```

Set the key once at startup, before the first conversion.

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

## How settings map

| Haukcode.WkHtmlToPdfDotNet | SelectPdf |
|---|---|
| `PaperSize` (any `PaperKind` or `PechkinPaperSize`) and `Orientation` | Exact page size in points |
| `Margins` (mm, cm or in) | Margins in points. Defaults to 10 mm on each side, as in wkhtmltopdf |
| `HeaderSettings` / `FooterSettings`: `Left`/`Center`/`Right`, `FontName`, `FontSize`, `Line`, `Spacing` | Text sections placed inside the margin, as wkhtmltopdf placed them |
| `[page]`, `[topage]`, `[frompage]`, `[date]`, `[isodate]`, `[time]`, `[title]`, `[doctitle]`, `[webpage]` | `{page_number}`, `{total_pages}`, or the value itself (case-insensitive, so `[toPage]` works) |
| `HtmlUrl` | HTML header/footer section |
| `ObjectSettings.Page` (URL or local file) / `HtmlContent` | `ConvertUrl` / `ConvertHtmlString` |
| Several `Objects` (HTML) | One PDF, page breaks between objects |
| `CoverSettings` | The cover goes first, with no header or footer |
| `TableOfContentsSettings` | Skipped with a `Warning` (no generated TOC page); use `Outline = true` for bookmarks |
| `ObjectSettings.Encoding` | Honoured by `GetContent()` |
| `WebSettings.EnableIntelligentShrinking`, `LoadSettings.ZoomFactor` | The scale of the content, as wkhtmltopdf applied it |
| `ColorMode.Grayscale` | Grayscale color space |
| `DocumentTitle`, `Outline`/`OutlineDepth`, `ImageQuality`, `PageOffset`, `ViewportSize`, `Out` | Title, h1..hN bookmarks, JPEG level, first page number, browser viewport, file output (returns an empty array, as before) |
| `WebSettings`: `PrintMediaType`, `EnableJavascript`, `Background`, `UserStyleSheet` | CSS media type, JavaScript, backgrounds, custom CSS |
| `LoadSettings`: `Username`/`Password`, `JSDelay`, `Proxy`, `CustomHeaders`, `Cookies`, `BlockLocalFileAccess`, `Post` | HTTP auth, conversion delay (rounded up to whole seconds), proxy, headers, cookies, local file access, POST fields |
| Events: `PhaseChanged`, `ProgressChanged`, `Finished`, `Error`, `Warning` | Raised at the same points |

Settings with no equivalent, such as `Copies`, `DPI` and `MinimumFontSize`, are ignored. Each one raises the `Warning` event so you can see it.

&nbsp;

## What behaves differently

- **Engine.** Chromium lays pages out like a current browser. Layouts that worked around WebKit's missing flexbox and grid support now render as written, and text metrics differ slightly. Use CSS `break-before`, `break-after` and `break-inside: avoid` to control page breaks.
- **HTML header/footer URLs.** wkhtmltopdf passed `?page=&topage=` to the `HtmlUrl` page. That isn't done here, so use text headers for page numbers.
- **Wide content without smart shrinking.** With `EnableIntelligentShrinking = false`, content wider than the page is shrunk to fit instead of being cut at the right edge.
- **Several objects.** The first content object's header, footer and web settings apply to the whole document.
- **Several objects with a URL.** Each object is converted on its own and the PDFs are appended, so `[page]` and `[topage]` count the pages of each object.
- **Table of contents.** No TOC page is generated; the PDF gets bookmarks when `Outline` is true.
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

This package is MIT licensed. Its public API is based on DinkToPdf's (MIT, © 2017 Rok Dvojmoc; see `THIRD-PARTY-DinkToPdf-LICENSE.txt`), in WkHtmlToPdf-DotNet's namespace. No WkHtmlToPdf-DotNet (LGPL) source is included. Rendering uses the licensed SelectPdf.Universal library, © Outside Software SRL, which needs its own license.
