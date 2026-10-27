// HoldAlarms example indicator for NinjaTrader 8. MIT licence.
// Copy HoldAlarmsClient.cs and this file into Documents\NinjaTrader 8\bin\Custom\Indicators,
// then compile in the NinjaScript Editor (F5). Paste your webhook URL into the indicator settings.
// Sends a test alert when realtime data starts and one alert per new bar (on a background thread).
#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.NinjaScript;
using HoldAlarms;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class HoldAlarmsNotifier : Indicator
    {
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "HoldAlarmsNotifier";
                Description = "Sends alerts to your HoldAlarms webhook URL.";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                WebhookUrl = "";
                SendTestOnStart = true;
            }
            else if (State == State.Realtime && SendTestOnStart)
            {
                HoldAlarmsClient.AlertAsync(WebhookUrl, "ninjatrader8", Instrument.FullName, "test", Close[0], null, "test alert", Log2);
            }
        }

        protected override void OnBarUpdate()
        {
            if (State != State.Realtime) return; // no alerts while loading history
            string id = Instrument.FullName + "-" + Time[0].ToString("yyyyMMddHHmmss");
            HoldAlarmsClient.AlertAsync(WebhookUrl, "ninjatrader8", Instrument.FullName, "bar_close", Close[0], id, null, Log2);
        }

        private void Log2(string message) { Print(message); }

        [Display(Name = "Webhook URL (keep it secret)", Order = 1, GroupName = "HoldAlarms")]
        public string WebhookUrl { get; set; }

        [Display(Name = "Send test alert on start", Order = 2, GroupName = "HoldAlarms")]
        public bool SendTestOnStart { get; set; }
    }
}
