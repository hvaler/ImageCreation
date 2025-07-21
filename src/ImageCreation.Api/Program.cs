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
// Estas NO se inyectar�n directamente en los Command Handlers, sino en la f�brica.
builder.Services.AddTransient<PublicOpenAiService>();
builder.Services.AddTransient<AzureOpenAiService>();

// Registra la f�brica de OpenAI.
// La f�brica es la que se inyectar� en CreateImageCommandHandler y decidir� qu� servicio concreto usar.
builder.Services.AddTransient<IOpenAiServiceFactory, OpenAiServiceFactory>();

// Registra el HttpClient gen�rico que ser� inyectado en UrlToBase64Converter.
builder.Services.AddHttpClient();

// Registra IUrlConverterService con su implementaci�n concreta.
builder.Services.AddScoped<IUrlConverterService, UrlToBase64Converter>();

// Registra IImageClassifierService con su implementaci�n concreta.
builder.Services.AddScoped<IImageClassifierService, AzureVisionClassifierService>();

// Registros para la base de datos SQL (Dapper) y la cach� (Redis).
// Estas interfaces ahora est�n en ImageCreation.Application.Interfaces.
builder.Services.AddScoped<IDapperRepository, DapperRepository>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// Configuraci�n y registro de EventStoreClient (Singleton) y la interfaz IEventStore (Scoped).
var eventStoreConnection = config.GetConnectionString("EventStoreConnection");
if (string.IsNullOrWhiteSpace(eventStoreConnection))
{
   throw new InvalidOperationException("EventStoreConnection not configured in appsettings.json.");
}
// Registra el cliente de EventStoreDB como Singleton, ya que es thread-safe y costoso de crear.
builder.Services.AddSingleton(new EventStoreClient(EventStoreClientSettings.Create(eventStoreConnection)));
// Registra IEventStore con su implementaci�n EventStoreService (que usa el EventStoreClient Singleton).
builder.Services.AddScoped<IEventStore, EventStoreService>();

// --- REGISTRO DE HANDLERS (APLICACI�N) ---

// Registra los Command Handlers.
builder.Services.AddScoped<ICommandHandler<CreateImageCommand, ImageDto>, CreateImageCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ClassifyImageCommand, ClassifiedImageDto>, ClassifyImageCommandHandler>();

// Registra los Query Handlers.
builder.Services.AddScoped<IQueryHandler<GetImageByIdQuery, ImageDto?>, GetImageByIdQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetImageBase64Query, string?>, GetImageBase64QueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetClassifiedImageByIdQuery, ClassifiedImageDto?>, GetClassifiedImageByIdQueryHandler>();

// --- REGISTRO DE PROYECTORES (APLICACI�N) ---
// Los proyectores son Scoped porque dependen de repositorios que son Scoped.
builder.Services.AddScoped<ImageRecordProjector>();
builder.Services.AddScoped<ClassifiedImageRecordProjector>();

// --- REGISTRO DE SERVICIOS DE FONDO (HOSTED SERVICES) ---
// Registra el EventStoreSubscriptionService como un Hosted Service.
// Este se iniciar� con la aplicaci�n y gestionar� las suscripciones a EventStoreDB.
// Recibir� IServiceScopeFactory para resolver los proyectores dentro de sus propios scopes.
builder.Services.AddHostedService<EventStoreSubscriptionService>();


// --- CONFIGURACI�N EST�NDAR DE ASP.NET CORE ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Habilita la interfaz de usuario de Swagger.
app.UseSwagger();
app.UseSwaggerUI();

// Mapea los controladores para las rutas API.
app.MapControllers();

// Ejecuta la aplicaci�n.
app.Run();