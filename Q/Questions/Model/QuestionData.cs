using System.Diagnostics.Metrics;


namespace KnowledgeAPI.Q.Questions.Model
{
    public class QuestionData
    {
        public string Workspace = string.Empty;
        public string TopId = string.Empty;
        public string PartitionKey {
            get {
                return Workspace + "/" + TopId;
            }
        }

        public string? ParentId { get; set; }
        public string? Id { get; set; }
        
        public string Title { get; set; }
        public List<AssignedAnswerData>? AssignedAnswers { get; set; }
        public List<RelatedFilterData>? RelatedFilters { get; set; }
        public int? Source { get; set; }
        public int? Status { get; set; }

        public QuestionData() { 
        }

        public QuestionData(string ParentId, string Title)
        {
            this.ParentId = ParentId;
            this.Title = Title; 
        }
    }

}
