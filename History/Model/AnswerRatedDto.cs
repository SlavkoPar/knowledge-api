using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using KnowledgeAPI.A.Answers.Model;
using KnowledgeAPI.Common;
using KnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Net;

namespace KnowledgeAPI.Hist.Model
{
    public class AnswerRatedDto 
    {
        public QuestionKey QuestionKey { get; set; }
        public AnswerKey AnswerKey { get; set; }
        public string? AnswerTitle { get; set; }

        public int NumOfFixed { get; set; }
        public int NumOfNotFixed { get; set; }
        public int NumOfNotClicked { get; set; }


        public AnswerRatedDto(QuestionKey questionKey, AssignedAnswer assignedAnswer)
        {
            /* argh
            QuestionKey = questionKey;
            AnswerKey = assignedAnswer.TopId;
            AnswerTitle = assignedAnswer.AnswerTitle;
            NumOfFixed = 0;
            NumOfNotFixed = 0;
            NumOfNotClicked = 0;
            */
        }

        public void Incr(AnswerRated answerRated)
        {
            //Console.WriteLine(JsonConvert.SerializeObject(history));
           
            if (answerRated.Fixed)
                NumOfFixed += 1;
            if (answerRated.NotFixed)
                NumOfNotFixed += 1;
            if (answerRated.NotClicked)
                NumOfNotClicked += 1;
        }
    }
 }



