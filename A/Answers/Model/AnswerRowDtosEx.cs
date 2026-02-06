using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using KnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Net;

namespace KnowledgeAPI.A.Answers.Model
{
    public class AnswerRowDtosEx
    {
        public AnswerRowDtosEx(List<AnswerRowDto> answerRowDtos, string msg)
        {
            this.answerRowDtos = answerRowDtos;
            this.msg = msg;
        }

        public AnswerRowDtosEx(List<AnswerRow> answerRows, string msg)
        {
            this.answerRowDtos = answerRows.Select(answerRow => new AnswerRowDto(answerRow)).ToList();
            this.msg = msg;
        }

        public List<AnswerRowDto>? answerRowDtos { get; set; }
        public string msg { get; set; }

        internal void Deconstruct(out List<AnswerRowDto> answerRowDtos, out string msg)
        {
            answerRowDtos = this.answerRowDtos;
            msg = this.msg;
        }
    }
}



