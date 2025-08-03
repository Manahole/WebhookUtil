using WebhookUtil.Components;
using WebhookUtil.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<WebhookService>();
builder.Services.AddHttpClient();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

// Map webhook endpoints dynamically
app.MapPost("/webhook/{webhookId}", async (string webhookId, HttpContext context, WebhookService service) =>
{
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    await service.ProcessWebhookMessage(webhookId, body, context.Request.Headers);
    return Results.Ok();
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.Run();