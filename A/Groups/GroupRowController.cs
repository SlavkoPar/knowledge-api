using Knowledge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KnowledgeAPI.A.Groups.Model;
using KnowledgeAPI.Q.Categories.Model;
using Newtonsoft.Json;
using System.Collections.Generic;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KnowledgeAPI.A.Groups
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class GroupRowController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        DbService dbService { get; set; }

        public GroupRowController(IConfiguration configuration)
        {
            dbService = new DbService(configuration);
            dbService.Initialize.Wait();
            Configuration = configuration;
        }

        /*

        [HttpGet("{workspace}/{all}")]
        // TODO uncomment after testing
        [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "workspace", "all" })]
        public async Task<IActionResult> GetAllCats(string workspace, string all)
        {
            try
            {
                // using (var db = new Db(this.Configuration))
                var rowService = new GroupRowService(dbService, workspace);
                List<CatDto> catDtos = await rowService.GetAllCats();
                return Ok(catDtos);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        */


        [HttpGet("{workspace}")]
        // TODO uncomment after testing
        //[ResponseCache(Duration = 10, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetAllRows(string workspace)
        {
            try
            {
                // using (var db = new Db(this.Configuration))
                var rowService = new GroupRowService(dbService, workspace);
                List<GroupRowDto> groupRowDtos = await rowService.GetAllRows();
                return Ok(groupRowDtos);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{workspace}/{toprows}")]
        // TODO uncomment after testing
        //[ResponseCache(Duration = 12, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "workspace", "id" })]
        public async Task<IActionResult> GetTopRows(string workspace, string toprows) // parentId is NULL
        {
            try
            {
                //using (var db = new Db(this.Configuration))
                //{
                //    await db.Initialize;
                //var group = new Group(_Db);
                var rowService = new GroupRowService(dbService, workspace);
                var groupKey = new GroupKey(workspace, "", "", null);
                List<GroupRow> subGroups = await rowService.GetSubRows(null, groupKey);
                //Console.WriteLine(JsonConvert.SerializeObject(subGroups.Select( c => c.Title).ToList()));
                subGroups.Sort(GroupRow.Comparer);
                //Console.WriteLine(JsonConvert.SerializeObject(subGroups.Select(c => c.Title).ToList()));
                if (subGroups != null)
                {
                    List<GroupRowDto> list = [];
                    foreach (GroupRow groupRow in subGroups)
                    {
                        list.Add(new GroupRowDto(groupRow));
                    }
                    return Ok(list);
                }
                //}
                return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        //[ResponseCache(Duration = 12, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "groupKeyParam" })]
        public async Task<IActionResult> GetGroupRowsUpTheTree([FromQuery(Name="grpKey")] GroupKey groupKey, int pageSize, string? includeAnswerId)
        //string workspace, string topId, string id
        {
            try
            {
                var rowService = new GroupRowService(dbService, groupKey.Workspace);
                //var groupKey = new GroupKey(workspace, topId, id);
                GroupRowEx groupRowEx = await rowService.GetRowsUpTheTree(groupKey, pageSize, includeAnswerId);

                if (groupRowEx != null && groupRowEx.groupRow == null)
                {
                    // if key from localStorage, doesn't exit any more, use topId
                    groupKey.Id = groupKey.TopId;
                    groupRowEx = await rowService.GetRowsUpTheTree(groupKey, pageSize, includeAnswerId);
                }
                //Console.WriteLine(JsonConvert.SerializeObject(groupEx));
                var groupDtoEx = new GroupRowDtoEx(groupRowEx);
                return Ok(groupDtoEx);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                Console.WriteLine(msg);
                return BadRequest(new GroupRowDtoEx(null, msg));
            }
        }


        [HttpGet("{hidrate}/{pageSize}/{includeAnswerId}")]
        public async Task<IActionResult> GetGroupRow(bool hidrate, int pageSize, string? includeAnswerId,
              [FromQuery(Name="grpKey")]GroupKey groupKey
        )
        {
            try
            {
                var rowService = new GroupRowService(dbService, groupKey.Workspace);
                // one fine day use this style
                //using (var db = new Db(this.Configuration)) {}

                GroupRow groupRow = await rowService.GetGroupRow(groupKey, hidrate, pageSize, includeAnswerId);
                var groupRowDto = new GroupRowDto(groupRow);
                var groupRowDtoEx = new GroupRowDtoEx(groupRowDto, "");
                if (groupRowDtoEx.groupRowDto != null)
                {
                    return Ok(groupRowDtoEx);
                }
                return NotFound(groupRowDtoEx);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
