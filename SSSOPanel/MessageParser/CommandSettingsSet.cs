using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PhotinoNET;

namespace SpinShareClient.MessageParser;

/// <summary>
/// A command that sets a setting given a <c>key</c> and <c>value</c>
/// </summary>
public class CommandSettingsSet : ICommand
{
    private SettingsManager? _settingsManager;
    
    public async Task Execute(PhotinoWindow? sender, object? data)
    {
        if (data == null) return;
        _settingsManager = SettingsManager.GetInstance();

        JArray dataArray = (JArray)data;
        foreach (var jToken in dataArray)
        {
            JObject dataItem = (JObject)jToken;
            
            string? key = dataItem.GetValue("key")?.ToObject<string>();
            object? value = dataItem.GetValue("value")?.ToObject<object>();
            
            if (key != null && value != null)
            {
                Console.WriteLine("Writing Setting: {0}", key);
            
                _settingsManager.Set(key, value);
            }
        }

        Message response = new() {
            Command = "settings-set-response",
            Data = _settingsManager.GetFull()
        };
        
        await Task.Yield();

        MessageHandler.SendResponse(sender, response);
    }
}