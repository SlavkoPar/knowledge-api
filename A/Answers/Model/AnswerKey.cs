using KnowledgeAPI.A.Groups.Model;
using Newtonsoft.Json;
using System.Diagnostics.Metrics;


namespace KnowledgeAPI.A.Answers.Model
{

    public class AnswerKeyDto
    {
        public string TopId { get; set; }
        public string Id { get; set; }

        public AnswerKeyDto()
        {
        }
        public AnswerKeyDto(AnswerKey answerKey)
        {
            TopId = answerKey.TopId;
            Id = answerKey.Id;
        }

    }

    public class AnswerKey
    {
        public string Workspace { get; set; }
        public string TopId { get; set; }
        public string Id { get; set; }
        public string? ParentId { get; set; }

        public AnswerKey()
        {
        }

        public AnswerKey(string workspace, string topId, string? parentId, string id)
        {
            Workspace = workspace;  
            TopId = topId;
            Id = id;
            ParentId = parentId;
        }

        public AnswerKey(GroupKey groupKey)
        {
            Workspace = groupKey.Workspace;
            TopId = groupKey.TopId;
            Id = string.Empty;
            ParentId = groupKey.Id;
        }

        public AnswerKey(Answer answer)
        {
            Workspace = answer.Workspace;
            TopId = answer.TopId;
            Id = answer.Id;
            ParentId = answer.ParentId!;
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
