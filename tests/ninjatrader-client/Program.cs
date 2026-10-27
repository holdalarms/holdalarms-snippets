using System; using HoldAlarms;
class P { static int Main(string[] a) {
  string b = a[0]; int fails = 0;
  Func<bool,string,int> ok = (c,m) => { Console.WriteLine((c?"  ok - ":"FAIL: ")+m); if(!c) fails++; return 0; };
  HoldAlarmsClient.TimeoutMs = 800; HoldAlarmsClient.Tries = 2; HoldAlarmsClient.RetryDelayMs = 50;
  string json = HoldAlarmsClient.BuildJson("ninjatrader8","ES 12-26","buy",5012.25,new DateTime(2026,10,27,14,5,0,DateTimeKind.Utc),"id-1","say \"hi\"\nGrüße");
  ok(json=="{\"source\":\"ninjatrader8\",\"symbol\":\"ES 12-26\",\"action\":\"buy\",\"price\":5012.25,\"fired_at\":\"2026-10-27T14:05:00Z\",\"id\":\"id-1\",\"note\":\"say \\\"hi\\\"\\nGrüße\"}", "BuildJson escapes and formats: "+json);
  ok(HoldAlarmsClient.SendAsync(b+"/ok", json).Result==200, "200 on first try");
  ok(HoldAlarmsClient.Send(b+"/ok", "BUY ES")==200, "text body 200");
  ok(HoldAlarmsClient.Send(b+"/flaky", json)==200, "503 then 200: repeated once");
  ok(HoldAlarmsClient.Send(b+"/401", json)==401, "401 returned, not repeated");
  var t0=DateTime.UtcNow; int rc=HoldAlarmsClient.Send(b+"/hang", json); double ms=(DateTime.UtcNow-t0).TotalMilliseconds;
  ok(rc==-1 && ms<3000, "hang: -1 after 2 tries within timeout ("+(int)ms+" ms)");
  ok(HoldAlarmsClient.Send("ftp://x", json)==-1, "non-https URL refused");
  return fails; } }
