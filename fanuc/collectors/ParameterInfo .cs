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

    public override async Task InitAxisAsync()
    {
        await Strategy.Apply(typeof(veneers.ParameterInfo), "ParameterInfo");
    }

    public override async Task CollectForEachPathAsync(short currentPath, string[] axis, string[] spindle,
    dynamic pathMarker)
    {

        var combinedDict = new Dictionary<dynamic, dynamic>();
        var finalDict = new Dictionary<dynamic, dynamic>();

        foreach (var macroEntry in Configuration)
        {

            if (macroEntry.ContainsKey("id"))
            {
                var id = macroEntry["id"];
                var number = short.Parse(macroEntry["number"]);
                var type = macroEntry["type"];
                short itype = 0;
                int bit = 0;
                dynamic parameterExpando = new ExpandoObject();
                //no axis
                if (!macroEntry.ContainsKey("axis") || macroEntry["axis"] == null)
                {
                    switch (type)
                    {
                        case "bit":
                            bit = int.Parse(macroEntry["bit"]);
                            dynamic parameter = await Strategy.Platform.RdParamByteNoAxisAsync(number);
                            parameterExpando.data = cleanup_output(parameter, type, bit);
                            combinedDict.Add(id, parameter);
                            finalDict.Add(id, parameterExpando.data);
                            break;
                        case "byte":
                            parameter = await Strategy.Platform.RdParamByteNoAxisAsync(number);
                            parameterExpando.data = cleanup_output(parameter, type);
                            combinedDict.Add(id, parameter);
                            finalDict.Add(id, parameterExpando.data);
                            break;
                        case "word":
                            parameter = await Strategy.Platform.RdParamWordNoAxisAsync(number);
                            parameterExpando.data = cleanup_output(parameter, type);
                            combinedDict.Add(id, parameter);
                            finalDict.Add(id, parameterExpando.data);
                            break;
                        case "double":
                            parameter = await Strategy.Platform.RdParamDoubleWordNoAxisAsync(number);
                            parameterExpando.data = cleanup_output(parameter, type);
                            combinedDict.Add(id, parameter);
                            finalDict.Add(id, parameterExpando.data);
                            break;
                        case "real":
                            parameter = await Strategy.Platform.RdParamRealNoAxisAsync(number);
                            parameterExpando.data = cleanup_output(parameter, type);
                            combinedDict.Add(id, parameter);
                            finalDict.Add(id, parameterExpando.data);
                            break;
                        default:
                            parameter = await Strategy.Platform.RdParamRealNoAxisAsync(number);
                            parameterExpando.data = cleanup_output(parameter, type);
                            combinedDict.Add(id, parameter);
                            finalDict.Add(id, parameterExpando.data);
                            break;
                    }

                    


                }
            }
        }


            await Strategy.Peel("ParameterInfo",
            new dynamic[]
            {
                combinedDict,
                finalDict
            },
            new dynamic[]
            {
            });

    }

    public override async Task CollectForEachAxisAsync(short currentPath, short currentAxis, string axisName,
    dynamic axisSplit, dynamic axisMarker)
    {
        var combinedDict = new Dictionary<dynamic, dynamic>();
        var finalDict = new Dictionary<dynamic, dynamic>();

        foreach (var macroEntry in Configuration)
        {
            if (macroEntry.ContainsKey("id"))
            {
                var id = macroEntry["id"];
                var number = short.Parse(macroEntry["number"]);
                var type = macroEntry["type"];
                short itype = 0;
                int bit = 0;
                bool isAxis = false;
                if (macroEntry.ContainsKey("axis"))
                {
                    if (macroEntry["axis"] != null)
                    {
                        foreach (var axisNum in macroEntry["axis"])
                        {
                            if (axisNum.ToUpper() == axisName)
                            {
                                isAxis = true;
                            }
                        }
             
                    if (isAxis)
                    {
                        var length = 0;
                        switch (type)
                        {

                            case "bit":
                                number = short.Parse(macroEntry["number"].Substring(0, macroEntry["number"].Length - 2));
                                length = 4 + 1 + 1;
                                itype = 1;
                                bit = macroEntry["number"][macroEntry["number"].Length - 1] - '0';
                                break;
                            case "byte":
                                length = 4 + 1 * 1;
                                itype = 1;
                                break;
                            case "word":
                                length = 4 + 2 * 1;
                                itype = 1;
                                break;
                            case "double":
                                length = 6 + 2 * 1;
                                itype = 1;
                                break;
                            case "real":
                                length = 4 + 8 * 1;
                                itype = 1;
                                break;
                            default:
                                Console.WriteLine("Invalid Type");
                                break;
                        }
                        dynamic parameter = await Strategy.Platform.RdParamAsync(number, currentAxis, (short)length, itype);
                        dynamic parameterExpando = new ExpandoObject();
                        parameterExpando.data = cleanup_output(parameter, type, bit);



                            combinedDict.Add(id, parameter);
                            finalDict.Add(id, parameterExpando.data);
                    }
                }
                }
            }
        }
        await Strategy.Peel("ParameterInfo",
            new dynamic[]
            {
                combinedDict,
                finalDict
            },
            new dynamic[]
            {
            });
    }


    private dynamic cleanup_output(dynamic parameter, string type, int bit = 0)
    {
        dynamic output = null;
        var paramNum = parameter.response.cnc_rdparam.param.GetType().Name;
        dynamic param = parameter.response.cnc_rdparam.param;

        //check if we are using data, rdata, or rdatas
        if (paramNum[paramNum.Length - 1] == '2')
        {
            param = param.rdata;
            output = rdata_lookup(param, type, bit);
        }
        else if(paramNum[paramNum.Length - 1] == '4')
        {
            param = param.rdatas;
            output = rdata_lookup(param, type, bit);
        }
        else
        {
            param = param.data;
            output = data_lookup(param, type, bit);
        }


        return output;
    }

    private dynamic data_lookup(dynamic param, string type, int bit)
    {
        dynamic output = null;
        switch (type)
        {
            case "bit":
                output = (param.cdata >> bit) & 1;
                break;
            case "byte":
                output = param.cdata;
                break;
            case "word":
                output = param.cdata;
                break;
            case "double":
                output = param.cdata;
                break;
            case "real":
                output = param.cdata;
                break;
            default:
                Console.WriteLine("Invalid Type");
                break;
        }
        return output;
    }

    private dynamic rdata_lookup(dynamic param, string type, int bit)
    {
        dynamic output = null;
        switch (type)
        {
            case "bit":
                output = (param.prm_val >> bit) & 1;
                break;
            case "byte":
                output = param.prm_val;
                output = output / (double)Math.Pow(10, param.dec_val);
                break;
            case "word":
                output = param.prm_val;
                output = output / (double)Math.Pow(10, param.dec_val);
                break;
            case "double":
                output = param.prm_val;
                output = output / (double)Math.Pow(10, param.dec_val);
                break;
            case "real":
                output = param.prm_val;
                output = output / (double)Math.Pow(10, param.dec_val);
                break;
            default:
                Console.WriteLine("Invalid Type");
                break;
        }
        return output;
    }
}



