using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using KnowledgeAPI.A.Answers.Model;
using KnowledgeAPI.Common;
using KnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Net;

namespace KnowledgeAPI.HistFilter.Model
{
    public class HistoryFilterDto //: RecordDto
    {
        public string Workspace { get; set; }
        public QuestionKey QuestionKey { get; set; }
        public string Filter { get; set; }
        public WhoWhenDto Created { get; set; }

        public HistoryFilterDto()
        {
        }
    }
 }


