# SelectPdf compatibility packages for wkhtmltopdf wrappers

Move a .NET application off wkhtmltopdf without rewriting its PDF code. These packages keep the API of the two most used .NET wkhtmltopdf wrappers, **DinkToPdf** and **Haukcode.WkHtmlToPdfDotNet**, and render with **Chromium** through [SelectPdf](https://selectpdf.com) instead of the archived Qt WebKit engine.

- **Keeps your code.** The namespaces and types are the wrapper's own (`IConverter`, `SynchronizedConverter`, `HtmlToPdfDocument`, `GlobalSettings`, `ObjectSettings`), so existing code builds against these packages.
- **Keeps your layout.** CSS sizes are scaled the way wkhtmltopdf's smart shrinking scaled them, headers and footers sit where wkhtmltopdf put them, and `[page]` / `[topage]` work in header and footer text.
- **Modern rendering.** Chromium renders flexbox, grid, web fonts and current JavaScript, which wkhtmltopdf's WebKit does not.
- **Runs everywhere .NET runs in production:** Windows x64/x86, Linux x64/ARM64, macOS on Apple Silicon, and Docker images with no `apt-get` lines.
- **No native wkhtmltopdf files** to copy, load or keep in sync per platform.

## Why move off wkhtmltopdf

wkhtmltopdf's GitHub repository is archived, and its last release, 0.12.6, dates from June 2020. DinkToPdf's last NuGet release is from 2017. Both still sit under a large number of .NET applications, which keep shipping an unmaintained browser engine, its native libraries, and on Linux the system packages it needs.

## Packages

| Replaces | Free package | Licensed package | Namespace |
|---|---|---|---|
| [DinkToPdf](https://www.nuget.org/packages/DinkToPdf) | [SelectPdf.Compat.DinkToPdf](src/SelectPdf.Compat.DinkToPdf/README.md) | [SelectPdf.Compat.DinkToPdf.Licensed](src/SelectPdf.Compat.DinkToPdf.Licensed/README.md) | `DinkToPdf` |
| [Haukcode.WkHtmlToPdfDotNet](https://www.nuget.org/packages/Haukcode.WkHtmlToPdfDotNet) | [SelectPdf.Compat.WkHtmlToPdfDotNet](src/SelectPdf.Compat.WkHtmlToPdfDotNet/README.md) | [SelectPdf.Compat.WkHtmlToPdfDotNet.Licensed](src/SelectPdf.Compat.WkHtmlToPdfDotNet.Licensed/README.md) | `WkHtmlToPdfDotNet` |

**Free or licensed?**

| | Free packages | `.Licensed` packages |
|---|---|---|
| Rendering library | [SelectPdf Community Edition](https://www.nuget.org/packages/SelectPdf.HtmlToPdf.Universal) (free for commercial use) | [SelectPdf.Universal](https://www.nuget.org/packages/SelectPdf.Universal) (needs a [SelectPdf license](https://selectpdf.com/pricing/)) |
| Pages per document | Up to 5. A longer document throws a clear error by default, so a cut PDF never reaches your users unnoticed | No limit |
| Several objects with a URL among them | Not supported (needs PDF merging) | Converted one by one and merged |
| Code | Identical | Identical, so moving up means changing the package reference |

Rotativa, Wkhtmltopdf.NetCore, NReco.PdfGenerator and TuesPechkin have no compatibility package yet; for those, call [SelectPdf.Universal](https://www.nuget.org/packages/SelectPdf.Universal) directly.

## Quick start

1. Swap the wrapper package for the compatibility package:

   ```bash
   dotnet remove package DinkToPdf
   dotnet add package SelectPdf.Compat.DinkToPdf
   ```

2. Add the SelectPdf engine package for the platform the application runs on, one per build:

   ```bash
   dotnet add package SelectPdf.Universal.Native.linux-x64
   ```

   | Engine package | Platform |
   |---|---|
   | `SelectPdf.Universal.Native.win-x64` | Windows, 64-bit process |
   | `SelectPdf.Universal.Native.win-x86` | Windows, 32-bit process |
   | `SelectPdf.Universal.Native.linux-x64` | Linux x64, glibc 2.30+ (Debian 11+, Ubuntu 20.04+, RHEL 9+) |
   | `SelectPdf.Universal.Native.linux-arm64` | Linux ARM64, glibc 2.30+ |
   | `SelectPdf.Universal.Native.osx-arm64` | macOS 13+ on Apple Silicon |

   The engine packages install into the same folder, so reference only one per build. The package READMEs show an MSBuild snippet that picks the engine by platform, for projects developed on Windows and deployed to Linux.

3. Delete the `libwkhtmltox` files and the code that loaded them, and remove the `apt-get` lines your Dockerfile needed for wkhtmltopdf. Your converter code stays as it is:

   ```csharp
   services.AddSingleton(typeof(IConverter),
       new SynchronizedConverter(new PdfTools()));
   ```

4. Build the Docker image from the official .NET images, with nothing else installed:

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

## How it works

Each conversion goes through three steps, all in `src/Shared`:

1. **`DocumentLayout`** turns the document's objects into conversions. Several HTML objects become one HTML document with page breaks between them, so page numbering runs across all of them, as it did in wkhtmltopdf.
2. **`ConversionPlan`** translates the wrapper settings into SelectPdf terms, with no SelectPdf types involved, so the mapping is unit-tested on its own: paper size and orientation, margins in points, header and footer placement inside the margins, wkhtmltopdf's `[page]`-style variables, and the scale wkhtmltopdf applied to the content.
3. **`SelectPdfRenderer`** runs the conversion with SelectPdf's `HtmlToPdf` converter and raises the wrapper's events (`PhaseChanged`, `ProgressChanged`, `Finished`, `Error`, `Warning`) at the same points as before.

Settings with no SelectPdf equivalent, such as `Copies` or `DPI`, are ignored and reported through the `Warning` event. `SelectPdfCompat.Configure` hands you the SelectPdf converter before each conversion, for any option the wrapper API has no setting for.

The package READMEs list every mapped setting and every behavior difference.

## About SelectPdf

[SelectPdf](https://selectpdf.com) makes PDF libraries for .NET and an online HTML to PDF API, and is developed by Outside Software SRL.

- **[SelectPdf.Universal](https://www.nuget.org/packages/SelectPdf.Universal)** is the cross-platform SelectPdf library and the one the `.Licensed` packages build on. It creates, edits and reads PDF documents in C# and VB.NET, and includes an HTML to PDF converter built on Chromium and a Word to PDF converter. It also covers merging, redaction, form filling, digital signatures with time stamps and signature validation, PDF/A and PDF/X output, tagged PDF/UA-1 and PDF/UA-2, ZUGFeRD / Factur-X electronic invoices, HTML to image, and PDF to text and PDF to image. It runs on Windows, Linux x64/ARM64 and macOS on Apple Silicon, on .NET 10, .NET 9 and .NET 8, and on .NET Framework 4.6.1+ through .NET Standard 2.0.
- **[SelectPdf Community Edition](https://selectpdf.com/community-edition/)** ([SelectPdf.HtmlToPdf.Universal](https://www.nuget.org/packages/SelectPdf.HtmlToPdf.Universal)) is the free HTML to PDF converter from the same library, for documents up to 5 pages. The free compatibility packages build on it. It uses the same `SelectPdf.Universal` namespace as the full library, so moving up needs no code change.
- **Licensing.** The library is sold as perpetual licenses per developer, starting at $499, and one license covers both the cross-platform SelectPdf.Universal and the Windows-only classic SelectPdf library. See [pricing](https://selectpdf.com/pricing/).
- **Other languages.** The [SelectPdf HTML to PDF API](https://selectpdf.com/html-to-pdf-api/) converts HTML to PDF from any language over HTTP.

Documentation: [selectpdf.com/html-to-pdf/docs](https://selectpdf.com/html-to-pdf/docs/) · Support: [support@selectpdf.com](mailto:support@selectpdf.com)

## Building and testing

```bash
dotnet test tests/SelectPdf.Compat.DinkToPdf.Tests
dotnet test tests/SelectPdf.Compat.WkHtmlToPdfDotNet.Tests
dotnet test tests/SelectPdf.Compat.DinkToPdf.Licensed.Tests
dotnet test tests/SelectPdf.Compat.WkHtmlToPdfDotNet.Licensed.Tests
```

All four packages build from the same code. `WKHTMLTOPDFDOTNET` selects the WkHtmlToPdfDotNet namespace and its few extra members (`CoverSettings`, `TableOfContentsSettings`, `LoadSettings.Post`); `SELECTPDF_LICENSED` builds on SelectPdf.Universal instead of the Community Edition. The tests cover the settings mapping and layout without the engine, and convert real documents through Chromium, including DinkToPdf's own README sample. The engine package used by the tests matches the machine running them.

## Issues

Found a setting that maps differently from wkhtmltopdf, or a document that renders differently? Open an issue with the settings and, if you can, the HTML.

## License

MIT. The public API follows DinkToPdf (MIT, © 2017 Rok Dvojmoc); see `THIRD-PARTY-DinkToPdf-LICENSE.txt`. No WkHtmlToPdf-DotNet (LGPL) source is included. These packages are not affiliated with DinkToPdf or WkHtmlToPdf-DotNet. The SelectPdf libraries they use are licensed separately.
