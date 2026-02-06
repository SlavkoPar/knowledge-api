using Knowledge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KnowledgeAPI.A.Answers;
using KnowledgeAPI.Common;
using KnowledgeAPI.Q.Categories;
using KnowledgeAPI.Q.Categories.Model;
using KnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Configuration;
using System.Drawing.Printing;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KnowledgeAPI.Q.Questions
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]

    public class QuestionChatController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        DbService dbService { get; set; }

        public QuestionChatController(IConfiguration configuration)
        {
            dbService = new DbService(configuration);
            dbService.Initialize.Wait();
            Configuration = configuration;
        }


        /*

        [HttpGet("{workspace}/{topId}/{parentId}/{startCursor}/{pageSize}")]
        [ResponseCache(Duration = 10, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "workspace", "topId", "parentId", "startCursor" })]
        public async Task<HttpResponseMessage> GetLegacyHtml(string workspace, string topId, string parentId, int startCursor, int pageSize)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            CategoryKey categoryKey = new CategoryKey(workspace, topId, id: parentId);
            var questionService = new QuestionService(dbService, workspace);
            var questionKey = new QuestionKey(categoryKey);
            QuestionsMore questionsMore = await questionService.GetQuestions(questionKey, startCursor, pageSize, null);
            // With this line:
            QuestionRowShort[] rows = questionsMore.QuestionRows.Select(row => new QuestionRowShort(row)).ToArray();
            StringBuilder sb = new StringBuilder();
            sb.Append(@"<table>
                            <thead>
                                <tr>
                                    <th>Id</th> <th>Title</th> <th>#Assigned Answers</th><th>Who</th><th>Created</th>
                                </tr>
                            </thead>
                        <tbody>");
            foreach (var row in rows)
            {
                sb.Append($@"<row>
                        <td>${row.Id}</td>
                        <td>${row.Title}></td>
                        <td>${row.AssignedAnswers}></td>
                        <td>${row.Who}></td>
                        <td>${row.When}></td>
                </row>");
            }
            sb.Append("<tbody></table>");
            string htmlContent = sb.ToString();
            response.Content = new StringContent(htmlContent);
            return response;
        }
        */

        
        [HttpGet("{workspace}/{topId}/{parentId}/{startCursor}/{pageSize}")]
        [ResponseCache(Duration = 10, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "workspace", "topId", "parentId", "startCursor" })]
        public async Task<IActionResult> GetQuestions(string workspace, string topId, string parentId, int startCursor, int pageSize)
        {
            string message = string.Empty;
            try
            {
                CategoryKey categoryKey = new CategoryKey(workspace, topId, id: parentId);
                var questionService = new QuestionService(dbService, workspace);
                var questionKey = new QuestionKey(categoryKey);
                QuestionsMore questionsMore = await questionService.GetQuestions(questionKey, startCursor, pageSize, null);
                // With this line:
                QuestionRowShort[] rows = questionsMore.QuestionRows.Select(row => new QuestionRowShort(row)).ToArray();
                //Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>> Count {0}", questionsMore.questions.Count);
                return Ok(new { rows, questionsMore.HasMoreQuestions });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        

    }
}
