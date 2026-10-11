using System;

#if WKHTMLTOPDFDOTNET
namespace WkHtmlToPdfDotNet
#else
namespace DinkToPdf
#endif
{
    /// <summary>
    /// Same public surface as the wrapper's SynchronizedConverter, so the usual registration keeps working:
    /// <code>services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));</code>
    /// The wkhtmltopdf wrappers funnelled every conversion through one thread because wkhtmltopdf is not thread-safe.
    /// SelectPdf converts concurrently (up to <c>HtmlToPdfOptions.MaximumConcurrentConversions</c>),
    /// so calls run on the caller's thread and no longer queue behind each other.
    /// </summary>
    public class SynchronizedConverter : BasicConverter
    {
        public SynchronizedConverter(ITools tools) : base(tools)
        {
        }

        public override byte[] Convert(IDocument document)
        {
            return Invoke(() => base.Convert(document));
        }

        /// <summary>Kept for API compatibility; runs the delegate directly.</summary>
        public TResult Invoke<TResult>(Func<TResult> @delegate)
        {
            if (@delegate == null) throw new ArgumentNullException(nameof(@delegate));
            return @delegate();
        }
#if WKHTMLTOPDFDOTNET

        public override void Dispose()
        {
            base.Dispose();
        }
#endif
    }
}
