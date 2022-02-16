﻿using System.Collections.Generic;
using System.Threading.Tasks;
using l99.driver.fanuc.strategies;

namespace l99.driver.fanuc.collectors
{
    public class MachineInfo : FanucMultiStrategyCollector
    {
        public MachineInfo(FanucMultiStrategy strategy) : base(strategy)
        {
            
        }

        public override async Task InitPathsAsync()
        {
            await strategy.Apply(typeof(fanuc.veneers.SysInfo), "machine");
        }
        
        public override async Task CollectForEachPathAsync(short current_path, string[] axis, string[] spindle, dynamic path_marker)
        {
            if (strategy.HasKeyed($"{this.GetType().Name}"))
                return;

            await strategy.SetKeyed($"{this.GetType().Name}", true);
            
            await strategy.SetNativeKeyed($"machine",
                await strategy.Platform.SysInfoAsync());
            
            var obs_machine = await strategy.Peel($"machine", 
                strategy.GetKeyed($"machine"));

            strategy.SetKeyed($"obs+focas_support", 
                obs_machine.veneer.LastArrivedValue.focas_support);
        }
    }
}