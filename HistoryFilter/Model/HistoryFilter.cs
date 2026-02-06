using KnowledgeAPI.A.Answers.Model;
using KnowledgeAPI.Common;
using KnowledgeAPI.Hist.Model;
using KnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;

namespace KnowledgeAPI.HistFilter.Model
{
    public enum USER_ANSWER_ACTION { NotFixed = 0, Fixed = 1, NotClicked = 2 };

    public class HistoryFilter : /*Record,*/ IDisposable
    {
        public string Type { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "partitionKey")]
        public string PartitionKey { get; set; }

        public QuestionKey QuestionKey { get; set; }
        public string Filter { get; set; }
        public WhoWhen Created { get; set; }


        public static DateTime centuryBegin = new DateTime(2025, 1, 1);
        public static string GeneratedId {  
            get
            {
                long elapsedTicks = DateTime.Now.Ticks - HistoryFilter.centuryBegin.Ticks;
                TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);
                return elapsedSpan.Ticks.ToString();
            }
        }  

        public HistoryFilter()
        {
        }

        public HistoryFilter(HistoryFilterDto dto)
        {
            Type = "historyFilter";
            PartitionKey = "historyFilter";
            Id = HistoryFilter.GeneratedId;
            QuestionKey = new QuestionKey(dto.Workspace,
                dto.QuestionKey.TopId,
                null,
                dto.QuestionKey.Id);
            Filter = dto.Filter;
            Created = new WhoWhen(dto.Created);
        }

        //public override string ToString() => 
        //    $"{PartitionKey}/{Id}, {Title} {ParentId} ";

        public void Deconstruct(out string partitionKey, out string id, 
            out QuestionKey questionKey, out string filter, out WhoWhen created)
        {
            partitionKey = PartitionKey;
            id = Id;
            questionKey = QuestionKey;
            filter = Filter;
            created = Created;
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
