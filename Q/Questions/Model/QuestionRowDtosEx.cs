using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using KnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Net;

namespace KnowledgeAPI.Q.Questions.Model
{
    public class QuestionRowDtosEx
    {
        public QuestionRowDtosEx(List<QuestionRowDto> questionRowDtos, string msg)
        {
            this.questionRowDtos = questionRowDtos;
            this.msg = msg;
        }

        public QuestionRowDtosEx(List<QuestionRow> questionRows, string msg)
        {
            this.questionRowDtos = questionRows.Select(questionRow => new QuestionRowDto(questionRow)).ToList();
            this.msg = msg;
        }

        public List<QuestionRowDto>? questionRowDtos { get; set; }
        public string msg { get; set; }

        internal void Deconstruct(out List<QuestionRowDto> questionRowDtos, out string msg)
        {
            questionRowDtos = this.questionRowDtos;
            msg = this.msg;
        }
    }
}



