using l99.driver.fanuc.strategies;
using MTConnect.Observations.Samples.Values;
using System.Numerics;
using static System.Runtime.InteropServices.JavaScript.JSType;

// ReSharper disable once CheckNamespace
namespace l99.driver.fanuc.collectors;


public class Pmc : FanucMultiStrategyCollector
{
    public Pmc(FanucMultiStrategy strategy, object configuration) : base(strategy, configuration)
    {
    }

    public override async Task InitPathsAsync()
    {
        await Strategy.Apply(typeof(veneers.Pmc), "pmc");
    }

    public override async Task CollectForEachPathAsync(short currentPath, string[] axis, string[] spindle,
        dynamic pathMarker)
    {

        foreach (var PmcEntry in Configuration)
        {
            if (PmcEntry.ContainsKey("id"))
            {
                var id = PmcEntry["id"];
                var addr = PmcEntry["address"];
                var type = PmcEntry["type"];
                short adr_type = 0;
                short data_type = 0;
                ushort s_number = 0;
                ushort e_number = 0;
                ushort length = 0;
                int IODBPMC_type = 0;

                if (type == "bit")
                {
                    adr_type = f_adr_type(addr[0]);
                    data_type = 0;
                    length = (ushort)(18);
                    s_number = 4933;
                    e_number = 4934;
                }
                else if (type == "byte")
                {
                    adr_type = f_adr_type(addr[0]);
                    data_type = 0;
                    length = (ushort)(18);
                    s_number = ushort.Parse(addr.Substring(1));
                    e_number = (ushort)(s_number + 1);
                }
                else if (type == "word")
                {
                    adr_type = f_adr_type(addr[0]);
                    data_type = 1;
                    length = (ushort)(18);
                    s_number = ushort.Parse(addr.Substring(1));
                    e_number = (ushort)(s_number + 1);
                }
                else if (type == "long")
                {
                    adr_type = f_adr_type(addr[0]);
                    data_type = 2;
                    length = (ushort)(18);
                    s_number = ushort.Parse(addr.Substring(1));
                    e_number = (ushort)(s_number + 1);
                }
                else if (type == "float32")
                {
                    adr_type = f_adr_type(addr[0]);
                    data_type = 4;
                    length = (ushort)(18);
                    s_number = ushort.Parse(addr.Substring(1));
                    e_number = (ushort)(s_number + 1);
                }


                await Strategy.Peel("pmc",
                new[]
                    {
                await Strategy.SetNativeKeyed(id, await Strategy.Platform.RdPmcRngAsync(adr_type, data_type, s_number, e_number, length, IODBPMC_type)),
                    },
                    new dynamic[]
                    {
                    });
            }
        }

    }

    private short f_adr_type(char adr_letter)
    {
        switch(adr_letter)
        {
            case 'G':
                return 0;
            case 'F':
                return 1;
            case 'Y':
                return 2;
            case 'X':
                return 3;
            case 'A':
                return 4;
            case 'R':
                return 5;
            case 'T':
                return 6;
            case 'K':
                return 7;
            case 'C':
                return 8;
            case 'D':
                return 9;
            default:
                return 0;
        }
    }
}
