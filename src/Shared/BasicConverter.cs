using System;
using System.Linq;

#if WKHTMLTOPDFDOTNET
namespace WkHtmlToPdfDotNet
#else
namespace DinkToPdf
#endif
{
    /// <summary>
    /// Same public surface as the wrapper's BasicConverter. Rendering is done by SelectPdf.Universal;
    /// the <see cref="ITools"/> instance is accepted for compatibility and only <c>Load()</c> is called.
    /// </summary>
    public class BasicConverter : IConverter
    {
#if WKHTMLTOPDFDOTNET
        public ITools Tools { get; }
#else
        public readonly ITools Tools;
#endif

        public IDocument ProcessingDocument { get; private set; }

        public event EventHandler<PhaseChangedArgs> PhaseChanged;
        public event EventHandler<ProgressChangedArgs> ProgressChanged;
        public event EventHandler<FinishedArgs> Finished;
        public event EventHandler<ErrorArgs> Error;
        public event EventHandler<WarningArgs> Warning;

        public BasicConverter(ITools tools)
        {
            Tools = tools;
        }

        public virtual byte[] Convert(IDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (document.GetObjects().Count() == 0)
                throw new ArgumentException("No objects is defined in document that was passed. At least one object must be defined.");

            ProcessingDocument = document;
            Tools?.Load();

            var renderer = new SelectPdfRenderer(message =>
                Warning?.Invoke(this, new WarningArgs { Document = document, Message = message }));
            try
            {
                byte[] result = renderer.Render(document, (current, count, description) =>
                {
                    PhaseChanged?.Invoke(this, new PhaseChangedArgs
                    {
                        Document = document, CurrentPhase = current, PhaseCount = count, Description = description
                    });
                    ProgressChanged?.Invoke(this, new ProgressChangedArgs
                    {
                        Document = document, Description = $"{100 * current / count}%"
                    });
                });
                Finished?.Invoke(this, new FinishedArgs { Document = document, Success = true });
                return result;
            }
            catch (Exception e)
            {
                Error?.Invoke(this, new ErrorArgs { Document = document, Message = e.Message });
                Finished?.Invoke(this, new FinishedArgs { Document = document, Success = false });
                if (SelectPdfCompat.ThrowOnError) throw;
                return new byte[0]; // the wrappers' behaviour on a failed conversion
            }
        }
#if WKHTMLTOPDFDOTNET

        /// <summary>Nothing native to release; kept because IConverter is IDisposable in WkHtmlToPdfDotNet.</summary>
        public virtual void Dispose()
        {
        }
#endif
    }
}
