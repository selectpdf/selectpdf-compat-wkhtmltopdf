using System;
using System.Globalization;

#if WKHTMLTOPDFDOTNET
namespace WkHtmlToPdfDotNet.Internal
#else
namespace DinkToPdf.Internal
#endif
{
    /// <summary>Parses wkhtmltopdf's proxy string: [http|socks5://][user[:password]@]host[:port].</summary>
    internal sealed class ProxySpec
    {
        public bool IsSocks5;
        public string Host;
        public int Port;
        public string Username;
        public string Password;

        public static ProxySpec Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string s = value.Trim();
            var spec = new ProxySpec();

            int schemeEnd = s.IndexOf("://", StringComparison.Ordinal);
            if (schemeEnd >= 0)
            {
                string scheme = s.Substring(0, schemeEnd).ToLowerInvariant();
                if (scheme == "socks5") spec.IsSocks5 = true;
                else if (scheme != "http")
                    throw new FormatException($"Unsupported proxy scheme '{scheme}'. Use http or socks5.");
                s = s.Substring(schemeEnd + 3);
            }

            int at = s.LastIndexOf('@');
            if (at >= 0)
            {
                string cred = s.Substring(0, at);
                s = s.Substring(at + 1);
                int colon = cred.IndexOf(':');
                spec.Username = Uri.UnescapeDataString(colon >= 0 ? cred.Substring(0, colon) : cred);
                spec.Password = colon >= 0 ? Uri.UnescapeDataString(cred.Substring(colon + 1)) : null;
            }

            s = s.TrimEnd('/');
            int portSep = s.LastIndexOf(':');
            if (portSep > 0 && !s.EndsWith("]", StringComparison.Ordinal))
            {
                spec.Host = s.Substring(0, portSep);
                spec.Port = int.Parse(s.Substring(portSep + 1), NumberStyles.None, CultureInfo.InvariantCulture);
            }
            else
            {
                spec.Host = s;
                spec.Port = spec.IsSocks5 ? 1080 : 8080;
            }
            if (spec.Host.Length == 0) throw new FormatException($"Proxy '{value}' has no host.");
            return spec;
        }
    }
}
