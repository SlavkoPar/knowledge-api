using Knowledge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KnowledgeAPI.Common;
using KnowledgeAPI.Q.Categories.Model;
using KnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Collections.Generic;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KnowledgeAPI.Q.Categories
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    //[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
    public class CategoryController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        DbService dbService { get; set; }

        public CategoryController(IConfiguration configuration)
        {
            dbService = new DbService(configuration);
            dbService.Initialize.Wait();
            Configuration = configuration;
        }

                

        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "partitionKey", "id", "pageSize", "includeQuestionId" })]
        [HttpGet]
        public async Task<IActionResult> GetCategory([FromQuery] CategoryQueryParams categoryQueryParams)
        {
            try
            {
                var (workspace, topId, partitionKey, id, startCursor, pageSize, includeQuestionId) = categoryQueryParams;
                CategoryKey categoryKey = new(workspace, topId, id);
                var categoryService = new CategoryService(dbService, workspace);
                CategoryEx categoryEx = await categoryService.GetCategory(categoryKey, true /* hidrate*/,
                       pageSize,
                       includeQuestionId //== "null" ? null : includeQuestionId
                );
                if (categoryEx.category != null)
                {
                    return Ok(new CategoryDtoEx(categoryEx));
                }
                return NotFound(new CategoryDtoEx(categoryEx));
            }
            catch (Exception ex)
            {
                return BadRequest(new CategoryDtoEx(ex.Message));
            }
        }


        [HttpGet("{workspace}/{topId}/{id}/{pageSize}/{includeQuestionId}")]
        //[ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "partitionKey", "id", "pageSize", "includeQuestionId" })]
        public async Task<IActionResult> GetCategory(string workspace, string topId, string id, int pageSize, string? includeQuestionId)
        {
            try
            {
                CategoryKey categoryKey = new (workspace, topId, id);

                //using(var db = new Db(this.Configuration))
                //{
                //    await db.Initialize;
                // TODO Question.Db = db;
                //var category = new Category(_Db);
                // var container = await Db.GetContainer(this.containerId);
                //Category cat = await category.GetCategory(
                //    partitionKey, id, true, pageSize, includeQuestionId=="null" ? null : includeQuestionId);
                var categoryService = new CategoryService(dbService, workspace);

                CategoryEx categoryEx = await categoryService.GetCategory(categoryKey, true /* hidrate*/,  
                       pageSize, 
                       includeQuestionId //== "null" ? null : includeQuestionId
                );
                if (categoryEx.category != null)
                {
                    return Ok(new CategoryDtoEx(categoryEx));
                }
                return NotFound(new CategoryDtoEx(categoryEx));
            }
            catch (Exception ex)
            {
                return BadRequest(new CategoryDtoEx(ex.Message));
            }
        }

        /*
        [HttpGet("{partitionKey}/{id}/{hidrate}")]
        //[ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "partitionKey", "id" })]
        public async Task<IActionResult> GetCategoryHidrated(string partitionKey, string id, bool hidrate)
        {
            // hidrate collections except questions
            try
            {
                CategoryKey categoryKey = new(partitionKey, id);
                // TODO what does  /partitionKey mean?
                //using (var db = new Db(this.Configuration))
                //{
                //await db.Initialize;
                //var category = new Category(db);
                var categoryService = new CategoryService(dbService);
                CategoryEx categoryEx = await categoryService.GetCategoryHidrated(categoryKey, 0, null);
                if (categoryEx.category != null)
                {
                    return Ok(new CategoryDtoEx(categoryEx));
                }
                //}
                return NotFound(new CategoryDtoEx(categoryEx));
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
        public async Task<IActionResult> GetCategoryWithSubCategories(string partitionKey, string id)
        {
            // hidrate collections except questions
            try
            {
                CategoryKey categoryKey = new(partitionKey, id);
                // TODO what does  /partitionKey mean?
                //using (var db = new Db(this.Configuration))
                //{
                //await db.Initialize;
                //var category = new Category(db);
                var categoryService = new CategoryService(dbService);
                CategoryEx categoryEx = await categoryService.GetCategoryWithSubCategories(categoryKey);
                if (categoryEx.category != null)
                {
                    return Ok(new CategoryDtoEx(categoryEx));
                }
                //}
                return NotFound(new CategoryDtoEx(categoryEx));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        */

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Post([FromBody] CategoryDto categoryDto)
        {
            try
            {
                Console.WriteLine("===>>> CreateCategory: {0} \n", categoryDto.Title);
                var categoryService = new CategoryService(dbService, categoryDto.Workspace);
                CategoryEx categoryEx = await categoryService.CreateCategory(categoryDto);
                return Ok(new CategoryDtoEx(categoryEx));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [Authorize]
        public async Task<IActionResult> Put([FromBody] CategoryDto categoryDto)
        {
            try
            {
                Console.WriteLine("===>>> UpdateCategory: {0} \n", categoryDto.Title);
                var categoryService = new CategoryService(dbService, categoryDto.Workspace);
                CategoryEx categoryEx = await categoryService.UpdateCategory(categoryDto);
                var (category, msg) = categoryEx;
                if (category != null)
                {
                    if (category.ParentId != categoryDto.ParentId)
                    {
                        // TODO we need to update category questions, too 
                        // category changed 
                        /*
                        await categoryService.UpdateHasSubCategories(
                            new CategoryKey(categoryDto.PartitionKey, categoryDto.ParentId!),
                            new WhoWhen(categoryDto.Modified!));
                        await categoryService.UpdateNumOfQuestions(
                            new CategoryKey(category.PartitionKey, category.ParentId!),
                            category.Modified!,
                            1);
                        */
                    }
                    return Ok(new CategoryDtoEx(categoryEx));
                }
                return NotFound(new CategoryDtoEx("Jok Found"));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //[HttpDelete("{partitionKey}, {id}")]
        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> Delete([FromBody] CategoryRowDto rowDto) //string PartitionKey, string id)
        {
            try
            {
                var categoryKey = new CategoryKey(rowDto);
                var categoryService = new CategoryService(dbService, rowDto.Workspace);
                CategoryEx categoryEx = await categoryService.ArchiveCategory(null, categoryKey, rowDto.Modified!.NickName);
                if (categoryEx.category != null)
                {
                    return Ok(new CategoryDtoEx(categoryEx));
                }
                else
                {
                    return NotFound(categoryEx);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
