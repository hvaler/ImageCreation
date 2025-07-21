using ImageCreation.Application.Interfaces; // Para IQuery

using System;

namespace ImageCreation.Application.Queries
{
   public class GetImageBase64Query : IQuery
   {
      public Guid Id { get; }

      public GetImageBase64Query(Guid id)
      {
         Id = id;
      }
   }
}