using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.Schemas;
using Knowledge.Services;
using Microsoft.AspNetCore.Authorization;
using KnowledgeAPI.Q.Categories.Model;
using KnowledgeAPI.Q.Questions.Model;
using KnowledgeAPI.A.Answers.Model;
using KnowledgeAPI.A.Answers;
using KnowledgeAPI.Q.Categories;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KnowledgeAPI.Q.Questions
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]

    public class QuestionAnswerController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        DbService dbService { get; set; }

        public QuestionAnswerController(IConfiguration configuration)
        {
            dbService = new DbService(configuration);
            dbService.Initialize.Wait();
            Configuration = configuration;
        }


        [HttpPost("Assign")]
        [Authorize]
        public async Task<IActionResult> AssignAnswer([FromBody] AssignedAnswerDto assignedAnswerDto)
        {
            try
            {
                var questionKey = assignedAnswerDto.QuestionKeyDto!;
                var workspace = questionKey.Workspace;
                var questionService = new QuestionService(dbService, workspace);

                QuestionEx questionEx = await questionService.AssignAnswer(questionKey, assignedAnswerDto);
                var (question, msg) = questionEx;
                if (question != null)
                {
                    var categoryService = new CategoryService(dbService, workspace);
                    var answerService = new AnswerService(dbService, workspace);
                    Question q = await questionService.SetAnswerTitles(question, categoryService, answerService);
                    return Ok(new QuestionDtoEx(new QuestionEx(q, "")));
                }

                return NotFound(new QuestionDtoEx(questionEx));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost("UnAssign")]
        [Authorize]
        public async Task<IActionResult> UnAssignAnswer([FromBody] AssignedAnswerDto assignedAnswerDto)
        {
            try
            {
                var questionKey = assignedAnswerDto.QuestionKeyDto!;
                var workspace = questionKey.Workspace;
                var questionService = new QuestionService(dbService, workspace);
                QuestionEx questionEx = await questionService.UnAssignAnswer(questionKey, assignedAnswerDto);
                var (question, msg) = questionEx;
                if (question != null)
                {
                    var categoryService = new CategoryService(dbService, workspace);
                    var answerService = new AnswerService(dbService, workspace);
                    var q = await questionService.SetAnswerTitles(question, categoryService, answerService);
                    return Ok(new QuestionDtoEx(new QuestionEx(q, "")));

                }
                return NotFound(new QuestionDtoEx(questionEx));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}