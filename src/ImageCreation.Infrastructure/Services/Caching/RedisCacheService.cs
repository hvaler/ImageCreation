using ImageCreation.Application.Interfaces;
using ImageCreation.Infrastructure.Interfaces;

using Microsoft.Extensions.Configuration;

using StackExchange.Redis;

namespace ImageCreation.Infrastructure.Services.Caching
{
   public class RedisCacheService : ICacheService
   {
      private readonly IDatabase _db;

      public RedisCacheService(IConfiguration config)
      {
         var conn = config.GetConnectionString("RedisConnection");
         if (string.IsNullOrEmpty(conn))
         {
            throw new ArgumentNullException(nameof(conn), "Redis connection string cannot be null or empty.");
         }
         var redis = ConnectionMultiplexer.Connect(conn);
         _db = redis.GetDatabase();
      }

      public Task SetAsync(string key, string value) => _db.StringSetAsync(key, value);

      public async Task<string?> GetAsync(string key) => await _db.StringGetAsync(key);
   }
}