namespace WkHtmlToPdfDotNet
{
    /// <summary>A cover page: an object rendered first, without header or footer.</summary>
    public class CoverSettings : ObjectSettings
    {
        public bool IsCover { get; set; }

        public CoverSettings()
        {
            IsCover = true;
        }
    }
}
