using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using KnowledgeAPI.A.Answers.Model;
using KnowledgeAPI.Common;
using KnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Net;

namespace KnowledgeAPI.Hist.Model
{
    public class AnswerRatedDtoListEx
    {

        public List<AnswerRatedDto> list { get; set; }
        public string msg { get; set; }


        public AnswerRatedDtoListEx(List<AnswerRatedDto>? list, string msg)
        {
            this.list = list ?? new List<AnswerRatedDto>();
            this.msg = msg;
        }

    }
 }



