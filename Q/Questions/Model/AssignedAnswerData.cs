using KnowledgeAPI.A.Answers.Model;
using KnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace KnowledgeAPI.Q.Questions.Model
{
    public class AssignedAnswerData: IDisposable
    {
        public string TopId { get; set; }
        public string Id { get; set; }

        //public AssignedAnswerData(AnswerKey answerKey)
        //{
        //    AnswerKey = answerKey;
        //}

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
