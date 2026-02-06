using Microsoft.Azure.Cosmos.Linq;
using KnowledgeAPI.A.Answers.Model;
using KnowledgeAPI.Common;
using Newtonsoft.Json;
using Azure.Search.Documents.Indexes;

namespace KnowledgeAPI.Q.Questions.Model
{
    public class QuestionRowShort
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public int AssignedAnswers { get; set; }

        public string Who { get; set; }

        public string When { get; set; }


        public QuestionRowShort()
        {
        }

        public QuestionRowShort(QuestionRow questionRow)
        {
            Id = questionRow.Id;
            Title = questionRow.Title;
            AssignedAnswers = questionRow.NumOfAssignedAnswers ?? 0;
            Who = questionRow.Created != null ? questionRow.Created.NickName : "unk";
            When = questionRow.Created != null ? questionRow.Created.Time.ToString("ddd, MMM dd, yy h:mm tt") : "unk";
        }

    }

    public class QuestionRow : Record
    {

        //[SearchableField()]
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string? ParentId { get; set; }

        //[SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public string Title { get; set; }

        //[VectorSearchField()]
        public List<float>? vectors { get; set; }

        //[SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]

        public int? NumOfAssignedAnswers { get; set; }

        [JsonProperty(PropertyName = "Included", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Included {  get; set; }

        public QuestionRow()
            : base(new WhoWhen("Admin"), null)
        {
            Type = "question";
            Title = string.Empty;
        }

        public QuestionRow(QuestionData questionData)
           : base(new WhoWhen("Admin"), null)
        {
            Type = "question";
            Workspace = questionData.Workspace;
            TopId = questionData.TopId;
            string s = DateTime.Now.Ticks.ToString();
            Id = questionData.Id ?? s.Substring(s.Length - 10);// Guid.NewGuid().ToString();
            ParentId = questionData.ParentId;
            Title = questionData.Title;
            vectors = [];
        }

        public QuestionRow(QuestionDto dto)
            : base(dto.Created, dto.Modified)
        {
            Type = "question";
            Workspace = dto.Workspace;
            TopId = dto.TopId;
            //string s = DateTime.Now.Ticks.ToString();
            //Id = s.Substring(s.Length - 10);// Guid.NewGuid().ToString();
            if (dto.Id.Equals("generateId")) {
                string s = DateTime.Now.Ticks.ToString();
                dto.Id = s.Substring(s.Length - 10);// Guid.NewGuid().ToString();
            }
            Id = dto.Id;
            Title = dto.Title;
            ParentId = dto.ParentId;
            vectors = [];
        }



        public QuestionRow(QuestionRow row)
            : base(row.Created, row.Modified)
        {
            Id = row.Id;
            Title = row.Title;
            ParentId = row.ParentId;
        }

        public void Deconstruct(out string workspace, out string topId, out string id, out string title, out string? parentId)
        {
            workspace = Workspace;
            topId = TopId;
            id = Id;
            title = Title;
            parentId = ParentId;
        }

    }

    public class Question : QuestionRow, IDisposable
    {
        //[SearchableField()]
        [JsonProperty(PropertyName = "partitionKey")]
        public string PartitionKey { get; set; }

        [JsonProperty(PropertyName = "CategoryTitle", NullValueHandling = NullValueHandling.Ignore)]
        public string? CategoryTitle { get; set; }

        [JsonProperty(PropertyName = "AssignedAnswers", NullValueHandling = NullValueHandling.Ignore)]
        public List<AssignedAnswer>? AssignedAnswers { get; set; }

        [JsonProperty(PropertyName = "RelatedFilters", NullValueHandling = NullValueHandling.Ignore)]
        public List<RelatedFilter> RelatedFilters { get; set; }
        public int? NumOfRelatedFilters { get; set; }

        public int Source { get; set; }
        public int Status { get; set; }

        public Question()
            : base()   
        {
            Type = "question";
            CategoryTitle = null;
            Source = 0;
            Status = 0;
        }


        public Question(QuestionData questionData)
            : base(questionData)
        {
            PartitionKey = questionData.Workspace + "/" + questionData.TopId;
            CategoryTitle = null;

            // Assigned Answers
            AssignedAnswers = [];
            if (questionData.AssignedAnswers != null)
            {
                foreach (var assignedAnswer in questionData.AssignedAnswers)
                {
                    AssignedAnswers.Add(new AssignedAnswer(assignedAnswer));
                }
            }
            NumOfAssignedAnswers = AssignedAnswers.Count;

            // Related Filters
            RelatedFilters = [];
            if (questionData.RelatedFilters != null)
            {
                foreach (var relatedFilterData in questionData.RelatedFilters)
                {
                    var relatedFilter = new RelatedFilter(relatedFilterData.Filter, new WhoWhen("Admin"));
                    RelatedFilters.Add(relatedFilter);
                }
            }
            NumOfRelatedFilters = RelatedFilters.Count;

            Source = 0;
            Status = 0;
        }

        public Question(QuestionDto questionDto)
        : base(questionDto)
        {
            Type = "question";
            PartitionKey = questionDto.Workspace + "/" + questionDto.TopId;
            CategoryTitle = null;
            //AssignedAnswers = questionDto.AssignedAnswers!;
            //NumOfAssignedAnswers = questionDto.NumOfAssignedAnswers;
            Source = questionDto.Source;
            Status = questionDto.Status;    
        }

        public Question(QuestionRow questionRow)
        : base(questionRow)
        {
            Type = "question";
            CategoryTitle = null;
            Source = 0;
            Status = 0;
        }

        //public override string ToString() => 
        //    $"{PartitionKey}/{Id}, {Title} {ParentId} ";

        public void Deconstruct(out string workspace, out string topId, out string partitionKey, out string id, out string title, out string? parentId,
                                out string type, out int source, out int status, 
                                out List<AssignedAnswer>? assignedAnswers, // out int? numOfAssignedAnswers) //,
                                out List<RelatedFilter>? relatedFilters) //, out int? numOfRelatedFilters)
        {
            workspace = Workspace;
            topId = TopId;
            partitionKey = PartitionKey;
            id = Id;
            title = Title;
            parentId = ParentId;
            type = Type;
            source = Source;
            status = Status;
            assignedAnswers = AssignedAnswers;
            //numOfAssignedAnswers = NumOfAssignedAnswers;
            //numOfRelatedFilters = NumOfRelatedFilters;
            relatedFilters = RelatedFilters;
        }

        protected List<Answer> Answers
        {
            get
            {
                // return q - 
                return new List<Answer>();
            }
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
