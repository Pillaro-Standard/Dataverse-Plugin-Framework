using Newtonsoft.Json;
using System;

namespace Pillaro.Dataverse.PluginFramework.Logging.Models;

public class LogDetail
{
    public string Name { get; }
    public string Detail { get; }
    public LogDetail(string name, string detail)
    {
        Name = name;
        Detail = detail;
    }
    public LogDetail(string name, object detail)
    {
        Name = name;
        try
        {
            Detail = JsonConvert.SerializeObject(detail);
        }
        catch (Exception ex)
        {
            Detail = JsonConvert.SerializeObject(ex.ToString());
        }
    }
}