using KnowledgeAPI.Common;
using Newtonsoft.Json;
using Azure.Search.Documents.Indexes;

namespace KnowledgeAPI.A.Answers.Model
{
    public class AnswerRow : Record
    {

        //[SearchableField()]
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string? ParentId { get; set; }

        //[SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public string Title { get; set; }

        //[VectorSearchField()]
        public List<float> vectors { get; set; }

        //[SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]

        public int? NumOfAssignedAnswers { get; set; }

        [JsonProperty(PropertyName = "Included", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Included {  get; set; }

        public AnswerRow()
            : base(new WhoWhen("Admin"), null)
        {
            Type = "answer";
            Title = string.Empty;
        }

        public AnswerRow(AnswerData answerData)
           : base(new WhoWhen("Admin"), null)
        {
            Type = "answer";
            Workspace = answerData.Workspace;
            TopId = answerData.TopId;
            string s = DateTime.Now.Ticks.ToString();
            Id = answerData.Id ?? s.Substring(s.Length - 10);// Guid.NewGuid().ToString();
            ParentId = answerData.ParentId;
            Title = answerData.Title;
            vectors = [];
        }

        public AnswerRow(AnswerDto dto)
            : base(dto.Created, dto.Modified)
        {
            Type = "answer";
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

        public AnswerRow(AnswerRow row)
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

    public class Answer : AnswerRow, IDisposable
    {
        //[SearchableField()]
        [JsonProperty(PropertyName = "partitionKey")]
        public string PartitionKey { get; set; }

        [JsonProperty(PropertyName = "GroupTitle", NullValueHandling = NullValueHandling.Ignore)]
        public string? GroupTitle { get; set; }


        //[JsonProperty(PropertyName = "RelatedFilters", NullValueHandling = NullValueHandling.Ignore)]
        //public List<RelatedFilter> RelatedFilters { get; set; }
        //public int? NumOfRelatedFilters { get; set; }

        public int Source { get; set; }
        public int Status { get; set; }

        public Answer()
            : base()   
        {
            Type = "answer";
            GroupTitle = null;
            Source = 0;
            Status = 0;
        }


        public Answer(AnswerData answerData)
            : base(answerData)
        {
            PartitionKey = answerData.Workspace + "/" + answerData.TopId;
            GroupTitle = null;
                       
            
            Source = 0;
            Status = 0;
        }

        public Answer(AnswerDto answerDto)
        : base(answerDto)
        {
            Type = "answer";
            PartitionKey = answerDto.Workspace + "/" + answerDto.TopId;
            GroupTitle = null;
            //AssignedAnswers = answerDto.AssignedAnswers!;
            //NumOfAssignedAnswers = answerDto.NumOfAssignedAnswers;
            Source = answerDto.Source;
            Status = answerDto.Status;    
        }

        public Answer(AnswerRow answerRow)
        : base(answerRow)
        {
            Type = "answer";
            GroupTitle = null;
            Source = 0;
            Status = 0;
        }

        //public override string ToString() => 
        //    $"{PartitionKey}/{Id}, {Title} {ParentId} ";

        public void Deconstruct(out string workspace, out string topId, out string partitionKey, out string id, out string title, out string? parentId,
                                out string type, out int source, out int status) //, out int? numOfRelatedFilters)
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
            //numOfAssignedAnswers = NumOfAssignedAnswers;
            //numOfRelatedFilters = NumOfRelatedFilters;
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
