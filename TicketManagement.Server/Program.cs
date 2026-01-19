using System.Text.Json;
using System.Text.Json.Serialization;
using TicketManagement.Server.Data;

var builder = WebApplication.CreateBuilder(args);



// Controllers
builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.DictionaryKeyPolicy = null;
        options.JsonSerializerOptions.ReferenceHandler = null;
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ REAL Allow‑All CORS (DEV ONLY)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// DI
builder.Services.AddScoped<TicketRepository>();

var app = builder.Build();

// ✅ Log every request (keep until stable)
app.Use(
    async (ctx, next) =>
    {
        Console.WriteLine(
            $"{DateTime.Now:HH:mm:ss} {ctx.Request.Method} {ctx.Request.Path} Origin={ctx.Request.Headers["Origin"]}"
        );
        await next();
    }
);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ✅ COMMENT OUT HTTPS REDIRECTION IN DEV
// app.UseHttpsRedirection();

app.UseCors(); // ✅ MUST be before MapControllers
app.MapControllers();
app.Run();