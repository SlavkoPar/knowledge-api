using Azure;
using Knowledge.Services;
using Microsoft.Azure.Cosmos;
using KnowledgeAPI.Common;
using KnowledgeAPI.Q.Categories.Model;
using KnowledgeAPI.Q.Questions;
using KnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Net;

namespace KnowledgeAPI.Q.Categories
{
    public class CategoryRowService : IDisposable
    {
        public DbService? Db { get; set; } = null;

        protected readonly string containerId = "Categories";
        private Container? _container = null;
        private string _workspace = null;


        public async Task<Container> container()
        {
            _container ??= await Db!.GetContainer(containerId);
            return _container;
        }

        public CategoryRowService()
        {
        }

        //public Category(IConfiguration configuration)
        //{
        //    Category.Db = new Db(configuration);
        //}

        public CategoryRowService(DbService db, string workspace)
        {
            Db = db;
            _workspace = workspace;
        }

        readonly string _catColumns = @"SELECT c.TopId, c.id, c.ParentId, c.Level, c.Title FROM c ";


        readonly string _rowColumns = @"SELECT '', c.Workspace, c.TopId, 
                         c.id, c.ParentId, 
                         c.Title, c.Link, c.Header, c.Kind, c.Level,
                         c.NumOfQuestions, c.HasSubCategories 
                         FROM c ";
        //c.NumOfQuestions, c.HasSubCategories 


        //[SearchableField()]
        [JsonProperty(PropertyName = "partitionKey")]
        public string PartitionKey { get; set; }

        //[SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public string Title { get; set; }

        //[VectorSearchField()]
        public List<float> vectors { get; set; }

        //[SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public string? ParentId { get; set; }

        public int? NumOfAssignedAnswers { get; set; }


        internal async Task<List<CategoryRowDto>> GetAllRows()
        {
            var myContainer = await container();
            var sqlQuery = @$"{_rowColumns} WHERE c.Workspace = '{_workspace}' AND c.Type='category' ORDER BY c.ParentId ASC";
            QueryDefinition queryDefinition = new(sqlQuery);
            FeedIterator<CategoryRow> queryResultSetIterator = myContainer.GetItemQueryIterator<CategoryRow>(queryDefinition);
            //List<CategoryDto> subCategories = new List<CategoryDto>();
            List<CategoryRowDto> dtos = [];
            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<CategoryRow> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (CategoryRow categoryRow in currentResultSet)
                {
                    dtos.Add(new CategoryRowDto(categoryRow));
                }
            }
            return dtos;
        }

        internal async Task<List<CatDto>> GetAllCats()
        {
            var myContainer = await container();
            var sqlQuery = @$"{_catColumns} WHERE c.Workspace = '{_workspace}'"; // ORDER BY c.ParentId ASC";
            QueryDefinition queryDefinition = new(sqlQuery);
            FeedIterator<CatDto> queryResultSetIterator = myContainer.GetItemQueryIterator<CatDto>(queryDefinition);
            List<CatDto> dtos = [];
            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<CatDto> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (CatDto catDto in currentResultSet)
                {
                    dtos.Add(catDto);
                }
            }
            return dtos;
        }

        internal async Task<List<CategoryRow>> GetSubRows(Container? cntr, CategoryKey categoryKey)
        {
            var myContainer = cntr != null ? cntr : await container();
            var (workspace, topId, partitionKey, id, parentId) = categoryKey;

            var sqlQuery = _rowColumns + 
                (
                    parentId == null
                        ? @$" WHERE c.Workspace = '{_workspace}' AND IS_NULL(c.ParentId) AND c.Type = 'category'"
                        : @$" WHERE c.Workspace = '{_workspace}' AND c.partitionKey = '{partitionKey}' AND c.Type = 'category' AND c.ParentId = '{id}'"
                )
                + " ORDER BY c.Title";
            //WHERE c.Workspace = '{_workspace}'
            //ORDER BY c.Title ASC";

            //var sqlQuery = partitionKey != null
            //? $"SELECT * FROM c WHERE c.partitionKey = '{partitionKey}' AND c.Type = 'category' AND "
            //: $"SELECT * FROM c WHERE c.Workspace = '{_workspace}' AND c.Type = 'category' AND "
            // for categories partitionKey is same as Id
            //+ (
            //    PartitionKey == "null"
            //        ? $""
            //        : $" c.partitionKey = '{PartitionKey}' AND "  
            //)

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

        public async Task<CategoryRowEx> GetRowWithSubCategories(Container container, CategoryKey categoryKey, bool hidrate)
        {
           // var (workspace, topId, id) = categoryKey;
            var (_, _, partitionKey, id, _) = categoryKey;
            try
            {
                Category category = await container!.ReadItemAsync<Category>(
                    id, 
                    new PartitionKey(partitionKey)
                );
                var categoryRow = new CategoryRow(category);

                categoryKey.ParentId = id;
                categoryRow.SubCategories = hidrate 
                    ? await GetSubRows(container, categoryKey)
                    : [];
                return new CategoryRowEx(categoryRow, "");
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                return new CategoryRowEx((CategoryRow?)null, ex.Message);
            }
        }

        public async Task<CategoryRowEx> GetRowsUpTheTree(CategoryKey categoryKey, int pageSize, string includeQuestionId)
        {
            var myContainer = await container();
            string message = string.Empty;
            var (workspace, topId, partitionKey, id, _) = categoryKey;
            var isBottomRow = true;
            try
            {
                string? parentId = null;
                CategoryRow? categoryRow = null;
                CategoryRow? child = null;
                do
                {
                    bool hidrate = !isBottomRow || includeQuestionId != "null" ? true : false; // includeAnswerId == "null" ? false : true;//groupRow != null; // do not hidrate row at the bottom
                    CategoryRowEx categoryRowEx = await GetRowWithSubCategories(myContainer, categoryKey, hidrate);
                    // Console.WriteLine("---------------------------------------------------");
                    // Console.WriteLine(JsonConvert.SerializeObject(categoryEx)); 
                    var (catRow, msg) = categoryRowEx;
                    if (catRow != null)
                    {
                        //child = cat;
                        if (catRow.HasSubCategories)
                        {
                            int index = catRow.SubCategories!.FindIndex(x => x.Id == child!.Id);
                            if (index >= 0)
                            {
                                catRow.SubCategories[index] = child!.ShallowCopy();
                            }
                        }
                        if (isBottomRow && includeQuestionId != "null")
                        {
                            if (catRow.NumOfQuestions > 0)
                            {
                                var questionService = new QuestionService(Db, workspace);
                                var questionKey = new QuestionKey(categoryKey);
                                QuestionsMore questionsMore = await questionService.GetQuestions(questionKey, 0, pageSize, includeQuestionId ?? "null");
                                catRow.QuestionRows = questionsMore.QuestionRows.ToList();
                                catRow.HasMoreQuestions = questionsMore.HasMoreQuestions;
                            }
                        }
                        isBottomRow = false;
                        catRow.IsExpanded = hidrate;
                        child = catRow.ShallowCopy();
                        parentId = catRow.ParentId!;
                        // partitionKey is the same as Id
                        categoryKey = new CategoryKey(workspace, topId, id: parentId);
                        categoryRow = catRow.DeepCopy();
                    }
                    else
                    {
                        message = msg;
                        parentId = null;
                    }
                } while (parentId != null);
                // put root id to each Category
                //if (categoryRow != null) {
                //    SetTopId(categoryRow, categoryRow.Id);
                //}
                return new CategoryRowEx(categoryRow, message);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                Console.WriteLine(message);
            }
            return new CategoryRowEx((CategoryRow?)null, message);
        }

        //void SetTopId(CategoryRow categoryRow, string topId)
        //{
        //    categoryRow.TopId = topId;
        //    Debug.Assert(categoryRow.SubCategories != null);
        //    categoryRow.SubCategories.ForEach(c => {
        //        SetTopId(c, topId);
        //    });
        //}

        /*
        public async Task<CategoryRowDtoEx> GetCategoryRow(CategoryKey categoryKey)
        {
            // used for node collapse
            var (PartitionKey, Id) = categoryKey;
            var myContainer = await container();
            var msg = string.Empty;
            try
            {
                // Read the item to see if it exists.  
                //ItemResponse<Category> aResponse =
                Category category = await myContainer!.ReadItemAsync<Category>(Id, new PartitionKey(PartitionKey));
                //Console.WriteLine(JsonConvert.SerializeObject(category));
                if (category == null)
                {
                    msg = "Not Found Bre";
                }
                else
                {
                    var categoryRowDto = new CategoryRowDto(new CategoryRow(category));
                    return new CategoryRowDtoEx(categoryRowDto, "");
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return new CategoryRowDtoEx(null, msg);
        }
        */

        public async Task<CategoryRow?> GetCategoryRow(CategoryKey categoryKey, 
            bool hidrate, int pageSize, string? includeQuestionId)
        {
            // used for node expand
            var (workspace, topId, partitionKey, id, _) =  categoryKey;
            var myContainer = await container();
            var msg = string.Empty;
            try
            {
                Category category = await myContainer!.ReadItemAsync<Category>(id, new PartitionKey(partitionKey));
                if (category == null)
                {
                    msg = "Not Found Bre";
                }
                else
                {
                    if (hidrate)
                    {
                        ///////////////////
                        // subCategoryRows
                        List<CategoryRow> subCategories = await GetSubRows(myContainer, categoryKey);
                        category.SubCategories = subCategories;

                        ///////////////////
                        // questionsRows
                        if (pageSize > 0)
                        {
                            if (category.NumOfQuestions > 0)
                            {
                                var questionService = new QuestionService(Db, workspace);
                                var questionKey = new QuestionKey(categoryKey);
                                QuestionsMore questionsMore = await questionService.GetQuestions(questionKey, 0, pageSize, includeQuestionId ?? "null");
                                category.QuestionRows = questionsMore.QuestionRows.ToList();
                                category.HasMoreQuestions = questionsMore.HasMoreQuestions;
                            }
                        }
                    }
                    var categoryRow = new CategoryRow(category);
                    return categoryRow; 
                    //var categoryRowDto = new CategoryRowDto(categoryRow);
                    //return new CategoryRowDtoEx(categoryRowDto, "");
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return null; // new CategoryRow(null, msg);
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



