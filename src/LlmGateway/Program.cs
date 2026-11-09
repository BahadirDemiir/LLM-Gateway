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

app.MapPost("/v1/chat/completions", async (HttpRequest request, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("openai");

    using var upstreamRequest = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
    {
        Content = new StreamContent(request.Body)
    };
    upstreamRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

    using var upstreamResponse = await client.SendAsync(upstreamRequest);
    var body = await upstreamResponse.Content.ReadAsStringAsync();

    return Results.Content(body, "application/json", statusCode: (int)upstreamResponse.StatusCode);
});

app.Run();