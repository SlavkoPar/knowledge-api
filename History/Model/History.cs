using KnowledgeAPI.A.Answers.Model;
using KnowledgeAPI.Common;
using KnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;

namespace KnowledgeAPI.Hist.Model
{
    public class History : /*Record,*/ IDisposable
    {
        public string Type { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "partitionKey")]
        public string PartitionKey { get; set; }

        public QuestionKey QuestionKey { get; set; }
        public AnswerKey AssignedAnswerKey { get; set; }
        public USER_ANSWER_ACTION UserAction { get; set; }
        public WhoWhen Created { get; set; }


        public static DateTime centuryBegin = new DateTime(2025, 1, 1);
        public static string GeneratedId {  
            get
            {
                long elapsedTicks = DateTime.Now.Ticks - History.centuryBegin.Ticks;
                TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);
                return elapsedSpan.Ticks.ToString();
            }
        }  

        public History()
        {
        }

  
        public History(HistoryData historyData)
        {
            Type = "history";
            PartitionKey = historyData.PartitionKey ?? "history";
            Id = History.GeneratedId;
            QuestionKey = historyData.QuestionKey;
            AssignedAnswerKey = historyData.AnswerKey;
            //Fixed = (short)historyData.UserAction;
            Created = new WhoWhen(historyData.NickName ?? "Admin");
            UserAction = historyData.UserAction;
        }

        public History(HistoryDto historyDto)
        {
            Type = "history";
            //PartitionKey = historyDto.PartitionKey ?? "history";
            
            Id = History.GeneratedId;
            QuestionKey = new QuestionKey(historyDto.Workspace,
                historyDto.QuestionKey.TopId, 
                null,
                historyDto.QuestionKey.Id);
            AssignedAnswerKey = new AnswerKey(historyDto.Workspace,
                historyDto.AnswerKey.TopId,
                null,
                historyDto.AnswerKey.Id);
            UserAction = (USER_ANSWER_ACTION)(historyDto.UserAction == "NotFixed"
                ? 0
                : historyDto.UserAction == "Fixed" 
                    ? 1
                    : 2);
            Created = new WhoWhen(historyDto.Created);
        }

        //public override string ToString() => 
        //    $"{PartitionKey}/{Id}, {Title} {ParentId} ";

        public void Deconstruct(out string partitionKey, out string id, out QuestionKey questionKey, out AnswerKey answerKey, out USER_ANSWER_ACTION userAction, out WhoWhen created)
        {
            partitionKey = PartitionKey;
            id = Id;
            questionKey = QuestionKey;
            answerKey = AssignedAnswerKey;
            userAction = UserAction;
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
