// HoldAlarms webhook client for NinjaTrader 8 (C#, .NET Framework 4.8). MIT licence, see LICENSE.
// Sends one alert to your webhook URL on a background thread, so OnBarUpdate is never blocked.
// No secrets in this file: you pass your own webhook URL.
// Repeats only on a network error, timeout or 5xx; a 4xx (for example 401, wrong token) is final.
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HoldAlarms
{
    public static class HoldAlarmsClient
    {
        /// <summary>Timeout per try in milliseconds.</summary>
        public static int TimeoutMs = 3000;
        /// <summary>Total tries (first try included).</summary>
        public static int Tries = 2;
        /// <summary>Wait between tries in milliseconds.</summary>
        public static int RetryDelayMs = 500;

        static HoldAlarmsClient()
        {
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }
        }

        /// <summary>Send a body in the background. Returns the final HTTP status, or -1 on a network error.</summary>
        public static Task<int> SendAsync(string url, string body, Action<string> log = null)
        {
            return Task.Run(() => Send(url, body, log));
        }

        /// <summary>Send a small JSON alert in the background.</summary>
        public static Task<int> AlertAsync(string url, string source, string symbol, string action, double price, string id = null, string note = null, Action<string> log = null)
        {
            return SendAsync(url, BuildJson(source, symbol, action, price, DateTime.UtcNow, id, note), log);
        }

        /// <summary>Blocking send. Prefer SendAsync from trading code.</summary>
        public static int Send(string url, string body, Action<string> log = null)
        {
            if (string.IsNullOrEmpty(url) || !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("http://127.0.0.1", StringComparison.Ordinal))
            {
                if (log != null) log("HoldAlarms: webhook URL must start with https://");
                return -1;
            }
            body = body ?? "";
            string trimmed = body.TrimStart();
            string contentType = trimmed.StartsWith("{") || trimmed.StartsWith("[") ? "application/json" : "text/plain; charset=utf-8";
            byte[] data = Encoding.UTF8.GetBytes(body);
            int code = -1;
            for (int attempt = 1; attempt <= Math.Max(1, Tries); attempt++)
            {
                code = PostOnce(url, data, contentType);
                if (code >= 200 && code < 300) { if (log != null) log("HoldAlarms: delivered (HTTP " + code + ")"); return code; }
                if (code >= 400 && code < 500) { if (log != null) log("HoldAlarms: rejected (HTTP " + code + "), check the webhook URL"); return code; }
                if (log != null) log("HoldAlarms: try " + attempt + " failed (" + (code < 0 ? "network error or timeout" : "HTTP " + code) + ")");
                if (attempt < Tries) Thread.Sleep(RetryDelayMs);
            }
            return code;
        }

        static int PostOnce(string url, byte[] data, string contentType)
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "POST";
                req.ContentType = contentType;
                req.UserAgent = "HoldAlarms-NinjaTrader/1.0";
                req.Timeout = TimeoutMs;
                req.ReadWriteTimeout = TimeoutMs;
                req.ContentLength = data.Length;
                using (Stream s = req.GetRequestStream()) s.Write(data, 0, data.Length);
                using (var resp = (HttpWebResponse)req.GetResponse()) return (int)resp.StatusCode;
            }
            catch (WebException e)
            {
                var r = e.Response as HttpWebResponse;
                if (r != null) { int c = (int)r.StatusCode; r.Close(); return c; }
                return -1;
            }
            catch (Exception) { return -1; }
        }

        public static string BuildJson(string source, string symbol, string action, double price, DateTime firedAtUtc, string id, string note)
        {
            var sb = new StringBuilder("{");
            sb.Append("\"source\":\"").Append(Esc(source)).Append("\",");
            sb.Append("\"symbol\":\"").Append(Esc(symbol)).Append("\",");
            sb.Append("\"action\":\"").Append(Esc(action)).Append("\",");
            sb.Append("\"price\":").Append(price.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"fired_at\":\"").Append(firedAtUtc.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture)).Append('"');
            if (!string.IsNullOrEmpty(id)) sb.Append(",\"id\":\"").Append(Esc(id)).Append('"');
            if (!string.IsNullOrEmpty(note)) sb.Append(",\"note\":\"").Append(Esc(note)).Append('"');
            return sb.Append('}').ToString();
        }

        public static string Esc(string s)
        {
            if (s == null) return "";
            var sb = new StringBuilder(s.Length + 8);
            foreach (char ch in s)
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < 0x20) sb.Append("\\u").Append(((int)ch).ToString("x4")); else sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
