//+------------------------------------------------------------------+
//| HoldAlarmsExample.mq4 - example EA: one alert per new bar         |
//| MIT licence. Replace the logic in OnTick() with your own signal.  |
//+------------------------------------------------------------------+
#property strict
#include <HoldAlarms.mqh>

input string InpWebhookUrl = "";        // Your HoldAlarms webhook URL (keep it secret)
input bool   InpSendTestOnStart = true; // Send one test alert when the EA starts

datetime g_last_bar = 0;

int OnInit()
  {
   if(InpWebhookUrl == "")
     {
      Print("HoldAlarms: paste your webhook URL into the EA inputs.");
      return INIT_PARAMETERS_INCORRECT;
     }
   if(InpSendTestOnStart)
      HoldAlarmsAlert(InpWebhookUrl, "test", "HoldAlarms example EA started");
   return INIT_SUCCEEDED;
  }

void OnTick()
  {
   datetime bar = iTime(_Symbol, _Period, 0);
   if(bar == g_last_bar) return;
   bool first = (g_last_bar == 0);
   g_last_bar = bar;
   if(first) return;                     // skip the bar the EA started on
   // Example signal: previous bar closed up -> "buy", down -> "sell".
   double o = iOpen(_Symbol, _Period, 1), c = iClose(_Symbol, _Period, 1);
   string action = (c > o) ? "buy" : "sell";
   HoldAlarmsAlert(InpWebhookUrl, action, "example: previous bar direction", _Symbol + "-" + IntegerToString((long)bar));
  }
