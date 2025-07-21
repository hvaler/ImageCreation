using EventStore.Client;

using ImageCreation.Application.Commands;
using ImageCreation.Application.DTOs;
using ImageCreation.Application.Handlers;

using ImageCreation.Application.Interfaces;
using ImageCreation.Application.Projections;
using ImageCreation.Application.Queries;
using ImageCreation.Infrastructure.Interfaces; 
using ImageCreation.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// --- REGISTRO DE SERVICIOS DE INFRAESTRUCTURA CONCRETA Y ABSTRACCIONES ---

// Registra las implementaciones concretas de OpenAI (Public y Azure).
// Estas NO se inyectarán directamente en los Command Handlers, sino en la fábrica.
builder.Services.AddTransient<PublicOpenAiService>();
builder.Services.AddTransient<AzureOpenAiService>();

// Registra la fábrica de OpenAI.
// La fábrica es la que se inyectará en CreateImageCommandHandler y decidirá qué servicio concreto usar.
builder.Services.AddTransient<IOpenAiServiceFactory, OpenAiServiceFactory>();

// Registra el HttpClient genérico que será inyectado en UrlToBase64Converter.
builder.Services.AddHttpClient();

// Registra IUrlConverterService con su implementación concreta.
builder.Services.AddScoped<IUrlConverterService, UrlToBase64Converter>();

// Registra IImageClassifierService con su implementación concreta.
builder.Services.AddScoped<IImageClassifierService, AzureVisionClassifierService>();

// Registros para la base de datos SQL (Dapper) y la caché (Redis).
// Estas interfaces ahora están en ImageCreation.Application.Interfaces.
builder.Services.AddScoped<IDapperRepository, DapperRepository>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// Configuración y registro de EventStoreClient (Singleton) y la interfaz IEventStore (Scoped).
var eventStoreConnection = config.GetConnectionString("EventStoreConnection");
if (string.IsNullOrWhiteSpace(eventStoreConnection))
{
   throw new InvalidOperationException("EventStoreConnection not configured in appsettings.json.");
}
// Registra el cliente de EventStoreDB como Singleton, ya que es thread-safe y costoso de crear.
builder.Services.AddSingleton(new EventStoreClient(EventStoreClientSettings.Create(eventStoreConnection)));
// Registra IEventStore con su implementación EventStoreService (que usa el EventStoreClient Singleton).
builder.Services.AddScoped<IEventStore, EventStoreService>();

// --- REGISTRO DE HANDLERS (APLICACIÓN) ---

// Registra los Command Handlers.
builder.Services.AddScoped<ICommandHandler<CreateImageCommand, ImageDto>, CreateImageCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ClassifyImageCommand, ClassifiedImageDto>, ClassifyImageCommandHandler>();

// Registra los Query Handlers.
builder.Services.AddScoped<IQueryHandler<GetImageByIdQuery, ImageDto?>, GetImageByIdQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetImageBase64Query, string?>, GetImageBase64QueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetClassifiedImageByIdQuery, ClassifiedImageDto?>, GetClassifiedImageByIdQueryHandler>();

// --- REGISTRO DE PROYECTORES (APLICACIÓN) ---
// Los proyectores son Scoped porque dependen de repositorios que son Scoped.
builder.Services.AddScoped<ImageRecordProjector>();
builder.Services.AddScoped<ClassifiedImageRecordProjector>();

// --- REGISTRO DE SERVICIOS DE FONDO (HOSTED SERVICES) ---
// Registra el EventStoreSubscriptionService como un Hosted Service.
// Este se iniciará con la aplicación y gestionará las suscripciones a EventStoreDB.
// Recibirá IServiceScopeFactory para resolver los proyectores dentro de sus propios scopes.
builder.Services.AddHostedService<EventStoreSubscriptionService>();


// --- CONFIGURACIÓN ESTÁNDAR DE ASP.NET CORE ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Habilita la interfaz de usuario de Swagger.
app.UseSwagger();
app.UseSwaggerUI();

// Mapea los controladores para las rutas API.
app.MapControllers();

// Ejecuta la aplicación.
app.Run();