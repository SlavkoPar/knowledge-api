using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using KnowledgeAPI.A.Answers.Model;
using KnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Net;

namespace KnowledgeAPI.Q.Questions.Model
{
    public class AssignedAnswerDto
    {

        //[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public QuestionKey? QuestionKeyDto {  get; set; }

        public string TopId {  get; set; }  
        public string Id { get; set; }
        public string? AnswerTitle { get; set; }
        public string? AnswerLink { get; set; }
        public WhoWhenDto? Created { get; set; }
        public WhoWhenDto? Modified { get; set; }

        public AssignedAnswerDto()
        {
        }

        //public AssignedAnswerDto(AssignedAnswer assignedAnswer)
        //{
        //    var (answerKey, created, answerTitle, Fixed, NotFixed, NotClicked) = assignedAnswer;
        //    AnswerKey = answerKey;
        //    Created = new WhoWhenDto(created);
        //    AnswerTitle = answerTitle;
        //    this.Fixed = Fixed;
        //    this.NotFixed = NotFixed;
        //    this.NotClicked = NotClicked;
        //}

        public AssignedAnswerDto(AssignedAnswer assignedAnswer)
        {
            var (topId, id, answerTitle, answerLink, created, modified, Fixed, NotFixed, NotClicked) = assignedAnswer;
            QuestionKeyDto = null;
            TopId = topId;
            Id = id;
            AnswerTitle = answerTitle ?? string.Empty;
            AnswerLink = answerLink ?? string.Empty;
            Created = new WhoWhenDto(created);
            Modified = new WhoWhenDto(modified);
        }

        internal void Deconstruct(out string topId, out string id, out string? answerTitle, out string? answerLink,
            out WhoWhenDto created, out WhoWhenDto? modified)
            //out uint Fixed, out uint NotFixed, out uint NotClicked)
        {
            topId = TopId;
            id = Id;
            answerTitle = AnswerTitle;
            answerLink = AnswerLink;
            created = Created;
            modified = Modified;
        }
    }

    
}
