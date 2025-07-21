# Proyecto ImageCreationPure

## Visión General

Este proyecto es una aplicación de ejemplo construida con **ASP.NET Core (.NET 9)**, que demuestra la implementación de una arquitectura limpia basada en **Domain-Driven Design (DDD)**, **CQRS (Command Query Responsibility Segregation)** y **Event Sourcing**. Permite a los usuarios generar imágenes a partir de descripciones y clasificar imágenes existentes utilizando servicios de IA externos.

## Características Clave

* **Generación de Imágenes:** Crea imágenes basadas en descripciones de texto (utilizando OpenAI o Azure OpenAI).
* **Clasificación de Imágenes:** Clasifica imágenes existentes desde una URL (utilizando Azure AI Vision).
* **Event Sourcing:** Todos los cambios de estado se registran como eventos inmutables en EventStoreDB.
* **CQRS:** Separación clara entre los flujos de comandos (escritura) y las consultas (lectura).
* **Proyecciones Asíncronas:** Los modelos de lectura (SQL Server, Redis Cache) se construyen de forma asíncrona a partir de los eventos del Event Store.
* **Arquitectura de Capas Limpia:** Organización modular con capas de Dominio, Aplicación, Infraestructura y API.

## Arquitectura Detallada

La solución se estructura en los siguientes proyectos (capas):

```mermaid
flowchart TD
    subgraph Client_UI["Client UI"]
        A["API Client: e.g., Browser, Mobile App"]
    end
    subgraph Presentation_Layer_Api["Presentation Layer (ImageCreation.Api)"]
        B["ImagesController"]
    end
    subgraph Application_Layer_App["Application Layer (ImageCreation.Application)"]
        subgraph Commands["Commands"]
            C1["CreateImageCommand"]
            C2["ClassifyImageCommand"]
            CH1["CreateImageCommandHandler"]
            CH2["ClassifyImageCommandHandler"]
        end
        subgraph Queries["Queries"]
            Q1["GetImageByIdQuery"]
            Q2["GetImageBase64Query"]
            Q3["GetClassifiedImageByIdQuery"]
            QH1["GetImageByIdQueryHandler"]
            QH2["GetImageBase64QueryHandler"]
            QH3["GetClassifiedImageByIdQueryHandler"]
        end
        subgraph Events_Projections["Events & Projections"]
            DE["IDomainEvent & Concrete Events: e.g. ImageCreatedEvent"]
            P1["ImageRecordProjector"]
            P2["ClassifiedImageRecordProjector"]
        end
        subgraph Application_Interfaces["Application Interfaces"]
            AI1["ICommandHandler"]
            AI2["IQueryHandler"]
            AI3["IEventStore"]
            AI4["IDapperRepository"]
            AI5["ICacheService"]
            AI6["IOpenAiService"]
            AI7["IImageClassifierService"]
            AI8["IUrlConverterService"]
            AI9["IOpenAiServiceFactory"]
        end
        subgraph DTOs["DTOs"]
            D1["ImageDto"]
            D2["ClassifiedImageDto"]
        end
    end
    subgraph Domain_Layer["Domain Layer (ImageCreation.Domain)"]
        subgraph Entities_Value_Objects["Entities & Value Objects"]
            E1["ImageRecord"]
            E2["ClassifiedImageRecord"]
            VO1["ImageDescription"]
            VO2["Base64Data"]
            VO3["ImageUrl"]
            VO4["ClassificationResult"]
        end
    end
    subgraph Infrastructure_Layer["Infrastructure Layer (ImageCreation.Infrastructure)"]
        subgraph Persistence_Messaging["Persistence & Messaging"]
            IP1["EventStoreService"]
            IP2["DapperRepository"]
            IP3["RedisCacheService"]
            IP4["EventStoreSubscriptionService: Hosted Service"]
        end
        subgraph External_Services["External Services"]
            IE1["PublicOpenAiService"]
            IE2["AzureOpenAiService"]
            IE3["AzureVisionClassifierService"]
            IE4["UrlToBase64Converter"]
        end
        subgraph Factories["Factories"]
            IF1["OpenAiServiceFactory"]
        end
    end
    subgraph External_Systems["External Systems"]
        ES1["EventStoreDB"]
        ES2["SQL Server Database"]
        ES3["Redis Cache"]
        ES4["OpenAI/Azure OpenAI API"]
        ES5["Azure AI Vision API"]
    end
    subgraph Testing_Layer["Testing Layer (ImageCreation.Tests)"]
        T["Unit/Integration Tests"]
    end
    A -- HTTP Requests (Commands/Queries) --> B
    B -- Dispatches ClassifyImageCommand --> CH2
    B -- Dispatches GetImageByIdQuery --> QH1
    B -- Dispatches GetImageBase64Query --> QH2
    B -- Dispatches GetClassifiedImageByIdQuery --> QH3
    CH1 -- Requests IOpenAiService from Factory --> AI9
    CH1 -- Publishes IDomainEvent --> AI3
    CH2 -- Requests IUrlConverterService for Base64 conversion --> AI8
    CH2 -- Requests IImageClassifierService for classification --> AI7
    CH2 -- Publishes IDomainEvent --> AI3
    AI9 -- Uses OpenAiServiceFactory --> IF1
    IF1 -- Returns PublicOpenAiService --> IE1
    IF1 -- Returns AzureOpenAiService --> IE2
    AI7 -- Uses AzureVisionClassifierService --> IE3
    IE3 -- Requests IUrlConverterService for image download --> AI8
    QH1 -- Reads from ICacheService --> AI5
    QH1 -- Reads from IDapperRepository --> AI4
    QH2 -- Reads from ICacheService --> AI5
    QH2 -- Reads from IDapperRepository --> AI4
    QH3 -- Reads from ICacheService --> AI5
    QH3 -- Reads from IDapperRepository --> AI4
    AI3 -- Implemented by EventStoreService --> IP1
    AI4 -- Implemented by DapperRepository --> IP2
    AI5 -- Implemented by RedisCacheService --> IP3
    AI6 -- Implemented by PublicOpenAiService --> IE1
    AI6 -- Implemented by AzureOpenAiService --> IE2
    AI7 -- Implemented by AzureVisionClassifierService --> IE3
    AI8 -- Implemented by UrlToBase64Converter --> IE4
    AI9 -- Implemented by OpenAiServiceFactory --> IF1
    IP1 -- Persists Events --> ES1
    IP4 -- Subscribes to Events --> ES1
    ES1 -- Publishes Events to Subscription Service --> IP4
    IP4 -- Routes ImageCreatedEvent --> P1
    IP4 -- Routes ImageClassifiedEvent --> P2
    P1 -- Writes to IDapperRepository (SQL Read Model) --> AI4
    P1 -- Writes to ICacheService (Redis Read Model) --> AI5
    P2 -- Writes to IDapperRepository (SQL Read Model) --> AI4
    P2 -- Writes to ICacheService (Redis Read Model) --> AI5
    IP2 -- Interacts with SQL Server --> ES2
    IP3 -- Interacts with Redis --> ES3
    IE1 -- Calls OpenAI/Azure OpenAI API --> ES4
    IE2 -- Calls OpenAI/Azure OpenAI API --> ES4
    IE3 -- Calls Azure AI Vision API --> ES5
    IE4 -- (Direct HTTP Call from HttpClient) --> B
    E1 <--> VO1
    E1 <--> VO2
    E2 <--> VO3
    E2 <--> VO2
    E2 <--> VO4
    CH1 -- Creates/Uses ImageRecord --> E1
    CH2 -- Creates/Uses ClassifiedImageRecord --> E2
    E1 -- Creates ImageCreatedEvent --> DE
    E2 -- Creates ImageClassifiedEvent --> DE
    B --> D1
    B -- Returns DTOs --> D2
    QH1 --> D1
    QH2 --> D1
    QH3 --> D2
