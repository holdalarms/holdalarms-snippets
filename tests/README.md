# Tests

- `ninjatrader-client`: builds `HoldAlarmsClient.cs` for .NET Framework 4.8 and .NET 8, then runs it (.NET 8)
  against `mock.js`, a local HTTP server: 200, text body, 503 then 200 (one repeat), 401 (no repeat), a hanging
  server (gives up after the timeout), and a non-https URL (refused).

  ```
  node mock.js 19951 &
  dotnet run -f net8.0 -- http://127.0.0.1:19951
  ```

- `ninjatrader-stub`: type-checks `HoldAlarmsNotifier.cs` against minimal stand-ins for NinjaTrader types
  (`dotnet build`). It is not a NinjaTrader compile; compile in the NinjaScript Editor before live use.
