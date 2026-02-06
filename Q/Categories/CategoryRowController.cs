using Microsoft.AspNetCore.Mvc;

using Knowledge.Services;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using Newtonsoft.Json;
using KnowledgeAPI.Q.Categories.Model;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KnowledgeAPI.Q.Categories
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class CategoryRowController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        DbService dbService { get; set; }

        public CategoryRowController(IConfiguration configuration)
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
                var rowService = new CategoryRowService(dbService, workspace);
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
                var rowService = new CategoryRowService(dbService, workspace);
                List<CategoryRowDto> categoryRowDtos = await rowService.GetAllRows();
                return Ok(categoryRowDtos);
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
                //var category = new Category(_Db);
                var rowService = new CategoryRowService(dbService, workspace);
                var categoryKey = new CategoryKey(workspace, "", "", null);
                List<CategoryRow> subCategories = await rowService.GetSubRows(null, categoryKey);
                //Console.WriteLine(JsonConvert.SerializeObject(subCategories.Select( c => c.Title).ToList()));
                subCategories.Sort(CategoryRow.Comparer);
                //Console.WriteLine(JsonConvert.SerializeObject(subCategories.Select(c => c.Title).ToList()));
                if (subCategories != null)
                {
                    List<CategoryRowDto> list = [];
                    foreach (CategoryRow categoryRow in subCategories)
                    {
                        list.Add(new CategoryRowDto(categoryRow));
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
        //[ResponseCache(Duration = 12, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "categoryKeyParam" })]
        public async Task<IActionResult> GetCategoryRowsUpTheTree([FromQuery(Name="catKey")] CategoryKey categoryKey, int pageSize, string? includeQuestionId)
        //string workspace, string topId, string id
        {
            try
            {
                var rowService = new CategoryRowService(dbService, categoryKey.Workspace);
                //var categoryKey = new CategoryKey(workspace, topId, id);
                CategoryRowEx categoryRowEx = await rowService.GetRowsUpTheTree(categoryKey, pageSize, includeQuestionId);
                if (categoryRowEx != null && categoryRowEx.categoryRow == null)
                {
                    // if key from localStorage, doesn't exit any more, use topId
                    categoryKey.Id = categoryKey.TopId;
                    categoryRowEx = await rowService.GetRowsUpTheTree(categoryKey, pageSize, includeQuestionId);
                }
                //Console.WriteLine(JsonConvert.SerializeObject(categoryEx));
                var categoryDtoEx = new CategoryRowDtoEx(categoryRowEx);
                return Ok(categoryDtoEx);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                Console.WriteLine(msg);
                return BadRequest(new CategoryRowDtoEx(null, msg));
            }
        }


        [HttpGet("{hidrate}/{pageSize}/{includeQuestionId}")]
        public async Task<IActionResult> GetCategoryRow(bool hidrate, int pageSize, string? includeQuestionId,
              [FromQuery(Name="catKey")]CategoryKey categoryKey
        )
        {
            try
            {
                var rowService = new CategoryRowService(dbService, categoryKey.Workspace);
                // one fine day use this style
                //using (var db = new Db(this.Configuration)) {}

                CategoryRow categoryRow = await rowService.GetCategoryRow(categoryKey, hidrate, pageSize, includeQuestionId);
                var categoryRowDto = new CategoryRowDto(categoryRow);
                var categoryRowDtoEx = new CategoryRowDtoEx(categoryRowDto, "");
                if (categoryRowDtoEx.categoryRowDto != null)
                {
                    return Ok(categoryRowDtoEx);
                }
                return NotFound(categoryRowDtoEx);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
