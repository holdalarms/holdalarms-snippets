// Minimal stand-ins for NinjaTrader 8 types, only to syntax/type-check HoldAlarmsNotifier.cs off Windows.
using System;
namespace NinjaTrader.NinjaScript {
  public enum State { SetDefaults, Configure, Active, DataLoaded, Historical, Transition, Realtime, Terminated }
  public enum Calculate { OnBarClose, OnEachTick, OnPriceChange }
  public class Instr { public string FullName = "ES 12-26"; }
  public class Series<T> { public T this[int i] { get { return default(T); } } }
  public abstract class IndicatorBase {
    public State State; public string Name, Description; public Calculate Calculate; public bool IsOverlay;
    public Instr Instrument = new Instr(); public Series<double> Close = new Series<double>(); public Series<DateTime> Time = new Series<DateTime>();
    public void Print(object o) { Console.WriteLine(o); }
    protected virtual void OnStateChange() {} protected virtual void OnBarUpdate() {}
  }
}
namespace NinjaTrader.NinjaScript.Indicators { public abstract class Indicator : NinjaTrader.NinjaScript.IndicatorBase {} }
