using System.Diagnostics.Metrics;


namespace KnowledgeAPI.A.Answers.Model
{
    public class AnswerData
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
        public int? Source { get; set; }
        public int? Status { get; set; }

        public AnswerData() { 
        }

        public AnswerData(string ParentId, string Title)
        {
            this.ParentId = ParentId;
            this.Title = Title; 
        }
    }

}
