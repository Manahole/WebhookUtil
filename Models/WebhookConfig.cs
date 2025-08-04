using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json.Serialization;

namespace WebhookUtil.Models
{
    public class WebhookConfig
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string ListenPath { get; set; } = string.Empty;
        public string TargetWebhookUrl { get; set; } = string.Empty;
        public int BufferTimeSeconds { get; set; } = 30;
        public int MaxBufferSize { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public string Cron { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [JsonIgnore]
        public int HookIn { get; set; }
        [JsonIgnore]
        public int HookOut { get; set; }
        [JsonIgnore]
        public int HookPending { get; set; }
        [JsonIgnore]
        public string Strategy { 
            get
            {
                if(BufferTimeSeconds > 0)
                {
                    return $"Buffer {BufferTimeSeconds}s {(MaxBufferSize > 0 ? $"Max {MaxBufferSize} items" : string.Empty)}";
                }
                return $"Cron : {Cron}";
            }
        }
    }

    public class WebhookMessage
    {
        public string WebhookId { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new();
        public DateTime ReceivedAt { get; set; } = DateTime.Now;
    }

    public class AggregatedWebhookPayload
    {
        public string WebhookId { get; set; } = string.Empty;
        public string WebhookName { get; set; } = string.Empty;
        public int MessageCount { get; set; }
        public List<WebhookMessage> Messages { get; set; } = new();
        public DateTime AggregatedAt { get; set; } = DateTime.Now;
        public TimeSpan BufferDuration { get; set; }
    }
}
