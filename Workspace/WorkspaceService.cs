using Azure;
using Knowledge.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using KnowledgeAPI.A.Answers;
using KnowledgeAPI.A.Answers.Model;
using KnowledgeAPI.A.Groups;
using KnowledgeAPI.A.Workspaces.Model;
using KnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace KnowledgeAPI.A.Workspaces
{
    public class WorkspaceService: GroupRowService
    {
        public DbService? Db { get; set; } = null;
        //public OpenAIService _openAIEmbeddingService = null;

        private Container? _container = null;
        protected string _workspace;

        public async Task<Container> container()
        {
            //ThroughputProperties throughputProperties = ThroughputProperties.CreateAutoscaleThroughput(1000);

            // Define new container properties including the vector indexing policy
            ContainerProperties properties = new ContainerProperties(id: containerId, partitionKeyPath: "/partitionKey")
            {
                // Set the default time to live for cache items to 1 day
                DefaultTimeToLive = null, // = 86400,

                // Define the vector embedding container policy
                //VectorEmbeddingPolicy = new(
                //new Collection<Embedding>(
                //[
                //    new Embedding()
                //    {
                //        Path = "/vectors",
                //        DataType = VectorDataType.Float32,
                //        DistanceFunction = DistanceFunction.Cosine,
                //        Dimensions = 1536
                //    }
                //])),
                //IndexingPolicy = new IndexingPolicy()
                //{
                //    // Define the vector index policy
                //    VectorIndexes = new()
                //    {
                //        new VectorIndexPath()
                //        {
                //            Path = "/vectors",
                //            Type = VectorIndexType.QuantizedFlat
                //        }
                //    }
                //}
            };

            _container ??= await Db!.GetContainer(containerId, properties); //, throughputProperties);
            return _container;
        }

        public WorkspaceService()
        {
        }

        //public Workspace(IConfiguration configuration)
        //{
        //    Workspace.Db = new Db(configuration);
        //}

        public WorkspaceService(DbService db, string workspace)
        {
            Db = db;
            // this._openAIEmbeddingService = db.openAIEmbeddingService;
            _workspace = workspace;
        }
     

        public async Task<WorkspaceEx> GetWorkspace(WorkspaceKey workspaceKey, bool hidrate, int pageSize, string? includeAnswerId)
        {
            var (partitonKey, id) = workspaceKey;
            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                //ItemResponse<Workspace> aResponse =
                Workspace workspace = await myContainer!.ReadItemAsync<Workspace>(id, new PartitionKey(partitonKey));
                //Console.WriteLine(JsonConvert.SerializeObject(workspace));

                if (workspace != null)
                {
                    /*
                    /////////////////////
                    //// subWorkspaceRows
                    //List<WorkspaceRow> subWorkspaces = await GetSubWorkspaceRows(myContainer, PartitionKey, Id);
                    //workspace.SubWorkspaces = subWorkspaces;
                    workspace.SubWorkspaces = [];
                    */
                }
                return new WorkspaceEx(workspace, "");
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                return new WorkspaceEx(null, ex.Message);
            }
        }

        /*public async Task<WorkspaceEx> GetWorkspaceWithSubWorkspaces(WorkspaceKey workspaceKey)
        {
            var myContainer = await container();
            var (TopId, Id) = workspaceKey;
            var partitionKey = workspaceKey.PartitionKey;
            try
            {
                Workspace workspace = await myContainer!.ReadItemAsync<Workspace>(Id, new PartitionKey(PartitionKey));
                var workspaceRow = new WorkspaceRow(workspace);
                List<WorkspaceRow> subWorkspaces = await GetSubWorkspaceRows(myContainer, PartitionKey, Id);
                    // bio neki []
                workspaceRow.SubWorkspaces = subWorkspaces;
                return new WorkspaceEx(workspace, "");
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                return new WorkspaceEx(null, ex.Message);
            }
        }*/

        public async Task<HttpStatusCode> CheckDuplicate(string id)
        {
            var sqlQuery = $"SELECT c.id FROM c WHERE c.Type = 'workspace' AND c.Id = '{id}'";
            QueryDefinition queryDefinition = new(sqlQuery);
            FeedIterator<string> queryResultSetIterator =
                _container!.GetItemQueryIterator<string>(queryDefinition);
            if (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<string> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                if (currentResultSet.Count == 0)
                {
                    throw new CosmosException("", HttpStatusCode.NotFound, 0, "0", 0);
                }
            }
            return HttpStatusCode.OK;
        }

        
        public async Task<WorkspaceEx> AddNewWorkspace(Container cntr, Workspace workspace)
        {
            var (partitionKey, email, displayName) = workspace;
            var myContainer = cntr != null ? cntr : await container();
            string msg = string.Empty;
            try
            {
                // Check if the id already exists
                ItemResponse<Workspace> aResponse =
                    await myContainer!.ReadItemAsync<Workspace>(
                        email,
                        new PartitionKey(partitionKey)
                    );
                msg = $"Workspace in database with Id: {email} already exists"; //, aResponse.Resource.Id
                Console.WriteLine(msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                //try
                //{
                //    // Check if the title already exists
                //    HttpStatusCode statusCode = await CheckDuplicate(email);
                //    msg = $"Workspace in database with Id: {email} already exists";
                //    Console.WriteLine(msg);
                //}
                //catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
                //{
                    // Create an item in container.Note we provide the value of the partition key for this item
                    /*
                    var vectors = _openAIEmbeddingService
                        .GetEmbeddingsAsync(JsonConvert.SerializeObject(new CatQuestEmbedded(workspace)))
                        .GetAwaiter()
                        .GetResult();
                    workspace.vectors = vectors!.ToList();
                    */

                    ItemResponse<Workspace> aResponse =
                        await myContainer!.CreateItemAsync(
                            workspace,
                            new PartitionKey(partitionKey)
                        );
                    // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                    Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", aResponse.Resource.Id, aResponse.RequestCharge);
                    return new WorkspaceEx(aResponse.Resource, "");
                //}
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return new WorkspaceEx(null, msg);
        }

        public async Task<WorkspaceEx> CreateWorkspace(WorkspaceDto workspaceDto)
        {
            //var (Id, PartitionKey) = workspaceDto;
            var myContainer = await container();
            var whoWhenDto = new WhoWhenDto(workspaceDto.DisplayName!);
            workspaceDto.Created = whoWhenDto;
            workspaceDto.Modified = whoWhenDto;
            var ws = new Workspace(workspaceDto);
            WorkspaceEx workspaceEx = await AddNewWorkspace(myContainer, ws);
            return workspaceEx;
        }


        public async Task<WorkspaceEx> GetWorkspace(WorkspaceKey workspaceKey)
        {
            var (email, partitionKey) = workspaceKey;
            string msg = string.Empty;
            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Workspace> aResponse =
                    await myContainer.ReadItemAsync<Workspace>(
                        email,
                        new PartitionKey(partitionKey)
                    );
                Workspace workspace = aResponse.Resource;
                return new WorkspaceEx(workspace, msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                msg = $"Workspace {partitionKey}/{email} NotFound in database.";
                Console.WriteLine(msg); //, aResponse.RequestCharge);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return new WorkspaceEx(null, msg);
        }

        public async Task<WorkspaceEx> ArchiveWorkspace(Container? cntr, WorkspaceKey workspaceKey, string nickName)
        {
            var myContainer = cntr != null ? cntr : await container();
            var (partitionKey, id) = workspaceKey;

            string msg = string.Empty;
            try
            {
                ItemResponse<Workspace> aResponse =
                    await myContainer.ReadItemAsync<Workspace>(id, new PartitionKey(partitionKey));
                Workspace workspace = aResponse.Resource;
               
                //workspace.Archived = new WhoWhen(workspaceDto.Modified!.NickName);
                //aResponse = await myContainer.ReplaceItemAsync(workspace, workspace.Id, new PartitionKey(workspace.PartitionKey));
                //msg = $"Archived Workspace {workspace.PartitionKey}/{workspace.Id}. {workspace.Title}";
                //Console.WriteLine(msg);
                await myContainer.DeleteItemAsync<Workspace>(
                        id,
                        new PartitionKey(partitionKey)
                    );

                workspace.PartitionKey = $"{workspace}/archived";
                workspace.Type += "archived";
                WorkspaceEx workspaceEx = await AddNewWorkspace(myContainer, workspace);

                return new WorkspaceEx(aResponse.Resource, "OK");
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                msg =$"Workspace {id} NotFound in database."; //, aResponse.RequestCharge);
                Console.WriteLine(msg);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return new WorkspaceEx(null, msg);
        }

        public void Dispose()
        {
            _container = null;
            Db = null;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }


    }
    
}



