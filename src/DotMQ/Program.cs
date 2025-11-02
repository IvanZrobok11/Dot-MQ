using DotMQ;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Configure CORS for Admin UI
builder.Services.AddCors(options =>
{
    options.AddPolicy("AdminUICors", policy =>
    {
        policy.WithOrigins("http://localhost:5001", "https://localhost:5002")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddBrokerServices(builder.Configuration);

var app = builder.Build();

// Configure middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseCors("AdminUICors");
app.UseRouting();
app.MapControllers();

// Run the application (hosted services start automatically)
await app.RunAsync();