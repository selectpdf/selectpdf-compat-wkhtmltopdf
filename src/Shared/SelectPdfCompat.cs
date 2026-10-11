using System;
using SelectPdf.Universal;

#if WKHTMLTOPDFDOTNET
namespace WkHtmlToPdfDotNet
#else
namespace DinkToPdf
#endif
{
    /// <summary>
    /// Settings specific to the SelectPdf implementation. Everything here is optional;
    /// existing wkhtmltopdf-wrapper code runs unchanged without touching it.
    /// </summary>
    public static class SelectPdfCompat
    {
        /// <summary>Pages the free SelectPdf Community Edition renders per document; later pages are cut.</summary>
        public const int CommunityPageLimit = 5;

        /// <summary>
        /// A failed conversion returns an empty byte array and raises <c>Error</c>, as the wkhtmltopdf
        /// wrappers did. Set to true to throw the underlying exception instead.
        /// </summary>
        public static bool ThrowOnError { get; set; }

        /// <summary>
        /// When output reaches <see cref="CommunityPageLimit"/> pages, a <c>Warning</c> is raised because
        /// content past that page was not rendered. Set to true to fail the conversion instead
        /// (with <see cref="ThrowOnError"/> false that means an empty result and an <c>Error</c> event).
        /// </summary>
        public static bool FailOnPageLimit { get; set; }

        /// <summary>
        /// Called after the wrapper settings are applied and before rendering, with the SelectPdf converter
        /// and the document object. Use it for options the wrapper API has no setting for.
        /// </summary>
        public static Action<HtmlToPdf, IObject> Configure { get; set; }
    }
}
