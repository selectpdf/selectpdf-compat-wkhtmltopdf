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
    /// existing wkhtmltopdf-wrapper code runs without touching it.
    /// </summary>
    public static class SelectPdfCompat
    {
        /// <summary>
        /// Pages the free SelectPdf Community Edition renders per document; later pages are cut.
        /// The .Licensed packages, built on SelectPdf.Universal, have no page limit.
        /// </summary>
        public const int CommunityPageLimit = 5;

        /// <summary>
        /// A failed conversion returns an empty byte array and raises <c>Error</c>, as the wkhtmltopdf
        /// wrappers did. Set to true to throw the underlying exception instead.
        /// </summary>
        public static bool ThrowOnError { get; set; }

        /// <summary>
        /// What happens when a document is longer than <see cref="CommunityPageLimit"/> pages in the free
        /// Community Edition. True (the default): <c>Convert</c> throws an <see cref="InvalidOperationException"/>
        /// that says how many pages the document had, so a cut document never reaches your users unnoticed.
        /// False: the first pages are returned and a <c>Warning</c> is raised. Has no effect in the .Licensed
        /// packages, which have no page limit.
        /// </summary>
        public static bool FailOnPageLimit { get; set; } = true;

        /// <summary>
        /// Called after the wrapper settings are applied and before rendering, with the SelectPdf converter
        /// and the document object. Use it for options the wrapper API has no setting for.
        /// </summary>
        public static Action<HtmlToPdf, IObject> Configure { get; set; }
    }
}
