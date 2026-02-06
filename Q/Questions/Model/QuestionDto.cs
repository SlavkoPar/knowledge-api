using Knowledge.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using KnowledgeAPI.A.Answers;
using KnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Net;
using KnowledgeAPI.Q.Questions.Model;

namespace KnowledgeAPI.Q.Questions.Model
{
    //public class QuestionRowDto : RecordDto
    public class QuestionRowDto
    {
        public string Workspace { get; set; }
        public string TopId { get; set; }
        public string Id { get; set; }
        public string? ParentId { get; set; }
        public string Title { get; set; }

        public string? CategoryTitle { get; set; }
        public bool? Included {  get; set; }
       
        public QuestionRowDto()
        {
        }

        public QuestionRowDto(QuestionRow questionRow)
        {
            //Console.WriteLine(JsonConvert.SerializeObject(question));
            TopId = questionRow.TopId;
            Id = questionRow.Id;
            Title = questionRow.Title;
            ParentId = questionRow.ParentId;
            Included = questionRow.Included;
        }

        public QuestionRowDto(Question question)
        {
            //Console.WriteLine(JsonConvert.SerializeObject(question));
            var questionKey = new QuestionKey(question);
            TopId = question.TopId;
            Id = question.Id;
            Title = question.Title;
            CategoryTitle = question.CategoryTitle;
            ParentId = question.ParentId;
            //
            // We don't modify question AssignedAnswers through QuestionDto
            //
        }
    }

    public class QuestionDto : RecordDto // QuestionRowDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string? CategoryTitle { get; set; }
        public string? OldParentId { get; set; }
        public List<AssignedAnswerDto>? AssignedAnswerDtos { get; set; }
        public int NumOfAssignedAnswers { get; set; }
        public List<RelatedFilterDto>? RelatedFilterDtos { get; set; }
        public int NumOfRelatedFilters { get; set; }
        public int Source { get; set; }
        public int Status { get; set; }

        public QuestionDto()
            : base()
        {
        }

        public QuestionDto(Question question)
        : base(question.Created, question.Modified) //, question.Archived)
        {
            ////////////////
            // QuestionDto
            TopId = question.TopId;
            Id = question.Id;
            Title = question.Title;
            CategoryTitle = question.CategoryTitle;
            ParentId = question.ParentId;

            ///////////////////////////////////////////////
            if (question.AssignedAnswers != null)
            {
                var assignedAnswers = question.AssignedAnswers;
                assignedAnswers.Sort(AssignedAnswer.Comparer); // put the most rated AssignedAnswers to the top
                AssignedAnswerDtos = assignedAnswers
                    .Select(assignedAnswer => new AssignedAnswerDto(assignedAnswer))
                    .ToList();
            }
            NumOfAssignedAnswers = question.NumOfAssignedAnswers ?? 0;

            /////////////////////////////////////////
            if (question.RelatedFilters != null)
            {
                var relatedFilters = question.RelatedFilters; 
                relatedFilters.Sort(RelatedFilter.Comparer); // put the most rated AssignedAnswers to the top
                RelatedFilterDtos = relatedFilters
                    //.Select(relatedFilters => new RelatedFilterDto(questionKey, relatedFilters))
                    .Select(relatedFilters => new RelatedFilterDto(relatedFilters))
                    .ToList();
            }
            NumOfRelatedFilters = question.NumOfRelatedFilters ?? 0 ;
            Source = question.Source;
            Status = question.Status;
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
