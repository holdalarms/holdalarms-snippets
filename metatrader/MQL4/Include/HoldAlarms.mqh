//+------------------------------------------------------------------+
//| HoldAlarms.mqh - send an alert from MetaTrader 4 to a webhook URL |
//| MIT licence. https://github.com/holdalarms/holdalarms-snippets    |
//|                                                                  |
//| Setup (once): Tools -> Options -> Expert Advisors ->             |
//|   tick "Allow WebRequest for listed URL" and add                 |
//|   https://api.holdalarms.com                                     |
//| WebRequest() works in Expert Advisors and scripts only, not in   |
//| indicators, and not in the Strategy Tester (MQL4 reference, build 600 and later).     |
//| It blocks until the reply arrives, so keep the timeout short.    |
//+------------------------------------------------------------------+
#ifndef HOLDALARMS_MQH
#define HOLDALARMS_MQH

// Escape a string for use inside a JSON value.
string HoldAlarmsJsonEscape(string s)
  {
   StringReplace(s, "\\", "\\\\");
   StringReplace(s, "\"", "\\\"");
   StringReplace(s, "\r", "\\r");
   StringReplace(s, "\n", "\\n");
   StringReplace(s, "\t", "\\t");
   return s;
  }

// POST body to url. JSON if it starts with { or [, otherwise plain text.
// Returns true on HTTP 2xx. Tries again only on a network error (-1) or a 5xx,
// so a 4xx (for example a wrong token, 401) is reported at once.
// timeout_ms: how long to wait for each try. HoldAlarms answers as soon as the
// alert is queued, so 3000 ms is plenty.
bool HoldAlarmsSend(const string url, const string body, const int timeout_ms = 3000, const int tries = 2)
  {
   if(StringLen(url) < 12 || StringFind(url, "https://") != 0)
     {
      Print("HoldAlarms: the webhook URL must start with https://");
      return false;
     }
   char data[];
   int n = StringToCharArray(body, data, 0, WHOLE_ARRAY, CP_UTF8);
   if(n > 0) ArrayResize(data, n - 1);          // drop the trailing 0 byte
   string first = StringSubstr(body, 0, 1);
   string headers = (first == "{" || first == "[")
                    ? "Content-Type: application/json\r\n"
                    : "Content-Type: text/plain; charset=utf-8\r\n";
   for(int i = 1; i <= tries; i++)
     {
      char result[];
      string result_headers;
      ResetLastError();
      int code = WebRequest("POST", url, headers, timeout_ms, data, result, result_headers);
      if(code >= 200 && code < 300)
        {
         Print("HoldAlarms: alert sent (HTTP ", code, ")");
         return true;
        }
      if(code == -1)
         Print("HoldAlarms: WebRequest error ", GetLastError(),
               ". Is https://api.holdalarms.com in Tools -> Options -> Expert Advisors -> Allow WebRequest?",
               " (try ", i, " of ", tries, ")");
      else
         Print("HoldAlarms: HTTP ", code, " ", CharArrayToString(result, 0, (int)MathMin(ArraySize(result), 200), CP_UTF8),
               " (try ", i, " of ", tries, ")");
      if(code >= 400 && code < 500) return false;   // not worth repeating
      if(i < tries) Sleep(500);
     }
   return false;
  }

// Convenience: a small JSON alert with symbol, action, price, time and your own id.
bool HoldAlarmsAlert(const string url, const string action, const string note = "", const string id = "")
  {
   string body = "{\"source\":\"metatrader4\",\"symbol\":\"" + HoldAlarmsJsonEscape(_Symbol) +
                 "\",\"action\":\"" + HoldAlarmsJsonEscape(action) +
                 "\",\"price\":" + DoubleToString(MarketInfo(_Symbol, MODE_BID), _Digits) +
                 ",\"fired_at\":\"" + TimeToString(TimeGMT(), TIME_DATE | TIME_SECONDS) + " UTC\"" +
                 (id != "" ? ",\"id\":\"" + HoldAlarmsJsonEscape(id) + "\"" : "") +
                 (note != "" ? ",\"note\":\"" + HoldAlarmsJsonEscape(note) + "\"" : "") + "}";
   return HoldAlarmsSend(url, body);
  }

#endif
