using Azure.Search.Documents.Indexes;
using Microsoft.AspNetCore.OutputCaching;
using KnowledgeAPI.Common;
using KnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System;

namespace KnowledgeAPI.Q.Categories.Model
{
    public class CatDto: IDisposable
    {
        public string TopId { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string? ParentId { get; set; } // Parent Category Id, it is null for Top Category Id

        public string Title { get; set; }
        public int Level { get; set; }

        public CatDto()
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

    }
}



