using Knowledge.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using KnowledgeAPI.A.Answers;
using KnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Net;

namespace KnowledgeAPI.A.Answers.Model
{
    //public class AnswerRowDto : RecordDto
    public class AnswerRowDto
    {
        public string Workspace { get; set; }
        public string TopId { get; set; }
        public string Id { get; set; }
        public string? ParentId { get; set; }
        public string Title { get; set; }

        public string? GroupTitle { get; set; }
        public bool? Included {  get; set; }
       
        public AnswerRowDto()
        {
        }

        public AnswerRowDto(AnswerRow answerRow)
        {
            //Console.WriteLine(JsonConvert.SerializeObject(answer));
            TopId = answerRow.TopId;
            Id = answerRow.Id;
            Title = answerRow.Title;
            ParentId = answerRow.ParentId;
            Included = answerRow.Included;
        }

        public AnswerRowDto(Answer answer)
        {
            //Console.WriteLine(JsonConvert.SerializeObject(answer));
            var answerKey = new AnswerKey(answer);
            TopId = answer.TopId;
            Id = answer.Id;
            Title = answer.Title;
            GroupTitle = answer.GroupTitle;
            ParentId = answer.ParentId;
            //
            // We don't modify answer AssignedAnswers through AnswerDto
            //
        }
    }

    public class AnswerDto : RecordDto // AnswerRowDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string? GroupTitle { get; set; }
        public string? OldParentId { get; set; }
        public int Source { get; set; }
        public int Status { get; set; }

        public AnswerDto()
            : base()

        {
        }

        public AnswerDto(Answer answer)
        : base(answer.Created, answer.Modified) //, answer.Archived)
        {
            ////////////////
            // AnswerDto
            TopId = answer.TopId;
            Id = answer.Id;
            Title = answer.Title;
            GroupTitle = answer.GroupTitle;
            ParentId = answer.ParentId;

                       
            Source = answer.Source;
            Status = answer.Status;
        }

        public void Deconstruct(out string workspace, out string topId, out string partitionKey, out string id,
                                out string? oldParentId,  out string? parentId,
                              out string title, out int source, out int status, out WhoWhenDto? modified)

        {
            workspace = Workspace;
            topId = TopId;
            partitionKey = PartitionKey;
            id = Id;
            oldParentId = OldParentId;
            parentId = ParentId;
            title = Title;
            source = Source;
            status = Status;
            modified = Modified;
        }

    }
}
