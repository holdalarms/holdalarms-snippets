//+------------------------------------------------------------------+
//| HoldAlarmsTest.mq5 - script: send one test alert, then stop        |
//+------------------------------------------------------------------+
#property strict
#property script_show_inputs
#include <HoldAlarms.mqh>
input string InpWebhookUrl = "";   // Your HoldAlarms webhook URL
void OnStart()
  {
   if(HoldAlarmsAlert(InpWebhookUrl, "test", "HoldAlarms test script"))
      Alert("HoldAlarms: test alert sent. Check Recent alerts on your dashboard.");
   else
      Alert("HoldAlarms: test alert failed. See the Experts tab for the reason.");
  }
