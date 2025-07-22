# ImageCreation: Generación y Clasificación de Imágenes impulsada por IA

**ImageCreation** es una aplicación moderna que demuestra una arquitectura basada en **CQRS (Command Query Responsibility Segregation)** y **Event Sourcing** para la generación y clasificación de imágenes utilizando tecnologías de Inteligencia Artificial.

## 🚀 Características Principales

* **Generación de Imágenes**: Crea imágenes a partir de descripciones textuales utilizando diferentes proveedores de IA (OpenAI, Azure OpenAI).
* **Clasificación de Imágenes**: Analiza imágenes de una URL y las clasifica en categorías predefinidas (ej. "Food", "Person") utilizando servicios de visión artificial (Azure AI Vision).
* **API RESTful**: Interfaz programática para interactuar con todas las funcionalidades de la aplicación.
* **Event Sourcing**: Todos los cambios de estado se registran como eventos inmutables en Event Store DB, proporcionando una auditoría completa y capacidades de "viaje en el tiempo".
* **Vistas Materializadas**: Datos optimizados para consultas (en SQL Server y Redis Cache) que se construyen de forma asíncrona a partir de los eventos.
* **Diseño Modular y Extensible**: Arquitectura basada en capas que facilita el mantenimiento y la evolución del sistema.

## ⚙️ Tecnologías Utilizadas

* **Backend**: ASP.NET Core (C#)
* **Base de Datos de Eventos**: [Event Store DB](https://www.eventstore.com/)
* **Base de Datos de Lectura (Vistas Materializadas)**: SQL Server (con Dapper como micro-ORM)
* **Caché**: Redis
* **Generación de Imágenes**:
    * [OpenAI API](https://openai.com/)
    * [Azure OpenAI Service](https://azure.microsoft.com/es-es/products/ai-services/openai-service)
* **Clasificación de Imágenes**:
    * [Azure AI Vision](https://azure.microsoft.com/es-es/products/ai-services/ai-vision)
* **Logging**: Microsoft.Extensions.Logging

## 🏗️ Arquitectura de la Aplicación

La aplicación sigue una arquitectura de diseño limpio con las siguientes capas principales:

* **`ImageCreation.Api`**: La capa de presentación que expone la API RESTful.
* **`ImageCreation.Application`**: Contiene la lógica de negocio de la aplicación, definiendo comandos, consultas, DTOs, handlers y proyectores.
* **`ImageCreation.Domain`**: El núcleo de la aplicación, con el modelo de dominio rico, Value Objects y Eventos de Dominio.
* **`ImageCreation.Infrastructure`**: Proporciona las implementaciones concretas para la persistencia de datos (SQL, Event Store DB), la caché (Redis) y la integración con servicios externos de IA.

Para una descripción detallada de la arquitectura, incluyendo flujos de datos y patrones de diseño aplicados, consulta el archivo [`ARCHITECTURE.md`](ARCHITECTURE.md) en este repositorio.

## 🚀 Puesta en Marcha (Desarrollo Local)

Para ejecutar esta aplicación localmente, necesitarás:

1.  **.NET SDK 8.0 o superior**
2.  **Docker Desktop** (para ejecutar SQL Server, Redis y Event Store DB como contenedores)
3.  **Claves API** para OpenAI, Azure OpenAI y Azure Vision (configuradas en `appsettings.Development.json` o variables de entorno).

**Pasos:**

1.  **Clonar el repositorio:**
    ```bash
    git clone [https://github.com/tu-usuario/ImageCreation.git](https://github.com/tu-usuario/ImageCreation.git)
    cd ImageCreation
    ```
2.  **Configurar `appsettings.Development.json`:**
    Asegúrate de que tu `appsettings.Development.json` en el proyecto `ImageCreation.Api` contenga tus cadenas de conexión y claves API. Un ejemplo base se proporciona en `appsettings.json`, pero para desarrollo, usa el archivo `appsettings.Development.json`.

    ```json
    // ImageCreation.Api/appsettings.Development.json
    {
       "ConnectionStrings": {
          "DefaultConnection": "Server=localhost,1433;Database=ImagesDb;User ID=sa;Password=Your_Strong_Password_Here;TrustServerCertificate=True;",
          "RedisConnection": "localhost:6379",
          "EventStoreConnection": "esdb://localhost:2113?tls=false"
       },
       "OpenAI": {
          "Platform": "Public", // o "Azure"
          "ApiKey": "sk-proj-YOUR_PUBLIC_OPENAI_API_KEY"
       },
       "AzureOpenAI": {
          "Platform": "Azure",
          "Endpoint": "[https://your-azure-openai-resource.openai.azure.com/](https://your-azure-openai-resource.openai.azure.com/)",
          "ApiKey": "YOUR_AZURE_OPENAI_API_KEY",
          "DeploymentName": "dall-e-3" // Nombre de tu despliegue DALL-E en Azure
       },
       "AzureVision": {
          "Endpoint": "[https://your-azure-vision-resource.cognitiveservices.azure.com/](https://your-azure-vision-resource.cognitiveservices.azure.com/)",
          "ApiKey": "YOUR_AZURE_VISION_API_KEY"
       },
       "Logging": {
          "LogLevel": {
             "Default": "Information",
             "Microsoft.AspNetCore": "Warning"
          }
       }
    }
    ```
    **Importante**: Reemplaza `Your_Strong_Password_Here`, `YOUR_PUBLIC_OPENAI_API_KEY`, `your-azure-openai-resource.openai.azure.com/`, `YOUR_AZURE_OPENAI_API_KEY`, `dall-e-3`, `your-azure-vision-resource.cognitiveservices.azure.com/` y `YOUR_AZURE_VISION_API_KEY` con tus propios valores.

3.  **Iniciar los Contenedores de Docker:**
    Necesitas ejecutar SQL Server, Redis y Event Store DB. Si tienes un `docker-compose.yml` en tu repositorio, puedes usar:
    ```bash
    docker-compose up -d
    ```
    Si no, necesitarás iniciar cada contenedor individualmente.

    * **SQL Server (ejemplo):**
        ```bash
        docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Your_Strong_Password_Here" -p 1433:1433 --name sqlserver_images -d [mcr.microsoft.com/mssql/server:2019-latest](https://mcr.microsoft.com/mssql/server:2019-latest)
        ```
    * **Redis (ejemplo):**
        ```bash
        docker run -p 6379:6379 --name redis_images -d redis/redis-stack-server:latest
        ```
    * **Event Store DB (ejemplo):**
        ```bash
        docker run -p 2113:2113 -p 1113:1113 --name eventstore_images -e EVENTSTORE_ENABLE_EXTERNAL_TCP=true -e EVENTSTORE_INSECURE=true -e EVENTSTORE_RUN_PROJECTIONS=All -d eventstore/eventstore:latest
        ```
    * **Crear la Base de Datos y Tablas (SQL Server):**
        Una vez que SQL Server esté corriendo, necesitarás crear la base de datos `ImagesDb` y las tablas `Images` y `ClassifiedImages`. Puedes usar un cliente SQL (Azure Data Studio, SSMS) o un script de migración si lo tienes.
        Ejemplo de DDL (Data Definition Language) básico para las tablas:
        ```sql
        -- ImagesDb
        CREATE TABLE Images (
            Id UNIQUEIDENTIFIER PRIMARY KEY,
            Description NVARCHAR(500) NOT NULL,
            Base64Data NVARCHAR(MAX) NOT NULL,
            CreatedAt DATETIME2 NOT NULL
        );

        CREATE TABLE ClassifiedImages (
            Id UNIQUEIDENTIFIER PRIMARY KEY,
            OriginalUrl NVARCHAR(MAX) NOT NULL,
            ClassifiedImageBase64 NVARCHAR(MAX) NOT NULL,
            ClassificationResult NVARCHAR(255) NOT NULL,
            ClassifiedAt DATETIME2 NOT NULL
        );
        ```

4.  **Ejecutar la Aplicación ASP.NET Core:**
    Navega al directorio del proyecto `ImageCreation.Api` y ejecuta:
    ```bash
    dotnet run
    ```
    La API se iniciará, usualmente en `https://localhost:7000` (o el puerto que configure tu proyecto).

5.  **Acceder a Swagger UI:**
    Abre tu navegador y ve a `https://localhost:7000/swagger` (o el puerto correspondiente). Podrás interactuar con los endpoints de la API.

## 🤝 Contribución

¡Las contribuciones son bienvenidas! Si encuentras un error o tienes una idea para una mejora, por favor, abre un "issue" o envía un "pull request".

## 📄 Licencia

Este proyecto está bajo la Licencia MIT. Consulta el archivo `LICENSE` para más detalles.

---