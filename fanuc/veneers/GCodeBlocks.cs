﻿using l99.driver.@base;
using l99.driver.fanuc.gcode;

// ReSharper disable once CheckNamespace
namespace l99.driver.fanuc.veneers
{
    public class GCodeBlocks : Veneer
    {
        private readonly Blocks _blocks;
        
        public GCodeBlocks(string name = "", bool isCompound = false, bool isInternal = false) : base(name, isCompound, isInternal)
        {
            _blocks = new Blocks();
            
            lastChangedValue = new
            {
                blocks = new List<gcode.Block>()
            };
        }
        
        protected override async Task<dynamic> AnyAsync(dynamic input, params dynamic?[] additionalInputs)
        {
            if (additionalInputs[0]?.success && additionalInputs[1]?.success)
            {
                if (input!= null && input?.success)
                {
                    _blocks.Add2(input?.response.cnc_rdblkcount.prog_bc,
                        additionalInputs[0]?.response.cnc_rdactpt.blk_no,
                        additionalInputs[1]?.response.cnc_rdexecprog.data);
                }
                else
                {
                    _blocks.Add1(additionalInputs[0]?.response.cnc_rdactpt.blk_no,
                        additionalInputs[1]?.response.cnc_rdexecprog.data);
                }

                var currentValue = new
                {
                    blocks = _blocks.ExecutedBlocks
                };
                
                
                //Console.WriteLine(_blocks.ToString(showMissedBlocks: true));
                /*
                if (current_value.blocks.Count() > 0)
                {
                    Console.WriteLine("--- executed ---");
                    foreach (var block in current_value.blocks)
                    {
                        Console.WriteLine(block.ToString());
                    }

                    Console.WriteLine("");
                }
                */

                await OnDataArrivedAsync(input, currentValue);
                
                var lastKeys = ((List<gcode.Block>)lastChangedValue.blocks).Select(x => x.BlockNumber);
                var currentKeys = ((List<gcode.Block>)currentValue.blocks).Select(x => x.BlockNumber);

                if (lastKeys.Except(currentKeys).Count() + currentKeys.Except(lastKeys).Count() > 0)
                {
                    await OnDataChangedAsync(input, currentValue);
                }
            }
            else
            {
                await OnHandleErrorAsync(input!=null ? input : additionalInputs[0]);
            }

            return new { veneer = this };
        }
    }
}