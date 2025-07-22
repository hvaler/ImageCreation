// ImageCreation.Infrastructure.Services/DapperRepository.cs
using Dapper;
using ImageCreation.Application.Interfaces;
using ImageCreation.Domain.Entities;
using ImageCreation.Domain.ValueObjects;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

using System.Data;
using System.Threading.Tasks;
using System;

namespace ImageCreation.Infrastructure.Services
{
   public class DapperRepository : IDapperRepository
   {
      private readonly string _connectionString;

      public DapperRepository(IConfiguration config)
      {
         var connectionString = config.GetConnectionString("DefaultConnection");
         if (connectionString is null)
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
         _connectionString = connectionString;
      }

      private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

      public async Task InsertAsync(ImageRecord record)
      {
         using IDbConnection db = CreateConnection();

         var sql = @"
                MERGE INTO Images AS Target
                USING (VALUES (@Id, @Description, @Base64Data, @PlatformUsed, @CreatedAt)) AS Source (Id, Description, Base64Data, PlatformUsed, CreatedAt)
                ON Target.Id = Source.Id
                WHEN MATCHED THEN
                    UPDATE SET
                        Description = Source.Description,
                        Base64Data = Source.Base64Data,
                        PlatformUsed = Source.PlatformUsed, -- ¡NUEVO!
                        CreatedAt = Source.CreatedAt
                WHEN NOT MATCHED BY TARGET THEN
                    INSERT (Id, Description, Base64Data, PlatformUsed, CreatedAt) -- ¡NUEVO!
                    VALUES (Source.Id, Source.Description, Source.Base64Data, Source.PlatformUsed, Source.CreatedAt);"; // ¡NUEVO!

         await db.ExecuteAsync(sql, new
         {
            Id = record.Id,
            Description = record.Description.Value,
            Base64Data = record.Base64Data.Value,
            PlatformUsed = record.PlatformUsed.Value, // ¡NUEVO!
            CreatedAt = record.CreatedAt
         });
      }

      public async Task<ImageRecord?> GetByIdAsync(string id)
      {
         using IDbConnection db = CreateConnection();
         var sql = "SELECT Id, Description, Base64Data, PlatformUsed, CreatedAt FROM Images WHERE Id = @Id"; // ¡NUEVO!

         var resultDto = await db.QuerySingleOrDefaultAsync<ImageRecordDto>(sql, new { Id = id });
         if (resultDto == null)
         {
            return null;
         }

         return new ImageRecord(
                         resultDto.Id,
                         new ImageDescription(resultDto.Description),
                         new Base64Data(resultDto.Base64Data),
                         new Platform(resultDto.PlatformUsed), // ¡NUEVO!
                         resultDto.CreatedAt
                     );
      }

      public async Task InsertClassifiedImageAsync(ClassifiedImageRecord record)
      {
         using IDbConnection db = CreateConnection();

         var sql = @"
                MERGE INTO ClassifiedImages AS Target
                USING (VALUES (@Id, @OriginalUrl, @ClassifiedImageBase64, @ClassificationResult, @ClassifiedAt)) AS Source (Id, OriginalUrl, ClassifiedImageBase64, ClassificationResult, ClassifiedAt)
                ON Target.Id = Source.Id
                WHEN MATCHED THEN
                    UPDATE SET
                        OriginalUrl = Source.OriginalUrl,
                        ClassifiedImageBase64 = Source.ClassifiedImageBase64,
                        ClassificationResult = Source.ClassificationResult,
                        ClassifiedAt = Source.ClassifiedAt
                WHEN NOT MATCHED BY TARGET THEN
                    INSERT (Id, OriginalUrl, ClassifiedImageBase64, ClassificationResult, ClassifiedAt)
                    VALUES (Source.Id, Source.OriginalUrl, Source.ClassifiedImageBase64, Source.ClassificationResult, Source.ClassifiedAt);";

         await db.ExecuteAsync(sql, new
         {
            Id = record.Id,
            OriginalUrl = record.OriginalUrl.Value,
            ClassifiedImageBase64 = record.ClassifiedImageBase64.Value,
            ClassificationResult = record.ClassificationResult.Value,
            ClassifiedAt = record.ClassifiedAt
         });
      }

      public async Task<ClassifiedImageRecord?> GetClassifiedImageByIdAsync(string id)
      {
         using IDbConnection db = CreateConnection();
         var sql = @"SELECT Id,
                               OriginalUrl,
                               ClassifiedImageBase64,
                               ClassificationResult,
                               ClassifiedAt
                        FROM ClassifiedImages WHERE Id = @Id";

         var resultDto = await db.QuerySingleOrDefaultAsync<ClassifiedImageRecordDto>(sql, new { Id = id });

         if (resultDto == null)
         {
            return null;
         }


         return new ClassifiedImageRecord(
             resultDto.Id,
             new ImageUrl(resultDto.OriginalUrl),
             new Base64Data(resultDto.ClassifiedImageBase64),
             new ClassificationResult(resultDto.ClassificationResult),
             resultDto.ClassifiedAt
         );
      }

      // --- Clases DTO privadas internas para mapeo de Dapper ---
      private class ImageRecordDto
      {
         public Guid Id { get; set; }
         public string Description { get; set; } = string.Empty;
         public string Base64Data { get; set; } = string.Empty;
         public string PlatformUsed { get; set; } = string.Empty; // ¡NUEVO!
         public DateTime CreatedAt { get; set; }
      }

      private class ClassifiedImageRecordDto
      {
         public Guid Id { get; set; }
         public string OriginalUrl { get; set; } = string.Empty;
         public string ClassifiedImageBase64 { get; set; } = string.Empty;
         public string ClassificationResult { get; set; } = string.Empty;
         public DateTime ClassifiedAt { get; set; }
      }
   }
}