# Documentaci�n de Arquitectura de ImageCreation

Este documento describe la arquitectura de la aplicaci�n **ImageCreation**, una soluci�n que permite generar y clasificar im�genes, utilizando principios de dise�o limpio, CQRS (Command Query Responsibility Segregation) y Event Sourcing. La aplicaci�n est� estructurada en m�ltiples capas para garantizar una clara separaci�n de responsabilidades, modularidad y escalabilidad.

## 1. Visi�n General de la Aplicaci�n

La aplicaci�n **ImageCreation** expone una API RESTful para:
* Generar im�genes a partir de descripciones de texto utilizando servicios de IA como OpenAI o Azure OpenAI.
* Clasificar im�genes existentes a partir de URLs utilizando servicios de IA como Azure Vision.
* Consultar informaci�n sobre las im�genes generadas y clasificadas.

El sistema utiliza **Event Store DB** como su principal mecanismo de persistencia para los eventos de dominio, y mantiene vistas materializadas en una **base de datos SQL** (para consultas optimizadas) y **Redis** (para cach�).

## 2. Estructura de Capas

La aplicaci�n sigue una arquitectura de capas bien definida, con dependencias unidireccionales de afuera hacia adentro (siguiendo el Principio de Inversi�n de Dependencias).

### 2.1. `ImageCreation.Api` (Capa de Presentaci�n)

Esta capa es una aplicaci�n ASP.NET Core que expone la API RESTful.

* **Responsabilidades**:
    * Recepci�n y enrutamiento de solicitudes HTTP.
    * Validaci�n b�sica de la entrada.
    * Delegaci�n de la ejecuci�n a los `Command Handlers` (para operaciones de escritura) y `Query Handlers` (para operaciones de lectura) de la capa de aplicaci�n.
    * Serializaci�n/deserializaci�n de datos (JSON).
    * Manejo de errores y devoluci�n de respuestas HTTP adecuadas.
    * Configuraci�n de la inyecci�n de dependencias para toda la aplicaci�n.
* **Componentes Clave**:
    * `Controllers`: Como `ImagesController`, que exponen los endpoints y orquestan la interacci�n con los Handlers.
    * `Program.cs`: Punto de entrada para la configuraci�n de servicios (inyecci�n de dependencias), middlewares y el arranque de la aplicaci�n.
    * `appsettings.json` / `appsettings.Development.json`: Archivos de configuraci�n para cadenas de conexi�n, API keys (gestionadas por entorno) y ajustes de logging.

### 2.2. `ImageCreation.Application` (Capa de Aplicaci�n)

Esta es la capa de orquestaci�n y l�gica de negocio espec�fica de la aplicaci�n.

* **Responsabilidades**:
    * Definir los casos de uso de la aplicaci�n mediante Comandos y Consultas.
    * Implementar los `Command Handlers` y `Query Handlers` para ejecutar estos casos de uso.
    * Manejar la interacci�n con el dominio (disparar eventos) y las interfaces de infraestructura.
    * Definir DTOs para el intercambio de datos.
    * Contener los Proyectores para mantener las vistas materializadas.
* **Componentes Clave**:
    * **Comandos (`Commands`)**: Objetos inmutables que representan intenciones de cambiar el estado del sistema. Ejemplos: `CreateImageCommand`, `ClassifyImageCommand`.
    * **DTOs (`DTOs`)**: Objetos para transferir datos entre capas. Ejemplos: `ImageDto`, `ClassifiedImageDto`.
    * **Handlers (`Handlers`)**:
        * `Command Handlers` (ej. `CreateImageCommandHandler`, `ClassifyImageCommandHandler`): Contienen la l�gica de negocio para procesar comandos, interact�an con el dominio (ej. creando `ImageRecord` o `ClassifiedImageRecord` y publicando eventos a `IEventStore`). Son la �nica fuente de cambio de estado en el sistema.
        * `Query Handlers` (ej. `GetImageByIdQueryHandler`, `GetImageBase64QueryHandler`, `GetClassifiedImageByIdQueryHandler`): Se encargan de recuperar datos de las vistas materializadas (usando `IDapperRepository` y `ICacheService`) sin modificar el estado. Implementan el patr�n Cache-Aside.
    * **Consultas (`Queries`)**: Objetos inmutables que encapsulan los par�metros para la recuperaci�n de datos. Ejemplos: `GetImageByIdQuery`, `GetImageBase64Query`, `GetClassifiedImageByIdQuery`.
    * **Proyectores (`Projections`)**: Componentes clave en Event Sourcing. Escuchan eventos de dominio desde Event Store DB (`EventStoreSubscriptionService` en la capa de infraestructura) y actualizan las vistas materializadas en la base de datos SQL y Redis. Garantizan la **idempotencia** en sus operaciones de persistencia. Ejemplos: `ImageRecordProjector`, `ClassifiedImageRecordProjector`.
    * **Interfaces (`Interfaces`)**: Define los contratos que la capa de aplicaci�n espera de la infraestructura, promoviendo el Principio de Inversi�n de Dependencias. Ejemplos: `ICommandHandler`, `IQueryHandler`, `IOpenAiService`, `IEventStore`, `ICacheService`, `IDapperRepository`, `IUrlConverterService`, `IOpenAiServiceFactory`.

### 2.3. `ImageCreation.Domain` (Capa de Dominio)

Esta es la capa central de la aplicaci�n, conteniendo la l�gica de negocio fundamental y las reglas m�s importantes. Es independiente de cualquier otra capa.

* **Responsabilidades**:
    * Modelar el negocio de forma precisa y robusta.
    * Encapsular el estado y el comportamiento esencial del negocio.
    * Definir los Eventos de Dominio como la "fuente de verdad".
    * Implementar validaciones intr�nsecas para asegurar la validez del modelo.
* **Componentes Clave**:
    * **Entidades (`Entities`)**: Objetos con identidad �nica y mutable. Ejemplos: `ImageRecord`, `ClassifiedImageRecord`.
    * **Value Objects (`ValueObjects`)**: Objetos inmutables definidos por sus atributos, con validaci�n intr�nseca. Ejemplos: `ImageDescription`, `Base64Data`, `ClassificationResult`, `ImageUrl`.
    * **Eventos de Dominio (`Events`)**: Hechos inmutables que ocurrieron en el dominio, sirviendo como la fuente de verdad. Ejemplos: `ImageCreatedEvent`, `ImageClassifiedEvent`. `IDomainEvent` es una interfaz de marcador.

### 2.4. `ImageCreation.Infrastructure` (Capa de Infraestructura)

Esta capa proporciona las implementaciones concretas de las interfaces definidas en la capa de Aplicaci�n. Gestiona los detalles t�cnicos y la interacci�n con sistemas externos.

* **Responsabilidades**:
    * Acceso a bases de datos (SQL Server, Event Store DB).
    * Integraci�n con servicios de cach� (Redis).
    * Comunicaci�n con APIs de inteligencia artificial (OpenAI, Azure Vision).
    * Manejo de operaciones de E/S de red (descarga de im�genes).
    * Ejecuci�n de servicios en segundo plano.
* **Componentes Clave**:
    * **Servicios de Persistencia**:
        * `DapperRepository`: Implementaci�n de `IDapperRepository` para SQL Server usando Dapper. Utiliza `MERGE` para idempotencia.
        * `EventStoreService`: Implementaci�n de `IEventStore` para publicar eventos en Event Store DB.
    * **Servicios de Cach�**:
        * `RedisCacheService`: Implementaci�n de `ICacheService` para interactuar con Redis.
    * **Servicios de IA y Utilidades**:
        * `OpenAiServiceFactory`: Implementaci�n de `IOpenAiServiceFactory` para seleccionar entre servicios OpenAI.
        * `PublicOpenAiService`: Implementaci�n de `IOpenAiService` para la API p�blica de OpenAI.
        * `AzureOpenAiService`: Implementaci�n de `IOpenAiService` para Azure OpenAI.
        * `UrlToBase64Converter`: Implementaci�n de `IUrlConverterService` para descargar y convertir URLs a Base64.
        * `AzureVisionClassifierService`: Implementaci�n de `IImageClassifierService` para clasificar im�genes con Azure AI Vision.
    * **Servicios de Fondo**:
        * `EventStoreSubscriptionService`: `IHostedService` que gestiona las suscripciones a Event Store DB y distribuye eventos a los Proyectores en la capa de aplicaci�n, creando un `IServiceScope` por cada evento para asegurar la correcta resoluci�n de dependencias.

## 3. Flujo de Datos y Comunicaci�n (Ejemplos)

### 3.1. Generaci�n de Im�genes (Comando `CreateImageCommand`)

1.  **`ImageCreation.Api`**: Cliente env�a `POST /api/Images/generate`. `ImagesController` recibe la petici�n, valida y crea `CreateImageCommand`.
2.  **`ImageCreation.Application`**: El controlador invoca `CreateImageCommandHandler`.
3.  `CreateImageCommandHandler` utiliza `IOpenAiServiceFactory` para obtener `IOpenAiService` (Public o Azure) y genera la imagen.
4.  `CreateImageCommandHandler` crea un `ImageRecord` y publica un `ImageCreatedEvent` en Event Store DB a trav�s de `IEventStore`.
5.  **`ImageCreation.Infrastructure`**: `EventStoreService` serializa y persiste el evento.
6.  **`ImageCreation.Infrastructure` (Servicio de Fondo)**: `EventStoreSubscriptionService` consume el `ImageCreatedEvent` de Event Store DB.
7.  **`ImageCreation.Application`**: `EventStoreSubscriptionService` delega el evento a `ImageRecordProjector`.
8.  `ImageRecordProjector` reconstruye `ImageRecord`, lo persiste de forma idempotente en SQL (`IDapperRepository`) y guarda el `ImageDto` en Redis (`ICacheService`).
9.  **`ImageCreation.Api`**: `ImagesController` devuelve `201 Created` con el `ImageDto`.

### 3.2. Consulta de Im�genes (Consulta `GetImageByIdQuery`)

1.  **`ImageCreation.Api`**: Cliente env�a `GET /api/Images/{id}`. `ImagesController` crea `GetImageByIdQuery`.
2.  **`ImageCreation.Application`**: El controlador invoca `GetImageByIdQueryHandler`.
3.  `GetImageByIdQueryHandler` primero intenta recuperar el `ImageDto` de `ICacheService` (Redis).
4.  Si no est� en cach�, `GetImageByIdQueryHandler` consulta `IDapperRepository` para obtener el `ImageRecord` desde la base de datos SQL.
5.  Si se encuentra en SQL, se convierte a `ImageDto` y se almacena en cach�.
6.  **`ImageCreation.Api`**: `ImagesController` devuelve `200 OK` con `ImageDto` o `404 Not Found`.

## 4. Patrones de Dise�o Clave

* **CQRS (Command Query Responsibility Segregation)**: Clara separaci�n de responsabilidades entre las operaciones de escritura (comandos) y lectura (consultas), cada una con sus propios modelos y handlers.
* **Event Sourcing**: Event Store DB es la fuente de verdad. Todos los cambios de estado se registran como una secuencia inmutable de Eventos de Dominio.
* **Entidades y Value Objects**: Modelan el dominio, encapsulando estado, comportamiento y validaciones.
* **Proyecciones / Vistas Materializadas**: Modelos de lectura optimizados que se construyen y mantienen actualizados as�ncronamente a partir de los eventos de dominio.
* **Inyecci�n de Dependencias (DI)**: Utilizado extensivamente para desacoplar los componentes, inyectando interfaces en lugar de implementaciones concretas.
* **Principios SOLID**:
    * **Single Responsibility Principle (SRP)**: Cada clase tiene una �nica raz�n para cambiar.
    * **Open/Closed Principle (OCP)**: Las entidades y m�dulos est�n abiertos para extensi�n, pero cerrados para modificaci�n.
    * **Liskov Substitution Principle (LSP)**: Las implementaciones de interfaces pueden ser sustituidas sin romper el comportamiento.
    * **Dependency Inversion Principle (DIP)**: Las capas de alto nivel (Application) no dependen de las capas de bajo nivel (Infrastructure); ambas dependen de abstracciones (interfaces).
* **Repository Pattern**: Abstrae las operaciones de persistencia de datos (en `DapperRepository`).
* **Cache-Aside Pattern**: Estrategia para usar la cach�, visible en los Query Handlers.
* **F�brica Abstracta (Factory Pattern)**: `IOpenAiServiceFactory` para la creaci�n de servicios de IA.
* **Hosted Services**: Para ejecutar l�gica de fondo (ej. `EventStoreSubscriptionService`).
* **Idempotencia**: Crucial en los proyectores para manejar eventos duplicados en un sistema distribuido.

## 5. Tecnolog�as Utilizadas

* **ASP.NET Core**: Framework para construir la API.
* **.NET (C#)**: Lenguaje de programaci�n.
* **Event Store DB**: Base de datos de Event Sourcing.
* **SQL Server**: Base de datos relacional para vistas materializadas.
* **Redis**: Servicio de cach�.
* **Dapper**: Micro-ORM para acceso a datos SQL.
* **OpenAI SDK (.NET)**: Para interactuar con OpenAI API.
* **Azure AI SDKs**: Para interactuar con Azure OpenAI y Azure Vision.
* **Newtonsoft.Json**: Para la serializaci�n/deserializaci�n de eventos de dominio (para compatibilidad con Event Store DB).
* **Microsoft.Extensions.Logging**: Para el registro de logs.

## 6. Consideraciones y Futuras Mejoras

* **Observabilidad**: A�adir m�tricas, tracing distribuido (ej. OpenTelemetry) para una mejor monitorizaci�n.
* **Manejo de Transacciones**: Aunque Event Sourcing gestiona la fuente de verdad, la eventual consistencia de las vistas materializadas requiere estrategias de reintento y monitoreo de errores.
* **Seguridad**: Implementar autenticaci�n y autorizaci�n para la API.
* **Despliegue**: Contenerizaci�n con Docker y orquestaci�n con Kubernetes para un despliegue y escalado robusto.
* **Manejo de Errores Avanzado**: Implementar colas de mensajes muertas (DLQ) para eventos que fallan persistentemente en los proyectores.
* **Versionado de Eventos**: Implementar una estrategia expl�cita para el versionado de eventos de dominio a medida que la aplicaci�n evoluciona.