# SelectPdf compatibility packages for wkhtmltopdf wrappers

Free drop-in replacements for .NET wkhtmltopdf wrappers. Your code keeps its API, and the rendering moves to Chromium through the free SelectPdf Community Edition (up to 5 pages per document) on Windows, Linux x64/ARM64, macOS and Docker.

| Replaces | Package | Namespace |
|---|---|---|
| [DinkToPdf](https://www.nuget.org/packages/DinkToPdf) | [SelectPdf.Compat.DinkToPdf](src/SelectPdf.Compat.DinkToPdf/README.md) | `DinkToPdf` |
| [Haukcode.WkHtmlToPdfDotNet](https://www.nuget.org/packages/Haukcode.WkHtmlToPdfDotNet) | [SelectPdf.Compat.WkHtmlToPdfDotNet](src/SelectPdf.Compat.WkHtmlToPdfDotNet/README.md) | `WkHtmlToPdfDotNet` |

Both packages build from the same engine code in `src/Shared`. `WKHTMLTOPDFDOTNET` selects the WkHtmlToPdfDotNet namespace and its few extra members.

```bash
dotnet test tests/SelectPdf.Compat.DinkToPdf.Tests
dotnet test tests/SelectPdf.Compat.WkHtmlToPdfDotNet.Tests
```

MIT licensed. The public API follows DinkToPdf (MIT); see `THIRD-PARTY-DinkToPdf-LICENSE.txt`.
