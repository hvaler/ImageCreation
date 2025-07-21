using ImageCreation.Application.Interfaces; // Para IQuery

using System;

namespace ImageCreation.Application.Queries
{
   public class GetClassifiedImageByIdQuery : IQuery
   {
      public Guid Id { get; }

      public GetClassifiedImageByIdQuery(Guid id)
      {
         Id = id;
      }
   }
}