using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using KnowledgeAPI.A.Answers.Model;
using KnowledgeAPI.Common;
using KnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Net;

namespace KnowledgeAPI.Hist.Model
{
    public class AnswerRated 
    {
        public QuestionKey QuestionKey { get; set; }
        public AnswerKey AnswerKey { get; set; }
        public string AnswerTitle { get; set; }

        public Boolean Fixed { get; set; }
        public Boolean NotFixed { get; set; }
        public Boolean NotClicked { get; set; }


        public AnswerRated()
        {
        }


        public AnswerRated(History history)
        {
            //Console.WriteLine(JsonConvert.SerializeObject(history));
            QuestionKey = history.QuestionKey;
            AnswerKey = history.AssignedAnswerKey;
            Fixed = history.UserAction == USER_ANSWER_ACTION.Fixed;
            NotFixed = history.UserAction == USER_ANSWER_ACTION.NotFixed;
            NotClicked = history.UserAction == USER_ANSWER_ACTION.NotClicked;
        }


        public AnswerRated(QuestionKey questionKey, AssignedAnswer assignedAnswer)
        {
            /* argh
            QuestionKey = questionKey;
            AnswerKey = assignedAnswer.TopId;
            AnswerTitle = assignedAnswer.AnswerTitle!;
            Fixed = true;
            NotFixed = false;
            NotClicked = false;
            */
        }

    }
 }



