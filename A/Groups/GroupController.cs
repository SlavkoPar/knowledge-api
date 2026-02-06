using Knowledge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KnowledgeAPI.A.Groups.Model;
using KnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Collections.Generic;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KnowledgeAPI.A.Groups
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    //[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
    public class GroupController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        DbService dbService { get; set; }

        public GroupController(IConfiguration configuration)
        {
            dbService = new DbService(configuration);
            dbService.Initialize.Wait();
            Configuration = configuration;
        }

       
        [HttpGet("{workspace}/{topId}/{id}/{pageSize}/{includeAnswerId}")]
        //[ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "partitionKey", "id", "pageSize", "includeAnswerId" })]
        public async Task<IActionResult> GetGroup(string workspace, string topId, string id, int pageSize, string? includeAnswerId)
        {
            try
            {
                GroupKey groupKey = new (workspace, topId, id);

                //using(var db = new Db(this.Configuration))
                //{
                //    await db.Initialize;
                // TODO Answer.Db = db;
                //var group = new Group(_Db);
                // var container = await Db.GetContainer(this.containerId);
                //Group cat = await group.GetGroup(
                //    partitionKey, id, true, pageSize, includeAnswerId=="null" ? null : includeAnswerId);
                var groupService = new GroupService(dbService, workspace);

                GroupEx groupEx = await groupService.GetGroup(groupKey, true /* hidrate*/,  
                       pageSize, 
                       includeAnswerId //== "null" ? null : includeAnswerId
                );
                if (groupEx.group != null)
                {
                    return Ok(new GroupDtoEx(groupEx));
                }
                return NotFound(new GroupDtoEx(groupEx));
            }
            catch (Exception ex)
            {
                return BadRequest(new GroupDtoEx(ex.Message));
            }
        }

        /*
        [HttpGet("{partitionKey}/{id}/{hidrate}")]
        //[ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "partitionKey", "id" })]
        public async Task<IActionResult> GetGroupHidrated(string partitionKey, string id, bool hidrate)
        {
            // hidrate collections except answers
            try
            {
                GroupKey groupKey = new(partitionKey, id);
                // TODO what does  /partitionKey mean?
                //using (var db = new Db(this.Configuration))
                //{
                //await db.Initialize;
                //var group = new Group(db);
                var groupService = new GroupService(dbService);
                GroupEx groupEx = await groupService.GetGroupHidrated(groupKey, 0, null);
                if (groupEx.group != null)
                {
                    return Ok(new GroupDtoEx(groupEx));
                }
                //}
                return NotFound(new GroupDtoEx(groupEx));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        */

        /*
        [HttpGet("{partitionKey}/{id}")]
        //[ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "partitionKey", "id" })]
        public async Task<IActionResult> GetGroupWithSubGroups(string partitionKey, string id)
        {
            // hidrate collections except answers
            try
            {
                GroupKey groupKey = new(partitionKey, id);
                // TODO what does  /partitionKey mean?
                //using (var db = new Db(this.Configuration))
                //{
                //await db.Initialize;
                //var group = new Group(db);
                var groupService = new GroupService(dbService);
                GroupEx groupEx = await groupService.GetGroupWithSubGroups(groupKey);
                if (groupEx.group != null)
                {
                    return Ok(new GroupDtoEx(groupEx));
                }
                //}
                return NotFound(new GroupDtoEx(groupEx));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        */

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Post([FromBody] GroupDto groupDto)
        {
            try
            {
                Console.WriteLine("===>>> CreateGroup: {0} \n", groupDto.Title);
                var groupService = new GroupService(dbService, groupDto.Workspace);
                GroupEx groupEx = await groupService.CreateGroup(groupDto);
                return Ok(new GroupDtoEx(groupEx));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [Authorize]
        public async Task<IActionResult> Put([FromBody] GroupDto groupDto)
        {
            try
            {
                Console.WriteLine("===>>> UpdateGroup: {0} \n", groupDto.Title);
                var groupService = new GroupService(dbService, groupDto.Workspace);
                GroupEx groupEx = await groupService.UpdateGroup(groupDto);
                var (group, msg) = groupEx;
                if (group != null)
                {
                    if (group.ParentId != groupDto.ParentId)
                    {
                        // TODO we need to update group answers, too 
                        // group changed 
                        /*
                        await groupService.UpdateHasSubGroups(
                            new GroupKey(groupDto.PartitionKey, groupDto.ParentId!),
                            new WhoWhen(groupDto.Modified!));
                        await groupService.UpdateNumOfAnswers(
                            new GroupKey(group.PartitionKey, group.ParentId!),
                            group.Modified!,
                            1);
                        */
                    }
                    return Ok(new GroupDtoEx(groupEx));
                }
                return NotFound(new GroupDtoEx("Jok Found"));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //[HttpDelete("{partitionKey}, {id}")]
        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> Delete([FromBody] GroupRowDto rowDto) //string PartitionKey, string id)
        {
            try
            {
                var groupKey = new GroupKey(rowDto);
                var groupService = new GroupService(dbService, rowDto.Workspace);
                GroupEx groupEx = await groupService.ArchiveGroup(null, groupKey, rowDto.Modified!.NickName);
                if (groupEx.group != null)
                {
                    return Ok(new GroupDtoEx(groupEx));
                }
                return NotFound(groupEx);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
