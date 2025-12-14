using Broker.AdminUI.Components;
using Broker.AdminUI.Services;
using Broker.Contracts;
using Refit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure Broker API Client
var brokerApiBaseUrl = builder.Configuration["BrokerApi:BaseUrl"] ?? "http://localhost:5000";
builder.Services.AddRefitClient<IBrokerApiClient>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(brokerApiBaseUrl));

// Configure MQTT Service
var brokerHost = builder.Configuration["BrokerApi:BrokerHost"] ?? "localhost";
var brokerPort = builder.Configuration.GetValue<int>("BrokerApi:BrokerPort", 1883);
builder.Services.AddSingleton<IMqttService>(sp => 
    new MqttService(brokerHost, brokerPort, sp.GetRequiredService<ILogger<MqttService>>()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
