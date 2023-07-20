using l99.driver.fanuc.strategies;
using static System.Runtime.InteropServices.JavaScript.JSType;

// ReSharper disable once CheckNamespace
namespace l99.driver.fanuc.collectors;


public class Macro : FanucMultiStrategyCollector
{
    public Macro(FanucMultiStrategy strategy, object configuration) : base(strategy, configuration)
    {
    }

    public override async Task InitPathsAsync()
    {
        await Strategy.Apply(typeof(veneers.Macro), "macro");
    }

    public override async Task CollectForEachPathAsync(short currentPath, string[] axis, string[] spindle,
        dynamic pathMarker)
    {
 
        foreach (var macroEntry in Configuration)
        {
            if (macroEntry.ContainsKey("id"))
            {
                var id = macroEntry["id"];
                short num = (short)macroEntry["number"];

                await Strategy.Peel("macro",
                    new[]
                    {
                    await Strategy.SetNativeKeyed(id, await Strategy.Platform.RdMacroAsync(id, num, 10)),
                    },
                    new dynamic[]
                    {
                    });
            }
        }


    }
}
