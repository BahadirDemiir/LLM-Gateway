using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

var apiKey = builder.Configuration["OpenAI:ApiKey"]
    ?? throw new InvalidOperationException("OpenAI:ApiKey ayarlý deðil.");

builder.Services.AddHttpClient("openai", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/");
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", apiKey);
});

var app = builder.Build();

app.MapPost("/v1/chat/completions", async (HttpContext context, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("openai");

    using var upstreamRequest = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
    {
        Content = new StreamContent(context.Request.Body)
    };
    upstreamRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

    using var upstreamResponse = await client.SendAsync(
        upstreamRequest, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

    context.Response.StatusCode = (int)upstreamResponse.StatusCode;
    context.Response.ContentType = upstreamResponse.Content.Headers.ContentType?.ToString();

    await using var upstreamStream = await upstreamResponse.Content.ReadAsStreamAsync(context.RequestAborted);
    await upstreamStream.CopyToAsync(context.Response.Body, context.RequestAborted);
});

app.Run();