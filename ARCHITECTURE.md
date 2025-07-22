﻿# Documentación de Arquitectura de ImageCreation

Este documento describe la arquitectura de la aplicación **ImageCreation**, una solución que permite generar y clasificar imágenes, utilizando principios de diseño limpio, CQRS (Command Query Responsibility Segregation) y Event Sourcing. La aplicación está estructurada en múltiples capas para garantizar una clara separación de responsabilidades, modularidad y escalabilidad.

## 1. Visión General de la Aplicación

La aplicación **ImageCreation** expone una API RESTful para:
* Generar imágenes a partir de descripciones de texto utilizando diferentes proveedores de inteligencia artificial (IA).
* Clasificar imágenes existentes a partir de URLs utilizando servicios de visión artificial.
* Consultar información sobre las imágenes generadas y clasificadas.

El sistema utiliza **Event Store DB** como su principal mecanismo de persistencia para los eventos de dominio, y mantiene vistas materializadas en una **base de datos SQL** (para consultas optimizadas) y **Redis** (para caché).

## 2. Estructura de Capas

La aplicación sigue una arquitectura de capas bien definida, con dependencias unidireccionales de afuera hacia adentro (siguiendo el Principio de Inversión de Dependencias).

### 2.1. `ImageCreation.Api` (Capa de Presentación)

Esta capa es una aplicación ASP.NET Core que expone la API RESTful.

* **Responsabilidades**:
    * Recepción y enrutamiento de solicitudes HTTP.
    * Validación básica de la entrada.
    * Delegación de la ejecución a los `Command Handlers` (para operaciones de escritura) y `Query Handlers` (para operaciones de lectura) de la capa de aplicación.
    * Serialización/deserialización de datos (JSON).
    * Manejo de errores y devolución de respuestas HTTP adecuadas.
    * Configuración de la inyección de dependencias para toda la aplicación.
* **Componentes Clave**:
    * `Controllers`: Como `ImagesController`, que exponen los endpoints y orquestan la interacción con los Handlers.
    * `Program.cs`: Punto de entrada para la configuración de servicios (inyección de dependencias), middlewares y el arranque de la aplicación.
    * `appsettings.json` / `appsettings.Development.json`: Archivos de configuración para cadenas de conexión, API keys (gestionadas por entorno) y ajustes de logging.

### 2.2. `ImageCreation.Application` (Capa de Aplicación)

Esta es la capa de orquestación y lógica de negocio específica de la aplicación.

* **Responsabilidades**:
    * Definir los casos de uso de la aplicación mediante Comandos y Consultas.
    * Implementar los `Command Handlers` y `Query Handlers` para ejecutar estos casos de uso.
    * Manejar la interacción con el dominio (disparar eventos) y las interfaces de infraestructura.
    * Definir DTOs para el intercambio de datos.
    * Contener los Proyectores para mantener las vistas materializadas.
* **Componentes Clave**:
    * **Comandos (`Commands`)**: Objetos inmutables que representan intenciones de cambiar el estado del sistema. Ejemplos: `CreateImageCommand`, `ClassifyImageCommand`.
    * **DTOs (`DTOs`)**: Objetos para transferir datos entre capas. Ejemplos: `ImageDto` (ahora incluye `PlatformUsed`), `ClassifiedImageDto`.
    * **Handlers (`Handlers`)**:
        * `Command Handlers` (ej. `CreateImageCommandHandler`, `ClassifyImageCommandHandler`): Contienen la lógica de negocio para procesar comandos, interactúan con el dominio (ej. creando `ImageRecord` o `ClassifiedImageRecord` y publicando eventos a `IEventStore`). Son la única fuente de cambio de estado en el sistema. `CreateImageCommandHandler` ahora usa `PlatformRequested` para determinar el servicio de IA.
        * `Query Handlers` (ej. `GetImageByIdQueryHandler`, `GetImageBase64QueryHandler`, `GetClassifiedImageByIdQueryHandler`): Se encargan de recuperar datos de las vistas materializadas (usando `IDapperRepository` y `ICacheService`) sin modificar el estado. Implementan el patrón Cache-Aside.
    * **Consultas (`Queries`)**: Objetos inmutables que encapsulan los parámetros para la recuperación de datos. Ejemplos: `GetImageByIdQuery`, `GetImageBase64Query`, `GetClassifiedImageByIdQuery`.
    * **Proyectores (`Projections`)**: Componentes clave en Event Sourcing. Escuchan eventos de dominio desde Event Store DB (`EventStoreSubscriptionService` en la capa de infraestructura) y actualizan las vistas materializadas en la base de datos SQL y Redis. Garantizan la **idempotencia** en sus operaciones de persistencia. Ejemplos: `ImageRecordProjector` (ahora procesa `PlatformUsed`), `ClassifiedImageRecordProjector`.
    * **Interfaces (`Interfaces`)**: Define los contratos que la capa de aplicación espera de la infraestructura, promoviendo el Principio de Inversión de Dependencias. Ejemplos: `ICommandHandler`, `IQueryHandler`, `IOpenAiService`, `IEventStore`, `ICacheService`, `IDapperRepository`, `IUrlConverterService`, `IOpenAiServiceFactory`.

### 2.3. `ImageCreation.Domain` (Capa de Dominio)

Esta es la capa central de la aplicación, conteniendo la lógica de negocio fundamental y las reglas más importantes. Es independiente de cualquier otra capa.

* **Responsabilidades**:
    * Modelar el negocio de forma precisa y robusta.
    * Encapsular el estado y el comportamiento esencial del negocio.
    * Definir los Eventos de Dominio como la "fuente de verdad".
    * Implementar validaciones intrínsecas para asegurar la validez del modelo.
* **Componentes Clave**:
    * **Entidades (`Entities`)**: Objetos con identidad única y mutable. Ejemplos: `ImageRecord` (ahora incluye `PlatformUsed`), `ClassifiedImageRecord`.
    * **Value Objects (`ValueObjects`)**: Objetos inmutables definidos por sus atributos, con validación intrínseca. Ejemplos: `ImageDescription`, `Base64Data`, `ClassificationResult`, `ImageUrl`, y el **nuevo `Platform`**.
    * **Eventos de Dominio (`Events`)**: Hechos inmutables que ocurrieron en el dominio, sirviendo como la fuente de verdad. Ejemplos: `ImageCreatedEvent` (ahora incluye `PlatformUsed`), `ImageClassifiedEvent`. `IDomainEvent` es una interfaz de marcador.

### 2.4. `ImageCreation.Infrastructure` (Capa de Infraestructura)

Esta capa proporciona las implementaciones concretas de las interfaces definidas en la capa de Aplicación. Gestiona los detalles técnicos y la interacción con sistemas externos.

* **Responsabilidades**:
    * Acceso a bases de datos (SQL Server, Event Store DB).
    * Integración con servicios de caché (Redis).
    * Comunicación con APIs de inteligencia artificial.
    * Manejo de operaciones de E/S de red (descarga de imágenes).
    * Ejecución de servicios en segundo plano.

* **Organización Mejorada de `Services`**:
    Para mejorar la claridad y la mantenibilidad, la carpeta `Services` se ha reorganizado en subcarpetas temáticas:

    ```
    ImageCreation.Infrastructure/
    ├── Dependencies/
    └── Services/
        ├── AI/                          # Para servicios de Inteligencia Artificial (generación y clasificación)
        │   ├── OpenAI/                  # OpenAI (API Pública) y Azure OpenAI
        │   ├── Google/                  # Google Generative AI (Imagen y modelos Gemini)
        │   ├── Stability/               # Stability AI (Stable Diffusion)
        │   ├── HuggingFace/             # Hugging Face Inference API
        │   └── Vision/                  # Clasificación de imágenes (ej. Azure Vision)
        ├── Caching/                     # Para servicios de caché (ej. Redis)
        ├── Data/                        # Para servicios de acceso a datos (ej. DapperRepository)
        ├── EventSourcing/               # Para servicios relacionados con Event Store DB
        ├── Utilities/                   # Para servicios auxiliares o transversales
        └── Factories/                   # Para las fábricas de servicios
    ```

* **Componentes Clave (en su nueva organización)**:
    * **`Services/AI/OpenAI/PublicOpenAiService.cs`**: Implementación de `IOpenAiService` para la API pública de OpenAI.
    * **`Services/AI/OpenAI/AzureOpenAiService.cs`**: Implementación de `IOpenAiService` para Azure OpenAI.
    * **`Services/AI/Stability/StabilityAIService.cs`**: Implementación de `IOpenAiService` para la API de Stability AI (ahora con `multipart/form-data` y modelo configurable).
    * **`Services/AI/Google/GoogleGenerativeAIService.cs` (Unificado)**: Reemplaza a `GoogleCloudAIService` y `GeminiProImageService`.
        * **Propósito**: Genera imágenes con modelos **Imagen** de Google (ej. `imagen-4.0-generate-preview-06-06`) a través de la Generative Language API. Soporta la API Key como parámetro de consulta y el formato de solicitud JSON específico para Imagen.
        * **Consideración**: Los modelos Imagen **requieren facturación habilitada** en el proyecto de Google Cloud.
        * **Nota sobre Gemini Pro/Flash**: Este servicio también sería el punto de integración para modelos de lenguaje Gemini (como `gemini-pro` o `gemini-2.0-flash`) si se extendieran sus capacidades para generación de texto o refinamiento de prompts, aunque no generen imágenes visuales directamente en Base64.
    * **`Services/AI/HuggingFace/HuggingFaceService.cs`**: Implementación de `IOpenAiService` para un modelo alojado a través de la Inference API de Hugging Face.
    * **`Services/AI/Vision/AzureVisionClassifierService.cs`**: Implementación de `IImageClassifierService` para la clasificación de imágenes con Azure AI Vision.
    * **`Services/Caching/RedisCacheService.cs`**: Implementación de `ICacheService` para interactuar con Redis.
    * **`Services/Data/DapperRepository.cs`**: Implementación de `IDapperRepository` para SQL Server usando Dapper. Ahora soporta la columna `PlatformUsed`.
    * **`Services/EventSourcing/EventStoreService.cs`**: Implementación de `IEventStore` para publicar eventos en Event Store DB.
    * **`Services/EventSourcing/EventStoreSubscriptionService.cs`**: `IHostedService` que gestiona las suscripciones a Event Store DB y distribuye eventos a los Proyectores, usando `IServiceScope` por evento.
    * **`Services/Utilities/UrlToBase64Converter.cs`**: Implementación de `IUrlConverterService` para descargar y convertir URLs a Base64.
    * **`Services/Factories/OpenAiServiceFactory.cs`**: Implementación de `IOpenAiServiceFactory` que ahora utiliza `IServiceProvider` para la **inicialización perezosa** de los servicios de IA, permitiendo el arranque de la aplicación incluso con credenciales faltantes para algunos servicios.

## 3. Flujo de Datos y Comunicación (Ejemplos)

### 3.1. Generación de Imágenes (Comando `CreateImageCommand`)

1.  **`ImageCreation.Api`**: Cliente envía `POST /api/Images/generate`. `ImagesController` recibe la petición, valida y crea `CreateImageCommand`.
2.  **`ImageCreation.Application`**: El controlador invoca `CreateImageCommandHandler`.
3.  `CreateImageCommandHandler` utiliza `IOpenAiServiceFactory` para obtener `IOpenAiService` (Public, Azure, Stability, Google, HuggingFace). La selección se basa en `platformRequested` del comando.
4.  El servicio de IA seleccionado (ej., `StabilityAIService` o `GoogleGenerativeAIService`) genera la imagen. Si el servicio de Google Imagen es seleccionado y la facturación no está habilitada en el proyecto de GCP, la API rechazará la solicitud. Para los modelos de lenguaje de Gemini, se espera una respuesta textual, y la implementación actual simulará una imagen Base64 para compatibilidad con la interfaz.
5.  `CreateImageCommandHandler` crea un `ImageRecord` (ahora incluyendo la `PlatformUsed`) y publica un `ImageCreatedEvent` en Event Store DB a través de `IEventStore`.
6.  **`ImageCreation.Infrastructure`**: `EventStoreService` serializa y persiste el evento.
7.  **`ImageCreation.Infrastructure` (Servicio de Fondo)**: `EventStoreSubscriptionService` consume el `ImageCreatedEvent` de Event Store DB.
8.  **`ImageCreation.Application`**: `EventStoreSubscriptionService` delega el evento a `ImageRecordProjector`.
9.  `ImageRecordProjector` reconstruye `ImageRecord` (incluyendo `PlatformUsed`), lo persiste de forma idempotente en SQL (`IDapperRepository`) y guarda el `ImageDto` (también con `PlatformUsed`) en Redis (`ICacheService`).
10. **`ImageCreation.Api`**: `ImagesController` devuelve `201 Created` con el `ImageDto`.

### 3.2. Consulta de Imágenes (Consulta `GetImageByIdQuery`)

1.  **`ImageCreation.Api`**: Cliente envía `GET /api/Images/{id}`. `ImagesController` crea `GetImageByIdQuery`.
2.  **`ImageCreation.Application`**: El controlador invoca `GetImageByIdQueryHandler`.
3.  `GetImageByIdQueryHandler` primero intenta recuperar el `ImageDto` de `ICacheService` (Redis).
4.  Si no está en caché, `GetImageByIdQueryHandler` consulta `IDapperRepository` para obtener el `ImageRecord` desde la base de datos SQL.
5.  Si se encuentra en SQL, se convierte a `ImageDto` (incluyendo `PlatformUsed`) y se almacena en caché.
6.  **`ImageCreation.Api`**: `ImagesController` devuelve `200 OK` con `ImageDto` o `404 Not Found`.

## 4. Patrones de Diseño Clave

* **CQRS (Command Query Responsibility Segregation)**: Clara separación de responsabilidades entre las operaciones de escritura (comandos) y lectura (consultas), cada una con sus propios modelos y handlers.
* **Event Sourcing**: Event Store DB es la fuente de verdad. Todos los cambios de estado se registran como una secuencia inmutable de Eventos de Dominio.
* **Entidades y Value Objects**: Modelan el dominio, encapsulando estado, comportamiento y validaciones. La adición del Value Object `Platform` para la plataforma de generación es un ejemplo.
* **Proyecciones / Vistas Materializadas**: Modelos de lectura optimizados que se construyen y mantienen actualizados asíncronamente a partir de los eventos de dominio.
* **Inyección de Dependencias (DI)**: Utilizado extensivamente para desacoplar los componentes, inyectando interfaces en lugar de implementaciones concretas.
* **Principios SOLID**:
    * **Single Responsibility Principle (SRP)**: Cada clase tiene una única razón para cambiar.
    * **Open/Closed Principle (OCP)**: Las entidades y módulos están abiertos para extensión, pero cerrados para modificación.
    * **Liskov Substitution Principle (LSP)**: Las implementaciones de interfaces pueden ser sustituidas sin romper el comportamiento.
    * **Dependency Inversion Principle (DIP)**: Las capas de alto nivel (Application) no dependen de las capas de bajo nivel (Infrastructure); ambas dependen de abstracciones (interfaces).
* **Repository Pattern**: Abstrae las operaciones de persistencia de datos (en `DapperRepository`).
* **Cache-Aside Pattern**: Estrategia para usar la caché, visible en los Query Handlers.
* **Fábrica Abstracta (Factory Pattern)**: `IOpenAiServiceFactory` para la creación de servicios de IA, ahora con **inicialización perezosa**.
* **Hosted Services**: Para ejecutar lógica de fondo (ej. `EventStoreSubscriptionService`).
* **Idempotencia**: Crucial en los proyectores para manejar eventos duplicados en un sistema distribuido.

## 5. Tecnologías Utilizadas

* **ASP.NET Core**: Framework para construir la API.
* **.NET (C#)**: Lenguaje de programación.
* **Event Store DB**: Base de datos de Event Sourcing.
* **SQL Server**: Base de datos relacional para vistas materializadas.
* **Redis**: Servicio de caché.
* **Dapper**: Micro-ORM para acceso a datos SQL.
* **OpenAI SDK (.NET)**: Para interactuar con OpenAI API.
* **Azure AI SDKs**: Para interactuar con Azure OpenAI y Azure Vision.
* **Google.AI.GenerativeLanguage NuGet Package**: Para interactuar con la Generative Language API de Google (incluyendo modelos Gemini e Imagen).
* **Newtonsoft.Json**: Para la serialización/deserialización de eventos de dominio (para compatibilidad con Event Store DB).
* **Microsoft.Extensions.Logging**: Para el registro de logs.

## 6. Consideraciones y Futuras Mejoras

* **Observabilidad**: Añadir métricas, tracing distribuido (ej. OpenTelemetry) para una mejor monitorización.
* **Manejo de Transacciones**: Aunque Event Sourcing gestiona la fuente de verdad, la eventual consistencia de las vistas materializadas requiere estrategias de reintento y monitoreo de errores.
* **Seguridad**: Implementar autenticación y autorización para la API.
* **Despliegue**: Contenerización con Docker y orquestación con Kubernetes para un despliegue y escalado robusto.
* **Manejo de Errores Avanzado**: Implementar colas de mensajes muertas (DLQ) para eventos que fallan persistentemente en los proyectores.
* **Versionado de Eventos**: Implementar una estrategia explícita para el versionado de eventos de dominio a medida que la aplicación evoluciona.
* **Políticas de Contenido de IA**: Continuar siendo consciente de las estrictas políticas de contenido de los proveedores de IA, especialmente Google, que pueden rechazar prompts por razones de seguridad o ética.
* **Modelos de Pago**: Los modelos avanzados de generación de imágenes como Google Imagen a menudo requieren facturación habilitada en las cuentas de proveedor.
