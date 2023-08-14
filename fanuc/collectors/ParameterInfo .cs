using l99.driver.fanuc.strategies;
using System;
using System.Dynamic;
using static System.Runtime.InteropServices.JavaScript.JSType;

// ReSharper disable once CheckNamespace
namespace l99.driver.fanuc.collectors;


public class ParameterInfo : FanucMultiStrategyCollector
{
    public ParameterInfo(FanucMultiStrategy strategy, object configuration) : base(strategy, configuration)
    {
    }

    public override async Task InitPathsAsync()
    {
        await Strategy.Apply(typeof(veneers.ParameterInfo), "ParameterInfo");
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
                short itype = 0;

                //no axis
                if (!macroEntry.ContainsKey("axis") || macroEntry["axis"] == null)
                {
                    switch (type)
                    {
                        case "byte":
                            dynamic parameter = await Strategy.Platform.RdParamByteNoAxisAsync(number);
                            combinedDict.Add(id, parameter);
                            break;
                        case "word":
                            parameter = await Strategy.Platform.RdParamWordNoAxisAsync(number);
                            combinedDict.Add(id, parameter);
                            break;
                        case "double":
                            parameter = await Strategy.Platform.RdParamDoubleWordNoAxisAsync(number);
                            combinedDict.Add(id, parameter);
                            break;
                        case "real":
                            parameter = await Strategy.Platform.RdParamRealNoAxisAsync(number);
                            combinedDict.Add(id, parameter);
                            break;
                        default:
                            parameter = await Strategy.Platform.RdParamRealNoAxisAsync(number);
                            combinedDict.Add(id, parameter);
                            break;
                    }


                }

                //axis
                else
                {
                    var axisNum = macroEntry["axis"];
                    var length = 0;
                    switch (type)
                    {
                        case "byte":
                            length = 4 + 1 + 1;
                            itype = 1; 
                            break;
                        case "word":
                            length = 4 + 2 * 1;
                            itype = 2;
                            break;
                        case "double":
                            length = 6 + 2 * 1;
                            itype = 3;
                            break;
                        case "real":
                            length = 4 + 8 * 1;
                            itype = 4;
                            break;
                        default:
                            Console.WriteLine("Invalid Type");  
                            break;
                    }

                    dynamic parameter = await Strategy.Platform.RdParamAsync(number, short.Parse(axisNum), (short)length, itype);
                    combinedDict.Add(id, parameter);
                }
            }
        }


            await Strategy.Peel("ParameterInfo",
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
