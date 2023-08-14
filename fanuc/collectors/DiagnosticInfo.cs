using l99.driver.fanuc.strategies;
using System;
using System.Dynamic;
using static System.Runtime.InteropServices.JavaScript.JSType;

// ReSharper disable once CheckNamespace
namespace l99.driver.fanuc.collectors;


public class DiagnosticInfo : FanucMultiStrategyCollector
{
    public DiagnosticInfo(FanucMultiStrategy strategy, object configuration) : base(strategy, configuration)
    {
    }

    public override async Task InitPathsAsync()
    {
        await Strategy.Apply(typeof(veneers.DiagnosticInfo), "DiagnosticInfo");
    }

    public override async Task CollectForEachPathAsync(short currentPath, string[] axis, string[] spindle,
    dynamic pathMarker)
    {

        var combinedDict = new Dictionary<dynamic, dynamic>();

        foreach (var macroEntry in Configuration)
        {

            if (macroEntry.ContainsKey("id"))
            {
                var id = macroEntry["id"];
                var number = short.Parse(macroEntry["number"]);
                var type = macroEntry["type"];

                //no axis
                if (!macroEntry.ContainsKey("axis") || macroEntry["axis"] == null)
                {
                    switch (type)
                    {
                        case "byte":
                            dynamic parameter = await Strategy.Platform.DiagnossByteFirstAxisAsync(number);
                            combinedDict.Add(id, parameter);
                            break;
                        case "word":
                            parameter = await Strategy.Platform.DiagnossWordFirstAxisAsync(number);
                            combinedDict.Add(id, parameter);
                            break;
                        case "double":
                            parameter = await Strategy.Platform.DiagnossDoubleWordFirstAxisAsync(number);
                            combinedDict.Add(id, parameter);
                            break;
                        case "real":
                            parameter = await Strategy.Platform.DiagnossRealFirstAxisAsync(number);
                            combinedDict.Add(id, parameter);
                            break;
                        default:
                            Console.WriteLine("Incorrect Type");
                            break;
                    }


                }

                //axis
                else
                {
                    var axisNum = macroEntry["axis"];
                    switch (type)
                    {
                        case "byte":
                            dynamic parameter = await Strategy.Platform.DiagnossByteAsync(number, axisNum);
                            combinedDict.Add(id, parameter);
                            break;
                        case "word":
                            parameter = await Strategy.Platform.DiagnossWordAsync(number, axisNum);
                            combinedDict.Add(id, parameter);
                            break;
                        case "double":
                            parameter = await Strategy.Platform.DiagnossDoubleWordAsync(number, axisNum);
                            combinedDict.Add(id, parameter);  
                            break;
                        default:
                            Console.WriteLine("Incorrect Type");
                            break;
                    }

                }
            }
        }

          
        await Strategy.Peel("DiagnosticInfo",
        new dynamic[]
        {
                combinedDict
            //finalDict
        },
        new dynamic[]
        {
        });

    }

}
