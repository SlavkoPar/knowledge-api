using KnowledgeAPI.Common;
using Newtonsoft.Json;

namespace KnowledgeAPI.Q.Questions.Model
{

    public class QuestionEx
    {
        public QuestionEx(Question? question, string msg)
        {
            this.question = question;
            this.msg = msg;
        }

        public QuestionEx(string msg)
        {
            this.question = null;
            this.msg = msg;
        }

        public Question? question { get; set; }
        public string msg { get; set; }

        internal void Deconstruct(out Question? question, out string msg)
        {
            question = this.question;
            msg = this.msg;
        }
    }
}
