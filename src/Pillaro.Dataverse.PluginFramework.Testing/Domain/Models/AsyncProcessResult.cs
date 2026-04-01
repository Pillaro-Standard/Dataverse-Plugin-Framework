namespace Pillaro.Dataverse.PluginFramework.Testing.Domain.Models;

public class AsyncProcessResult
{
    public Guid AsyncOperationId { get; set; }
    public int StateCode { get; set; }
    public int StatusCode { get; set; }
    public string? Message { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime CompletedOn { get; set; }
}