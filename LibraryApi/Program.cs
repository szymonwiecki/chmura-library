using System.Text;
using LibraryApi.Azure.BlobStorage;
using LibraryApi.Azure.QueueStorage;
using LibraryApi.Azure.ServiceBus;
using LibraryApi.Data;
using LibraryApi.Models;
using LibraryApi.Patterns.Command;
using LibraryApi.Patterns.Observer;
using LibraryApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// JWT - klucz z appsettings.json
var jwtKey = builder.Configuration["JwtKey"]
    ?? throw new InvalidOperationException("JwtKey is not configured in appsettings.json");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// Azure SQL Database przez EF Core
builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Azure Redis Cache - jeśli niedostępny, aplikacja działa dalej bez cache
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    try
    {
        var connString = sp.GetRequiredService<IConfiguration>()["Redis:ConnectionString"]
            ?? "localhost:6379,abortConnect=false";
        var opts = ConfigurationOptions.Parse(connString);
        opts.AbortOnConnectFail = false;
        opts.ConnectTimeout = 5000;
        opts.SyncTimeout = 3000;
        return ConnectionMultiplexer.Connect(opts);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Redis unavailable — caching disabled, falling back to direct DB access.");
        var opts = new ConfigurationOptions { AbortOnConnectFail = false };
        opts.EndPoints.Add("localhost:6379");
        return ConnectionMultiplexer.Connect(opts);
    }
});

// Azure services - Blob Storage, Queue Storage, Service Bus
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();
builder.Services.AddSingleton<IQueueService, QueueService>();
builder.Services.AddSingleton<IServiceBusService, ServiceBusService>();

// Wzorzec Observer - publisher + subskrybent Service Bus
builder.Services.AddSingleton<BookEventPublisher>();
builder.Services.AddSingleton<IBookEventSubscriber, ServiceBusSubscriber>();

// Wzorzec Proxy - CachedBookService opakowuje BookService i dodaje Redis cache
builder.Services.AddScoped<BookService>();
builder.Services.AddScoped<IBookService>(sp =>
    new CachedBookService(
        sp.GetRequiredService<BookService>(),
        sp.GetRequiredService<IConnectionMultiplexer>(),
        sp.GetRequiredService<ILogger<CachedBookService>>()));

// AI - generowanie opisów przez Claude (Anthropic)
builder.Services.AddHttpClient<IAiService, AiService>();

// Wzorzec Proxy - CachedGoogleBooksService opakowuje GoogleBooksService i dodaje Redis cache
builder.Services.AddHttpClient<GoogleBooksService>();
builder.Services.AddScoped<IGoogleBooksService>(sp =>
    new CachedGoogleBooksService(
        sp.GetRequiredService<GoogleBooksService>(),
        sp.GetRequiredService<IConnectionMultiplexer>(),
        sp.GetRequiredService<ILogger<CachedGoogleBooksService>>()));

// Wzorzec Command - historia operacji CRUD zapisywana w Azure Table Storage
builder.Services.AddSingleton<ICommandHistoryStore, AzureTableCommandHistoryStore>();
builder.Services.AddScoped<CommandInvoker>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Library API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Podaj token JWT w formacie: Bearer {token}"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
    options.EnableAnnotations();
});

builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// Podłącz subskrybentów Observera po zbudowaniu kontenera DI
var publisher = app.Services.GetRequiredService<BookEventPublisher>();
foreach (var sub in app.Services.GetServices<IBookEventSubscriber>())
    publisher.Subscribe(sub);

// Seed bazy danych - błąd połączenia nie crashuje całej aplikacji
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<LibraryContext>();
        context.Database.EnsureCreated();
        if (!context.Books.Any())
        {
            context.Books.AddRange(
                new Book { Title = "Witality 2.0", Author = "Ben Greenfield", PublishedYear = 2020, Genre = "Biology" },
                new Book { Title = "Biohacking", Author = "Karol Wyszomirski", PublishedYear = 2021, Genre = "Motivation" }
            );
            context.SaveChanges();
        }
        logger.LogInformation("Database ready.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed — check ConnectionStrings:DefaultConnection. App will continue but /api/Books will not work until the DB is reachable.");
    }
}

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
