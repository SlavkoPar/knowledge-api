using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using KnowledgeAPI.A.Answers.Model;
using KnowledgeAPI.Common;
using KnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Net;

namespace KnowledgeAPI.Hist.Model
{
    public class HistoryDto //: RecordDto
    {
        public string Workspace {  get; set; }
        public string? Type { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string? Id { get; set; }

        //[JsonProperty(PropertyName = "partitionKey")]
        //public string? PartitionKey { get; set; }

        public QuestionKeyDto QuestionKey { get; set; }
        public AnswerKeyDto AnswerKey { get; set; }
        public string UserAction { get; set; }
        public WhoWhenDto Created { get; set; }


        public HistoryDto()
        {
        }

        public HistoryDto(History history)
        {
            //Console.WriteLine(JsonConvert.SerializeObject(history));
            //PartitionKey = history.PartitionKey;
            //Id = history.Id;
            QuestionKey = new QuestionKeyDto(history.QuestionKey);
            AnswerKey = new AnswerKeyDto(history.AssignedAnswerKey);
            UserAction = history.UserAction == USER_ANSWER_ACTION.NotFixed
                ? "NotFixed"
                : history.UserAction == USER_ANSWER_ACTION.Fixed
                    ? "Fixed"
                    : "UnDefined";
            Created = new WhoWhenDto(history.Created);
        }
    }
 }



