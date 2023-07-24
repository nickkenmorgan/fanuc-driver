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

        bool success = true;
        foreach (var macrolist in nativeInputs[0])
        {
            if (!macrolist.Value.success)
            {
                success = false;
                break;
            }

        }
        if (success)
        {
            dynamic currentValue = nativeInputs[0];
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