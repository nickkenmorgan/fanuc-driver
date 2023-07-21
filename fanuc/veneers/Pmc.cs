using System.Dynamic;
using l99.driver.@base;

// ReSharper disable once CheckNamespace
namespace l99.driver.fanuc.veneers;

public class Pmc : Veneer
{
    public Pmc(Veneers veneers, string name = "", bool isCompound = false, bool isInternal = false) : base(
        veneers, name, isCompound, isInternal)
    {

    }
    protected override async Task<dynamic> AnyAsync(dynamic[] nativeInputs, dynamic[] additionalInputs)
    {
        if (nativeInputs.All(o => o.success == true))
        {
            

           var buff = nativeInputs[0];
            var bit = nativeInputs[0].request.pmc_rdpmcrng.bit;
            dynamic currentValue = new ExpandoObject();
            currentValue.id = nativeInputs[0].id;
            //LastChangedValue = nativeInputs.Length != 1 ? nativeInputs[0] : JObject.FromObject(new {lastchanged = false});


            //looking for bits
            if (bit <= 5 && bit >= 0)
            {
                currentValue.type = "Bit";
                currentValue.cdata = nativeInputs[0].response.pmc_rdpmcrng.buf.cdata[bit];
            }
            else if (bit == 6)
            {
                currentValue.type = "Byte";
                currentValue.cdata = nativeInputs[0].response.pmc_rdpmcrng.buf.cdata;
            }
            else if (bit == 7)
            {
                currentValue.type = "Word";
                currentValue.cdata = nativeInputs[0].response.pmc_rdpmcrng.buf.idata;
            }
            else if (bit == 8)
            {
                currentValue.type = "Long";
                currentValue.cdata = nativeInputs[0].response.pmc_rdpmcrng.buf.ldata;
            }
            else
            {
                currentValue.type = "32 Float";
                currentValue.cdata = nativeInputs[0].response.pmc_rdpmcrng.buf.cdata;
            }
            //   var buff = nativeInputs[0];

            // convert state list to dictionary
            currentValue.Buffer = buff;

            await OnDataArrivedAsync(nativeInputs, additionalInputs, currentValue);

            if (((object)currentValue).IsDifferentString((object)LastChangedValue))
                await OnDataChangedAsync(nativeInputs, additionalInputs, currentValue);
        }
        else
        {
            await OnHandleErrorAsync(nativeInputs, additionalInputs);
        }
        return new { veneer = this };
    }
}