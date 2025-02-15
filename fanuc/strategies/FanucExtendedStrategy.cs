﻿using System.Diagnostics;
using l99.driver.@base;
using l99.driver.fanuc.utils;
using l99.driver.fanuc.veneers;

// ReSharper disable once CheckNamespace
namespace l99.driver.fanuc.strategies;

public class FanucExtendedStrategy : FanucStrategy
{
    private readonly List<dynamic> _focasInvocations = new();

    private readonly IntermediateModelGenerator _intermediateModel;

    private readonly Dictionary<string, dynamic> _propertyBag;
    private readonly Stopwatch _sweepWatch = new();
    private SegmentEnum _currentCollectSegment = SegmentEnum.None;
    private SegmentEnum _currentInitSegment = SegmentEnum.None;
    private int _failedInvocationCountDuringSweep;

    private StrategyStateEnum _strategyState = StrategyStateEnum.Unknown;
    private int _sweepRemaining;

    protected FanucExtendedStrategy(Machine machine) : base(machine)
    {
        _sweepRemaining = SweepMs;
        _propertyBag = new Dictionary<string, dynamic>();
        _intermediateModel = new IntermediateModelGenerator();
    }

    private void CatchFocasPerformance(dynamic focasNativeReturnObject)
    {
        _focasInvocations.Add(new
        {
            focasNativeReturnObject.method,
            focasNativeReturnObject.invocationMs,
            focasNativeReturnObject.rc
        });

        if (focasNativeReturnObject.rc != 0) _failedInvocationCountDuringSweep++;
    }

    private dynamic? GetCurrentPropertyBagKey()
    {
        switch (_currentCollectSegment)
        {
            case SegmentEnum.None:
            case SegmentEnum.Begin:
            case SegmentEnum.Root:
            case SegmentEnum.End:
                return "none";

            case SegmentEnum.Path:
                return Get("current_path");

            case SegmentEnum.Axis:
                return string.Join("/", Get("axis_split"));

            case SegmentEnum.Spindle:
                return string.Join("/", Get("spindle_split"));
        }

        return "none";
    }

    public dynamic? Get(string propertyBagKey)
    {
        if (_propertyBag.ContainsKey(propertyBagKey))
            return _propertyBag[propertyBagKey];
        return null;
    }

    public dynamic? GetKeyed(string propertyBagKey)
    {
        return Get($"{propertyBagKey}+{GetCurrentPropertyBagKey()}");
    }

    private bool Has(string propertyBagKey)
    {
        return _propertyBag.ContainsKey(propertyBagKey);
    }

    public bool HasKeyed(string propertyBagKey)
    {
        return Has($"{propertyBagKey}+{GetCurrentPropertyBagKey()}");
    }

    private async Task<dynamic?> Set(string propertyBagKey, dynamic? value)
    {
        return await SetInternal(propertyBagKey, value, false);
    }

    public async Task<dynamic?> SetKeyed(string propertyBagKey, dynamic? value)
    {
        return await SetInternal($"{propertyBagKey}+{GetCurrentPropertyBagKey()}", value, false);
    }

    public async Task<dynamic?> SetNative(string propertyBagKey, dynamic? value)
    {
        return await SetInternal(propertyBagKey, value, true);
    }

    public async Task<dynamic?> SetNativeNull(string propertyBagKey)
    {
        return await SetInternal(propertyBagKey,
            new {@null = true, success = true, method = "null", invocationMs = 0L, rc = 0});
    }

    public async Task<dynamic?> SetNativeKeyed(string propertyBagKey, dynamic? value)
    {
        return await SetInternal($"{propertyBagKey}+{GetCurrentPropertyBagKey()}", value, true);
    }

    public async Task<dynamic?> SetNativeNullKeyed(string propertyBagKey)
    {
        return await SetInternal($"{propertyBagKey}+{GetCurrentPropertyBagKey()}",
            new {@null = true, success = true, method = "null", invocationMs = 0L, rc = 0});
    }

    private async Task<dynamic?> SetInternal(string propertyBagKey, dynamic? value, bool nativeResponse = true)
    {
        if (_propertyBag.ContainsKey(propertyBagKey))
        {
            _propertyBag[propertyBagKey] = value!;
            if (nativeResponse)
                return await HandleNativeResponsePropertyBagAssignment(propertyBagKey, value);
        }
        else
        {
            _propertyBag.Add(propertyBagKey, value);
            if (nativeResponse)
                return await HandleNativeResponsePropertyBagAssignment(propertyBagKey, value);
        }

        return value;
    }

    private async Task<dynamic?> HandleNativeResponsePropertyBagAssignment(string key, dynamic value)
    {
        CatchFocasPerformance(value);
        return value;
    }

    private async Task<dynamic?> PeelInternal(string veneerKey, dynamic[] nativeInputs, dynamic[] additionalInputs)
    {
        switch (_currentCollectSegment)
        {
            case SegmentEnum.None:
                break;

            case SegmentEnum.Begin:
                return await Machine.PeelVeneerAsync(veneerKey, nativeInputs, additionalInputs);

            case SegmentEnum.Root:
                return await Machine.PeelVeneerAsync(veneerKey, nativeInputs, additionalInputs);

            case SegmentEnum.Path:
                return await Machine.PeelAcrossVeneerAsync(Get("current_path"), veneerKey, nativeInputs,
                    additionalInputs);

            case SegmentEnum.Axis:
                return await Machine.PeelAcrossVeneerAsync(Get("axis_split"), veneerKey, nativeInputs,
                    additionalInputs);

            case SegmentEnum.Spindle:
                return await Machine.PeelAcrossVeneerAsync(Get("spindle_split"), veneerKey, nativeInputs,
                    additionalInputs);

            case SegmentEnum.End:
                return await Machine.PeelVeneerAsync(veneerKey, nativeInputs, additionalInputs);
        }

        return null;
    }

    public async Task<dynamic?> Peel(string veneerKey, dynamic[] nativeInputs, dynamic[] additionalInputs)
    {
        if (nativeInputs.Length == 0)
            return null;
        return await PeelInternal(veneerKey, nativeInputs, additionalInputs);
    }
    
    public async Task Apply(string veneerType, string veneerName, bool isCompound = false, bool isInternal = false)
    {
        try
        {
            Type veneerTypeType = Type.GetType($"l99.driver.fanuc.veneers.{veneerType}")!;
            await Apply(veneerTypeType, veneerName, isCompound, isInternal);
        }
        catch (Exception e)
        {
            Logger.Error($"[{Machine.Id}] Failed to apply veneer {veneerType}");
        } 
    }
    
    public async Task Apply(Type veneerType, string veneerName, bool isCompound = false, bool isInternal = false)
    {
        switch (_currentInitSegment)
        {
            case SegmentEnum.None:
                break;

            case SegmentEnum.Begin:
                break;

            case SegmentEnum.Root:
                Machine.ApplyVeneer(veneerType, veneerName, isCompound, isInternal);
                _intermediateModel.AddRootItem(veneerName, veneerType);
                break;

            case SegmentEnum.Path:
                Machine.ApplyVeneerAcrossSlices(veneerType, veneerName, isCompound, isInternal);
                _intermediateModel.AddPathItem(veneerName, veneerType);
                break;

            case SegmentEnum.Axis:
                Machine.ApplyVeneerAcrossSlices(Get("current_path"), veneerType, veneerName, isCompound, isInternal);
                _intermediateModel.AddAxisItem(veneerName, veneerType);
                break;

            case SegmentEnum.Spindle:
                Machine.ApplyVeneerAcrossSlices(Get("current_path"), veneerType, veneerName, isCompound, isInternal);
                _intermediateModel.AddSpindleItem(veneerName, veneerType);
                break;

            case SegmentEnum.End:
                break;
        }
    }

    public override async Task SweepAsync(int delayMs = -1)
    {
        _sweepRemaining = SweepMs - (int) _sweepWatch.ElapsedMilliseconds;
        if (_sweepRemaining < 0) _sweepRemaining = SweepMs;
        Logger.Trace($"[{Machine.Id}] Sweep delay: {_sweepRemaining}ms");

        await base.SweepAsync(_sweepRemaining);
    }

    public override async Task<dynamic?> InitializeAsync()
    {
        var initMinutes = 0;
        var initStopwatch = new Stopwatch();
        initStopwatch.Start();

        Logger.Info($"[{Machine.Id}] Strategy initializing");

        try
        {
            _currentInitSegment = SegmentEnum.None;

            while (!Machine.VeneersApplied)
            {
                // connect focas
                var connect = await Platform.ConnectAsync();

                // init strategy if able to connect
                if (connect.success)
                {
                    // build intermediate model
                    _intermediateModel.Start(Machine);

                    #region init root veneers

                    _currentInitSegment = SegmentEnum.Root;

                    await Apply(typeof(FocasPerf), "focas_perf", isInternal: true, isCompound: true);
                    await Apply(typeof(Connection), "connection", isInternal: true);
                    await Apply(typeof(GetPath), "paths", isInternal: true);

                    // init root veneers in user strategy
                    await InitRootAsync();

                    #endregion

                    #region init path veneers

                    _currentInitSegment = SegmentEnum.Path;

                    // retrieve controller paths
                    var paths = await Platform.GetPathAsync();

                    var pathNumbers = Enumerable
                        .Range(
                            (int) paths.response.cnc_getpath.path_no,
                            (int) paths.response.cnc_getpath.maxpath_no)
                        .ToList()
                        .ConvertAll(x => (short) x);

                    // following veneers will be applied over each path
                    Machine.SliceVeneer(pathNumbers.Cast<dynamic>());

                    await Apply(typeof(RdAxisname), "axis_names", isInternal: true);
                    await Apply(typeof(RdSpindlename), "spindle_names", isInternal: true);

                    // init path veneers in user strategy
                    await InitPathsAsync();

                    #endregion

                    #region init axis+spindle veneers

                    //_currentInitSegment = SegmentEnum.AXIS;

                    // iterate paths
                    foreach (var currentPath in pathNumbers)
                    {
                        // build intermediate model
                        _intermediateModel.AddPath(currentPath);
                        // set current path
                        var path = await Platform.SetPathAsync(currentPath);
                        // read axes and spindles for current path
                        var axes = await Platform.RdAxisNameAsync();
                        var spindles = await Platform.RdSpdlNameAsync();
                        dynamic axisAndSpindleSlices = new List<dynamic>();

                        // axes - get fields from focas response
                        var fieldsAxes = axes.response
                            .cnc_rdaxisname.axisname.GetType().GetFields();
                        for (var x = 0; x <= axes.response.cnc_rdaxisname.data_num - 1; x++)
                        {
                            // get axis name
                            var axis = fieldsAxes[x]
                                .GetValue(axes.response.cnc_rdaxisname.axisname);

                            // build intermediate model
                            _intermediateModel.AddAxis(currentPath, AxisName(axis));

                            axisAndSpindleSlices.Add(AxisName(axis));
                        }

                        // spindles - get fields from focas response
                        var fieldsSpindles = spindles.response
                            .cnc_rdspdlname.spdlname.GetType().GetFields();
                        for (var x = 0; x <= spindles.response.cnc_rdspdlname.data_num - 1; x++)
                        {
                            // get spindle name
                            var spindle = fieldsSpindles[x]
                                .GetValue(spindles.response.cnc_rdspdlname.spdlname);

                            // build intermediate model
                            _intermediateModel.AddSpindle(currentPath, SpindleName(spindle));

                            axisAndSpindleSlices.Add(SpindleName(spindle));
                        }

                        // following veneers will be applied over axes+spindles
                        Machine.SliceVeneer(
                            currentPath,
                            axisAndSpindleSlices.ToArray());

                        // store current path
                        await Set("current_path", currentPath);

                        // init axis veneers in user strategy
                        _currentInitSegment = SegmentEnum.Axis;
                        await InitAxisAsync();

                        // init spindle veneers in user strategy
                        _currentInitSegment = SegmentEnum.Spindle;
                        await InitSpindleAsync();
                    }

                    #endregion

                    await PostInitAsync();

                    // disconnect focas
                    var disconnect = await Platform.DisconnectAsync();

                    Machine.VeneersApplied = true;

                    _currentInitSegment = SegmentEnum.None;

                    // build intermediate model
                    _intermediateModel.Finish();
                    await Machine.Handler.OnGenerateIntermediateModelAsync(_intermediateModel.Model);
                    await Machine.Transport.OnGenerateIntermediateModelAsync(_intermediateModel.Model);

                    Logger.Info($"[{Machine.Id}] Strategy initialized");
                }
                else
                {
                    if (initMinutes == 0 || initStopwatch.ElapsedMilliseconds > 60000)
                    {
                        Logger.Warn($"[{Machine.Id}] Strategy initialization pending ({initMinutes} min)");
                        initMinutes++;
                        initStopwatch.Restart();
                    }

                    await Task.Delay(SweepMs);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"[{Machine.Id}] Strategy initialization failed");
        }

        initStopwatch.Stop();

        return null;
    }

    protected virtual async Task PostInitAsync()
    {
        await Task.FromResult(0);
    }

    /// <summary>
    ///     Applied Veneers:
    ///     FocasPerf as "focas_perf",
    ///     Connection as "connection",
    ///     GetPath as "paths"
    /// </summary>
    protected virtual async Task InitRootAsync()
    {
        await Task.FromResult(0);
    }

    protected virtual async Task InitUserRootAsync()
    {
        await Task.FromResult(0);
    }

    /// <summary>
    ///     Applied Veneers:
    ///     FocasPerf as "focas_perf",
    ///     Connection as "connection",
    ///     GetPath as "paths",
    ///     RdAxisname as "axis_names",
    ///     RdSpindlename as "spindle_names"
    /// </summary>
    protected virtual async Task InitPathsAsync()
    {
        await Task.FromResult(0);
    }

    protected virtual async Task InitUserPathsAsync()
    {
        await Task.FromResult(0);
    }

    /// <summary>
    ///     Applied Veneers:
    ///     FocasPerf as "focas_perf",
    ///     Connection as "connection",
    ///     GetPath as "paths",
    ///     RdAxisname as "axis_names",
    ///     RdSpindlename as "spindle_names"
    /// </summary>
    protected virtual async Task InitAxisAsync()
    {
        await Task.FromResult(0);
    }

    protected virtual async Task InitSpindleAsync()
    {
        await Task.FromResult(0);
    }

    protected virtual async Task InitUserAxisAndSpindleAsync(short currentPath)
    {
        await Task.FromResult(0);
    }

    protected override async Task<dynamic?> CollectAsync()
    {
        try
        {
            _currentInitSegment = SegmentEnum.None;

            _focasInvocations.Clear();
            _failedInvocationCountDuringSweep = 0;

            _currentCollectSegment = SegmentEnum.Begin;

            await CollectBeginAsync();

            if (Get("connect")!.success)
            {
                if (_strategyState == StrategyStateEnum.Unknown)
                {
                    Logger.Info($"[{Machine.Id}] Strategy started");
                    _strategyState = StrategyStateEnum.Ok;
                }
                else if (_strategyState == StrategyStateEnum.Failed)
                {
                    Logger.Info($"[{Machine.Id}] Strategy recovered");
                    _strategyState = StrategyStateEnum.Ok;
                }

                _currentInitSegment = SegmentEnum.Root;
                _currentCollectSegment = SegmentEnum.Root;

                // reset to first path, issue #92
                await Set("path", await Platform.SetPathAsync(0));
                
                await Peel("paths",
                    new[]
                    {
                        await SetNative("paths",
                            await Platform.GetPathAsync())
                    },
                    new dynamic[]
                    {
                    });

                await InitUserRootAsync();
                await CollectRootAsync();

                _currentInitSegment = SegmentEnum.Path;
                await InitUserPathsAsync();

                _currentInitSegment = SegmentEnum.Axis;

                for (short currentPath = Get("paths")!.response.cnc_getpath.path_no;
                     currentPath <= Get("paths")!.response.cnc_getpath.maxpath_no;
                     currentPath++)
                {
                    _currentCollectSegment = SegmentEnum.Path;

                    await Set("current_path", currentPath);

                    await InitUserAxisAndSpindleAsync(currentPath);

                    await Set("path", await Platform.SetPathAsync(currentPath));
                    var pathMarker = PathMarker(Get("path")!.request.cnc_setpath.path_no);
                    dynamic pathMarkerFull = new[] {pathMarker};
                    Machine.MarkVeneer(currentPath, pathMarkerFull);

                    await Peel("axis_names",
                        new[]
                        {
                            await SetNativeKeyed("axis_names",
                                await Platform.RdAxisNameAsync())
                        },
                        new dynamic[]
                        {
                        });

                    var fieldsAxes = GetKeyed("axis_names")!
                        .response.cnc_rdaxisname.axisname.GetType().GetFields();

                    short axisCount = GetKeyed("axis_names")!
                        .response.cnc_rdaxisname.data_num;

                    var axisNames = new string[axisCount];

                    for (short i = 0; i < axisCount; i++)
                        axisNames[i] = AxisName(fieldsAxes[i]
                            .GetValue(GetKeyed("axis_names")!
                                .response.cnc_rdaxisname.axisname));

                    await Peel("spindle_names",
                        new[]
                        {
                            await SetNativeKeyed("spindle_names",
                                await Platform.RdSpdlNameAsync())
                        },
                        new dynamic[]
                        {
                        });

                    var fieldsSpindles = GetKeyed("spindle_names")!
                        .response.cnc_rdspdlname.spdlname.GetType().GetFields();

                    short spindleCount = GetKeyed("spindle_names")!
                        .response.cnc_rdspdlname.data_num;

                    var spindleNames = new string[spindleCount];

                    for (short i = 0; i < spindleCount; i++)
                        spindleNames[i] = SpindleName(fieldsSpindles[i]
                            .GetValue(GetKeyed("spindle_names")!
                                .response.cnc_rdspdlname.spdlname));

                    await CollectForEachPathAsync(currentPath, axisNames, spindleNames, pathMarkerFull);

                    for (short currentAxis = 1; currentAxis <= axisNames.Length; currentAxis++)
                    {
                        _currentCollectSegment = SegmentEnum.Axis;
                        dynamic axisName = axisNames[currentAxis - 1];
                        //Debug.Print($"PATH:{current_path} AXIS:{axis_name}");
                        var axisMarker = AxisMarker(currentAxis, axisName);
                        dynamic axisMarkerFull = new[] {pathMarker, axisMarker};
                        await Set("axis_split", new[] {currentPath.ToString(), axisName});

                        Machine.MarkVeneer(Get("axis_split"), axisMarkerFull);

                        await CollectForEachAxisAsync(currentPath, currentAxis, axisName, Get("axis_split"),
                            axisMarkerFull);
                    }

                    for (short currentSpindle = 1; currentSpindle <= spindleNames.Length; currentSpindle++)
                    {
                        _currentCollectSegment = SegmentEnum.Spindle;
                        dynamic spindleName = spindleNames[currentSpindle - 1];
                        //Debug.Print($"PATH:{current_path} SPINDLE:{spindle_name}");
                        var spindleMarker = SpindleMarker(currentSpindle, spindleName);
                        dynamic spindleMarkerFull = new[] {pathMarker, spindleMarker};
                        await Set("spindle_split", new[] {currentPath.ToString(), spindleName});

                        Machine.MarkVeneer(Get("spindle_split"), spindleMarkerFull);

                        await CollectForEachSpindleAsync(currentPath, currentSpindle, spindleName, Get("spindle_split"),
                            spindleMarkerFull);
                    }
                }
            }
            else
            {
                if (_strategyState == StrategyStateEnum.Unknown || _strategyState == StrategyStateEnum.Ok)
                {
                    Logger.Warn($"[{Machine.Id}] Strategy failed to connect");
                    _strategyState = StrategyStateEnum.Failed;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"[{Machine.Id}] Strategy sweep failed at segment {_currentCollectSegment}");
            _strategyState = StrategyStateEnum.Failed;
        }
        finally
        {
            _currentInitSegment = SegmentEnum.None;
            _currentCollectSegment = SegmentEnum.End;

            await CollectEndAsync();
        }

        return null;
    }

    /// <summary>
    ///     Available Data:
    ///     Connection => get("connection") (after base is called)
    /// </summary>
    protected virtual async Task CollectBeginAsync()
    {
        _sweepWatch.Restart();

        bool mustConnect = true;
        if (Machine.Configuration.strategy["stay_connected"] && _strategyState == StrategyStateEnum.Ok)
        {
            mustConnect = false;
        }

        if (mustConnect)
        {
            await SetNative("connect",
                await Platform.ConnectAsync());
            await Peel("connection",
                new dynamic[]
                {
                    Get("connect")!
                },
                new dynamic[]
                {
                    "connect"
                });
        }
    }

    /// <summary>
    ///     Available Data:
    ///     Connect => get("connect"),
    ///     GetPath => get("paths")
    /// </summary>
    protected virtual async Task CollectRootAsync()
    {
        await Task.FromResult(0);
    }

    /// <summary>
    ///     Available Data:
    ///     Connect => get("connect"),
    ///     GetPath => get("paths"),
    ///     RdAxisName => get("axis_names"),
    ///     RdSpdlName => get("spindle_names")
    /// </summary>
    protected virtual async Task CollectForEachPathAsync(short currentPath, string[] axis, string[] spindle,
        dynamic pathMarker)
    {
        await Task.FromResult(0);
    }

    /// <summary>
    ///     Available Data:
    ///     Connect => get("connect"),
    ///     GetPath => get("paths"),
    ///     RdAxisName => get("axis_names"),
    ///     RdSpdlName => get("spindle_names")
    /// </summary>
    protected virtual async Task CollectForEachAxisAsync(short currentPath, short currentAxis, string axisName,
        dynamic axisSplit, dynamic axisMarker)
    {
        await Task.FromResult(0);
    }

    /// <summary>
    ///     Available Data:
    ///     Connect => get("connect"),
    ///     GetPath => get("paths"),
    ///     RdAxisName => get("axis_names"),
    ///     RdSpdlName => get("spindle_names")
    /// </summary>
    protected virtual async Task CollectForEachSpindleAsync(short currentPath, short currentSpindle, string spindleName,
        dynamic spindleSplit, dynamic spindleMarker)
    {
        await Task.FromResult(0);
    }

    /// <summary>
    ///     Available Data:
    ///     Connect => get("connect"),
    ///     GetPath => get("paths"),
    ///     RdAxisName => get("axis_names"),
    ///     RdSpdlName => get("spindle_names"),
    ///     Disconnect => get("disconnect") (after base is called)
    /// </summary>
    protected virtual async Task CollectEndAsync()
    {
        bool mustDisconnect = true;
        if (Machine.Configuration.strategy["stay_connected"] && _strategyState != StrategyStateEnum.Failed)
        {
            mustDisconnect = false;
        }

        if (mustDisconnect)
        {
            await SetNative("disconnect",
                await Platform.DisconnectAsync());
            await Peel("connection",
                new dynamic[]
                {
                    Get("disconnect")!
                },
                new dynamic[]
                {
                    "disconnect"
                });
        }
        
        await Machine.PeelVeneerAsync("focas_perf",
            new dynamic[]
            {
            },
            new dynamic[]
            {
                new
                {
                    sweepMs = _sweepWatch.ElapsedMilliseconds,
                    focas_invocations = _focasInvocations
                }
            });

        //TODO: make veneer
        LastSuccess = Get("connect").success;
        IsHealthy = _failedInvocationCountDuringSweep == 0;
    }

    private enum StrategyStateEnum
    {
        Unknown,
        Ok,
        Failed
    }

    private enum SegmentEnum
    {
        None,
        Begin,
        Root,
        Path,
        Axis,
        Spindle,
        End
    }
}