using System;
using System.Dynamic;
using l99.driver.@base;
using YamlDotNet.Core.Tokens;

// ReSharper disable once CheckNamespace
namespace l99.driver.fanuc.veneers;

public class DiagnosticInfo : Veneer
{
    public DiagnosticInfo(Veneers veneers, string name = "", bool isCompound = false, bool isInternal = false) : base(
        veneers, name, isCompound, isInternal)
    {

    }
    protected override async Task<dynamic> AnyAsync(dynamic[] nativeInputs, dynamic[] additionalInputs)
    {

        bool success = true;
        foreach (var parameterList in nativeInputs[0])
        {
            if (!parameterList.Value.success)
            {
                success = false;
                break;
            }

        }
        if (success)
        {

            dynamic currentValue = nativeInputs[0];
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