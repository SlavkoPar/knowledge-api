using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Knowledge.Services;
using Microsoft.Azure.Cosmos;
using KnowledgeAPI.A.Answers;
using KnowledgeAPI.A.Answers.Model;
using KnowledgeAPI.Common;
using KnowledgeAPI.Q.Categories;
using KnowledgeAPI.Q.Categories.Model;
using KnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Net;


namespace KnowledgeAPI.Q.Questions
{
    public class QuestionService : IDisposable
    {
        public DbService? Db { get; set; } = null;
        //public OpenAIService? _openAIEmbeddingService { get; set; }  = null;


        private readonly string containerId = "Questions";
        private Container? _container = null;
        private string _workspace = null;


     
        readonly string _rowColumns = @"'', '', c.TopId, c.id, c.ParentId, 
                            c.Title, c.Vectors, c.NumOfAssignedAnswers
                            FROM c ";
        // c.Type = 'question' AND 


        protected string getPartitionKey(string topId)
        {
            return _workspace + "/" + topId;
        }


        public async Task<Container> container()
        {
            //ThroughputProperties throughputProperties = ThroughputProperties.CreateAutoscaleThroughput(1000);

            // Define new container properties including the vector indexing policy
            ContainerProperties properties = new ContainerProperties(id: containerId, partitionKeyPath: "/partitionKey")
            {
                // Set the default time to live for cache items to 1 day
                DefaultTimeToLive = null     //86400,

                //// Define the vector embedding container policy
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
        public QuestionService()
        {
        }

        public QuestionService(DbService Db, string workspace)
        {
            this.Db = Db;
            // this._openAIEmbeddingService = Db.openAIEmbeddingService;
            _workspace = workspace;
        }
                 
        public async Task<HttpStatusCode> CheckDuplicate(string ws, string? Title, string? Id = null)
        {
            var sqlQuery = Title != null
                ? $"SELECT c.id FROM c WHERE c.Workspace = '{ws}' AND c.Type = 'question' AND c.Title = '{Title.Trim().Replace("\'", "\\'")}' "
                : $"SELECT c.id FROM c WHERE c.Workspace = '{ws}' AND c.Type = 'question' AND c.Id = '{Id}' ";
            QueryDefinition queryDefinition = new(sqlQuery);
            FeedIterator<string> queryResultSetIterator =
                _container!.GetItemQueryIterator<string>(queryDefinition);
            if (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<string> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                if (currentResultSet.Count == 0)
                {
                    throw new CosmosException("Question with Title doesn't exist", HttpStatusCode.NotFound, 0, "0", 0);
                }
            }
            return HttpStatusCode.Found;
        }

        public async Task<QuestionEx> AddQuestion(QuestionData questionData)
        {
            var myContainer = await container();
            //Console.WriteLine(JsonConvert.SerializeObject(questionData));
            string msg = string.Empty;
            try
            {
                var question = new Question(questionData);
                //Console.WriteLine("----->>>>> " + JsonConvert.SerializeObject(question));
                // Read the item to see if it exists.  
                await CheckDuplicate(questionData.Workspace, questionData.Title);
                msg = $":::::: Item in database with Title: {questionData.Title} already exists";
                Console.WriteLine(msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var question = new Question(questionData);
                QuestionEx questionEx = await AddNewQuestion(myContainer, question);
                return questionEx;
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                msg = ex.Message;
                Console.WriteLine(msg);
            }
            return new QuestionEx(null, msg);
        }

        public async Task<ItemResponse<Question>> CreateItemAsync(Question question)
        {
            var myContainer = await container();
            ItemResponse<Question> aResponse =
                        await myContainer.CreateItemAsync(
                                question,
                                new PartitionKey(question.PartitionKey)
                            );
            return aResponse;
        }

        public async Task<QuestionEx> AddNewQuestion(Container? cntr, Question question)
        {
            var (workspace, topId, partitionKey, id, title, parentId, type, source, status, assignedAnswers, relatedFilters) = question;
            var myContainer = cntr != null ? cntr : await container();
            string msg = string.Empty;
            try
            {
                // Check if the id already exists
                ItemResponse<Question> aResponse =
                    await myContainer!.ReadItemAsync<Question>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                msg = $"Question in database with id: {id} already exists\n";
                Console.WriteLine(msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                try
                {
                    // Check if the title already exists
                    HttpStatusCode statusCode = await CheckDuplicate(workspace, title);
                    msg = $"Question in database with Title: {title} already exists";
                    Console.WriteLine(msg);
                }
                catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
                {

                    /*
                    var vectors = _openAIEmbeddingService!
                        .GetEmbeddingsAsync(JsonConvert.SerializeObject(new CatQuestEmbedded(question)))
                        .GetAwaiter()
                        .GetResult();
                    question.vectors = vectors!.ToList();
                    */
                    ItemResponse<Question> aResponse =
                        await myContainer!.CreateItemAsync(
                                question,
                                new PartitionKey(partitionKey)
                            );
                    // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                    // Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", aResponse.Resource.Id, aResponse.RequestCharge);
                    return new QuestionEx(aResponse.Resource, "");
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return new QuestionEx(null, msg);
        }
        public async Task<int> CountNumOfQuestions(CategoryKey categoryKey)
        {
            var (workspace, topId, partitionKey, id, _) = categoryKey;
            var sql = $"SELECT value count(1) FROM c WHERE c.Type = 'question' " +
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

        public async Task<QuestionEx> CreateQuestion(CategoryService categoryService, QuestionDto questionDto)
        {
            var myContainer = await container();
            try
            {
                var question = new Question(questionDto);
                QuestionEx questionEx = await AddNewQuestion(myContainer, question);

                var q = questionEx.question;
                if (q != null)
                {
                    // Update the item fields
                    var categoryKey = new CategoryKey(questionDto);
                    int numOfQuestions = await CountNumOfQuestions(categoryKey);
                    Console.WriteLine($"============================ num: {numOfQuestions}");
                    //Category category = new Category(questionEx.question);
                    await categoryService.UpdateNumOfQuestions(
                           categoryKey,
                           new WhoWhen(questionDto.Created!),
                           numOfQuestions);
                }
                return questionEx;
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                return new QuestionEx(null, ex.Message);
            }
        }

        public async Task<QuestionEx> GetQuestion(QuestionKey questionKey)
        {
            var myContainer = await container();
            Question? question = null;
            string msg = string.Empty;
            try
            {
                var (_, _, partitionKey, id, _) = questionKey;
                question = await myContainer.ReadItemAsync<Question>(
                    id,
                    new PartitionKey(partitionKey)
                );
                return new QuestionEx(question, msg);
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
            //Console.WriteLine(JsonConvert.SerializeObject(question));
            return new QuestionEx(null, msg);
        }

        public async Task<QuestionEx> UpdateQuestion(QuestionDto questionDto, CategoryService categoryService)
        {
            var (workspace, topId, partitionKey, id, oldParentId, newParentId, title, source, status, modified) = questionDto;

            //Console.WriteLine(JsonConvert.SerializeObject(questionDto));
            //Console.WriteLine("========================UpdateQuestion-3");

            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Question> aResponse =
                    await myContainer.ReadItemAsync<Question>(
                        id,
                        new PartitionKey(partitionKey!)
                    );  
                Question question = aResponse.Resource;

                var doUpdate = true;
                if (!question.Title.Equals(title, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        HttpStatusCode statusCode = await CheckDuplicate(workspace, title);
                        doUpdate = false;
                        var msg = $"Question with Title: \"{title}\" already exists in database.";
                        Console.WriteLine(msg);
                        return new QuestionEx(msg);
                    }
                    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        //question.Title = q.Title;
                    }
                }
                if (doUpdate)
                {
                    // Update the item fields
                    question.Title = title;
                    question.Source = source;
                    question.Status = status;
                    question.ParentId = newParentId;
                    // question.PartitionKey = newParentId!;

                    if (modified != null) {
                        question.Modified = new WhoWhen(modified.NickName);
                    }

                    if (oldParentId != newParentId)
                    {
                        var categoryKey = new CategoryKey(workspace, topId, id);
                        // parent category changed
                        string msg = await ArchiveQuestion(myContainer, question);
                        if (msg.Equals(String.Empty))
                        {
                            QuestionEx ex = await AddNewQuestion(myContainer, question);
                            if (ex.question != null)
                            {
                                Console.WriteLine("DODAO QUESTION  sa novom parentCategory");
                                categoryKey.ParentId = oldParentId;
                                int numOfQuestions = await CountNumOfQuestions(categoryKey);
                                await categoryService.UpdateNumOfQuestions(
                                       categoryKey,
                                       new WhoWhen(modified!),
                                       numOfQuestions);

                                categoryKey.ParentId = newParentId!;
                                numOfQuestions = await CountNumOfQuestions(categoryKey);
                                await categoryService.UpdateNumOfQuestions(
                                       categoryKey,
                                       new WhoWhen(modified!),
                                       numOfQuestions);
                            }
                            else
                            {
                                Console.WriteLine("AddNewQuestion PROBLEMOS: " + ex.msg);
                            }
                        }
                    }
                    else
                    {
                        aResponse = await myContainer.ReplaceItemAsync(question, question.Id);
                        question = aResponse.Resource;
                    }
                    Console.WriteLine(JsonConvert.SerializeObject(question, Formatting.Indented));
                    var questionEx = new QuestionEx(question, string.Empty);
                    return questionEx;
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var msg = $"Question Id: \"{questionDto.Id}\" Not Found in database.";
                Console.WriteLine(msg); 
                return new QuestionEx(msg);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return new QuestionEx("Server Problemos Update");
        }


        public async Task<QuestionEx> UpdateQuestion(Question q, List<AssignedAnswer> assignedAnswers)
        {
            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Question> aResponse =
                    await myContainer!.ReadItemAsync<Question>(
                        q.Id,
                        new PartitionKey(q.PartitionKey)
                    );
                Question question = aResponse.Resource;
                question.AssignedAnswers = assignedAnswers;
                question.NumOfAssignedAnswers = assignedAnswers.Count;
                question.Modified = q.Modified;
                aResponse = await myContainer.ReplaceItemAsync(question, question.Id, new PartitionKey(question.PartitionKey));
                return new QuestionEx(aResponse.Resource, "");
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var msg = $"Question Id: \"{q.Id}\" Not Found in database.";
                Console.WriteLine(msg);
                return new QuestionEx(null, msg);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return new QuestionEx(null, "Server Problem Update");
        }


        public async Task<QuestionEx> UpdateQuestionFilters(Question q, List<RelatedFilter> relatedFilters)
        {
            var (workspace, topId, partitionKey, id, title, parentId, type, source, status, assignedAnswers, _) = q;

            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Question> aResponse =
                    await myContainer!.ReadItemAsync<Question>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                Question question = aResponse.Resource;
                question.RelatedFilters = relatedFilters;
                question.NumOfRelatedFilters = relatedFilters.Count;
                question.Modified = q.Modified!;
                aResponse = await myContainer.ReplaceItemAsync(question, id, new PartitionKey(partitionKey));
                return new QuestionEx(aResponse.Resource, "");
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var msg = $"Question Id: \"{id}\" Not Found in database.";
                Console.WriteLine(msg);
                return new QuestionEx(null, msg);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return new QuestionEx(null, "Server Problem Update");
        }


        public async Task<string> ArchiveQuestion(Container? cntr, Question question)
        {
            var myContainer = cntr != null ? cntr : await container();
            //var (workspace, topId, partitionKey, id, oldParentId, newParentId, title, source, status, modified) = questionDto;
            var (workspace, topId, partitionKey, id, title, parentId, type, source, status, assignedAnswers, relatedFilters) = question;

            Question? q = null;
            var message = string.Empty;
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Question> aResponse = 
                    await myContainer.ReadItemAsync<Question>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                q = aResponse.Resource;

                // now delete question
                await myContainer.DeleteItemAsync<Question>(
                        id,
                        new PartitionKey(partitionKey)
                    );

                // Partition keys are crucial for how data is distributed across partitions.
                // PartitionKey is immutable.
                // 1) Item should be deleted
                // 2) Recreated with the new partitionKey
                // Currently we keep only last question archived
                q.PartitionKey = $"{workspace}/archived";
                q.Type = "archived";
                aResponse = await myContainer.ReadItemAsync<Question>(
                        id,
                        new PartitionKey(q.PartitionKey)
                    );
                await myContainer.ReplaceItemAsync(q, id);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                if (q != null)
                {
                    //question.Title += " ARCHIVED"; // otherwise updated Question can't be added
                    await AddNewQuestion(myContainer, q);
                    Console.WriteLine("DODAO ARCHIVED");
                }
                // message = $"Question item {id} NotFound in database.";
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

        public async Task<QuestionsMore> GetQuestions(QuestionKey questionKey, int startCursor, int pageSize, string? includeQuestionId)
        {
            var (workspace, topId, partitionKey, _, parentId) = questionKey;
            var myContainer = await container();
            try
            {
                string sqlQuery = $@"SELECT {_rowColumns} 
                    WHERE c.partitionKey='{workspace}/{topId}' AND c.ParentId = '{parentId}' 
                    ORDER BY c.Title 
                    OFFSET {startCursor} 
                    LIMIT {((includeQuestionId == null) ? pageSize : 9999)}";

                int n = 0;
                bool included = false;

                List<QuestionRow> list = [];
                QueryDefinition queryDefinition = new QueryDefinition(sqlQuery);
                FeedIterator<QuestionRow> queryResultSetIterator = myContainer!.GetItemQueryIterator<QuestionRow>(queryDefinition);
                while (queryResultSetIterator.HasMoreResults)
                {
                    FeedResponse<QuestionRow> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    foreach (QuestionRow questionRow in currentResultSet)
                    {
                        if (includeQuestionId != null && questionRow.Id == includeQuestionId)
                        {
                            included = true;
                            questionRow.Included = true;
                        }
                        else
                        {
                            questionRow.Included = false;
                        }


                        //Console.WriteLine(">>>>>>>> question is: {0}", JsonConvert.SerializeObject(question));
                        list.Add(questionRow);
                        n++;
                        if (n >= pageSize && (includeQuestionId == null || included))
                        {
                            return new QuestionsMore(list, true);
                        }
                    }
                    return new QuestionsMore(list, false);
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return new QuestionsMore([], false);
        }

        public async Task<QuestionRowDtosEx> SearchQuestionRows(IConfigurationSection section, string workspace, string userQuery, int count)
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
                SearchResults<QuestionRow> response;
                SearchOptions options = new SearchOptions()
                {
                    //Filter = "Rating gt 4",
                    //OrderBy = { "Title asc" }
                };
                */

                //options.Select.Add("ParentId");
                //options.Select.Add("Title");
                //options.Select.Add("id");

                //response = srchclient.Search<Question>("questions", options);
                //var embeddingVector = _openAIEmbeddingService!.GetEmbeddingsAsync(userQuery)
                //    .GetAwaiter()
                //    .GetResult();

                //.GetEmbeddingsAsync(userQuery)
                /*
                var vectors = _openAIEmbeddingService!
                        .GetEmbeddingsAsync(JsonConvert.SerializeObject(new CatQuestEmbedded("question", userQuery)))
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
                                    WHERE x.Type = 'question' AND x.similarityScore > @similarityScore ORDER BY x.similarityScore desc";

                var similarityScore = 0.60;
                var queryDefinition = new QueryDefinition(queryText)
                    .WithParameter("@vectors", vectors)
                    .WithParameter("@similarityScore", similarityScore);
                
                using FeedIterator<QuestionRow> resultSet = myContainer.GetItemQueryIterator<QuestionRow>(queryDefinition);

                var questionRows = new List<QuestionRow>();
                while (resultSet.HasMoreResults)
                {
                    FeedResponse<QuestionRow> resp = await resultSet.ReadNextAsync();
                    questionRows.AddRange(resp);
                }
                */
                /*
                var queryDef = new QueryDefinition(
                    query: @$"SELECT c.id, c.partitionKey, c.Type, c.Title, VectorDistance(c.contentVector, @vectors) AS SimilarityScore 
                                FROM c WHERE c.Type = 'question' 
                                ORDER BY VectorDistance(c.contentVector, @vectors)")
                    .WithParameter("@vectors", vectors);

                using FeedIterator<QuestionRow> feed = myContainer.GetItemQueryIterator<QuestionRow>(
                    queryDefinition: queryDef
                );
                var questionRows = new List<QuestionRow>();
                while (feed.HasMoreResults)
                {
                    FeedResponse<QuestionRow> resp = await feed.ReadNextAsync();
                    foreach (QuestionRow questionRow in resp)
                    {
                        //Console.WriteLine($"Found item:\t{item}");
                        questionRows.Add(questionRow);
                    }
                }
                */

                //(string completion, int promptTokens, int completionTokens) = _openAIEmbeddingService
                //    .GetChatCompletionAsync(userQuery, JsonConvert.SerializeObject(questionRows))
                //    .GetAwaiter()
                //    .GetResult();

                /*
                var questionDtos = new List<QuestionRowDto>();
                foreach (var row in questionRows) //.FindAll(q => q.Type == "question"))
                {
                    row.ParentId = row.PartitionKey;
                    row.vectors = null;
                    questionDtos.Add(new QuestionRowDto(row));
                }

                return questionDtos;
                */
                var words = userQuery //.ToLower()
                            .Replace("?", "")
                            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Where(w => w.Length > 2)
                            .ToList();

                // order of fields matters
                var sqlQuery = $"SELECT {_rowColumns} WHERE c.Workspace = '{workspace}' AND c.Type = 'question' AND ";
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
                using (FeedIterator<QuestionRow> queryResultSetIterator = 
                    myContainer!.GetItemQueryIterator<QuestionRow>(queryDefinition))
                {
                    while (queryResultSetIterator.HasMoreResults)
                    {
                        FeedResponse<QuestionRow> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                        return new QuestionRowDtosEx(currentResultSet.ToList(), string.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return new QuestionRowDtosEx(new List<QuestionRowDto>(), string.Empty);
        }

        /*
        public async Task<List<QuestionRowDto>> SearchQuestionRows(string filter, int count)
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

        public async Task<QuestionEx> AssignAnswer(QuestionKey questionKey, AssignedAnswerDto assignedAnswerDto)
        {
            var (topId, id, answerTitle, answerLink, created, modified) = assignedAnswerDto;
            QuestionEx questionEx = await GetQuestion(questionKey);
            var (question, msg) = questionEx;
            if (question != null)
            {
                var assignedAnswers = question.AssignedAnswers ?? new List<AssignedAnswer>();
                assignedAnswers.Add(new AssignedAnswer(assignedAnswerDto));
                question.Modified = new WhoWhen(created);
                questionEx = await UpdateQuestion(question, assignedAnswers);
            }
            return questionEx;
        }

        public async Task<QuestionEx> UnAssignAnswer(QuestionKey questionKey, AssignedAnswerDto assignedAnswerDto)
        {
            var (topId, id, answerTitle, answerLink, created, modified) = assignedAnswerDto;
            QuestionEx questionEx = await GetQuestion(questionKey);
            var (question, msg) = questionEx;
            if (question != null)
            {
                var assignedAnswers = question.AssignedAnswers.FindAll(a => !(a.TopId == topId && a.Id == id));
                question.Modified = new WhoWhen(created);
                questionEx = await UpdateQuestion(question, assignedAnswers);
            }
            return questionEx;
        }


        public async Task<Question> SetAnswerTitles(Question question, 
            CategoryService categoryService, AnswerService answerService)
        {
            var (workspace, topId, partitionKey, id, title, parentId, type, source, status, 
                    assignedAnswers, relatedFilters) = question;
            var categoryKey = new CategoryKey(question);
            // get category Title
            CategoryEx categoryEx = await categoryService.GetCategory(categoryKey);
            var (category, message) = categoryEx;
            question.CategoryTitle = category != null ? category.Title : "NotFound Category";
            await SetAnswerTitles(question, answerService);
            return question;
        }

        public async Task<Question> SetAnswerTitles(Question question, AnswerService answerService)
        {
            var list = new List<(string, string)>();
            var (workspace, topId, partitionKey, id, title, parentId, type, source, status, assignedAnswers, _) = question;
            if (assignedAnswers != null && assignedAnswers.Count > 0)
            {
                var dict = assignedAnswers
                    .GroupBy(p => p.TopId)
                    .ToDictionary(k => k.Key, g => g.Select(t => t.Id).ToList()); // /*.OrderBy(t => t.Sequence)*/.

                Dictionary<string, AnswerTitleLink> dict2 = await answerService.GetTitlesAndLinks(dict);
                foreach (var assignedAnswer in assignedAnswers)
                {
                   AnswerTitleLink? titleLink;
                    if (dict2.TryGetValue(assignedAnswer.Id, out titleLink))
                    {
                        assignedAnswer.AnswerTitle = titleLink.Title;
                        assignedAnswer.AnswerLink = titleLink.Link;
                    }
                    else
                    {
                        assignedAnswer.AnswerTitle = "NotFound =>" + assignedAnswer.Id;
                        assignedAnswer.AnswerLink = "NotFound =>" + assignedAnswer.Id;
                    }
                }
            }
            return question;
        }

        // --------------------------------------------------------
        //                  Filters
        // --------------------------------------------------------

        public async Task<QuestionEx> AssignFilter(RelatedFilterDto dto)
        {
            var (questionKey, filter, created, modified, numOfUsage) = dto;
            QuestionEx questionEx = await GetQuestion(questionKey!);
            var (question, msg) = questionEx;
            if (question != null)
            {
                var relatedFilters = question.RelatedFilters ?? [];
                relatedFilters.Add(new RelatedFilter(dto));
                question.Modified = new WhoWhen(created);
                questionEx = await UpdateQuestionFilters(question, relatedFilters);
            }
            return questionEx;
        }

        //public async Task<QuestionEx> UnAssignFilter(RelatedFilterDto dto)
        //{
        //    var (questionKey, filter, created, modified) = dto;

        //    QuestionEx questionEx = await GetQuestion(questionKey!);
        //    var (question, msg) = questionEx;
        //    if (question != null)
        //    {
        //        var relatedFilters = question.RelatedFilters.FindAll(a => a.FilterKey.Id != filterKey.Id);
        //        question.Modified = new WhoWhen(created);
        //        questionEx = await UpdateQuestionFilters(question, relatedFilters);
        //    }
        //    return questionEx;
        //}


        //public async Task<Question> SetFilterTitles(Question question,
        //    CategoryService categoryService, FilterService filterService)
        //{
        //    var (PartitionKey, Id, Title, ParentId, Type, Source, Status, RelatedFilters) = question;
        //    CategoryKey categoryKey = new(PartitionKey, question.ParentId!);
        //    // get category Title
        //    CategoryEx categoryEx = await categoryService.GetCategory(categoryKey);
        //    var (category, message) = categoryEx;
        //    question.CategoryTitle = category != null ? category.Title : "NotFound Category";
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
        //    await SetFilterTitles(question, filterService);
        //    return question;
        //}

        //public async Task<Question> SetFilterTitles(Question question, FilterService filterService)
        //{
        //    var (PartitionKey, Id, Title, ParentId, Type, Source, Status, RelatedFilters) = question;
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

        //    return question;
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
