using ImageCreation.Application.Interfaces; // Para IQuery

using System;

namespace ImageCreation.Application.Queries
{
   public class GetImageByIdQuery : IQuery
   {
      public Guid Id { get; }

      public GetImageByIdQuery(Guid id)
      {
         Id = id;
      }
   }
}