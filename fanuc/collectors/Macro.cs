using l99.driver.fanuc.strategies;
using System;
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

            var combinedDict = new Dictionary<dynamic, dynamic>();
             
        
            foreach (var macroEntry in Configuration)
            {
            bool exlusionbool = true;
            var exclusionsList = new Dictionary<dynamic, dynamic>();

            if (macroEntry.ContainsKey("id"))
                    {
                        var id = macroEntry["id"];
                        short num = (short)macroEntry["number"];
                        dynamic macro = await Strategy.Platform.RdMacroAsync(id, num, 10);

                        if (macroEntry.ContainsKey("exclusions"))
                        {
                            exclusionsList = macroEntry["exclusions"];
                            if (exclusionsList != null) {
                                foreach (var exclusion in exclusionsList)
                                {
                                    if (exclusion.Key == currentPath.ToString())
                                    {
                                        exlusionbool = false;
                                        break;

                                    }
                                }
                            }
                        }

                        if (exlusionbool) {
                            combinedDict.Add(id, macro);
                         }

                         


                    }
            }


        await Strategy.Peel("macro",
            new dynamic[]
            {
                combinedDict
            },
            new dynamic[]
            {
            });

    }

}
