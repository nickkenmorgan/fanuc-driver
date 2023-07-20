using System.Dynamic;
using l99.driver.@base;

// ReSharper disable once CheckNamespace
namespace l99.driver.fanuc.veneers;

public class Macro : Veneer
{
    public Macro(Veneers veneers, string name = "", bool isCompound = false, bool isInternal = false) : base(
        veneers, name, isCompound, isInternal)
    {

    }
    protected override async Task<dynamic> AnyAsync(dynamic[] nativeInputs, dynamic[] additionalInputs)
    {
        if (nativeInputs.All(o => o.success == true))
        {
            var datano = nativeInputs[0].response.cnd_rdmacro.macro.datano;
            var mcr_val =  nativeInputs[0].response.cnd_rdmacro.macro.mcr_val;
            var id = nativeInputs[0].response.id;
         //   var size = nativeInputs[0].response.size;

            dynamic state = new ExpandoObject();
           //state.id = id;
            state.datano = datano;
            state.mcr_val = mcr_val;

            Dictionary<dynamic, dynamic> dict = new Dictionary<dynamic, dynamic>();
            dict.Add(id, state);

            dynamic currentValue = new ExpandoObject();
            // convert state list to dictionary
            currentValue.Macro = dict;

            await OnDataArrivedAsync(nativeInputs, additionalInputs, currentValue);

            if (((object)currentValue).IsDifferentString((object) LastChangedValue))
                await OnDataChangedAsync(nativeInputs, additionalInputs, currentValue);
        }
        else
        {
            await OnHandleErrorAsync(nativeInputs, additionalInputs);
        }
        return new { veneer = this };
    }
}