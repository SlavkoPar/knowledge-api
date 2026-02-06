using KnowledgeAPI.Q.Categories.Model;
using Newtonsoft.Json;
using System.Diagnostics.Metrics;


namespace KnowledgeAPI.Q.Questions.Model
{

    public class QuestionKeyDto
    {
        public string TopId { get; set; }
        public string Id { get; set; }

        public QuestionKeyDto()
        {
        }
        public QuestionKeyDto(QuestionKey questionKey)
        {
            TopId = questionKey.TopId;
            Id = questionKey.Id;
        }

    }

    public class QuestionKey
    {
        public string Workspace { get; set; }
        public string TopId { get; set; }
        public string Id { get; set; }
        public string? ParentId { get; set; }

        public QuestionKey()
        {
        }

        public QuestionKey(string workspace, string topId, string? parentId, string id)
        {
            Workspace = workspace;  
            TopId = topId;
            Id = id;
            ParentId = parentId;
        }

        public QuestionKey(CategoryKey categoryKey)
        {
            Workspace = categoryKey.Workspace;
            TopId = categoryKey.TopId;
            Id = string.Empty;
            ParentId = categoryKey.Id;
        }

        public QuestionKey(Question question)
        {
            Workspace = question.Workspace;
            TopId = question.TopId;
            Id = question.Id;
            ParentId = question.ParentId!;
        }


        protected string PartitionKey
        {
            get
            {
                return Workspace + "/" + TopId;
            }
        }

        public void Deconstruct(out string workspace, out string topId, out string partitionKey, out string id, out string? parentId)
        {
            workspace = Workspace;
            topId = TopId;
            partitionKey = PartitionKey;
            id = Id;
            parentId = ParentId;
        }

    }

}
