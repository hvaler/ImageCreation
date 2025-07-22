# ImageCreation: Generación y Clasificación de Imágenes impulsada por IA

**ImageCreation** es una aplicación moderna que demuestra una arquitectura basada en **CQRS (Command Query Responsibility Segregation)** y **Event Sourcing** para la generación y clasificación de imágenes utilizando tecnologías de Inteligencia Artificial.

## 🚀 Características Principales

* **Generación de Imágenes**: Crea imágenes a partir de descripciones textuales utilizando diferentes proveedores de IA: OpenAI (DALL-E), Azure OpenAI, Stability AI (Stable Diffusion), Google Generative AI (modelos Imagen y Gemini), y Hugging Face.
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
    * [Stability AI](https://stability.ai/) (Stable Diffusion)
    * [Google Generative AI](https://ai.google.dev/) (Modelos Imagen y Gemini)
    * [Hugging Face](https://huggingface.co/)
* **Clasificación de Imágenes**:
    * [Azure AI Vision](https://azure.microsoft.com/es-es/products/ai-services/ai-vision)
* **Logging**: Microsoft.Extensions.Logging

## 🏗️ Arquitectura de la Aplicación

La aplicación sigue una arquitectura de diseño limpio con las siguientes capas principales:

* **`ImageCreation.Api`**: La capa de presentación que expone la API RESTful.
* **`ImageCreation.Application`**: Contiene la lógica de negocio de la aplicación, definiendo comandos, consultas, DTOs, handlers y proyectores.
* **`ImageCreation.Domain`**: El núcleo de la aplicación, con el modelo de dominio rico, Value Objects y Eventos de Dominio.
* **`ImageCreation.Infrastructure`**: Proporciona las implementaciones concretas para la persistencia de datos (SQL, Event Store DB), la caché (Redis) y la integración con servicios externos de IA. Sus servicios están organizados en subcarpetas para mayor claridad (ej., `AI/OpenAI`, `Data`, `EventSourcing`, etc.).

Para una descripción detallada de la arquitectura, incluyendo flujos de datos y patrones de diseño aplicados, consulta el archivo [`ARCHITECTURE.md`](ARCHITECTURE.md) en este repositorio.

## 🚀 Puesta en Marcha (Desarrollo Local)

Para ejecutar esta aplicación localmente, necesitarás:

1.  **.NET SDK 8.0 o superior**
2.  **Docker Desktop** (para ejecutar SQL Server, Redis y Event Store DB como contenedores)
3.  **Claves API** para los servicios de IA que desees probar (configuradas en `appsettings.Development.json` o variables de entorno).

**Pasos:**

1.  **Clonar el repositorio:**
    ```bash
    git clone [https://github.com/tu-usuario/ImageCreation.git](https://github.com/tu-usuario/ImageCreation.git)
    cd ImageCreation
    ```
2.  **Configurar `appsettings.Development.json`:**
    Asegúrate de que tu `appsettings.Development.json` en el proyecto `ImageCreation.Api` contenga tus cadenas de conexión y claves API. Un ejemplo de la estructura se proporciona a continuación.

    ```json
    // ImageCreation.Api/appsettings.Development.json
    {
       "ConnectionStrings": {
          "DefaultConnection": "Server=localhost,1433;Database=ImagesDb;User ID=sa;Password=Your_Strong_Password_Here;TrustServerCertificate=True;",
          "RedisConnection": "localhost:6379",
          "EventStoreConnection": "esdb://localhost:2113?tls=false"
       },
       "OpenAI": {
          "Platform": "Public",
          "ApiKey": "sk-proj-YOUR_PUBLIC_OPENAI_API_KEY"
       },
       "AzureOpenAI": {
          "Platform": "Azure",
          "Endpoint": "[https://your-azure-openai-resource.openai.azure.com/](https://your-azure-openai-resource.openai.azure.com/)",
          "ApiKey": "YOUR_AZURE_OPENAI_API_KEY",
          "DeploymentName": "dall-e-3"
       },
       "AzureVision": {
          "Endpoint": "[https://your-azure-vision-resource.cognitiveservices.azure.com/](https://your-azure-vision-resource.cognitiveservices.azure.com/)",
          "ApiKey": "YOUR_AZURE_VISION_API_KEY"
       },
       "StabilityAI": {
          "Platform": "Stability",
          "ApiKey": "sk-YOUR_STABILITY_AI_API_KEY",
          "DefaultModel": "core" // Puedes usar "ultra" o "sd3"
       },
       "GoogleGenerativeAI": { // Configuración unificada para Google AI (Imagen y Gemini)
          "ApiKey": "YOUR_GOOGLE_GENERATIVE_AI_API_KEY", // Clave de Google AI Studio (generativelanguage.googleapis.com)
          "DefaultImageModel": "imagen-4.0-generate-preview-06-06", // Modelo para generación de imágenes (requiere facturación)
          "DefaultTextModel": "gemini-pro", // Modelo para generación de texto/conversación
          "ApiVersion": "v1beta"
       },
       "HuggingFace": {
          "Platform": "HuggingFace",
          "Endpoint": "[https://api-inference.huggingface.co/models/stabilityai/stable-diffusion-xl-base-1.0](https://api-inference.huggingface.co/models/stabilityai/stable-diffusion-xl-base-1.0)", // Reemplaza con el modelo que quieras
          "ApiKey": "hf_YOUR_HUGGING_FACE_TOKEN"
       },
       "Logging": {
          "LogLevel": {
             "Default": "Information",
             "Microsoft.AspNetCore": "Warning"
          }
       }
    }
    ```
    **Importante**: Reemplaza todos los valores `YOUR_..._KEY` o `your-...-resource` con tus propias credenciales y configuraciones.

3.  **Instalar Paquetes NuGet (si es la primera vez o si hay cambios):**
    Asegúrate de que los paquetes necesarios estén instalados en tus proyectos. Por ejemplo, `Google.AI.GenerativeLanguage` para la integración de Google AI, `StackExchange.Redis` para Redis, `Dapper` para SQL, etc.

4.  **Iniciar los Contenedores de Docker:**
    Asegúrate de que SQL Server, Redis y Event Store DB estén corriendo. Puedes usar `docker-compose.yml` si tienes uno configurado para tu proyecto. Si no:
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
        Una vez que SQL Server esté corriendo, crea la base de datos `ImagesDb` y las tablas `Images` y `ClassifiedImages`. **Asegúrate de añadir la columna `PlatformUsed` a la tabla `Images`**.
        ```sql
        -- SQL para la tabla Images (ejemplo, incluye PlatformUsed)
        CREATE TABLE Images (
            Id UNIQUEIDENTIFIER PRIMARY KEY,
            Description NVARCHAR(500) NOT NULL,
            Base64Data NVARCHAR(MAX) NOT NULL,
            PlatformUsed NVARCHAR(50) NOT NULL DEFAULT 'Public', -- ¡NUEVA COLUMNA!
            CreatedAt DATETIME2 NOT NULL
        );

        -- SQL para la tabla ClassifiedImages
        CREATE TABLE ClassifiedImages (
            Id UNIQUEIDENTIFIER PRIMARY KEY,
            OriginalUrl NVARCHAR(MAX) NOT NULL,
            ClassifiedImageBase64 NVARCHAR(MAX) NOT NULL,
            ClassificationResult NVARCHAR(255) NOT NULL,
            ClassifiedAt DATETIME2 NOT NULL
        );
        ```

5.  **Ejecutar la Aplicación ASP.NET Core:**
    Navega al directorio del proyecto `ImageCreation.Api` y ejecuta:
    ```bash
    dotnet run
    ```
    La API se iniciará, usualmente en `https://localhost:7000` (o el puerto que configure tu proyecto).

6.  **Acceder a Swagger UI y Probar:**
    Abre tu navegador y ve a `https://localhost:7000/swagger`. Podrás interactuar con los endpoints de la API.
    * Para generar imágenes, usa `POST /api/Images/generate`. En el cuerpo de la solicitud, especifica la plataforma deseada en `platformRequested` (ej., `"Public"`, `"Azure"`, `"Stability"`, `"Google"`, `"HuggingFace"`). Recuerda que algunos modelos (como Google Imagen) pueden requerir facturación activa y que las APIs de texto de Gemini tienen estrictas políticas de contenido para prompts.

## 🤝 Contribución

¡Las contribuciones son bienvenidas! Si encuentras un error o tienes una idea para una mejora, por favor, abre un "issue" o envía un "pull request".

## 📄 Licencia

Este proyecto está bajo la [Licencia MIT](https://opensource.org/licenses/MIT).