using System; using System.Text.Json; using cAlgo.Robots;
int fails = 0; void ok(bool c, string m) { Console.WriteLine((c ? "ok - " : "FAIL - ") + m); if (!c) fails++; }
var t = new DateTime(2026, 10, 27, 14, 5, 0, DateTimeKind.Utc);
var j = HoldAlarmsJson.Build("EURGBP", "buy", 0.86712, t, "EURGBP-buy-1", "say \"hi\"\nGrüße\u0001");
Console.WriteLine(j);
var d = JsonDocument.Parse(j).RootElement;
ok(d.GetProperty("source").GetString() == "ctrader", "source");
ok(d.GetProperty("price").GetDouble() == 0.86712, "price round-trips");
ok(d.GetProperty("fired_at").GetString() == "2026-10-27T14:05:00Z", "fired_at ISO");
ok(d.GetProperty("note").GetString() == "say \"hi\"\nGrüße\u0001", "note escaped");
ok(JsonDocument.Parse(HoldAlarmsJson.Build("X", "a", double.NaN, t, "i", null)).RootElement.GetProperty("price").ValueKind == JsonValueKind.Null, "NaN -> null");
var de = System.Globalization.CultureInfo.GetCultureInfo("de-DE"); System.Globalization.CultureInfo.CurrentCulture = de;
ok(HoldAlarmsJson.Build("X", "a", 1.5, t, "i", "").Contains("\"price\":1.5"), "decimal point under de-DE");
ok(HoldAlarmsJson.ShouldRetry(0) && HoldAlarmsJson.ShouldRetry(503) && !HoldAlarmsJson.ShouldRetry(401) && !HoldAlarmsJson.ShouldRetry(403), "retry only on 0/5xx (403 cloud not retried)");
ok(HoldAlarmsJson.UrlAllowed("https://api.holdalarms.com/hook/x") && !HoldAlarmsJson.UrlAllowed("http://api.holdalarms.com/hook/x"), "https only");
Console.WriteLine(fails == 0 ? "PASS ctrader-json" : "FAIL ctrader-json"); return fails;
