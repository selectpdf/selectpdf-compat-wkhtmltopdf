using System;
using DinkToPdf.Contracts;
using SelectPdf.Universal;

namespace DinkToPdf
{
    /// <summary>
    /// Settings specific to the SelectPdf implementation of the DinkToPdf API.
    /// Everything here is optional; DinkToPdf code runs unchanged without touching it.
    /// </summary>
    public static class DinkToPdfCompat
    {
        /// <summary>
        /// DinkToPdf returns an empty byte array when a conversion fails (and raises <c>Error</c>).
        /// That is the default here too. Set to true to throw the underlying exception instead.
        /// </summary>
        public static bool ThrowOnError { get; set; }

        /// <summary>
        /// Called for every document object after the DinkToPdf settings are applied and before rendering.
        /// Use it for SelectPdf options DinkToPdf has no setting for, for example tagged PDF/UA output:
        /// <code>DinkToPdfCompat.Configure = (converter, obj) => converter.Options.Tagged = true;</code>
        /// </summary>
        public static Action<HtmlToPdf, IObject> Configure { get; set; }

        /// <summary>Shortcut for <c>SelectPdf.Universal.GlobalProperties.LicenseKey</c>.</summary>
        public static string LicenseKey
        {
            get => GlobalProperties.LicenseKey;
            set => GlobalProperties.LicenseKey = value;
        }
    }
}
