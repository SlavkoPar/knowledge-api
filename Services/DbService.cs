

using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualBasic;
using KnowledgeAPI.A;
using KnowledgeAPI.A.Answers.Model;
using KnowledgeAPI.A.Groups;
using KnowledgeAPI.A.Groups.Model;
using KnowledgeAPI.Q;
using KnowledgeAPI.Q.Categories;
using KnowledgeAPI.Q.Categories.Model;
using Newtonsoft.Json;
using System.Net;
using System.Text.RegularExpressions;

namespace Knowledge.Services
{
    public class DbService : IDisposable
    {
        private readonly IConfiguration Configuration;
        private readonly string CosmosDBAccountUri;
        private readonly string CosmosDBAccountPrimaryKey;

        private CosmosClient? cosmosClient = null;
        private Database? _database = null;
        readonly Dictionary<string, Container?> containers = [];

        // The name of the database and container we will create
        private readonly string databaseId = "Knowledge";

        public bool Initiated { get; protected set; }

        public OpenAIService openAIEmbeddingService = null;


        public DbService(IConfiguration configuration)
        {
            Configuration = configuration;
            CosmosDBAccountUri = configuration["CosmosDBAccountUri"];
            CosmosDBAccountPrimaryKey = configuration["CosmosDBAccountPrimaryKey"];

            cosmosClient = new CosmosClient(
                CosmosDBAccountUri,
                CosmosDBAccountPrimaryKey,
                new CosmosClientOptions()
                {
                    ApplicationName = "KnowledgeAPI",
                    AllowBulkExecution = true
                }
            );

            Initialize = CreateInstanceAsync();
            openAIEmbeddingService = new OpenAIService(configuration.GetSection("MySearch"));
        }
        public Task Initialize { get; }

        private async Task CreateInstanceAsync()
        {
            await Task.Delay(1);
            //bool created = await CreateDatabaseIfNotExistsAsync();
            //if (created)
            //{
            //    Console.WriteLine("Created Database: {0}\n", database!.Id);
            //    await AddInitialData();
            //}
            Initiated = true;
        }


        public async Task<string> CreateDatabaseIfNotExistsAsync()
        {
            // Create a new database
            DatabaseResponse response = await cosmosClient!.CreateDatabaseIfNotExistsAsync(databaseId);
            _database = response.Database;
            bool created = response.StatusCode == HttpStatusCode.Created;
            if (created)
            {
                return "OK Database created!";
                //string ret = await AddInitialData();
                //return ret == string.Empty ? "OKDatabase created!" : ret;
            }
            else
            {
                return "Database already exists!";
            }
        }

        // <CreateContainerAsync>
        /// <summary>
        /// Create the container if it does not exist. 
        /// Specifiy "/partitionKey" as the partition key path since we're storing family information, to ensure good distribution of requests and storage.
        /// </summary>
        /// <returns></returns>
        private async Task<Container> CreateContainerAsync(string containerId, ContainerProperties? properties, ThroughputProperties? throughputProperties)
        {
            // Create a new container
            if (properties == null)
            {
                var cprops = new ContainerProperties()
                {
                    Id = containerId,
                    PartitionKeyPath = "/partitionKey"
                };
                var container = await _database!.CreateContainerIfNotExistsAsync(cprops);
                containers.Add(containerId, container);
                return container;
            }
            else
            {
                var container = await _database!.CreateContainerIfNotExistsAsync(properties, throughputProperties!);
                containers.Add(containerId, container);
                return container;
            }
        }
        // </CreateContainerAsync>


        // <ScaleContainerAsync>
        /// <summary>
        /// Scale the throughput provisioned on an existing Container.
        /// You can scale the throughput (RU/s) of your container up and down to meet the needs of the workload. Learn more: https://aka.ms/cosmos-request-units
        /// </summary>
        /// <returns></returns>
        private async Task ScaleContainerAsync(string containerId)
        {
            // Read the current throughput
            try
            {
                Container? container = containers[containerId];
                int? throughput = await container!.ReadThroughputAsync();
                if (throughput.HasValue)
                {
                    Console.WriteLine("Current provisioned throughput : {0}\n", throughput.Value);
                    int newThroughput = throughput.Value + 100;
                    // Update throughput
                    await container.ReplaceThroughputAsync(newThroughput);
                    Console.WriteLine("New provisioned throughput : {0}\n", newThroughput);
                }
            }
            catch (CosmosException cosmosException) when (cosmosException.StatusCode == HttpStatusCode.BadRequest)
            {
                Console.WriteLine("Cannot read container throuthput.");
                Console.WriteLine(cosmosException.ResponseBody);
            }

        }
        // </ScaleContainerAsync>

        List<string> workspaces = ["DEMO"]; // , "SLINDZA"


        public async Task<string> AddInitialGroupData()
        {
            string msg = string.Empty;
            try
            {
                DatabaseResponse response = await cosmosClient!.CreateDatabaseIfNotExistsAsync(databaseId);
                _database = response.Database;
                foreach (var ws in workspaces)
                {
                    var groupService = new GroupService(this, ws);
                    using StreamReader r = new($"InitialData/{ws}/groups-answers.json");
                    string json = r.ReadToEnd();
                    GroupsData? groups = JsonConvert.DeserializeObject<GroupsData>(json);
                    foreach (var topGroupData in groups!.Groups)
                    {
                        topGroupData.Workspace = groups!.Workspace;
                        topGroupData.TopId = topGroupData.Id;
                        topGroupData.ParentId = null;
                        topGroupData.Level = 1;
                        msg = await groupService.AddGroup(topGroupData);
                        if (msg != string.Empty)
                            break;
                        await Task.Delay(1000);
                        Thread.Sleep(1000);
                    }
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return msg;
        }

        public async Task<string> AddInitialCategoryData()
        {
            string msg = string.Empty;
            try
            {
                DatabaseResponse response = await cosmosClient!.CreateDatabaseIfNotExistsAsync(databaseId);
                _database = response.Database;
                foreach (var ws in workspaces)
                {
                    var categoryService = new CategoryService(this, ws);
                    using StreamReader r = new($"InitialData/{ws}/categories-questions.json");
                    string json = r.ReadToEnd();
                    CategoriesData? categoriesData = JsonConvert.DeserializeObject<CategoriesData>(json);
                    foreach (var topCategoryData in categoriesData!.Categories)
                    {
                        topCategoryData.Workspace = categoriesData!.Workspace;
                        topCategoryData.TopId = topCategoryData.Id;
                        topCategoryData.ParentId = null;
                        topCategoryData.Level = 1;
                        msg = await categoryService.AddCategory(topCategoryData);
                        if (msg != string.Empty)
                            break;
                        await Task.Delay(2000);
                        Thread.Sleep(2000);
                    }
                }
                await Task.Delay(2000);
                Thread.Sleep(2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return msg;
        }

        public async Task<Container> GetContainer(string containerId, ContainerProperties? properties=null, ThroughputProperties? throughputProperties=null)
        {
            if (_database == null)
            {
                _database = cosmosClient!.GetDatabase(databaseId);
                await _database.ReadAsync(); // TODO treba li ovo?
                //    bool created = await this.CreateDatabaseIfNotExistsAsync();
                //    if (created)
                //    {
                //        Console.WriteLine("Created Database: {0}\n", this.database.Id);
                //        await this.AddInitialData();
                //    }
            }
            Container? container;
            if (containers.ContainsKey(containerId))
            {
                container = containers[containerId];
            }
            else 
            {
                container = await CreateContainerAsync(containerId, properties, throughputProperties);
                //await ScaleContainerAsync();
            }
            return container!;
        }

        // <GetStartedAsync>
        /// <summary>
        /// Entry point to call methods that operate on Azure Cosmos DB resources in this sample
        /// </summary>
        //public async Task GetStartedAsync()
        //{
            //// Create a new instance of the Cosmos Client
            //this.cosmosClient = new CosmosClient(
            //    CosmosDBAccountUri,
            //    CosmosDBAccountPrimaryKey,
            //    new CosmosClientOptions()
            //    {
            //        ApplicationName = "KnowledgeCosmos"
            //    }
            //);

            //await this.CreateDatabaseAsync();
            //await this.CreateContainerAsync();
            //await this.ScaleContainerAsync();
            //await this.AddItemsToContainerAsync();
            //await this.QueryItemsAsync();
            //await this.ReplaceFamilyItemAsync();
            // await this.DeleteFamilyItemAsync();
            // await this.DeleteDatabaseAndCleanupAsync();
        //}


        public void Dispose()
        {
            foreach (var container in containers) {
                containers[container.Key] = null;
            }   
            containers.Clear();

            _database = null;
            if (cosmosClient != null) {
                cosmosClient.Dispose();
                cosmosClient = null;
            }

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        // </GetStartedDemoAsync>

    }


}
