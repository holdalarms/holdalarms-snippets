# HoldAlarms snippets

Free, MIT-licensed code that sends alerts from trading platforms to a webhook URL.
It works with any HTTPS webhook. We wrote it for [HoldAlarms](https://holdalarms.com), which answers
each alert as soon as it is queued, then delivers it to your own endpoint with retries and a
dead-letter queue.

These files contain **no secrets**. You paste your own webhook URL into the inputs. Keep that URL
private: the token in it identifies your account.

| Platform | Folder | Setup guide |
|---|---|---|
| MetaTrader 5 / MetaTrader 4 (MQL5 / MQL4) | [`metatrader/`](metatrader/) | [holdalarms.com/docs/metatrader-setup](https://holdalarms.com/docs/metatrader-setup) |
| NinjaTrader 8 (NinjaScript C#) | [`ninjatrader/`](ninjatrader/) | [holdalarms.com/docs/ninjatrader-setup](https://holdalarms.com/docs/ninjatrader-setup) |

## MetaTrader 5 and 4

1. Copy `metatrader/MQL5/Include/HoldAlarms.mqh` into your terminal's `MQL5/Include` folder
   (File → Open Data Folder). For MetaTrader 4 use the `MQL4` folder.
2. In the terminal, open Tools → Options → Expert Advisors, tick
   **Allow WebRequest for listed URL** and add `https://api.holdalarms.com`.
3. In your Expert Advisor or script:

   ```mql5
   #include <HoldAlarms.mqh>
   input string InpWebhookUrl = ""; // your webhook URL

   // anywhere in your EA:
   HoldAlarmsAlert(InpWebhookUrl, "buy");                         // small JSON alert
   HoldAlarmsSend(InpWebhookUrl, "{\"ticker\":\"EURGBP\",\"action\":\"sell\"}"); // your own body
   ```

4. Run `Scripts/HoldAlarmsTest` once to send a test alert.

Good to know (from the [MQL5 reference](https://www.mql5.com/en/docs/network/webrequest)):

- `WebRequest()` works only in Expert Advisors and scripts, not in indicators (error 4014), and not in the Strategy Tester.
- It blocks until the reply arrives. The snippet waits at most 3 seconds per try and tries twice on a network error or 5xx. It does not repeat a 4xx such as a wrong token (401).
- A body that starts with `{` or `[` goes out as `application/json`, anything else as `text/plain`.
- Add your own `id` field if your endpoint needs to spot a repeat.

Not tested in the Strategy Tester, because MetaTrader doesn't allow `WebRequest()` there.

## NinjaTrader 8

1. Copy `ninjatrader/HoldAlarmsClient.cs` and `ninjatrader/HoldAlarmsNotifier.cs` into
   `Documents\NinjaTrader 8\bin\Custom\Indicators`.
2. Open the NinjaScript Editor and press F5 to compile.
3. Add the HoldAlarmsNotifier indicator to a chart and paste your webhook URL into its settings.
   It sends a test alert when realtime data starts, then one alert per closed bar.
4. In your own indicator or strategy, call:

   ```csharp
   HoldAlarms.HoldAlarmsClient.AlertAsync(url, "ninjatrader8", Instrument.FullName, "buy", Close[0]);
   HoldAlarms.HoldAlarmsClient.SendAsync(url, "{\"ticker\":\"ES\",\"action\":\"sell\"}");
   ```

Good to know:

- The send runs on a background thread, so `OnBarUpdate` isn't held up. Results show in the NinjaScript Output window.
- Each try waits at most 3 seconds; it repeats once on a network error or 5xx, never on a 4xx.
- The example skips historical bars, so loading a chart doesn't fire old alerts.
- `HoldAlarmsClient.cs` builds for .NET Framework 4.8 and was tested against a local mock server (see [`tests/`](tests/)).
  The indicator was type-checked against stand-ins, not inside NinjaTrader.

## Licence

MIT, see [LICENSE](LICENSE). HoldAlarms is not affiliated with MetaQuotes or NinjaTrader. MetaTrader is a trademark of MetaQuotes Ltd. NinjaTrader is a trademark of its owner.
