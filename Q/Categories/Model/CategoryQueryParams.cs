using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using KnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Text.Json;

namespace KnowledgeAPI.Q.Categories.Model
{
    public class CategoryQueryParams
    {
        public string Workspace { get; set; } = "DEMO";
        public string TopId { get; set; } = "MTS";
        public string Id { get; set; } = "REMOTECTRLS";

        public int StartCursor { get; set; } = 0;
        public int PageSize { get; set; } = 20;
        public string? IncludeQuestionId { get; set; } = null;

        public CategoryQueryParams()
        {
        }

   
        protected string PartitionKey {    
            get { 
                return Workspace + "/" + TopId;
            } 
        }

        public void Deconstruct(out string workspace, out string topId, out string partitionKey, out string id, 
            out int startCursor, out int pageSize, out string? includeQuestionId)
        {
            workspace = Workspace;
            topId = TopId;
            partitionKey = Workspace + "/" + TopId;
            id = Id;
            startCursor = StartCursor;
            pageSize = PageSize;
            includeQuestionId = IncludeQuestionId;
        }

       
    }
 
}
