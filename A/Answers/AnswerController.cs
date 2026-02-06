using Knowledge.Services;
using KnowledgeAPI.A.Answers.Model;
using KnowledgeAPI.A.Groups;
using KnowledgeAPI.A.Groups.Model;
using KnowledgeAPI.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KnowledgeAPI.A.Answers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class AnswerController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        DbService dbService { get; set; }

        public AnswerController(IConfiguration configuration)
        {
            dbService = new DbService(configuration);
            dbService.Initialize.Wait();
            Configuration = configuration;
        }


        [HttpGet("{workspace}/{topId}/{parentId}/{startCursor}/{pageSize}/{includeAnswerId}")]
        [ResponseCache(Duration = 10, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "workspace", "topId", "parentId", "startCursor" })]
        public async Task<IActionResult> GetAnswers(string workspace, string topId, string parentId,
            int startCursor, int pageSize, string? includeAnswerId)
        {
            string message = string.Empty;
            try
            {
                var groupService = new GroupService(dbService, workspace);
                GroupKey groupKey = new GroupKey(workspace, topId, id: parentId);
                GroupEx groupEx = await groupService.GetGroup(groupKey);
                var (group, msg) = groupEx;
                if (group != null)
                {
                    var answerService = new AnswerService(dbService, workspace);
                    var answerKey = new AnswerKey(groupKey);
                    AnswersMore answersMore = await answerService.GetAnswers(answerKey, startCursor, pageSize, includeAnswerId);
                    //Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>> Count {0}", answersMore.answers.Count);
                    var groupDto = new GroupDto(groupKey, answersMore);
                    groupDto.Title = group.Title;
                    return Ok(new GroupDtoEx(groupDto, msg));
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return Ok(new GroupDtoEx(message));

        }

        [HttpGet]
        public async Task<IActionResult> GetAnswer([FromQuery(Name = "qKey")] AnswerKey answerKey)
        {
            try
            {
                var workspace = answerKey.Workspace;
                var answerService = new AnswerService(dbService, workspace);
                AnswerEx answerEx = await answerService.GetAnswer(answerKey);
                var (answer, msg) = answerEx;
                if (answer == null)
                    return NotFound(new AnswerDtoEx(answerEx));
                var groupService = new GroupService(dbService, workspace);
                var q = await answerService.SetAnswerTitles(answer, groupService, answerService);
                return Ok(new AnswerDtoEx(q));
            }
            catch (Exception ex)
            {
                return BadRequest(new AnswerDtoEx(ex.Message));
            }
        }

        [HttpGet("{workspace}/{userQuery}/{count}/{svasta}/{nesto}")]
        [ResponseCache(Duration = 5, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "workspace", "userQuery", "count" })]
        public async Task<IActionResult> SearchAnswerRows(string workspace, string userQuery, int count, string svasta, string nesto)
        {
            try
            {
                var answerService = new AnswerService(dbService, workspace);
                var section = Configuration.GetSection("SearchClient");
                AnswerRowDtosEx rowDtosEx = await answerService
                    .SearchAnswerRows(section, workspace, userQuery, count);
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
        public async Task<IActionResult> Post([FromBody] AnswerDto answerDto)
        {
            try
            {
                //Console.WriteLine("*********=====>>>>>> answerDto");
                //Console.WriteLine(JsonConvert.SerializeObject(answerDto));

                var groupService = new GroupService(dbService, answerDto.Workspace);
                var answerService = new AnswerService(dbService, answerDto.Workspace);

                AnswerEx answerEx = await answerService.CreateAnswer(groupService, answerDto);
                //Console.WriteLine("*********=====>>>>>> answerEx");
                //Console.WriteLine(JsonConvert.SerializeObject(answerEx));
                //var answer = answerEx.answer;
                //if (answer != null)
                //{
                //    //Group group = new Group(answerEx.answer);
                //    answerDto.Modified = answerDto.Created; // to be used for group
                //    await groupService.UpdateNumOfAnswers(
                //           new GroupKey(answerDto),
                //           new WhoWhen(answerDto.Modified!),
                //           +1);
                //}
                return Ok(new AnswerDtoEx(answerEx));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [Authorize]
        public async Task<IActionResult> Put([FromBody] AnswerDto answerDto)
        {
            try
            {
                var workspace = answerDto.Workspace;
                var answerService = new AnswerService(dbService, workspace);
                var groupService = new GroupService(dbService, workspace);

                AnswerEx answerEx = await answerService.UpdateAnswer(answerDto, groupService);
                var (updatedAnswer, msg) = answerEx;
                if (updatedAnswer != null)
                {
                    var q = await answerService.SetAnswerTitles(updatedAnswer, groupService, answerService);
                    return Ok(new AnswerDtoEx(q));
                }
                return NotFound(answerEx);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> Delete([FromBody] AnswerDto answerDto) //string PartitionKey, string id)
        {
            try
            {
                var groupService = new GroupService(dbService, answerDto.Workspace);
                var answerService = new AnswerService(dbService, answerDto.Workspace);
                String msg = await answerService.ArchiveAnswer(null, new Answer(answerDto));
                if (msg.Equals(String.Empty))
                {
                    await groupService.UpdateNumOfAnswers(
                           new GroupKey(answerDto),
                           new WhoWhen(answerDto.Modified!),
                           -1);
                    return Ok(new AnswerDtoEx(answerDto));
                }
                return NotFound(new AnswerDtoEx(msg));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
