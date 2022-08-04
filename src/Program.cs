using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var app = builder.Build();


var mqttConfig = new MqttConfig();
app.Configuration.GetSection(nameof(MqttConfig)).Bind(mqttConfig);
var log = app.Services.GetRequiredService<ILogger<Program>>();

log.LogTrace("Url: {MqttUrl}, HttpToken: {HttpToken}", mqttConfig.Url, mqttConfig.HttpToken);

// Configure the HTTP request pipeline.
//app.UseHttpsRedirection();


var httpClient = new HttpClient();
var mqttFactory = new MqttFactory();

using (var mqttClient = mqttFactory.CreateManagedMqttClient())
{

    var mqttClientOptionsBuilder = new MqttClientOptionsBuilder()
        .WithTcpServer(mqttConfig.Url.Host, mqttConfig.Url.Port)
        .WithTls(o =>
        {
            o.UseTls = mqttConfig.UseTls;
        })
        .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500);

    if (mqttConfig.HasCredentials)
        mqttClientOptionsBuilder = mqttClientOptionsBuilder.WithCredentials(mqttConfig.User, mqttConfig.Password);

    var mqttClientOptions = mqttClientOptionsBuilder.Build();

    var managedMqttClientOptions = new ManagedMqttClientOptionsBuilder()
        .WithClientOptions(mqttClientOptions)
        .Build();

    await mqttClient.StartAsync(managedMqttClientOptions);


    // TODO Add a JSON file or similiar describing which messaegs that can actually be sent?
    // TODO Add  support for automatic home assistant entity creation (when first http message is received?). Or we can have a specific endpoint for that
    //      or we can have it in the config file. Possible to use YAML file in etc/...?

    app.MapGet("/mqtt/{token}/{*topic}", async (HttpRequest request, string token, string topic) =>
    {
        log.LogInformation("RECV: {Url}", $"/mqtt/*redacted*/{topic}");

        if (token != mqttConfig.HttpToken)
            return Results.Unauthorized();
        
        var applicationMessageBuilder = new MqttApplicationMessageBuilder().WithTopic(topic);
        if (TryGetPayloadFromQueryString(request.Query, out var payLoad))
            applicationMessageBuilder = applicationMessageBuilder.WithPayload(payLoad);

        log.LogInformation("SEND: {Topic}, {Payload}", topic, payLoad ?? "");
        await mqttClient.EnqueueAsync(applicationMessageBuilder.Build());
        return Results.Ok();
    });

    // TODO Add support for POST??


    app.Run();

    // Wait  until the queue is fully processed or until 10 seconds have expired
    SpinWait.SpinUntil(
        () => mqttClient.PendingApplicationMessagesCount == 0, 
        (int)TimeSpan.FromSeconds(10).TotalMilliseconds);
}

// Convert a query string into a JSON object
bool TryGetPayloadFromQueryString(IQueryCollection query, out string payLoad)
{
    payLoad = "";
    var queryItems = query.Where(q => !string.IsNullOrWhiteSpace(q.Key) && q.Value.Any());
    if (queryItems.Any())
    {
        // We dont support duplicate key names or value arrays for now.
        // We only support number, bool and strings as key values as well.
        var queryAsDictionary = queryItems
            .GroupBy(q => q.Key, q => q.Value.First())
            .ToDictionary(q => q.Key, q => ConvertToJsonObject(q.First()));
        
        payLoad = JsonSerializer.Serialize(queryAsDictionary);
        return true;
    }

    return false;
}

object? ConvertToJsonObject(string v)
{
    if (double.TryParse(v, out var number))
        return number;
    if (bool.TryParse(v, out var boolVal))
        return boolVal;
    return v;
}