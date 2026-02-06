using Azure.Search.Documents.Indexes;
using Microsoft.AspNetCore.OutputCaching;
using KnowledgeAPI.Common;
using KnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System;

namespace KnowledgeAPI.Q.Categories.Model
{
    public class CatQuestEmbedded 
    {

        [SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        private string Type { get; set; }

        [VectorSearchField()]
        private List<float>? vectors { get; set; }

        [SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        private string Title { get; set; }


        public CatQuestEmbedded(Question question)
        {
            Type = question.Type;
            Title = question.Title;
        }

        public CatQuestEmbedded(Category category)
        {
            Type = category.Type;
            Title = category.Title;
        }

        public CatQuestEmbedded(string type, string userQuery)
        {
            Type = type;
            Title = userQuery;
        }
    }
}



