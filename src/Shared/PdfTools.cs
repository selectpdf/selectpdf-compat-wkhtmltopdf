using System;
using System.Reflection;

#if WKHTMLTOPDFDOTNET
namespace WkHtmlToPdfDotNet
#else
namespace DinkToPdf
#endif
{
    /// <summary>
    /// Stand-in for the wrapper's PdfTools, which loaded the native libwkhtmltox library.
    /// Nothing native is loaded here: no libwkhtmltox.dll/.so/.dylib needs to be copied next to the app.
    /// The low-level wkhtmltopdf calls are not available and throw <see cref="NotSupportedException"/>;
    /// <see cref="BasicConverter"/> and <see cref="SynchronizedConverter"/> never call them.
    /// </summary>
    public sealed class PdfTools : ITools
    {
        public bool IsLoaded { get; private set; }

        public void Load() => IsLoaded = true;

        public bool ExtendedQt() => true;

        public string GetLibraryVersion()
        {
            var v = typeof(PdfTools).GetTypeInfo().Assembly.GetName().Version;
            return $"{typeof(PdfTools).GetTypeInfo().Assembly.GetName().Name} {v} (SelectPdf Chromium engine)";
        }

        public void Dispose() { }

        static NotSupportedException Native(string name) => new NotSupportedException(
            $"PdfTools.{name} is a raw wkhtmltopdf call. This SelectPdf compatibility package does not use wkhtmltopdf; " +
            "convert through BasicConverter or SynchronizedConverter.");

        public IntPtr CreateGlobalSettings() => throw Native(nameof(CreateGlobalSettings));
        public int SetGlobalSetting(IntPtr settings, string name, string value) => throw Native(nameof(SetGlobalSetting));
        public string GetGlobalSetting(IntPtr settings, string name) => throw Native(nameof(GetGlobalSetting));
        public void DestroyGlobalSetting(IntPtr settings) => throw Native(nameof(DestroyGlobalSetting));
        public IntPtr CreateObjectSettings() => throw Native(nameof(CreateObjectSettings));
        public int SetObjectSetting(IntPtr settings, string name, string value) => throw Native(nameof(SetObjectSetting));
        public string GetObjectSetting(IntPtr settings, string name) => throw Native(nameof(GetObjectSetting));
        public void DestroyObjectSetting(IntPtr settings) => throw Native(nameof(DestroyObjectSetting));
        public IntPtr CreateConverter(IntPtr globalSettings) => throw Native(nameof(CreateConverter));
        public void AddObject(IntPtr converter, IntPtr objectSettings, byte[] data) => throw Native(nameof(AddObject));
        public void AddObject(IntPtr converter, IntPtr objectSettings, string data) => throw Native(nameof(AddObject));
        public bool DoConversion(IntPtr converter) => throw Native(nameof(DoConversion));
        public void DestroyConverter(IntPtr converter) => throw Native(nameof(DestroyConverter));
        public byte[] GetConversionResult(IntPtr converter) => throw Native(nameof(GetConversionResult));
        public int SetPhaseChangedCallback(IntPtr converter, VoidCallback callback) => throw Native(nameof(SetPhaseChangedCallback));
        public int SetProgressChangedCallback(IntPtr converter, VoidCallback callback) => throw Native(nameof(SetProgressChangedCallback));
        public int SetFinishedCallback(IntPtr converter, IntCallback callback) => throw Native(nameof(SetFinishedCallback));
        public int SetWarningCallback(IntPtr converter, StringCallback callback) => throw Native(nameof(SetWarningCallback));
        public int SetErrorCallback(IntPtr converter, StringCallback callback) => throw Native(nameof(SetErrorCallback));
        public int GetPhaseCount(IntPtr converter) => throw Native(nameof(GetPhaseCount));
        public int GetCurrentPhase(IntPtr converter) => throw Native(nameof(GetCurrentPhase));
        public string GetPhaseDescription(IntPtr converter, int phase) => throw Native(nameof(GetPhaseDescription));
        public string GetProgressString(IntPtr converter) => throw Native(nameof(GetProgressString));
    }
}
