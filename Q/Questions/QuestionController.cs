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

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KnowledgeAPI.Q.Questions
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class QuestionController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        DbService dbService { get; set; }

        public QuestionController(IConfiguration configuration)
        {
            dbService = new DbService(configuration);
            dbService.Initialize.Wait();
            Configuration = configuration;
        }


        [HttpGet("{workspace}/{topId}/{parentId}/{startCursor}/{pageSize}/{includeQuestionId}")]
        [ResponseCache(Duration = 10, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "workspace", "topId", "parentId", "startCursor" })]
        public async Task<IActionResult> GetQuestions(string workspace, string topId, string parentId, 
            int startCursor, int pageSize, string? includeQuestionId)
        {
            string message = string.Empty;
            try
            {
                var categoryService = new CategoryService(dbService, workspace);
                CategoryKey categoryKey = new CategoryKey(workspace, topId, id:parentId);
                CategoryEx categoryEx = await categoryService.GetCategory(categoryKey);
                var (category, msg) = categoryEx;
                if (category != null)
                {
                    var questionService = new QuestionService(dbService, workspace);
                    var questionKey = new QuestionKey(categoryKey);
                    QuestionsMore questionsMore = await questionService.GetQuestions(questionKey, startCursor, pageSize, includeQuestionId);
                    //Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>> Count {0}", questionsMore.questions.Count);
                    var categoryDto = new CategoryDto(categoryKey, questionsMore);
                    categoryDto.Title = category.Title;
                    return Ok(new CategoryDtoEx(categoryDto, msg));
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return BadRequest(message); 
            }
            return Ok(new CategoryDtoEx(message));

        }

        [HttpGet]
        public async Task<IActionResult> GetQuestion([FromQuery(Name="qKey")] QuestionKey questionKey)
        {
            try
            {
                var workspace = questionKey.Workspace;
                var questionService = new QuestionService(dbService, workspace);
                QuestionEx questionEx = await questionService.GetQuestion(questionKey);
                var (question, msg) = questionEx;
                if (question == null)
                    return NotFound(new QuestionDtoEx(questionEx));
                var categoryService = new CategoryService(dbService, workspace);
                var answerService = new AnswerService(dbService, workspace);
                var q = await questionService.SetAnswerTitles(question, categoryService, answerService);
                return Ok(new QuestionDtoEx(q));
            }
            catch (Exception ex)
            {
                return BadRequest(new QuestionDtoEx(ex.Message));
            }
        }

        [HttpGet("{workspace}/{userQuery}/{count}/{svasta}/{nesto}")]
        [ResponseCache(Duration = 5, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "workspace", "userQuery", "count" })]
        public async Task<IActionResult> SearchQuestionRows(string workspace, string userQuery, int count, string svasta, string nesto)
        {
            try
            {
                var questionService = new QuestionService(dbService, workspace);
                var section = Configuration.GetSection("SearchClient");
                QuestionRowDtosEx rowDtosEx = await questionService
                    .SearchQuestionRows(section, workspace, userQuery, count);
                //Console.WriteLine(JsonConvert.SerializeObject(rowDtos));
                return Ok(rowDtosEx);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Post([FromBody] QuestionDto questionDto)
        {
            try
            {
                var categoryService = new CategoryService(dbService, questionDto.Workspace);
                var questionService = new QuestionService(dbService, questionDto.Workspace);

                QuestionEx questionEx = await questionService.CreateQuestion(categoryService, questionDto);
                return Ok(new QuestionDtoEx(questionEx));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [Authorize]
        public async Task<IActionResult> Put([FromBody] QuestionDto questionDto)
        {
            try
            {
                var workspace = questionDto.Workspace;
                var questionService = new QuestionService(dbService, workspace);
                var categoryService = new CategoryService(dbService, workspace);
                var answerService = new AnswerService(dbService, workspace);

                QuestionEx questionEx = await questionService.UpdateQuestion(questionDto, categoryService);
                var (updatedQuestion, msg) = questionEx;
                if (updatedQuestion != null)
                {
                    var q = await questionService.SetAnswerTitles(updatedQuestion, categoryService, answerService);
                    return Ok(new QuestionDtoEx(q));
                }
                return NotFound(questionEx);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> Delete([FromBody] QuestionDto questionDto) //string PartitionKey, string id)
        {
            try
            {
                var categoryService = new CategoryService(dbService, questionDto.Workspace);
                var questionService = new QuestionService(dbService, questionDto.Workspace);
                String msg = await questionService.ArchiveQuestion(null, new Question(questionDto));
                if (msg.Equals(String.Empty))
                {
                    var categoryKey = new CategoryKey(questionDto);
                    int numOfQuestions = await questionService.CountNumOfQuestions(categoryKey);
                    await categoryService.UpdateNumOfQuestions(
                           categoryKey,
                           new WhoWhen(questionDto.Modified!),
                           numOfQuestions);
                    return Ok(new QuestionDtoEx(questionDto));
                }
                return NotFound(new QuestionDtoEx(msg));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
