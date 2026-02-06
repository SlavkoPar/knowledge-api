using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using KnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Net;

namespace KnowledgeAPI.A.Answers.Model
{
   

    public class AnswerDtoEx
    {
        //public AnswerDtoEx(AnswerDto? answerDto, string msg)
        //{
        //    this.answerDto = answerDto;
        //    this.msg = msg;
        //}
        public AnswerDtoEx(AnswerEx answerEx)
        {
            var (answer, msg) = answerEx;
            answerDto = answer != null ? new AnswerDto(answer) : null;
            this.msg = msg;
        }

        public AnswerDtoEx(AnswerDto answerDto)
        {
            this.answerDto = answerDto;
            this.msg = String.Empty;
        }
        public AnswerDtoEx(Answer answer)
        {
            this.answerDto = new AnswerDto(answer);
            this.msg = String.Empty;
        }

        public AnswerDtoEx(string msg)
        {
            answerDto = null;
            this.msg = msg;
        }


        public AnswerDto? answerDto { get; set; }
        public string msg { get; set; }

        internal void Deconstruct(out AnswerDto answerDto, out string msg)
        {
            answerDto = this.answerDto;
            msg = this.msg;
        }
    }

}



