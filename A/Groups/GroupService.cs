using Azure;
using Knowledge.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using KnowledgeAPI.A.Answers;
using KnowledgeAPI.A.Answers.Model;
using KnowledgeAPI.A.Groups.Model;
using KnowledgeAPI.Common;
using KnowledgeAPI.Q.Categories.Model;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace KnowledgeAPI.A.Groups
{
    public class GroupService : GroupRowService
    {
        // public DbService? Db { get; set; } = null;
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

        public GroupService()
        {
        }

        //public Group(IConfiguration configuration)
        //{
        //    Group.Db = new Db(configuration);
        //}

        public GroupService(DbService db, string workspace)
        {
            Db = db;
            // this._openAIEmbeddingService = db.openAIEmbeddingService;
            _workspace = workspace;
        }
     

        public async Task<GroupEx> GetGroup(GroupKey groupKey, bool hidrate, int pageSize, string? includeAnswerId)
        {
            var (workspace, topId, partitonKey, id, _) = groupKey;
            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                //ItemResponse<Group> aResponse =
                Group group = await myContainer!.ReadItemAsync<Group>(id, new PartitionKey(partitonKey));
                //Console.WriteLine(JsonConvert.SerializeObject(group));

                if (group != null)
                {
                    /*
                    /////////////////////
                    //// subGroupRows
                    //List<GroupRow> subGroups = await GetSubGroupRows(myContainer, PartitionKey, Id);
                    //group.SubGroups = subGroups;
                    group.SubGroups = [];
                    */

                    ///////////////////
                    // answers
                    if (pageSize > 0)
                    {
                        // hidrate collections except answers, like  group.x = hidrate;  
                        if (group.NumOfAnswers > 0)
                        {
                            var answerService = new AnswerService(Db, workspace);
                            var answerKey = new AnswerKey(groupKey);
                            AnswersMore answersMore = await answerService.GetAnswers(answerKey, 0, pageSize, includeAnswerId??"null");
                            group.AnswerRows = answersMore.AnswerRows.ToList(); // .Select(answerRow => new Answer(answerRow))
                            group.HasMoreAnswers = answersMore.HasMoreAnswers;
                        }
                    }
                }
                return new GroupEx(group, "");
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                return new GroupEx(null, ex.Message);
            }
        }

        // TODO make CtrlController as the base class for:  GroupRowController and GroupController
        internal async Task<List<GroupRow>> GetSubGroupRows(Container myContainer, string PartitionKey, string id)
        {
            var sqlQuery = $"SELECT * FROM c WHERE c.Type = 'group'  AND "
            + (
                id == "null"
                    ? $" IS_NULL(c.ParentId)"
                    : $" c.ParentId = '{id}'"
            );
            QueryDefinition queryDefinition = new(sqlQuery);
            FeedIterator<Group> queryResultSetIterator = myContainer!.GetItemQueryIterator<Group>(queryDefinition);
            List<GroupRow> subCategorRows = [];
            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<Group> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (Group group in currentResultSet)
                {
                    subCategorRows.Add(new GroupRow(group));
                }
            }
            return subCategorRows;
        }


        /*public async Task<GroupEx> GetGroupWithSubGroups(GroupKey groupKey)
        {
            var myContainer = await container();
            var (TopId, Id) = groupKey;
            var partitionKey = groupKey.PartitionKey;
            try
            {
                Group group = await myContainer!.ReadItemAsync<Group>(Id, new PartitionKey(PartitionKey));
                var groupRow = new GroupRow(group);
                List<GroupRow> subGroups = await GetSubGroupRows(myContainer, PartitionKey, Id);
                    // bio neki []
                groupRow.SubGroups = subGroups;
                return new GroupEx(group, "");
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                return new GroupEx(null, ex.Message);
            }
        }*/

        public async Task<HttpStatusCode> CheckDuplicate(string ws, string title, string? id = null) //AnswerData answerData)
        {
            var sqlQuery = $"SELECT c.id FROM c WHERE c.Workspace = '{ws}' AND c.Type = 'group' AND " +
                $"(c.Title = '{title.Replace("\'", "\\'")}'" + 
                   (id != null ? $" OR c.Id = '{id}')" : $")");
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

        public async Task<string> AddGroup(GroupData groupData)
        {
            var (workspace, topId, partitionKey, id, title, link, header, parentId, kind, level, variations, 
                    groups, answers) = groupData;
            var myContainer = await container();
            string msg = string.Empty;
            try
            {
                //if (id == "DOMAIN")
                //{
                //    for (var i = 1; i <= 15; i++) 
                //        answers!.Add(new AnswerData(id, $"Test row for DOMAIN " + i.ToString("D3")));
                //}
                var cat = new Group(groupData);
                cat.Doc1 = string.Empty;
                GroupEx groupEx = await AddNewGroup(myContainer, cat);
                if (groupEx.group != null)
                {
                    Group group = groupEx.group;
                    if (groups != null)
                    {
                        foreach (var subGroupData in groups)
                        {
                            subGroupData.Workspace = workspace;
                            subGroupData.TopId = topId;
                            subGroupData.ParentId = group.Id;
                            subGroupData.Level = group.Level + 1;
                            msg = await AddGroup(subGroupData);
                            if (msg != string.Empty)
                                break;
                            await Task.Delay(1000);
                            Thread.Sleep(1000);
                        }
                    }
                    if (answers != null)
                    {
                        var answerService = new AnswerService(Db!, workspace);

                        List<Task> tasks = new List<Task>(500);
                        foreach (var answerData in answers)
                        {
                            answerData.Workspace = workspace;
                            answerData.TopId = topId;
                            answerData.ParentId = group.Id;
                            //AnswerEx answerEx = await answerService.AddAnswer(answerData);
                            //if (answerEx.answer == null)
                            //{
                            //    msg = answerEx.msg;
                            //    break;
                            //}
                            var answer = new Answer(answerData);
                            //Container answersContainer = await answerService.container();
                            var aResp = answerService.CreateItemAsync(answer);
                            tasks.Add(aResp
                                .ContinueWith(itemResponse =>
                                {
                                    if (!itemResponse.IsCompletedSuccessfully)
                                    {
                                        AggregateException innerExceptions = itemResponse.Exception.Flatten();
                                        if (innerExceptions.InnerExceptions.FirstOrDefault(innerEx => innerEx is CosmosException) is CosmosException cosmosException)
                                        {
                                            Console.WriteLine($"Received {cosmosException.StatusCode} ({cosmosException.Message}).");
                                        }
                                        else
                                        {
                                            Console.WriteLine($"Exception {innerExceptions.InnerExceptions.FirstOrDefault()}.");
                                        }
                                    }
                                }));
                        }
                        // Wait until all are done
                        await Task.WhenAll(tasks);
                    }
                    // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                    // Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", aResponse.Resource.Id, aResponse.RequestCharge);
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return msg;
        }

        public async Task<GroupEx> AddNewGroup(Container cntr, Group group)
        {
            var (workspace, topId, partitionKey, id, parentId, title, link, header, level, kind,
                hasSubGroups, subGroups,
                hasMoreAnswers, numOfAnswers, answerRows, variations, isExpanded, doc1) = group;
            var myContainer = cntr != null ? cntr : await container();
            string msg = string.Empty;
            try
            {
                // Check if the id already exists
                ItemResponse<Group> aResponse =
                    await myContainer!.ReadItemAsync<Group>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                msg = $"Group in database with Id: {id} already exists"; //, aResponse.Resource.Id
                Console.WriteLine(msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                try
                {
                    // Check if the title already exists
                    HttpStatusCode statusCode = await CheckDuplicate(workspace, title, id);
                    msg = $"Group in database with Id: {id} or Title: {title} already exists";
                    Console.WriteLine(msg);
                }
                catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
                {
                    // Create an item in container.Note we provide the value of the partition key for this item
                    /*
                    var vectors = _openAIEmbeddingService
                        .GetEmbeddingsAsync(JsonConvert.SerializeObject(new CatQuestEmbedded(group)))
                        .GetAwaiter()
                        .GetResult();
                    group.vectors = vectors!.ToList();
                    */

                    ItemResponse<Group> aResponse =
                        await myContainer!.CreateItemAsync(
                            group,
                            new PartitionKey(partitionKey)
                        );
                    // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                    Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", aResponse.Resource.Id, aResponse.RequestCharge);
                    return new GroupEx(aResponse.Resource, "");
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return new GroupEx(null, msg);
        }

        public async Task<GroupEx> CreateGroup(GroupDto groupDto)
        {
            //var (Id, PartitionKey) = groupDto;
            groupDto.Id = groupDto.Title.Trim().Replace(' ', '_').ToUpper();
            if (groupDto.Id.Trim().ToLower() == "archived" || groupDto.Id.Trim().ToLower() == "root")
                return new GroupEx(null, "Title can't be 'archived or root' ");
            var myContainer = await container();
            var group = new Group(groupDto);
            group.Doc1 = string.Empty;
            GroupEx groupEx = await AddNewGroup(myContainer, group);
            // update parentId
            if (group.ParentId != null)
            {
                await UpdateHasSubGroups(myContainer, group.PartitionKey, group.ParentId, group.Created!.NickName); //.ParentId, group.Created!.NickName);
            }
            return groupEx;
        }

        /*
        public async Task<GroupEx> UpdateGroup(GroupDto groupDto)
        {
            var myContainer = await container();
            string msg = string.Empty;
            try
            {
                var (partitionKey, id, parentId, title, link, level, kind, variations, modified) = groupDto;
                // Read the item to see if it exists.  
                ItemResponse<Group> aResponse =
                    await myContainer!.ReadItemAsync<Group>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                Group group = aResponse.Resource;
                // Update the item fields
                group.Title = title;
                group.Link = link;
                group.Kind = kind;
                group.Variations = variations;
                group.ParentId = parentId;
                if (modified != null)
                {
                    group.Modified = new WhoWhen(modified.NickName);
                }
                aResponse = await myContainer.ReplaceItemAsync(group, id, new PartitionKey(partitionKey));
                Console.WriteLine("Updated Group [{0},{1}].\n \tBody is now: {2}\n", title, id, group);

                // update parentId
                //groupDto.Modified = groupDto.Modified;
                //await UpdateHasSubGroups(groupDto);

                return new GroupEx(group, msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                msg = $"Group Id: {groupDto.Id} NotFound in database."; //, aResponse.RequestCharge);
                Console.WriteLine(msg); //, aResponse.RequestCharge);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                msg = ex.Message;
                Console.WriteLine(ex.Message);
            }
            return new GroupEx(null, msg);
        }
        */


        public async Task<GroupEx> UpdateGroup(GroupDto groupDto)
        {
            var myContainer = await container();
            string msg = string.Empty;
            try
            {
                var (workspace, topId, partitionKey, id, parentId, title, link, level, kind, variations, modified, doc1) = groupDto;
                // Read the item to see if it exists.  
                ItemResponse<Group> aResponse =
                    await myContainer!.ReadItemAsync<Group>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                Group group = aResponse.Resource;
                var doUpdate = true;
                if (!group.Title.Equals(title, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        HttpStatusCode statusCode = await CheckDuplicate(workspace, title);
                        doUpdate = false;
                        msg = $"Answer with Title: \"{title}\" already exists in database.";
                        Console.WriteLine(msg);
                        return new GroupEx(null, msg);
                    }
                    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        //answer.Title = q.Title;
                    }
                }
                if (doUpdate)
                {
                    if (group.ParentId != parentId)
                    {
                        // changed Group
                    }
                    // Update the item fields
                    group.Title = title;
                    group.Link = link;
                    group.Kind = kind;
                    group.Variations = variations;
                    group.ParentId = parentId;
                    group.Modified = new WhoWhen(modified!.NickName);
                    aResponse = await myContainer.ReplaceItemAsync(group, id, new PartitionKey(partitionKey));
                    return new GroupEx(aResponse.Resource, "");
                }
                return new GroupEx(group, msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                msg = $"Group Id: {groupDto.Id} NotFound in database."; //, aResponse.RequestCharge);
                Console.WriteLine(msg); //, aResponse.RequestCharge);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                msg = ex.Message;
                Console.WriteLine(ex.Message);
            }
            return new GroupEx(null, msg);
        }

        public async Task<Group> UpdateNumOfAnswers(GroupKey groupKey, WhoWhen modified, int num)
        {
            var (workspace, topId, partitionKey, id, _) = groupKey;
            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Group> aResponse =
                    await myContainer!.ReadItemAsync<Group>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                Group group = aResponse.Resource;
                
                // Update the item fields
                group.NumOfAnswers = num;
                group.Modified = new WhoWhen(modified!.NickName);
                aResponse = await myContainer.ReplaceItemAsync(group, group.Id, new PartitionKey(group.PartitionKey));
                Console.WriteLine("===>>> Updated Group NumOfAnswers [{0},{1}].\n", group.Title, group.Id);
                return group;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine("Group item {0}/{1} NotFound in database.\n", partitionKey, id); //, aResponse.RequestCharge);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        public async Task<Group> UpdateHasSubGroups(Container cntr, string partitionKey, string id, string nickName)
        {
            var myContainer = cntr != null ? cntr : await container();
            try
            {
                var PartitionKey = partitionKey;
                // Read the item to see if it exists.  
                ItemResponse<Group> aResponse =
                    await myContainer!.ReadItemAsync<Group>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                Group group = aResponse.Resource;

                var sql = $"SELECT value count(1) FROM c WHERE c.Type = 'group' " +
                    $"AND c.partitionKey='{partitionKey}' " +
                    $"AND c.ParentId='{id}' ";

                int num = await CountItems(myContainer, sql);
                Console.WriteLine($"============================ num: {num}");

                group.HasSubGroups = num > 0;
                group.Modified = new WhoWhen(nickName);

                aResponse = await myContainer.ReplaceItemAsync(group, id, new PartitionKey(partitionKey));
                return group;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine("Group item {0}/{1} NotFound in database.\n", ParentId, ParentId); //, aResponse.RequestCharge);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return null;
        }


        public async Task<int> CountItems(Container myContainer, string sqlQuery)
        {
            int count = 0;
            var query = myContainer.GetItemQueryIterator<int>(new QueryDefinition(sqlQuery));
            while (query.HasMoreResults)
            {
                FeedResponse<int> response = await query.ReadNextAsync();
                count += response.Resource.FirstOrDefault();
            }
            return count;
        }

        public async Task<GroupEx> GetGroup(GroupKey groupKey)
        {
            var (workspace, topId, partitionKey, id, _) = groupKey;
            string msg = string.Empty;
            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Group> aResponse =
                    await myContainer.ReadItemAsync<Group>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                Group group = aResponse.Resource;
                return new GroupEx(group, msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                msg = $"Group {partitionKey}/{id} NotFound in database.";
                Console.WriteLine(msg); //, aResponse.RequestCharge);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return new GroupEx(null, msg);
        }

        public async Task<GroupEx> ArchiveGroup(Container? cntr, GroupKey categoryKey, string nickName)
        {
            var myContainer = cntr != null ? cntr : await container();
            var (workspace, topId, partitionKey, id, parentId) = categoryKey;

            string msg = string.Empty;
            try
            {
                ItemResponse<Group> aResponse =
                    await myContainer.ReadItemAsync<Group>(id, new PartitionKey(partitionKey));
                Group group = aResponse.Resource;
                if (group.HasSubGroups)
                {
                    return new GroupEx(null, "HasSubCategories");
                }
                else if (group.NumOfAnswers > 0)
                {
                    return new GroupEx(null, "HasAnswers");
                }
                await myContainer.DeleteItemAsync<Group>(
                        id,
                        new PartitionKey(partitionKey)
                    );

                group.PartitionKey = $"{workspace}/archived";
                group.Type += "archived";
                group.Modified = new WhoWhen(nickName);
                GroupEx categoryEx = await AddNewGroup(myContainer, group);

                // update parentGroup
                if (parentId != null)
                {
                    var parentGrp = await UpdateHasSubGroups(myContainer, partitionKey, parentId, group.Modified!.NickName);
                    return new GroupEx(parentGrp, "OK");
                }
                else
                {
                    return new GroupEx(group, "OK"); // parentGroup of topRow is null
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                msg = $"Group {id} NotFound in database."; //, aResponse.RequestCharge);
                Console.WriteLine(msg);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return new GroupEx(null, msg);
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



