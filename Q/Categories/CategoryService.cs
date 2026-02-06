using Azure;
using Knowledge.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using KnowledgeAPI.Common;
using KnowledgeAPI.Q.Categories.Model;
using KnowledgeAPI.Q.Questions;
using KnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace KnowledgeAPI.Q.Categories
{
    public class CategoryService : CategoryRowService
    {
        //public DbService? Db { get; set; } = null;
        //public OpenAIService _openAIEmbeddingService = null;

        private Container? _container = null;
        protected string _workspace;

        public async Task<Container> container()
        {
            ThroughputProperties throughputProperties = ThroughputProperties.CreateAutoscaleThroughput(2000);

            // Define new container properties including the vector indexing policy
            ContainerProperties properties = new ContainerProperties(id: containerId, partitionKeyPath: "/partitionKey")
            {
                // Set the default time to live for cache items to 1 day
                DefaultTimeToLive = null // = 86400,

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

        public CategoryService()
        {
        }

        //public Category(IConfiguration configuration)
        //{
        //    Category.Db = new Db(configuration);
        //}

        public CategoryService(DbService db, string workspace)
        {
            Db = db;
            //this._openAIEmbeddingService = db.openAIEmbeddingService;
            _workspace = workspace;
        }
     

        public async Task<CategoryEx> GetCategory(CategoryKey categoryKey, bool hidrate, int pageSize, string? includeQuestionId)
        {
            var (workspace, topId, partitonKey, id, _) = categoryKey;
            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                //ItemResponse<Category> aResponse =
                Category category = await myContainer!.ReadItemAsync<Category>(id, new PartitionKey(partitonKey));
                //Console.WriteLine(JsonConvert.SerializeObject(category));

                if (category != null)
                {
                    /*
                    /////////////////////
                    //// subCategoryRows
                    //List<CategoryRow> subCategories = await GetSubCategoryRows(myContainer, PartitionKey, Id);
                    //category.SubCategories = subCategories;
                    category.SubCategories = [];
                    */

                    ///////////////////
                    // questions
                    if (pageSize > 0)
                    {
                        // hidrate collections except questions, like  category.x = hidrate;  
                        if (category.NumOfQuestions > 0)
                        {
                            var questionService = new QuestionService(Db, workspace);
                            var questionKey = new QuestionKey(categoryKey);
                            QuestionsMore questionsMore = await questionService.GetQuestions(questionKey, 0, pageSize, includeQuestionId??"null");
                            category.QuestionRows = questionsMore.QuestionRows.ToList(); // .Select(questionRow => new Question(questionRow))
                            category.HasMoreQuestions = questionsMore.HasMoreQuestions;
                        }
                    }
                }
                return new CategoryEx(category, "");
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                return new CategoryEx(null, ex.Message);
            }
        }

        // TODO make CtrlController as the base class for:  CategoryRowController and CategoryController
        internal async Task<List<CategoryRow>> GetSubCategoryRows(Container myContainer, string PartitionKey, string id)
        {
            var sqlQuery = $"SELECT * FROM c WHERE c.Type = 'category'  AND "
            + (
                id == "null"
                    ? $" IS_NULL(c.ParentId)"
                    : $" c.ParentId = '{id}'"
            );
            QueryDefinition queryDefinition = new(sqlQuery);
            FeedIterator<Category> queryResultSetIterator = myContainer!.GetItemQueryIterator<Category>(queryDefinition);
            List<CategoryRow> subCategorRows = [];
            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<Category> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (Category category in currentResultSet)
                {
                    subCategorRows.Add(new CategoryRow(category));
                }
            }
            return subCategorRows;
        }


        /*public async Task<CategoryEx> GetCategoryWithSubCategories(CategoryKey categoryKey)
        {
            var myContainer = await container();
            var (TopId, Id) = categoryKey;
            var partitionKey = categoryKey.PartitionKey;
            try
            {
                Category category = await myContainer!.ReadItemAsync<Category>(Id, new PartitionKey(PartitionKey));
                var categoryRow = new CategoryRow(category);
                List<CategoryRow> subCategories = await GetSubCategoryRows(myContainer, PartitionKey, Id);
                    // bio neki []
                categoryRow.SubCategories = subCategories;
                return new CategoryEx(category, "");
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                return new CategoryEx(null, ex.Message);
            }
        }*/

        public async Task<HttpStatusCode> CheckDuplicate(string ws, string title, string? id = null) //QuestionData questionData)
        {
            var sqlQuery = $"SELECT c.id FROM c WHERE c.Workspace = '{ws}' AND c.Type = 'category' AND " +
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

        public async Task<string> AddCategory(CategoryData categoryData)
        {
            var (workspace, topId, partitionKey, id, title, link, header, parentId, kind, level, variations, 
                    categories, questions) = categoryData;
            Console.WriteLine(topId, id);
            var myContainer = await container();
            string msg = string.Empty;
            try
            {
                //if (id == "DOMAIN")
                //{
                //    for (var i = 1; i <= 15; i++) 
                //        questions!.Add(new QuestionData(id, $"Test row for DOMAIN " + i.ToString("D3")));
                //}
                var cat = new Category(categoryData);
                cat.Doc1 = string.Empty;
                CategoryEx categoryEx = await AddNewCategory(myContainer, cat);
                if (categoryEx.category != null)
                {
                    await Task.Delay(1000);
                    Thread.Sleep(1000);
                    Category category = categoryEx.category;
                    if (categories != null)
                    {
                        foreach (var subCategoryData in categories)
                        {
                            subCategoryData.Workspace = workspace;
                            subCategoryData.TopId = topId;
                            subCategoryData.ParentId = category.Id;
                            subCategoryData.Level = category.Level + 1;
                            msg = await AddCategory(subCategoryData);
                            if (msg != string.Empty)
                                break;
                            
                        }
                    }
                    if (questions != null)
                    {
                        
                        var questionService = new QuestionService(Db!, workspace);

                        //List<Task> tasks = new List<Task>(500);
                        foreach (var questionData in questions)
                        {
                            await Task.Delay(1000);
                            Thread.Sleep(1000);
                            questionData.Workspace = workspace;
                            questionData.TopId = topId;
                            questionData.ParentId = category.Id;
                            QuestionEx questionEx = await questionService.AddQuestion(questionData);
                            if (questionEx.question == null)
                            {
                                msg = questionEx.msg;
                                break;
                            }
                            
                            /*
                            var question = new Question(questionData);
                            //Container questionsContainer = await questionService.container();
                            var aResp = questionService.CreateItemAsync(question);*/
                            //tasks.Add(aResp
                            //    .ContinueWith(itemResponse =>
                            //    {
                            //        if (!itemResponse.IsCompletedSuccessfully)
                            //        {
                            //            AggregateException innerExceptions = itemResponse.Exception.Flatten();
                            //            if (innerExceptions.InnerExceptions.FirstOrDefault(innerEx => innerEx is CosmosException) is CosmosException cosmosException)
                            //            {
                            //                Console.WriteLine($"Received {cosmosException.StatusCode} ({cosmosException.Message}).");
                            //            }
                            //            else
                            //            {
                            //                Console.WriteLine($"Exception {innerExceptions.InnerExceptions.FirstOrDefault()}.");
                            //            }
                            //        }
                            //    }));
                        }
                        // Wait until all are done
                       // await Task.WhenAll(tasks);
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

        public async Task<CategoryEx> AddNewCategory(Container cntr, Category category)
        {
            var (workspace, topId, partitionKey, id, parentId, title, link, header, level, kind,
                hasSubCategories, subCategories,
                hasMoreQuestions, numOfQuestions, questionRows, variations, isExpanded, doc1) = category;
            var myContainer = cntr != null ? cntr : await container();
            string msg = string.Empty;
            try
            {
                // Check if the id already exists
                ItemResponse<Category> aResponse =
                    await myContainer!.ReadItemAsync<Category>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                msg = $"Category in database with Id: {id} already exists"; //, aResponse.Resource.Id
                Console.WriteLine(msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                try
                {
                    // Check if the title already exists
                    HttpStatusCode statusCode = await CheckDuplicate(workspace, title, id);
                    msg = $"Category in database with Id: {id} or Title: {title} already exists";
                    Console.WriteLine(msg);
                }
                catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
                {
                    // Create an item in container.Note we provide the value of the partition key for this item
                    /*
                    var vectors = _openAIEmbeddingService
                        .GetEmbeddingsAsync(JsonConvert.SerializeObject(new CatQuestEmbedded(category)))
                        .GetAwaiter()
                        .GetResult();
                    category.vectors = vectors!.ToList();
                    */

                    ItemResponse<Category> aResponse =
                        await myContainer!.CreateItemAsync(
                            category,
                            new PartitionKey(partitionKey)
                        );
                    // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                    Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", aResponse.Resource.Id, aResponse.RequestCharge);
                    return new CategoryEx(aResponse.Resource, "");
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return new CategoryEx(null, msg);
        }

        public async Task<CategoryEx> CreateCategory(CategoryDto categoryDto)
        {
            //var (Id, PartitionKey) = categoryDto;
            categoryDto.Id = categoryDto.Title.Trim().Replace(' ', '_').ToUpper();
            if (categoryDto.Id.Trim().ToLower() == "archived" || categoryDto.Id.Trim().ToLower() == "root")
                return new CategoryEx(null, "Title can't be 'archived or root' ");
            var myContainer = await container();
            var category = new Category(categoryDto);
            category.Doc1 = string.Empty;
            CategoryEx categoryEx = await AddNewCategory(myContainer, category);
            // update parentId
            if (category.ParentId != null)
            {
                await UpdateHasSubCategories(myContainer, category.PartitionKey, category.ParentId, category.Created!.NickName);
            }
            return categoryEx;
        }

        /*
        public async Task<CategoryEx> UpdateCategory(CategoryDto categoryDto)
        {
            var myContainer = await container();
            string msg = string.Empty;
            try
            {
                var (partitionKey, id, parentId, title, link, level, kind, variations, modified) = categoryDto;
                // Read the item to see if it exists.  
                ItemResponse<Category> aResponse =
                    await myContainer!.ReadItemAsync<Category>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                Category category = aResponse.Resource;
                // Update the item fields
                category.Title = title;
                category.Link = link;
                category.Kind = kind;
                category.Variations = variations;
                category.ParentId = parentId;
                if (modified != null)
                {
                    category.Modified = new WhoWhen(modified.NickName);
                }
                aResponse = await myContainer.ReplaceItemAsync(category, id, new PartitionKey(partitionKey));
                Console.WriteLine("Updated Category [{0},{1}].\n \tBody is now: {2}\n", title, id, category);

                // update parentId
                //categoryDto.Modified = categoryDto.Modified;
                //await UpdateHasSubCategories(categoryDto);

                return new CategoryEx(category, msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                msg = $"Category Id: {categoryDto.Id} NotFound in database."; //, aResponse.RequestCharge);
                Console.WriteLine(msg); //, aResponse.RequestCharge);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                msg = ex.Message;
                Console.WriteLine(ex.Message);
            }
            return new CategoryEx(null, msg);
        }
        */


        public async Task<CategoryEx> UpdateCategory(CategoryDto categoryDto)
        {
            var myContainer = await container();
            string msg = string.Empty;
            try
            {
                var (workspace, topId, partitionKey, id, parentId, title, link, level, kind, variations, modified, doc1) = categoryDto;
                // Read the item to see if it exists.  
                ItemResponse<Category> aResponse =
                    await myContainer!.ReadItemAsync<Category>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                Category category = aResponse.Resource;
                var doUpdate = true;
                if (!category.Title.Equals(title, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        HttpStatusCode statusCode = await CheckDuplicate(workspace, title);
                        doUpdate = false;
                        msg = $"Question with Title: \"{title}\" already exists in database.";
                        Console.WriteLine(msg);
                        return new CategoryEx(null, msg);
                    }
                    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        //question.Title = q.Title;
                    }
                }
                if (doUpdate)
                {
                    if (category.ParentId != parentId)
                    {
                        // changed Category
                    }
                    // Update the item fields
                    category.Title = title;
                    category.Link = link;
                    category.Kind = kind;
                    category.Variations = variations;
                    category.ParentId = parentId;
                    category.Modified = new WhoWhen(modified!.NickName);
                    aResponse = await myContainer.ReplaceItemAsync(category, id, new PartitionKey(partitionKey));
                    return new CategoryEx(aResponse.Resource, "");
                }
                return new CategoryEx(category, msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                msg = $"Category Id: {categoryDto.Id} NotFound in database."; //, aResponse.RequestCharge);
                Console.WriteLine(msg); //, aResponse.RequestCharge);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                msg = ex.Message;
                Console.WriteLine(ex.Message);
            }
            return new CategoryEx(null, msg);
        }

        public async Task<Category> UpdateNumOfQuestions(CategoryKey categoryKey, WhoWhen modified, int numOfQuestions)//, int incr)
        {
            var (workspace, topId, partitionKey, id, _) = categoryKey;
            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Category> aResponse =
                    await myContainer!.ReadItemAsync<Category>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                Category category = aResponse.Resource;

                category.NumOfQuestions = numOfQuestions; // incr;
                category.Modified = new WhoWhen(modified!.NickName);
                aResponse = await myContainer.ReplaceItemAsync(category, id, new PartitionKey(category.PartitionKey));
                Console.WriteLine("===>>> Updated Category NumOfQuestions [{0},{1}].\n", category.Title, id);
                return category;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine("Category item {0}/{1} NotFound in database.\n", partitionKey, id); //, aResponse.RequestCharge);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        public async Task<Category> UpdateHasSubCategories(Container cntr, string partitionKey, string id, string nickName) 
        {
            var myContainer = cntr != null ? cntr : await container();
            try
            {
                var PartitionKey = partitionKey;
                // Read the item to see if it exists.  
                ItemResponse<Category> aResponse =
                    await myContainer!.ReadItemAsync<Category>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                Category category = aResponse.Resource;

                var sql = $"SELECT value count(1) FROM c WHERE c.Type = 'category' " +
                    $"AND c.partitionKey='{partitionKey}' " +
                    $"AND c.ParentId='{id}' ";

                int num = await CountItems(myContainer, sql);
                Console.WriteLine($"============================ num: {num}");

                category.HasSubCategories = num > 0;
                category.Modified = new WhoWhen(nickName);

                aResponse = await myContainer.ReplaceItemAsync(category, id, new PartitionKey(partitionKey));
                return category;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine("Category item {0}/{1} NotFound in database.\n", ParentId, ParentId); //, aResponse.RequestCharge);
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

        public async Task<CategoryEx> GetCategory(CategoryKey categoryKey)
        {
            var (workspace, topId, partitionKey, id, _) = categoryKey;
            string msg = string.Empty;
            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Category> aResponse =
                    await myContainer.ReadItemAsync<Category>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                Category category = aResponse.Resource;
                return new CategoryEx(category, msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                msg = $"Category {partitionKey}/{id} NotFound in database.";
                Console.WriteLine(msg); //, aResponse.RequestCharge);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return new CategoryEx(null, msg);
        }

        public async Task<CategoryEx> ArchiveCategory(Container? cntr, CategoryKey categoryKey, string nickName)
        {
            var myContainer = cntr != null ? cntr : await container();
            var (workspace, topId, partitionKey, id, parentId) = categoryKey;

            string msg = string.Empty;
            try
            {
                ItemResponse<Category> aResponse =
                    await myContainer.ReadItemAsync<Category>(id, new PartitionKey(partitionKey));
                Category category = aResponse.Resource;
                if (category.HasSubCategories)
                {
                    return new CategoryEx(null, "HasSubCategories");
                }
                else if (category.NumOfQuestions > 0 )
                {
                    return new CategoryEx(null, "HasQuestions");
                }
                await myContainer.DeleteItemAsync<Category>(
                        id,
                        new PartitionKey(partitionKey)
                    );

                category.PartitionKey = $"{workspace}/archived";
                category.Type += "archived";
                category.Modified = new WhoWhen(nickName);  
                CategoryEx categoryEx = await AddNewCategory(myContainer, category);

                // update parentCategory
                if (parentId != null)
                {
                    var parentCat = await UpdateHasSubCategories(myContainer, partitionKey, parentId, category.Modified!.NickName);
                    return new CategoryEx(parentCat, "OK");
                }
                else
                {
                    return new CategoryEx(category, "OK"); // parent of topRow is null
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                msg =$"Category {id} NotFound in database."; //, aResponse.RequestCharge);
                Console.WriteLine(msg);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return new CategoryEx(null, msg);
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



