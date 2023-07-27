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

            var combinedConfig = new Dictionary<dynamic, dynamic>();
            var exclusionsList = new Dictionary<dynamic, dynamic>();

            foreach (var macroEntry in Configuration)
            {
                    if (macroEntry.ContainsKey("id"))
                    {
                        var id = macroEntry["id"];
                        short num = (short)macroEntry["number"];
                        dynamic macro = await Strategy.Platform.RdMacroAsync(id, num, 10);

                        combinedConfig.Add(id, macro);

                        if (macroEntry.ContainsKey("exclusions"))
                        {
                            exclusionsList = macroEntry["exclusions"];
                        }
                    }
            }

           await Strategy.SetKeyed("macroConfig", new Dictionary<dynamic, dynamic>(combinedConfig));

            if (exclusionsList != null)
            {
                await Strategy.SetKeyed("exclusionsList", exclusionsList);
            }


        }

    public override async Task CollectForEachAxisAsync(short currentPath, short currentAxis, string axisName,
        dynamic axisSplit, dynamic axisMarker)
    {


        var combinedDict = new Dictionary<dynamic, dynamic>();
        var exclusionsList = new Dictionary<dynamic, dynamic>();

        exclusionsList = Strategy.Get($"exclusionsList+{currentPath}");
        combinedDict = Strategy.Get($"macroConfig+{currentPath}");

        var path = "-1";
        var axis = new List<dynamic>();
        bool isAxis = true;

        //are there exclusions in the config
        if (exclusionsList != null)
        {
            foreach (var exclusion in exclusionsList)
            {
                path = exclusion.Key;
                if (path == currentPath.ToString())
                {
                    if (exclusion.Value != null)
                    {
                        axis = new List<dynamic>(exclusion.Value);
                    }
                    else
                    {
                        axis.Add(
                            "none"
                        );
                    }
                    break;
                }
            }

            foreach (var axisLetter in axis)
            {
                if (axisLetter.ToString() == axisName || axisLetter.ToString() == "%")
                {
                    isAxis = false;
                    break;
                }
            }
        }

        if (isAxis)
        {
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
}
