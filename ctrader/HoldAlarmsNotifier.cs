// HoldAlarms notifier cBot for cTrader (cTrader Algo, C#). MIT licence, see LICENSE.
// Sends an alert to your HoldAlarms webhook URL with an HTTP POST. HoldAlarms answers as soon as
// the alert is queued, then delivers it to your own endpoint and retries if that fails.
//
// Notes (from the cTrader Algo docs):
//  - AccessRights.None is enough for Http.* calls.
//  - Http does not work in cTrader Cloud: no request is sent and the response has StatusCode 403.
//    Run this cBot in cTrader Desktop or the cTrader CLI.
//  - In backtesting and optimisation Http sends real requests. This cBot sends nothing unless
//    RunMode is RealTime, so a backtest doesn't send hundreds of alerts.
using System;
using System.Globalization;
using System.Text;
using cAlgo.API;

namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.None, AddIndicators = false, TimeZone = TimeZones.UTC)] // UTC so fired_at is true UTC
    public class HoldAlarmsNotifier : Robot
    {
        // Paste your HoldAlarms webhook URL here, e.g. https://api.holdalarms.com/hook/<your token>.
        // The token identifies your account: don't share screenshots of this setting.
        [Parameter("Webhook URL", DefaultValue = "")]
        public string WebhookUrl { get; set; }

        [Parameter("Send test alert on start", DefaultValue = true)]
        public bool SendTestOnStart { get; set; }

        [Parameter("Alert on each new bar", DefaultValue = false)]
        public bool AlertOnNewBar { get; set; }

        protected override void OnStart()
        {
            if (SendTestOnStart) SendAlert("test", Bars.ClosePrices.LastValue, "HoldAlarms test from cTrader");
        }

        protected override void OnBar()
        {
            if (AlertOnNewBar) SendAlert("bar", Bars.ClosePrices.Last(1), "new bar " + TimeFrame);
        }

        // Call this from your own logic, e.g. SendAlert("buy", Symbol.Ask, "breakout").
        public void SendAlert(string action, double price, string note)
        {
            if (RunningMode != RunningMode.RealTime) return; // backtests send real requests otherwise
            string url = (WebhookUrl ?? "").Trim();
            if (!HoldAlarmsJson.UrlAllowed(url)) { Print("HoldAlarms: set Webhook URL to your https:// HoldAlarms URL"); return; }
            string id = SymbolName + "-" + action + "-" + Server.Time.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            string body = HoldAlarmsJson.Build(SymbolName, action, price, Server.Time, id, note);
            Post(url, body, 1);
        }

        private void Post(string url, string body, int attempt)
        {
            var request = new HttpRequest(new Uri(url))
            {
                Method = HttpMethod.Post,
                Body = body,
                Timeout = TimeSpan.FromMilliseconds(HoldAlarmsJson.TimeoutMs),
            };
            request.Headers.Add("Content-Type", "application/json");
            // SendAsync keeps the cBot running while the request is in flight.
            Http.SendAsync(request, response =>
            {
                if (response.IsSuccessful) return;
                int code = response.StatusCode;
                if (HoldAlarmsJson.ShouldRetry(code) && attempt < HoldAlarmsJson.Tries)
                {
                    Timer.Start(TimeSpan.FromMilliseconds(HoldAlarmsJson.RetryDelayMs));
                    _pending = () => Post(url, body, attempt + 1);
                    return;
                }
                Print("HoldAlarms: alert not accepted (HTTP {0}){1}", code, code == 403 ? " - Http is not available in cTrader Cloud" : "");
            });
        }

        private Action _pending;
        protected override void OnTimer()
        {
            Timer.Stop();
            var p = _pending; _pending = null; p?.Invoke();
        }
    }

    // Kept separate so it can be tested outside cTrader.
    public static class HoldAlarmsJson
    {
        public const int TimeoutMs = 3000, Tries = 2, RetryDelayMs = 500;

        public static bool UrlAllowed(string url) =>
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("http://127.0.0.1", StringComparison.Ordinal);

        // Retry only when nothing came back (0) or the server had a problem (5xx); 4xx means fix the setup.
        public static bool ShouldRetry(int code) => code <= 0 || code >= 500;

        public static string Build(string symbol, string action, double price, DateTime firedAt, string id, string note)
        {
            var sb = new StringBuilder();
            sb.Append("{\"source\":\"ctrader\",\"symbol\":\"").Append(Esc(symbol))
              .Append("\",\"action\":\"").Append(Esc(action))
              .Append("\",\"price\":").Append(double.IsNaN(price) || double.IsInfinity(price) ? "null" : price.ToString("R", CultureInfo.InvariantCulture))
              .Append(",\"fired_at\":\"").Append(firedAt.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture))
              .Append("\",\"id\":\"").Append(Esc(id))
              .Append("\",\"note\":\"").Append(Esc(note)).Append("\"}");
            return sb.ToString();
        }

        public static string Esc(string s)
        {
            if (s == null) return "";
            var sb = new StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4")); else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
