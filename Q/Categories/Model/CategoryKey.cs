using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using KnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Text.Json;

namespace KnowledgeAPI.Q.Categories.Model
{
    public class CategoryKey
    {
        public string Workspace {  get; set; }
        public string TopId { get; set; }
        public string Id { get; set; }

        public string? ParentId { get; set; }


        public CategoryKey()
        {
        }

        public CategoryKey(CategoryKey categoryKey)
        {
            var (workspace, topId, _, id, parentId) = categoryKey;
            Workspace = workspace;
            TopId = topId;
            Id = id;
            ParentId = parentId;
        }

        public CategoryKey(string workspace, string topId, string id, string? parentId=null)
        {
            Workspace = workspace;
            TopId = topId;
            Id = id;
            ParentId = parentId;
        }

        public CategoryKey(Category category)
        {
            Workspace = category.Workspace;
            TopId = category.TopId;
            Id = category.Id;
            ParentId = category.ParentId;
        }


        public CategoryKey(CategoryRowDto rowDto)
        {
            Workspace = rowDto.Workspace;
            TopId = rowDto.TopId;
            Id = rowDto.Id;
            ParentId = rowDto.ParentId;
        }

        public CategoryKey(QuestionDto rowDto)
        {
            Workspace = rowDto.Workspace;
            TopId = rowDto.TopId;
            Id = rowDto.ParentId!;
            ParentId = null;
        }

        public CategoryKey(Question question)
        {
            Workspace = question.Workspace;
            TopId = question.TopId;
            Id = question.ParentId!;
            ParentId = null;
        }

        public CategoryKey(CategoryDto categoryDto)
        {
            Workspace = categoryDto.Workspace;
            TopId = categoryDto.TopId;
            Id = categoryDto.Id;
            ParentId = categoryDto.ParentId;
        }

       

        protected string PartitionKey {    
            get { 
                return Workspace + "/" + TopId;
            } 
        }

        public void Deconstruct(out string workspace, out string topId, out string partitionKey, out string id, out string? parentId)
        {
            workspace = Workspace;
            topId = TopId;
            partitionKey = Workspace + "/" + TopId;
            id = Id;
            parentId = ParentId;
        }

       
    }
 
}
