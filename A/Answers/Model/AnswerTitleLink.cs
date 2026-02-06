using Newtonsoft.Json;
using System.Diagnostics.Metrics;


namespace KnowledgeAPI.A.Answers.Model
{
    public class AnswerTitleLink
    {
        public string? id { get; set; }
        public string Title { get; set; }
        public string? Link { get; set; }

        public AnswerTitleLink()
        {
        }

        public AnswerTitleLink(AnswerTitleLink answerTitleLink)
        {
            this.id = answerTitleLink.id;
            this.Title = answerTitleLink.Title;
            this.Link = answerTitleLink.Link;
        }

        public AnswerTitleLink(string id, string title, string? link)
        {
            this.id = id;
            this.Title = title;
            this.Link = link;
        }


    }

}
