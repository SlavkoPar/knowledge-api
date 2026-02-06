using Microsoft.AspNetCore.Mvc;
using Knowledge.Services;
using Microsoft.Azure.Cosmos.Core;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KnowledgeAPI.Admin
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    //[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
    public class AdminController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        DbService dbService { get; set; }

        public AdminController(IConfiguration configuration)
        {
            dbService = new DbService(configuration);
            dbService.Initialize.Wait();
            Configuration = configuration;
        }

    
        [HttpGet]
        public async Task<IActionResult> CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                string str = await dbService.CreateDatabaseIfNotExistsAsync();
                return str.StartsWith("OK") ? Ok(str) : BadRequest(str);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{AddInitialGroupData}")]
        public async Task<IActionResult> AddInitialGroups(string AddInitialGroupData = "Answers")
        {
            try
            {
                string str = await dbService.AddInitialGroupData();
                return str == string.Empty ? Ok("Added Groups/Answers") : BadRequest(str);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{AddInitialCategoryData}/{nesto}")]
        public async Task<IActionResult> AddInitialCategories(string AddInitialCategoryData = "Questions", string nesto = "xyz")
        {
            try
            {
                string str = await dbService.AddInitialCategoryData();
                return str == string.Empty ? Ok("Added Categories/Questions") : BadRequest(str);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
