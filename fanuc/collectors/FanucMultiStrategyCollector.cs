using l99.driver.fanuc.strategies;
using System.Linq;
// ReSharper disable once CheckNamespace
namespace l99.driver.fanuc.collectors;

public class FanucMultiStrategyCollector
{
    protected readonly ILogger Logger;
    protected readonly FanucMultiStrategy Strategy;
    protected readonly dynamic Configuration;
    public bool Enabled { get; private set; }

    protected FanucMultiStrategyCollector(FanucMultiStrategy strategy, dynamic configuration)
    {
        Logger = LogManager.GetLogger(GetType().FullName);

        Strategy = strategy;
        Configuration = configuration;

  
        if (Configuration == null) Configuration = new Dictionary<object, object>();
        /*
        //check if we are using the Macro class, if we are convert from list to Dictionary
        string str = Logger.Name.Substring(Logger.Name.Length - 5);
        if (str == "Macro")
        {
            var temp = new List<object>(Configuration);
            Configuration = new Dictionary<object, object>();
            for (var i = 0; i < temp.Count; i++)
            {
                Configuration.Add(i, temp[i]);
            }
        }

        */
        //check if we are using the Macro class, if we are convert from list to Dictionary
        string str = Logger.Name.Substring(Logger.Name.Length - 5);
        if (str == "Macro")
        {
            if (!Configuration[Configuration.Count - 1].ContainsKey("enabled")) {
                Dictionary<object, object> keyValue = new Dictionary<object, object>
                {
                    { "enabled", true }
                };

                Configuration.Add(keyValue);
            }
           
            Enabled = Configuration[Configuration.Count - 1]["enabled"];
        }

        else
        {
            if (!Configuration.ContainsKey("enabled")) Configuration.Add("enabled", true);
            {

            }
            Enabled = Configuration["enabled"];
        }
        }

    public virtual async Task InitRootAsync()
    {
        await Task.FromResult(0);
    }

    public virtual async Task InitPathsAsync()
    {
        await Task.FromResult(0);
    }

    public virtual async Task InitAxisAsync()
    {
        await Task.FromResult(0);
    }

    public virtual async Task InitSpindleAsync()
    {
        await Task.FromResult(0);
    }

    public virtual async Task PostInitAsync(Dictionary<string, List<string>> structure)
    {
        await Task.FromResult(0);
    }

    public virtual async Task CollectRootAsync()
    {
        await Task.FromResult(0);
    }

    public virtual async Task CollectForEachPathAsync(short currentPath, string[] axis, string[] spindle,
        dynamic pathMarker)
    {
        await Task.FromResult(0);
    }

    public virtual async Task CollectForEachAxisAsync(short currentPath, short currentAxis, string axisName,
        dynamic axisSplit, dynamic axisMarker)
    {
        await Task.FromResult(0);
    }

    public virtual async Task CollectForEachSpindleAsync(short currentPath, short currentSpindle, string spindleName,
        dynamic spindleSplit, dynamic spindleMarker)
    {
        await Task.FromResult(0);
    }
}