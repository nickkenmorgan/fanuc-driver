using l99.driver.fanuc.strategies;
using l99.driver.fanuc.veneers;
using MTConnect.Observations.Samples.Values;
using System.Collections.Generic;
using System.Dynamic;
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
        var combinedDict = new Dictionary<dynamic, dynamic>();

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
                int bit = -1;

                if (type == "bit")
                {
                    adr_type = f_adr_type(addr[0]);
                    data_type = 0;
                    length = (ushort)(9);
                    s_number = ushort.Parse(addr.Substring(1, addr.Length - 3));
                    e_number = s_number;
                    bit = addr[addr.Length - 1] - '0';
                }
                else if (type == "byte")
                {
                    adr_type = f_adr_type(addr[0]);
                    data_type = 0;
                    length = (ushort)(9);
                    s_number = ushort.Parse(addr.Substring(1));
                    e_number = (ushort)(s_number);
                    bit = 6;
                }
                else if (type == "word")
                {
                    adr_type = f_adr_type(addr[0]);
                    data_type = 1;
                    length = (ushort)(10);
                    s_number = ushort.Parse(addr.Substring(1));
                    e_number = (ushort)(s_number + 1);
                    IODBPMC_type = 1;
                    bit = 7;
                }

                // is long 4 or 8 bytes?
                else if (type == "long")
                {
                    adr_type = f_adr_type(addr[0]);
                    data_type = 2;
                    length = (ushort)(12);
                    s_number = ushort.Parse(addr.Substring(1));
                    e_number = (ushort)(s_number + 3);
                    IODBPMC_type = 2;
                    bit = 8;
                }
                else if (type == "float32")
                {
                    adr_type = f_adr_type(addr[0]);
                    data_type = 4;
                    length = (ushort)(12);
                    s_number = ushort.Parse(addr.Substring(1));
                    e_number = (ushort)(s_number + 3);
                }
                else if (type == "float64")
                {
                    adr_type = f_adr_type(addr[0]);
                    data_type = 5;
                    length = (ushort)(16);
                    s_number = ushort.Parse(addr.Substring(1));
                    e_number = (ushort)(s_number + 7);
                }


                dynamic pmc =  await Strategy.Platform.RdPmcRngAsync(adr_type, data_type, s_number, e_number, length, IODBPMC_type, bit, id);
                dynamic pmcExpando = new ExpandoObject();

                pmcExpando.data = pmc;

                //Bit
                if (bit <= 5 && bit >= 0)
                {
                    pmcExpando.cdata = (pmc.response.pmc_rdpmcrng.buf.cdata[0] >> bit) &1;
                }

                //Byte
                else if (bit == 6)
                {
                    pmcExpando.cdata = pmc.response.pmc_rdpmcrng.buf.cdata[0];
                }

                //Word
                else if (bit == 7)
                {
                    pmcExpando.idata = pmc.response.pmc_rdpmcrng.buf.idata[0];
                }

                //Long
                else if (bit == 8)
                {
                    pmcExpando.ldata = pmc.response.pmc_rdpmcrng.buf.ldata[0];
                }

                //32 float
                else
                {
                    pmcExpando.cdata = pmc.response.pmc_rdpmcrng.buf.cdata[0];
                }



                combinedDict.Add(pmc.id + "_" + type, pmcExpando);






            }
        }

        // await Strategy.SetNativeKeyed("pmcDict", new Dictionary<dynamic, dynamic>(combinedDict));

        await Strategy.Peel("pmc",
            new dynamic[]
            {
                combinedDict
            },
            new dynamic[]
            {
            });



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
            case 'M':
                return 10;
            case 'N':
                return 11;
            case 'E':
                return 11;
            case 'Z':
                return 12;
            default:
                return 0;
        }
    }
}
