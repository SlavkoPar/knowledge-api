using Azure.Search.Documents.Indexes;
using Microsoft.AspNetCore.OutputCaching;
using KnowledgeAPI.A.Answers.Model;
using KnowledgeAPI.Common;
using Newtonsoft.Json;
using System;

namespace KnowledgeAPI.A.Groups.Model
{
    public class GrpQuestEmbedded 
    {

        [SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        private string Type { get; set; }

        [VectorSearchField()]
        private List<float>? vectors { get; set; }

        [SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        private string Title { get; set; }


        public GrpQuestEmbedded(Answer answer)
        {
            Type = answer.Type;
            Title = answer.Title;
        }

        public GrpQuestEmbedded(Group group)
        {
            Type = group.Type;
            Title = group.Title;
        }

        public GrpQuestEmbedded(string type, string userQuery)
        {
            Type = type;
            Title = userQuery;
        }
    }
}



