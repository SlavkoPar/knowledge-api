using Azure;
using Knowledge.Services;
using Microsoft.Azure.Cosmos;
using KnowledgeAPI.Common;
using KnowledgeAPI.A.Groups.Model;
using KnowledgeAPI.A.Answers;
using KnowledgeAPI.A.Answers.Model;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Net;

namespace KnowledgeAPI.A.Groups
{
    public class GroupRowService : IDisposable
    {
        public DbService? Db { get; set; } = null;

        protected readonly string containerId = "Groups";
        private Container? _container = null;
        private string _workspace = null;


        public async Task<Container> container()
        {
            _container ??= await Db!.GetContainer(containerId);
            return _container;
        }

        public GroupRowService()
        {
        }

        //public Group(IConfiguration configuration)
        //{
        //    Group.Db = new Db(configuration);
        //}

        public GroupRowService(DbService db, string workspace)
        {
            Db = db;
            _workspace = workspace;
        }

        readonly string _catColumns = @"SELECT c.TopId, c.id, c.ParentId, c.Level, c.Title FROM c ";


        readonly string _rowColumns = @"SELECT '', c.Workspace, c.TopId, 
                         c.id, c.ParentId, 
                         c.Title, c.Link, c.Header, c.Kind, c.Level,
                         c.NumOfAnswers, c.HasSubGroups 
                         FROM c ";
        //c.NumOfAnswers, c.HasSubGroups 


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


        internal async Task<List<GroupRowDto>> GetAllRows()
        {
            var myContainer = await container();
            var sqlQuery = @$"{_rowColumns} WHERE c.Workspace = '{_workspace}' AND c.Type = 'group' ORDER BY c.ParentId ASC";
            QueryDefinition queryDefinition = new(sqlQuery);
            FeedIterator<GroupRow> queryResultSetIterator = myContainer.GetItemQueryIterator<GroupRow>(queryDefinition);
            //List<GroupDto> subGroups = new List<GroupDto>();
            List<GroupRowDto> dtos = [];
            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<GroupRow> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (GroupRow groupRow in currentResultSet)
                {
                    dtos.Add(new GroupRowDto(groupRow));
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

        internal async Task<List<GroupRow>> GetSubRows(Container? cntr, GroupKey groupKey)
        {
            var myContainer = cntr != null ? cntr : await container();
            var (workspace, topId, partitionKey, id, parentId) = groupKey;

            var sqlQuery = _rowColumns + 
                (
                    parentId == null
                        ? @$" WHERE c.Workspace = '{_workspace}' AND IS_NULL(c.ParentId) AND c.Type = 'group'"
                        : @$" WHERE c.Workspace = '{_workspace}' AND c.partitionKey = '{partitionKey}' AND c.Type = 'group' AND c.ParentId = '{id}'"
                )
                + " ORDER BY c.Title";
            //WHERE c.Workspace = '{_workspace}'
            //ORDER BY c.Title ASC";

            //var sqlQuery = partitionKey != null
            //? $"SELECT * FROM c WHERE c.partitionKey = '{partitionKey}' AND c.Type = 'group' AND "
            //: $"SELECT * FROM c WHERE c.Workspace = '{_workspace}' AND c.Type = 'group' AND "
            // for categories partitionKey is same as Id
            //+ (
            //    PartitionKey == "null"
            //        ? $""
            //        : $" c.partitionKey = '{PartitionKey}' AND "  
            //)

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

        public async Task<GroupRowEx> GetRowWithSubGroups(Container container, GroupKey groupKey, bool hidrate)
        {
           // var (workspace, topId, id) = groupKey;
            var (_, _, partitionKey, id, _) = groupKey;
            try
            {
                Group group = await container!.ReadItemAsync<Group>(
                    id, 
                    new PartitionKey(partitionKey)
                );
                var groupRow = new GroupRow(group);

                groupKey.ParentId = id;
                groupRow.SubGroups = hidrate 
                    ? await GetSubRows(container, groupKey)
                    : [];
                return new GroupRowEx(groupRow, "");
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                return new GroupRowEx((GroupRow?)null, ex.Message);
            }
        }

        public async Task<GroupRowEx> GetRowsUpTheTree(GroupKey groupKey, int pageSize, string includeAnswerId)
        {
            var myContainer = await container();
            string message = string.Empty;
            var (workspace, topId, partitionKey, id, _) = groupKey;
            var isBottomRow = true;
            try
            {
                string? parentId = null;
                GroupRow? groupRow = null;
                GroupRow? child = null;
                do
                {
                    bool hidrate = !isBottomRow || includeAnswerId != "null" ? true : false; // includeAnswerId == "null" ? false : true;//groupRow != null; // do not hidrate row at the bottom
                    GroupRowEx groupRowEx = await GetRowWithSubGroups(myContainer, groupKey, hidrate);
                    // Console.WriteLine("---------------------------------------------------");
                    // Console.WriteLine(JsonConvert.SerializeObject(groupEx)); 
                    var (catRow, msg) = groupRowEx;
                    if (catRow != null)
                    {
                        //child = cat;
                        if (catRow.HasSubGroups)
                        {
                            int index = catRow.SubGroups!.FindIndex(x => x.Id == child!.Id);
                            if (index >= 0)
                            {
                                catRow.SubGroups[index] = child!.ShallowCopy();
                            }
                        }
                        if (isBottomRow && includeAnswerId != "null")
                        {
                            if (catRow.NumOfAnswers > 0)
                            {
                                var questionService = new AnswerService(Db, workspace);
                                var questionKey = new AnswerKey(groupKey);
                                AnswersMore questionsMore = await questionService.GetAnswers(questionKey, 0, pageSize, includeAnswerId ?? "null");
                                catRow.AnswerRows = questionsMore.AnswerRows.ToList();
                                catRow.HasMoreAnswers = questionsMore.HasMoreAnswers;
                            }
                        }
                        isBottomRow = false;
                        catRow.IsExpanded = hidrate;
                        child = catRow.ShallowCopy();
                        parentId = catRow.ParentId!;
                        // partitionKey is the same as Id
                        groupKey = new GroupKey(workspace, topId, id: parentId);
                        groupRow = catRow.DeepCopy();
                    }
                    else
                    {
                        message = msg;
                        parentId = null;
                    }
                } while (parentId != null);
                // put root id to each Group
                //if (groupRow != null) {
                //    SetTopId(groupRow, groupRow.Id);
                //}
                return new GroupRowEx(groupRow, message);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                Console.WriteLine(message);
            }
            return new GroupRowEx((GroupRow?)null, message);
        }

        //void SetTopId(GroupRow groupRow, string topId)
        //{
        //    groupRow.TopId = topId;
        //    Debug.Assert(groupRow.SubGroups != null);
        //    groupRow.SubGroups.ForEach(c => {
        //        SetTopId(c, topId);
        //    });
        //}

        /*
        public async Task<GroupRowDtoEx> GetGroupRow(GroupKey groupKey)
        {
            // used for node collapse
            var (PartitionKey, Id) = groupKey;
            var myContainer = await container();
            var msg = string.Empty;
            try
            {
                // Read the item to see if it exists.  
                //ItemResponse<Group> aResponse =
                Group group = await myContainer!.ReadItemAsync<Group>(Id, new PartitionKey(PartitionKey));
                //Console.WriteLine(JsonConvert.SerializeObject(group));
                if (group == null)
                {
                    msg = "Not Found Bre";
                }
                else
                {
                    var groupRowDto = new GroupRowDto(new GroupRow(group));
                    return new GroupRowDtoEx(groupRowDto, "");
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return new GroupRowDtoEx(null, msg);
        }
        */

        public async Task<GroupRow?> GetGroupRow(GroupKey groupKey, 
            bool hidrate, int pageSize, string? includeAnswerId)
        {
            // used for node expand
            var (workspace, topId, partitionKey, id, _) =  groupKey;
            var myContainer = await container();
            var msg = string.Empty;
            try
            {
                Group group = await myContainer!.ReadItemAsync<Group>(id, new PartitionKey(partitionKey));
                if (group == null)
                {
                    msg = "Not Found Bre";
                }
                else
                {
                    if (hidrate)
                    {
                        ///////////////////
                        // subGroupRows
                        List<GroupRow> subGroups = await GetSubRows(myContainer, groupKey);
                        group.SubGroups = subGroups;

                        ///////////////////
                        // questionsRows
                        if (pageSize > 0)
                        {
                            if (group.NumOfAnswers > 0)
                            {
                                var questionService = new AnswerService(Db, workspace);
                                var questionKey = new AnswerKey(groupKey);
                                AnswersMore questionsMore = await questionService.GetAnswers(questionKey, 0, pageSize, includeAnswerId ?? "null");
                                group.AnswerRows = questionsMore.AnswerRows.ToList();
                                group.HasMoreAnswers = questionsMore.HasMoreAnswers;
                            }
                        }
                    }
                    var groupRow = new GroupRow(group);
                    return groupRow; 
                    //var groupRowDto = new GroupRowDto(groupRow);
                    //return new GroupRowDtoEx(groupRowDto, "");
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return null; // new GroupRow(null, msg);
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



