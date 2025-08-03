namespace WebhookUtil.Services
{
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Text.Json;
    using System.Text;
    using WebhookUtil.Models;

    public class WebhookService
    {
        private readonly Dictionary<string, WebhookConfig> _webhookConfigs = [];
        private readonly Dictionary<string, Subject<WebhookMessage>> _webhookSubjects = [];
        private readonly Dictionary<string, IDisposable> _subscriptions = [];
        private readonly IHttpClientFactory _httpClientFactory;

        public event Action? ConfigurationsChanged;

        public WebhookService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            ReloadWebhookConfigs().ToList().ForEach(AddWebhook);
        }

        public void AddWebhook(WebhookConfig config)
        {
            _webhookConfigs[config.Id] = config;

            // Create a new Subject for this webhook
            var subject = new Subject<WebhookMessage>();
            _webhookSubjects[config.Id] = subject;

            // Set up the Rx.Buffer with time and count
            var subscription = subject
                .SwitchIf(() => config.MaxBufferSize > 0, 
                    s => s.Buffer(TimeSpan.FromSeconds(config.BufferTimeSeconds), config.MaxBufferSize),
                    s => s.Buffer(TimeSpan.FromSeconds(config.BufferTimeSeconds))
                )
                .Where(messages => messages.Any()) // Only process if there are messages
                .Subscribe(async messages =>
                {
                    await SendAggregatedMessages(config, messages.ToList());
                });

            _subscriptions[config.Id] = subscription;
            
            ConfigurationsChanged?.Invoke();
        }

        public void RemoveWebhook(string webhookId)
        {
            if (_subscriptions.TryGetValue(webhookId, out var subscription))
            {
                subscription.Dispose();
                _subscriptions.Remove(webhookId);
            }

            if (_webhookSubjects.TryGetValue(webhookId, out var subject))
            {
                subject.OnCompleted();
                subject.Dispose();
                _webhookSubjects.Remove(webhookId);
            }
            if(_webhookConfigs.TryGetValue(webhookId, out var config))
            {
                RemoveWebhookConfig(config);
                _webhookConfigs.Remove(webhookId);
            }
            
            ConfigurationsChanged?.Invoke();
        }

        public async Task ProcessWebhookMessage(string webhookId, string body, IHeaderDictionary headers)
        {

            var configKvp = _webhookConfigs.FirstOrDefault(x => x.Value.ListenPath == webhookId); 
            var config = configKvp.Value;
            if (config is null || !config.IsActive)
                return;

            if (!_webhookSubjects.TryGetValue(configKvp.Key, out var subject))
                return;

            var message = new WebhookMessage
            {
                WebhookId = webhookId,
                Body = body,
                Headers = headers.ToDictionary(h => h.Key, h => string.Join(',', h.Value.Select(x => x))),
                ReceivedAt = DateTime.Now
            };

            subject.OnNext(message);
        }

        private async Task SendAggregatedMessages(WebhookConfig config, List<WebhookMessage> messages)
        {
            try
            {
                var payload = new AggregatedWebhookPayload
                {
                    WebhookId = config.Id,
                    WebhookName = config.Name,
                    MessageCount = messages.Count,
                    Messages = messages,
                    AggregatedAt = DateTime.Now,
                    BufferDuration = TimeSpan.FromSeconds(config.BufferTimeSeconds)
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                using var httpClient = _httpClientFactory.CreateClient();
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                await httpClient.PostAsync(config.TargetWebhookUrl, content);
            }
            catch (Exception ex)
            {
                // Log error - in production you'd want proper logging
                Console.WriteLine($"Error sending aggregated webhook: {ex.Message}");
            }
        }

        private const string _directory = ".";

        public static void SaveWebhookConfig(WebhookConfig config)
        {
            File.WriteAllText($"hook_{config.Id}.json", JsonSerializer.Serialize(config));
        }

        public static void RemoveWebhookConfig(WebhookConfig config)
        {
            File.Delete($"hook_{config.Id}.json");
        }

        private static IEnumerable<WebhookConfig> ReloadWebhookConfigs()
        {
            return new DirectoryInfo(_directory)
                .GetFiles()
                .Where(x => x.Name.StartsWith("hook_"))
                .Select(x => File.ReadAllText(x.FullName))
                .Select(x => JsonSerializer.Deserialize<WebhookConfig>(x));
        }

        

        public IEnumerable<WebhookConfig> GetAllWebhooks() => _webhookConfigs.Values;

        public WebhookConfig? GetWebhook(string id) => _webhookConfigs.TryGetValue(id, out var config) ? config : null;
    }
}
