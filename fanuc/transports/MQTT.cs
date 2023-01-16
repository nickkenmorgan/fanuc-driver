﻿using l99.driver.@base;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using Scriban;

// ReSharper disable once CheckNamespace
namespace l99.driver.fanuc.transports;

// ReSharper disable once InconsistentNaming
// ReSharper disable once UnusedType.Global
public class MQTT : Transport
{
    private MqttClientOptions _options = null!;
    private IMqttClient _client = null!;

    private string _key = string.Empty;
    private int _connectionFailCount;
    private bool _connectionSkipped;
    
    private Template _topicTemplate = null!;
    
    public MQTT(Machine machine, object cfg) : base(machine, cfg)
    {
    }

    public override async Task<dynamic?> CreateAsync()
    {
        //TODO: validate config
        _topicTemplate = Template.Parse(Machine.Configuration.transport["topic"]);
        _key = $"{Machine.Configuration.transport["net"]["type"]}://{Machine.Configuration.transport["net"]["ip"]}:{Machine.Configuration.transport["net"]["port"]}/{Machine.Id}";
        
        var factory = new MqttFactory();
        MqttClientOptionsBuilder builder;

        switch (Machine.Configuration.transport["net"]["type"])
        {
            case "ws":
                builder = new MqttClientOptionsBuilder()
                    .WithWebSocketServer($"{Machine.Configuration.transport["net"]["ip"]}:{Machine.Configuration.transport["net"]["port"]}");
                break;
            default:
                builder = new MqttClientOptionsBuilder()
                    .WithTcpServer(Machine.Configuration.transport["net"]["ip"], Machine.Configuration.transport["net"]["port"]);
                break;
        }
        
        if (!Machine.Configuration.transport["anonymous"])
        {
            byte[]? passwordBuffer = null;

            if (Machine.Configuration.transport["password"] != null)
                passwordBuffer = Encoding.UTF8.GetBytes(Machine.Configuration.transport["password"]);

            builder = builder.WithCredentials(Machine.Configuration.transport["user"], passwordBuffer);
        }

        _options = builder.Build();
        _client = factory.CreateMqttClient();
        
        await ConnectAsync();
        return null;
    }
    
    public override async Task SendAsync(params dynamic[] parameters)
    {
        var @event = parameters[0];
        var veneer = parameters[1];
        var data = parameters[2];

        string topic;
        string payload;
        bool retained = true;
        
        switch (@event)
        {
            case "DATA_ARRIVE":
                topic = await _topicTemplate.RenderAsync(new { machine = Machine, veneer}, member => member.Name);
                payload = JObject.FromObject(data).ToString(Formatting.None);
                break;
            
            case "SWEEP_END":
                topic = $"fanuc/{Machine.Id}/sweep";
                payload = JObject.FromObject(data).ToString(Formatting.None);

                await ConnectAsync();
                
                break;
            
            case "INT_MODEL":
                topic = $"fanuc/{Machine.Id}/$model";
                payload = data;
                break;
            
            default:
                return;
        }
        
        if (_client.IsConnected)
        {
            Logger.Trace($"{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()} PUB {payload.Length}b => {topic}\n{payload}");
            
            var msg = new MqttApplicationMessageBuilder()
                .WithRetainFlag(retained)
                .WithTopic(topic)
                .WithPayload(payload)
                .Build();
            
            await _client.PublishAsync(msg, CancellationToken.None);
        }
    }
    
    public override async Task ConnectAsync()
    {
        if (Machine.Configuration.machine.enabled)
        {
            if (!_client.IsConnected)
            {
                if (_connectionFailCount == 0)
                {
                    Logger.Info($"[{Machine.Id}] Connecting broker '{_key}': {_options.ChannelOptions}");
                }
                else
                {
                    Logger.Debug($"[{Machine.Id}] Connecting broker '{_key}': {_options.ChannelOptions}");
                }
                
                try
                {
                    await _client.ConnectAsync(_options, CancellationToken.None);
                    //_client.UseApplicationMessageReceivedHandler(async (e) => { await handleIncomingMessage(e); });
                    Logger.Info($"[{Machine.Id}] Connected broker '{_key}': {_options.ChannelOptions}");
                    _connectionFailCount = 0;
                }
                catch (MqttCommunicationTimedOutException)
                {
                    if (_connectionFailCount == 0)
                    {
                        Logger.Warn($"[{Machine.Id}] Broker connection timeout '{_key}': {_options.ChannelOptions}");
                    }
                    else
                    {
                        Logger.Debug($"[{Machine.Id}] Broker connection timeout '{_key}': {_options.ChannelOptions}");
                    }

                    _connectionFailCount++;
                }
                catch (MqttCommunicationException)
                {
                    if (_connectionFailCount == 0)
                    {
                        Logger.Warn($"[{Machine.Id}] Broker connection failed '{_key}': {_options.ChannelOptions}");
                    }
                    else
                    {
                        Logger.Debug($"[{Machine.Id}] Broker connection failed '{_key}': {_options.ChannelOptions}");
                    }
                    
                    _connectionFailCount++;
                }
            }
        }
        else
        {
            if (!_connectionSkipped)
            {
                Logger.Info($"[{Machine.Id}] Skipping broker connection '{_key}': {_options.ChannelOptions}");
                _connectionSkipped = true;
            }
        }
    }
    
    public override async Task OnGenerateIntermediateModelAsync(dynamic model)
    {
        await SendAsync("INT_MODEL", null, model.model);
    }
}