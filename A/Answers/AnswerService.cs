using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Knowledge.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.Cosmos;
using KnowledgeAPI.A.Answers.Model;
using KnowledgeAPI.A.Groups;
using KnowledgeAPI.A.Groups.Model;
using KnowledgeAPI.Common;
using KnowledgeAPI.Q.Categories;
using KnowledgeAPI.Q.Categories.Model;
using KnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Net;


namespace KnowledgeAPI.A.Answers
{
    public class AnswerService : IDisposable
    {
        public DbService? Db { get; set; } = null;
        //public OpenAIService? _openAIEmbeddingService { get; set; }  = null;


        private readonly string containerId = "Answers";
        private Container? _container = null;
        private string _workspace = null;


     
        readonly string _rowColumns = @"'', '', c.TopId, c.id, c.ParentId, 
                            c.Title, c.Vectors, c.NumOfAssignedAnswers
                            FROM c ";
        // c.Type = 'answer' AND 


        protected string getPartitionKey(string topId)
        {
            return _workspace + "/" + topId;
        }


        public async Task<Container> container()
        {
           // ThroughputProperties throughputProperties = ThroughputProperties.CreateAutoscaleThroughput(1000);

            // Define new container properties including the vector indexing policy
            ContainerProperties properties = new ContainerProperties(id: containerId, partitionKeyPath: "/partitionKey")
            {
                // Set the default time to live for cache items to 1 day
                DefaultTimeToLive = null,     //86400,

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


        //public string? PartitionKey { get; set; } = null;
        public AnswerService()
        {
        }

        public AnswerService(DbService Db, string workspace)
        {
            this.Db = Db;
            // this._openAIEmbeddingService = Db.openAIEmbeddingService;
            _workspace = workspace;
        }
                 
        public async Task<HttpStatusCode> CheckDuplicate(string ws, string? Title, string? Id = null)
        {
            var sqlQuery = Title != null
                ? $"SELECT c.id FROM c WHERE c.Workspace = '{ws}' AND c.Type = 'answer' AND c.Title = '{Title.Trim().Replace("\'", "\\'")}' "
                : $"SELECT c.id FROM c WHERE c.Workspace = '{ws}' AND c.Type = 'answer' AND c.Id = '{Id}' ";
            QueryDefinition queryDefinition = new(sqlQuery);
            FeedIterator<string> queryResultSetIterator =
                _container!.GetItemQueryIterator<string>(queryDefinition);
            if (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<string> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                if (currentResultSet.Count == 0)
                {
                    throw new CosmosException("Answer with Title doesn't exist", HttpStatusCode.NotFound, 0, "0", 0);
                }
            }
            return HttpStatusCode.Found;
        }

        public async Task<AnswerEx> AddAnswer(AnswerData answerData)
        {
            var myContainer = await container();
            //Console.WriteLine(JsonConvert.SerializeObject(answerData));
            string msg = string.Empty;
            try
            {
                var answer = new Answer(answerData);
                //Console.WriteLine("----->>>>> " + JsonConvert.SerializeObject(answer));
                // Read the item to see if it exists.  
                await CheckDuplicate(answerData.Workspace, answerData.Title);
                msg = $":::::: Item in database with Title: {answerData.Title} already exists";
                Console.WriteLine(msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var answer = new Answer(answerData);
                AnswerEx answerEx = await AddNewAnswer(myContainer, answer);
                return answerEx;
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                msg = ex.Message;
                Console.WriteLine(msg);
            }
            return new AnswerEx(null, msg);
        }

        public async Task<ItemResponse<Answer>> CreateItemAsync(Answer answer)
        {
            var myContainer = await container();
            ItemResponse<Answer> aResponse =
                        await myContainer.CreateItemAsync(
                                answer,
                                new PartitionKey(answer.PartitionKey)
                            );
            return aResponse;
        }

        public async Task<AnswerEx> AddNewAnswer(Container? cntr, Answer answer)
        {
            var (workspace, topId, partitionKey, id, title, parentId, type, source, status) = answer;
            var myContainer = cntr != null ? cntr : await container();
            string msg = string.Empty;
            try
            {
                // Check if the id already exists
                ItemResponse<Answer> aResponse =
                    await myContainer!.ReadItemAsync<Answer>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                msg = $"Answer in database with id: {id} already exists\n";
                Console.WriteLine(msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                try
                {
                    // Check if the title already exists
                    HttpStatusCode statusCode = await CheckDuplicate(workspace, title);
                    msg = $"Answer in database with Title: {title} already exists";
                    Console.WriteLine(msg);
                }
                catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
                {

                    /*
                    var vectors = _openAIEmbeddingService!
                        .GetEmbeddingsAsync(JsonConvert.SerializeObject(new CatQuestEmbedded(answer)))
                        .GetAwaiter()
                        .GetResult();
                    answer.vectors = vectors!.ToList();
                    */
                    ItemResponse<Answer> aResponse =
                        await myContainer!.CreateItemAsync(
                                answer,
                                new PartitionKey(partitionKey)
                            );
                    // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                    // Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", aResponse.Resource.Id, aResponse.RequestCharge);
                    return new AnswerEx(aResponse.Resource, "");
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return new AnswerEx(null, msg);
        }

        public async Task<int> CountNumOfAnswers(GroupKey groupKey)
        {
            var (workspace, topId, partitionKey, id, _) = groupKey;
            var sql = $"SELECT value count(1) FROM c WHERE c.Type = 'answer' " +
                $"AND c.partitionKey='{partitionKey}' " +
                $"AND c.ParentId='{id}' ";
            //int numOfQuestions = await questionService.CountItems(sql);
            //Console.WriteLine($"============================ num: {numOfQuestions}");
            var myContainer = await container();
            int count = 0;
            var query = myContainer.GetItemQueryIterator<int>(new QueryDefinition(sql));
            while (query.HasMoreResults)
            {
                FeedResponse<int> response = await query.ReadNextAsync();
                count += response.Resource.FirstOrDefault();
            }
            return count;
        }

        public async Task<AnswerEx> CreateAnswer(GroupService groupService, AnswerDto answerDto)
        {
            var myContainer = await container();
            try
            {
                var answer = new Answer(answerDto);
                AnswerEx answerEx = await AddNewAnswer(myContainer, answer);

                var a = answerEx.answer;
                if (a != null)
                {
                    // Update the item fields
                    var answerKey = new GroupKey(answerDto);
                    int numOfAnswers = await CountNumOfAnswers(answerKey);
                    //Category category = new Category(questionEx.question);
                    await groupService.UpdateNumOfAnswers(
                           answerKey,
                           new WhoWhen(answerDto.Created!),
                           numOfAnswers);
                }

                return answerEx;
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                return new AnswerEx(null, ex.Message);
            }
        }

        public async Task<AnswerEx> GetAnswer(AnswerKey answerKey)
        {
            var myContainer = await container();
            Answer? answer = null;
            string msg = string.Empty;
            try
            {
                var (_, _, partitionKey, id, _) = answerKey;
                answer = await myContainer.ReadItemAsync<Answer>(
                    id,
                    new PartitionKey(partitionKey)
                );
                return new AnswerEx(answer, msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                msg = "NotFound";
                Console.WriteLine(msg);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                msg = ex.Message;
                Console.WriteLine(msg);
            }
            //Console.WriteLine(JsonConvert.SerializeObject(answer));
            return new AnswerEx(null, msg);
        }

        public async Task<AnswerEx> UpdateAnswer(AnswerDto answerDto, GroupService groupService)
        {
            var (workspace, topId, partitionKey, id, oldParentId, newParentId, title, source, status, modified) = answerDto;

            //Console.WriteLine(JsonConvert.SerializeObject(answerDto));
            //Console.WriteLine("========================UpdateAnswer-3");

            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Answer> aResponse =
                    await myContainer.ReadItemAsync<Answer>(
                        id,
                        new PartitionKey(partitionKey!)
                    );  
                Answer answer = aResponse.Resource;

                var doUpdate = true;
                if (!answer.Title.Equals(title, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        HttpStatusCode statusCode = await CheckDuplicate(workspace, title);
                        doUpdate = false;
                        var msg = $"Answer with Title: \"{title}\" already exists in database.";
                        Console.WriteLine(msg);
                        return new AnswerEx(msg);
                    }
                    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        //answer.Title = q.Title;
                    }
                }
                if (doUpdate)
                {
                    // Update the item fields
                    answer.Title = title;
                    answer.Source = source;
                    answer.Status = status;
                    answer.ParentId = newParentId;
                    // answer.PartitionKey = newParentId!;

                    if (modified != null) {
                        answer.Modified = new WhoWhen(modified.NickName);
                    }

                    if (oldParentId != newParentId)
                    {
                        var groupKey = new GroupKey(workspace, topId, id);
                        // parent group changed
                        string msg = await ArchiveAnswer(myContainer, answer);
                        if (msg.Equals(String.Empty))
                        {
                            AnswerEx ex = await AddNewAnswer(myContainer, answer);
                            if (ex.answer != null)
                            {
                                Console.WriteLine("DODAO ANSWER  sa novom praentGroup");
                                groupKey.ParentId = oldParentId;
                                int numOfAnswers = await CountNumOfAnswers(groupKey);
                                await groupService.UpdateNumOfAnswers(
                                       groupKey,
                                       new WhoWhen(modified!),
                                       numOfAnswers);

                                groupKey.ParentId = newParentId!;
                                numOfAnswers = await CountNumOfAnswers(groupKey);
                                await groupService.UpdateNumOfAnswers(
                                       groupKey,
                                       new WhoWhen(modified!),
                                       numOfAnswers);
                            }
                            else
                            {
                                Console.WriteLine("AddNewAnswer PROBLEMOS: " + ex.msg);
                            }
                        }
                    }
                    else
                    {
                        aResponse = await myContainer.ReplaceItemAsync(answer, answer.Id);
                        answer = aResponse.Resource;
                    }
                    Console.WriteLine(JsonConvert.SerializeObject(answer, Formatting.Indented));
                    var answerEx = new AnswerEx(answer, string.Empty);
                    return answerEx;
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var msg = $"Answer Id: \"{answerDto.Id}\" Not Found in database.";
                Console.WriteLine(msg); 
                return new AnswerEx(msg);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return new AnswerEx("Server Problemos Update");
        }


        public async Task<AnswerEx> UpdateAnswer(Answer q)
        {
            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Answer> aResponse =
                    await myContainer!.ReadItemAsync<Answer>(
                        q.Id,
                        new PartitionKey(q.PartitionKey)
                    );
                Answer answer = aResponse.Resource;
                answer.Modified = q.Modified;
                aResponse = await myContainer.ReplaceItemAsync(answer, answer.Id, new PartitionKey(answer.PartitionKey));
                return new AnswerEx(aResponse.Resource, "");
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var msg = $"Answer Id: \"{q.Id}\" Not Found in database.";
                Console.WriteLine(msg);
                return new AnswerEx(null, msg);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return new AnswerEx(null, "Server Problem Update");
        }

         
        public async Task<string> ArchiveAnswer(Container? cntr, Answer answer)
        {
            var myContainer = cntr != null ? cntr : await container();
            //var (workspace, topId, partitionKey, id, oldParentId, newParentId, title, source, status, modified) = answerDto;
            var (workspace, topId, partitionKey, id, title, parentId, type, source, status) = answer;

            Answer? q = null;
            var message = string.Empty;
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Answer> aResponse = 
                    await myContainer.ReadItemAsync<Answer>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                q = aResponse.Resource;

                // now delete answer
                await myContainer.DeleteItemAsync<Answer>(
                        id,
                        new PartitionKey(partitionKey)
                    );

                // Partition keys are crucial for how data is distributed across partitions.
                // PartitionKey is immutable.
                // 1) Item should be deleted
                // 2) Recreated with the new partitionKey
                // Currently we keep only last answer archived
                q.PartitionKey = $"{workspace}/archived";
                q.Type = "archived";
                aResponse = await myContainer.ReadItemAsync<Answer>(
                        id,
                        new PartitionKey(q.PartitionKey)
                    );
                await myContainer.ReplaceItemAsync(q, id);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                if (q != null)
                {
                    //answer.Title += " ARCHIVED"; // otherwise updated Answer can't be added
                    await AddNewAnswer(myContainer, q);
                    Console.WriteLine("DODAO ARCHIVED");
                }
                // message = $"Answer item {id} NotFound in database.";
                //Console.WriteLine(message); //, aResponse.RequestCharge);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                message = ex.Message;   
                Console.WriteLine(ex.Message);
            }
            return message;
        }

        public async Task<AnswersMore> GetAnswers(AnswerKey answerKey, int startCursor, int pageSize, string? includeAnswerId)
        {
            var (workspace, topId, partitionKey, _, parentId) = answerKey;
            var myContainer = await container();
            try
            {
                string sqlQuery = $@"SELECT {_rowColumns} 
                    WHERE c.partitionKey='{workspace}/{topId}' AND c.ParentId = '{parentId}' 
                    ORDER BY c.Title 
                    OFFSET {startCursor} 
                    LIMIT {((includeAnswerId == null) ? pageSize : 9999)}";

                int n = 0;
                bool included = false;

                List<AnswerRow> list = [];
                QueryDefinition queryDefinition = new QueryDefinition(sqlQuery);
                FeedIterator<AnswerRow> queryResultSetIterator = myContainer!.GetItemQueryIterator<AnswerRow>(queryDefinition);
                while (queryResultSetIterator.HasMoreResults)
                {
                    FeedResponse<AnswerRow> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    foreach (AnswerRow answerRow in currentResultSet)
                    {
                        if (includeAnswerId != null && answerRow.Id == includeAnswerId)
                        {
                            included = true;
                            answerRow.Included = true;
                        }
                        else
                        {
                            answerRow.Included = false;
                        }


                        //Console.WriteLine(">>>>>>>> answer is: {0}", JsonConvert.SerializeObject(answer));
                        list.Add(answerRow);
                        n++;
                        if (n >= pageSize && (includeAnswerId == null || included))
                        {
                            return new AnswersMore(list, true);
                        }
                    }
                    return new AnswersMore(list, false);
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return new AnswersMore([], false);
        }

        public async Task<AnswerRowDtosEx> SearchAnswerRows(IConfigurationSection section, string workspace, string userQuery, int count)
        {
            var myContainer = await container();
            string msg = string.Empty;
            try
            {
                // Create a SearchClient to load and query documents
                // Create a SearchIndexClient to send create/delete index commands
                string serviceName = section["endpoint"]!;
                string apiKey = section.GetSection("credential").GetSection("key").Value ?? string.Empty;
                string indexName = section["indexname"]!;
                Uri serviceEndpoint = new Uri($"https://{serviceName}.search.windows.net/");
                AzureKeyCredential credential = new AzureKeyCredential(apiKey);
                SearchIndexClient adminClient = new SearchIndexClient(serviceEndpoint, credential);

                /*
                SearchClient srchclient = new SearchClient(serviceEndpoint, indexName, credential);
                SearchResults<AnswerRow> response;
                SearchOptions options = new SearchOptions()
                {
                    //Filter = "Rating gt 4",
                    //OrderBy = { "Title asc" }
                };
                */

                //options.Select.Add("ParentId");
                //options.Select.Add("Title");
                //options.Select.Add("id");

                //response = srchclient.Search<Answer>("answers", options);
                //var embeddingVector = _openAIEmbeddingService!.GetEmbeddingsAsync(userQuery)
                //    .GetAwaiter()
                //    .GetResult();

                //.GetEmbeddingsAsync(userQuery)
                /*
                var vectors = _openAIEmbeddingService!
                        .GetEmbeddingsAsync(JsonConvert.SerializeObject(new CatQuestEmbedded("answer", userQuery)))
                        .GetAwaiter()
                        .GetResult();
                */
                //var retrivedDocs = cosmosService.SingleVectorSearch(embeddingVector, 0.50)
                //    .GetAwaiter()
                //    .GetResult();

                //  x.ParentId,
                // c.ParentId, 
                /*
                string queryText = @$"SELECT Top {count} x.partitionKey, x.id, x.Type, x.Title, x.similarityScore 
                            FROM (SELECT c.partitionKey, c.id, c.Type, c.Title,  
                                VectorDistance(c.vectors, @vectors, false) as similarityScore FROM c) x
                                    WHERE x.Type = 'answer' AND x.similarityScore > @similarityScore ORDER BY x.similarityScore desc";

                var similarityScore = 0.60;
                var queryDefinition = new QueryDefinition(queryText)
                    .WithParameter("@vectors", vectors)
                    .WithParameter("@similarityScore", similarityScore);
                
                using FeedIterator<AnswerRow> resultSet = myContainer.GetItemQueryIterator<AnswerRow>(queryDefinition);

                var answerRows = new List<AnswerRow>();
                while (resultSet.HasMoreResults)
                {
                    FeedResponse<AnswerRow> resp = await resultSet.ReadNextAsync();
                    answerRows.AddRange(resp);
                }
                */
                /*
                var queryDef = new QueryDefinition(
                    query: @$"SELECT c.id, c.partitionKey, c.Type, c.Title, VectorDistance(c.contentVector, @vectors) AS SimilarityScore 
                                FROM c WHERE c.Type = 'answer' 
                                ORDER BY VectorDistance(c.contentVector, @vectors)")
                    .WithParameter("@vectors", vectors);

                using FeedIterator<AnswerRow> feed = myContainer.GetItemQueryIterator<AnswerRow>(
                    queryDefinition: queryDef
                );
                var answerRows = new List<AnswerRow>();
                while (feed.HasMoreResults)
                {
                    FeedResponse<AnswerRow> resp = await feed.ReadNextAsync();
                    foreach (AnswerRow answerRow in resp)
                    {
                        //Console.WriteLine($"Found item:\t{item}");
                        answerRows.Add(answerRow);
                    }
                }
                */

                //(string completion, int promptTokens, int completionTokens) = _openAIEmbeddingService
                //    .GetChatCompletionAsync(userQuery, JsonConvert.SerializeObject(answerRows))
                //    .GetAwaiter()
                //    .GetResult();

                /*
                var answerDtos = new List<AnswerRowDto>();
                foreach (var row in answerRows) //.FindAll(q => q.Type == "answer"))
                {
                    row.ParentId = row.PartitionKey;
                    row.vectors = null;
                    answerDtos.Add(new AnswerRowDto(row));
                }

                return answerDtos;
                */
                var words = userQuery //.ToLower()
                            .Replace("?", "")
                            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Where(w => w.Length > 2)
                            .ToList();

                // order of fields matters
                var sqlQuery = $"SELECT {_rowColumns} WHERE c.Workspace = '{workspace}' AND c.Type = 'answer' AND ";
                if (words.Count == 1)
                {
                    sqlQuery += $" CONTAINS(c.Title, \"{words[0]}\", true) ";
                }
                else
                {
                    sqlQuery += "(";
                    for (var i=0; i < words.Count; i++)
                    {
                        if (i > 0)
                            sqlQuery += " OR ";
                        sqlQuery += $" CONTAINS(c.Title, \"{words[i]}\", true) ";
                    }
                    sqlQuery += ")";
                }
                sqlQuery += $" ORDER BY c.Title OFFSET 0 LIMIT {count}";
                Console.WriteLine(sqlQuery);   

                QueryDefinition queryDefinition = new QueryDefinition(sqlQuery);    
                using (FeedIterator<AnswerRow> queryResultSetIterator = 
                    myContainer!.GetItemQueryIterator<AnswerRow>(queryDefinition))
                {
                    while (queryResultSetIterator.HasMoreResults)
                    {
                        FeedResponse<AnswerRow> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                        return new AnswerRowDtosEx(currentResultSet.ToList(), string.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return new AnswerRowDtosEx(new List<AnswerRowDto>(), string.Empty);
        }

        /*
        public async Task<List<AnswerRowDto>> SearchAnswerRows(string filter, int count)
        {
            var myContainer = await container();
            try
            {
                string queryText = @"SELECT Top 3 x.name,x.description, x.ingredients, x.cuisine,x.difficulty, x.prepTime,x.cookTime,x.totalTime,x.servings, x.similarityScore
                            FROM (SELECT c.name,c.description, c.ingredients, c.cuisine,c.difficulty, c.prepTime,c.cookTime,c.totalTime,c.servings,
                                VectorDistance(c.vectors, @vectors, false) as similarityScore FROM c) x
                                    WHERE x.similarityScore > @similarityScore ORDER BY x.similarityScore desc";

        var queryDef = new QueryDefinition(
                query: queryText)
            .WithParameter("@vectors", vectors)
            .WithParameter("@similarityScore", similarityScore);
    
        using FeedIterator<Recipe> resultSet = _container.GetItemQueryIterator<Recipe>(queryDefinition: queryDef);
    
        List<Recipe> recipes = new List<Recipe>();
        while (resultSet.HasMoreResults)
        {
            FeedResponse<Recipe> response = await resultSet.ReadNextAsync();
    recipes.AddRange(response);
        }
return recipes;
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return [];
        }
        */


        // --------------------------------------------------------
        //                  Assigned Answers
        // --------------------------------------------------------



        public async Task<Answer> SetAnswerTitles(Answer answer, 
            GroupService groupService, AnswerService answerService)
        {
            var (workspace, topId, partitionKey, id, title, parentId, type, source, status) = answer;
            var groupKey = new GroupKey(answer);
            // get group Title
            GroupEx groupEx = await groupService.GetGroup(groupKey);
            var (group, message) = groupEx;
            answer.GroupTitle = group != null ? group.Title : "NotFound Group";
            await SetAnswerTitles(answer, answerService);
            return answer;
        }

        public async Task<Dictionary<string, AnswerTitleLink>> GetTitlesAndLinks(Dictionary<string, List<string>> dict)
        {
            var myContainer = await container();
            List<AnswerTitleLink> list = [];
            try
            {
                foreach (var (key, value) in dict)
                {
                    string str = string.Join("','", value.ToArray());
                    string partitionKey = _workspace + "/" + key;
                    string sqlQuery = @$"SELECT c.id, c.Title, c.Link FROM c 
                        WHERE c.partitionKey = '{partitionKey}' AND c.Type = 'answer' AND
                        ARRAY_CONTAINS(['{str}'], c.id, false)";

                    QueryDefinition queryDefinition = new QueryDefinition(sqlQuery);
                    FeedIterator<AnswerTitleLink> queryResultSetIterator = myContainer!.GetItemQueryIterator<AnswerTitleLink>(queryDefinition);
                    while (queryResultSetIterator.HasMoreResults)
                    {
                        FeedResponse<AnswerTitleLink> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                        foreach (AnswerTitleLink answerTitleLink in currentResultSet)
                        {
                            //Console.WriteLine(">>>>>>>> answer is: {0}", JsonConvert.SerializeObject(answer));
                            list.Add(answerTitleLink);
                        }
                        //return list.ToDictionary(x => x.Id!, x => new AnswerTitleLink(x));
                    }
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this reans.
                Console.WriteLine(ex.Message);
            }
            return list.ToDictionary(x => x.id!, x => new AnswerTitleLink(x));
        }


        public async Task<Answer> SetAnswerTitles(Answer answer, AnswerService answerService)
        {
            var list = new List<(string, string)>();
            var (workspace, topId, partitionKey, id, title, parentId, type, source, status) = answer;
            return answer;
        }

        // --------------------------------------------------------
        //                  Filters
        // --------------------------------------------------------


        //public async Task<AnswerEx> UnAssignFilter(RelatedFilterDto dto)
        //{
        //    var (answerKey, filter, created, modified) = dto;

        //    AnswerEx answerEx = await GetAnswer(answerKey!);
        //    var (answer, msg) = answerEx;
        //    if (answer != null)
        //    {
        //        var relatedFilters = answer.RelatedFilters.FindAll(a => a.FilterKey.Id != filterKey.Id);
        //        answer.Modified = new WhoWhen(created);
        //        answerEx = await UpdateAnswerFilters(answer, relatedFilters);
        //    }
        //    return answerEx;
        //}


        //public async Task<Answer> SetFilterTitles(Answer answer,
        //    GroupService groupService, FilterService filterService)
        //{
        //    var (PartitionKey, Id, Title, ParentId, Type, Source, Status, RelatedFilters) = answer;
        //    GroupKey groupKey = new(PartitionKey, answer.ParentId!);
        //    // get group Title
        //    GroupEx groupEx = await groupService.GetGroup(groupKey);
        //    var (group, message) = groupEx;
        //    answer.GroupTitle = group != null ? group.Title : "NotFound Group";
        //    //if (RelatedFilters.Count > 0)
        //    //{
        //    //    var filterIds = RelatedFilters.Select(a => a.FilterKey.Id).Distinct().ToList();
        //    //    Dictionary<string, FilterTitleLink> dict = await filterService.GetTitlesAndLinks(filterIds);
        //    //    Console.WriteLine(JsonConvert.SerializeObject(dict));
        //    //    foreach (var relatedFilters in RelatedFilters)
        //    //    {
        //    //        FilterTitleLink titleLink = dict[relatedFilters.FilterKey.Id];
        //    //        relatedFilters.FilterTitle = titleLink.Title;
        //    //        relatedFilters.FilterLink = titleLink.Link;
        //    //    }
        //    //}
        //    await SetFilterTitles(answer, filterService);
        //    return answer;
        //}

        //public async Task<Answer> SetFilterTitles(Answer answer, FilterService filterService)
        //{
        //    var (PartitionKey, Id, Title, ParentId, Type, Source, Status, RelatedFilters) = answer;
        //    if (RelatedFilters.Count > 0)
        //    {
        //        //var filterIds = RelatedFilters.Select(a => a.FilterKey.Id).Distinct().ToList();
        //        //Dictionary<string, string> filterTitles = await filterService.GetTitlesAndLinks(filterIds);
        //        //Console.WriteLine(JsonConvert.SerializeObject(filterTitles));
        //        //foreach (var relatedFilters in RelatedFilters)
        //        //    relatedFilters.FilterTitle = filterTitles[relatedFilters.FilterKey.Id];
        //        var filterIds = RelatedFilters.Select(a => a.FilterKey.Id).Distinct().ToList();
        //        Dictionary<string, FilterTitleLink> dict = await filterService.GetTitlesAndLinks(filterIds);
        //        Console.WriteLine(JsonConvert.SerializeObject(dict));
        //        foreach (var relatedFilters in RelatedFilters)
        //        {
        //            FilterTitleLink titleLink = dict[relatedFilters.FilterKey.Id];
        //            relatedFilters.FilterTitle = titleLink.Title;
        //            relatedFilters.FilterLink = titleLink.Link;
        //        }
        //    }

        //    return answer;
        //}

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
