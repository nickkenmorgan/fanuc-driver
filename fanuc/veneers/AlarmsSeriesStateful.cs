﻿using System.Dynamic;
using System.Linq;
using l99.driver.@base;

// ReSharper disable once CheckNamespace
namespace l99.driver.fanuc.veneers;

public class AlarmsSeriesStateful : AlarmsSeries
{
    public AlarmsSeriesStateful(Veneers veneers, string name = "", bool isCompound = false, bool isInternal = false) 
        : base(veneers, name, isCompound, isInternal)
    {
    }

    protected override async Task<dynamic> AnyAsync(dynamic[] nativeInputs, dynamic[] additionalInputs)
    {
        /*
            nativeInputs
                0: current alarms
                1: previous alarms
            
            additionalInputs
                0: currentPath
                1: axis list
                2: focas support observation
        */
        if (nativeInputs[0].success == true)
        {
            var path = additionalInputs[0];
            var axis = additionalInputs[1];
            var obsFocasSupport = additionalInputs[2];
            var currentInput = nativeInputs[0];
            
            // alarm count and object from NC
            AlarmsWrapper currentAlarmWrapper = GetAlarmsWrapperFromInput(currentInput);
            // alarm list from NC
            List<dynamic> currentAlarmList = GetAlarmListFromAlarms(currentAlarmWrapper, path, axis, obsFocasSupport);
            // alarm states from previous sweep
            Dictionary<dynamic, dynamic> previousStatesDict = LastChangedValue == null ? new Dictionary<dynamic, dynamic>() : LastArrivedValue.alarms;
            // alarm states dict for this sweep
            Dictionary<dynamic, dynamic> currentStatesDict = new Dictionary<dynamic, dynamic>();
            
            // update alarm states
            foreach (var state in previousStatesDict.Values)
            {
                dynamic newState = new ExpandoObject();

                newState.id = state.id;
                newState.time_triggered = state.time_triggered;
                newState.time_cleared = state.time_cleared;
                newState.time_elapsed = state.time_elapsed;
                newState.is_triggered = state.is_triggered;
                newState.trigger_count = state.trigger_count;
                // base alarm
                newState.path = state.path;
                newState.axis_code = state.axis_code;
                newState.axis = state.axis;
                newState.number = state.number;
                newState.message = state.message;
                newState.type_code = state.type_code;
                newState.type = state.type;


                // increment elapsed time
                if (state.is_triggered == true)
                {
                    newState.time_elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - state.time_triggered;
                    
                    // alarm was removed from NC
                    if (!currentAlarmList.Exists(alm => state.id == alm.id))
                    {
                        newState.time_cleared = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        newState.is_triggered = false;
                    }
                }
                
                currentStatesDict.Add(newState.id, newState);
            }
            
            // iterate NC alarms
            foreach (var alarm in currentAlarmList)
            {
                var state = currentStatesDict.ContainsKey(alarm.id) ? currentStatesDict[alarm.id] : null;
                
                // new trigger
                if (state == null)
                {
                    dynamic newState = new ExpandoObject();
                    
                    newState.id = alarm.id;
                    newState.time_triggered = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    newState.time_cleared = 0;
                    newState.time_elapsed = 0;
                    newState.is_triggered = true;
                    newState.trigger_count = 1;
                    // base alarm
                    newState.path = alarm.path;
                    newState.axis_code = alarm.axis_code;
                    newState.axis = alarm.axis;
                    newState.number = alarm.number;
                    newState.message = alarm.message;
                    newState.type_code = alarm.type_code;
                    newState.type = alarm.type;
                    
                    currentStatesDict.Add(newState.id, newState);
                }
                // re-trigger
                else if (state.is_triggered == false)
                {
                    state.time_triggered = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    state.time_cleared = 0;
                    state.time_elapsed = 0;
                    state.is_triggered = true;
                    state.trigger_count = state.trigger_count + 1;
                }
            }

            dynamic currentValue = new ExpandoObject();
            // convert state list to dictionary
             currentValue.alarms = currentStatesDict;

            await OnDataArrivedAsync(nativeInputs, additionalInputs, currentValue);

            if (((object)currentValue).IsDifferentString((object) LastChangedValue))
                await OnDataChangedAsync(nativeInputs, additionalInputs, currentValue);
        }
        else
        {
            await OnHandleErrorAsync(nativeInputs, additionalInputs);
        }

        return new {veneer = this};
    }
}