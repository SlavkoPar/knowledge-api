using System.Collections.Generic;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace KnowledgeAPI.A.Answers.Model
{
    public class AnswersMore
    {
        public List<AnswerRow> AnswerRows { get; set; }
        public bool HasMoreAnswers { get; set; }
        public AnswersMore(List<AnswerRow> answerRows, bool hasMore)
        {
            AnswerRows = answerRows;
            HasMoreAnswers = hasMore;
        }
    }
}

