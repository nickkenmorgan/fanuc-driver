using l99.driver.fanuc.strategies;
using System;
using System.Dynamic;
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
            var finalDict = new Dictionary<dynamic, dynamic>();


        foreach (var macroEntry in Configuration)
        {
            bool pathbool = false;

            if (macroEntry.ContainsKey("path"))
            {

                //checks if the value is a string or a list and stores in pathlist
                var pathList = macroEntry["path"] switch
                {
                    List<string> list => list,
                    List<object> objectList => objectList.Select(obj => obj.ToString()).ToList(),
                    string singleValue => new List<string> { singleValue },
                    _ => new List<string>()
                };

                pathbool = pathList.Any(path => path == currentPath.ToString());


                if (pathbool)
                {
                    var id = macroEntry["id"];
                    short num = (short)macroEntry["number"];
                    dynamic macro = await Strategy.Platform.RdMacroAsync(num, 10);
                    double macroVal = macro.response.cnd_rdmacro.macro.mcr_val;
                    var dec_val = macro.response.cnd_rdmacro.macro.dec_val;
                    macroVal = macroVal / (double)Math.Pow(10, dec_val);
                    var nr = new
                    {
                        id = id,
                        Value = macroVal
                    };

                    combinedDict.Add(id + "_macro", macro);
                    finalDict.Add(id, nr);
                }
            }
        }


        await Strategy.Peel("macro",
            new dynamic[]
            {
                combinedDict,
                finalDict
            },
            new dynamic[]
            {
            });

    }

}
