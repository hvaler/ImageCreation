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
graph TD
    subgraph Client/UI
        A[API Client (e.g., Browser, Mobile App)]
    end

    subgraph Presentation Layer (ImageCreation.Api)
        B[ImagesController]
    end

    subgraph Application Layer (ImageCreation.Application)
        subgraph Commands
            C1[CreateImageCommand]
            C2[ClassifyImageCommand]
            CH1[CreateImageCommandHandler]
            CH2[ClassifyImageCommandHandler]
        end
        subgraph Queries
            Q1[GetImageByIdQuery]
            Q2[GetImageBase64Query]
            Q3[GetClassifiedImageByIdQuery]
            QH1[GetImageByIdQueryHandler]
            QH2[GetImageBase64QueryHandler]
            QH3[GetClassifiedImageByIdQueryHandler]
        end
        subgraph Events & Projections
            DE[IDomainEvent & Concrete Events (e.g.,<br/>ImageCreatedEvent)]
            P1[ImageRecordProjector]
            P2[ClassifiedImageRecordProjector]
        end
        subgraph Application Interfaces
            AI1[ICommandHandler]
            AI2[IQueryHandler]
            AI3[IEventStore]
            AI4[IDapperRepository]
            AI5[ICacheService]
            AI6[IOpenAiService]
            AI7[IImageClassifierService]
            AI8[IUrlConverterService]
            AI9[IOpenAiServiceFactory]
        end
        subgraph DTOs
            D1[ImageDto]
            D2[ClassifiedImageDto]
        end
    end

    subgraph Domain Layer (ImageCreation.Domain)
        subgraph Entities & Value Objects
            E1[ImageRecord]
            E2[ClassifiedImageRecord]
            VO1[ImageDescription]
            VO2[Base64Data]
            VO3[ImageUrl]
            VO4[ClassificationResult]
        end
    end

    subgraph Infrastructure Layer (ImageCreation.Infrastructure)
        subgraph Persistence & Messaging
            IP1[EventStoreService]
            IP2[DapperRepository]
            IP3[RedisCacheService]
            IP4[EventStoreSubscriptionService (Hosted Service)]
        end
        subgraph External Services
            IE1[PublicOpenAiService]
            IE2[AzureOpenAiService]
            IE3[AzureVisionClassifierService]
            IE4[UrlToBase64Converter]
        end
        subgraph Factories
            IF1[OpenAiServiceFactory]
        end
    end

    subgraph External Systems
        ES1[EventStoreDB]
        ES2[SQL Server Database]
        ES3[Redis Cache]
        ES4[OpenAI/Azure OpenAI API]
        ES5[Azure AI Vision API]
    end

    subgraph Testing Layer (ImageCreation.Tests)
        T[Unit/Integration Tests]
    end

    A --> B: HTTP Requests (Commands/Queries)
    B --> CH1: Dispatches CreateImageCommand
    B --> CH2: Dispatches ClassifyImageCommand
    B --> QH1: Dispatches GetImageByIdQuery
    B --> QH2: Dispatches GetImageBase64Query
    B --> QH3: Dispatches GetClassifiedImageByIdQuery

    CH1 --> AI9: Requests IOpenAiService from Factory
    CH1 --> AI3: Publishes IDomainEvent
    CH2 --> AI8: Requests IUrlConverterService for Base64 conversion
    CH2 --> AI7: Requests IImageClassifierService for classification
    CH2 --> AI3: Publishes IDomainEvent

    AI9 --> IF1: Uses OpenAiServiceFactory
    IF1 --> IE1: Returns PublicOpenAiService
    IF1 --> IE2: Returns AzureOpenAiService

    AI7 --> IE3: Uses AzureVisionClassifierService
    IE3 --> AI8: Requests IUrlConverterService for image download

    QH1 --> AI5: Reads from ICacheService
    QH1 --> AI4: Reads from IDapperRepository
    QH2 --> AI5: Reads from ICacheService
s    QH2 --> AI4: Reads from IDapperRepository
    QH3 --> AI5: Reads from ICacheService
    QH3 --> AI4: Reads from IDapperRepository

    AI3 --> IP1: Implemented by EventStoreService
    AI4 --> IP2: Implemented by DapperRepository
    AI5 --> IP3: Implemented by RedisCacheService
    AI6 --> IE1: Implemented by PublicOpenAiService
    AI6 --> IE2: Implemented by AzureOpenAiService
    AI7 --> IE3: Implemented by AzureVisionClassifierService
    AI8 --> IE4: Implemented by UrlToBase64Converter
    AI9 --> IF1: Implemented by OpenAiServiceFactory

    IP1 --> ES1: Persists Events
    IP4 --> ES1: Subscribes to Events

    ES1 --> IP4: Publishes Events to Subscription Service
    IP4 --> P1: Routes ImageCreatedEvent
    IP4 --> P2: Routes ImageClassifiedEvent

    P1 --> AI4: Writes to IDapperRepository (SQL Read Model)
    P1 --> AI5: Writes to ICacheService (Redis Read Model)
    P2 --> AI4: Writes to IDapperRepository (SQL Read Model)
    P2 --> AI5: Writes to ICacheService (Redis Read Model)

    IP2 --> ES2: Interacts with SQL Server
    IP3 --> ES3: Interacts with Redis

    IE1 --> ES4: Calls OpenAI/Azure OpenAI API
    IE2 --> ES4: Calls OpenAI/Azure OpenAI API
    IE3 --> ES5: Calls Azure AI Vision API
    IE4 --> B: (Direct HTTP Call from HttpClient) - not directly to external system, but a utility

    E1 <--> VO1, VO2
    E2 <--> VO3, VO2, VO4

    CH1 --> E1: Creates/Uses ImageRecord
    CH2 --> E2: Creates/Uses ClassifiedImageRecord
    E1 --> DE: Creates ImageCreatedEvent
    E2 --> DE: Creates ImageClassifiedEvent

    B --> D1, D2: Returns DTOs
    QH1 --> D1
    QH2 --> D1
    QH3 --> D2

    T --> B, C1, C2, CH1, CH2, Q1, Q2, Q3, QH1, QH2, QH3, DE, P1, P2, E1, E2, VO1, VO2, VO3, VO4, IP1, IP2, IP3, IP4, IE1, IE2, IE3, IE4, IF1: Tests components and interactions.