using Azure.Search.Documents.Indexes;
using Microsoft.AspNetCore.OutputCaching;
using KnowledgeAPI.A.Answers.Model;
using KnowledgeAPI.Common;
using Newtonsoft.Json;
using System;

namespace KnowledgeAPI.A.Groups.Model
{
    public class Group : Record, IDisposable
    {
        [SearchableField()]
        [JsonProperty(PropertyName = "partitionKey")]
        public string PartitionKey { get; set; }
        public string? ParentId { get; set; } // Parent Group Id, it is null for Top Group Id

        [JsonProperty(PropertyName = "id")]
        [SearchableField()]
        public string Id { get; set; }

        [VectorSearchField()]

        public List<float> vectors { get; set; }
                
        [SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public string Title { get; set; }

        public string? Link { get; set; }
        public string Header { get; set; }

        public int Kind { get; set; }
        public int Level { get; set; }

        [JsonProperty(PropertyName = "Variations", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Variations { get; set; }
        public int NumOfAnswers { get; set; }
        public bool HasSubGroups { get; set; }

        [JsonProperty(PropertyName = "SubGroups", NullValueHandling = NullValueHandling.Ignore)]
        public List<GroupRow>? SubGroups { get; set; }

        [JsonProperty(PropertyName = "Answers", NullValueHandling = NullValueHandling.Ignore)]
        public List<AnswerRow>? AnswerRows { get; set; }

        [JsonProperty(PropertyName = "HasMoreAnswers", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasMoreAnswers { get; set; }

        [JsonProperty(PropertyName = "IsExpanded", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsExpanded { get; set; }

        public string Doc1 { get; set; } // just to differentiate IGroupRow and IGroup ad FrontEnd


        public Group()
            : base()
        {
            Type = "group";
        }

        //public Group(GroupRow row)
        //{
        //    var (partitionKey, id, parentId, title, link, header, level, kind,
        //                   hasSubGroups, subGroups,
        //                   hasMoreAnswers, numOfAnswers, answerRows, variations, isExpanded, topId) = row;
        //    PartitionKey = partitionKey;
        //    Id = id;
        //    ParentId = parentId;
        //    Title = title;
        //    Link = link;
        //    Header = header;
        //    Level = level!;
        //    Kind = kind;
        //    HasSubGroups = hasSubGroups;
        //    SubGroups = subGroups; //.Select(c => new GroupRow(c)).ToList();
        //    HasMoreAnswers = hasMoreAnswers;
        //    NumOfAnswers = numOfAnswers;
        //    AnswerRows = answerRows;
        //    Variations = [];
        //    IsExpanded = false;
        //    TopId = topId;
        //}

        public Group(Answer answer)
          : base()
        {
            Id = answer.ParentId!;
            PartitionKey = answer.PartitionKey;
        }


        public Group(GroupData groupData)
            : base(new WhoWhen("Admin"), null)
        {
            var (workspace, topId, partitionKey, id, title, link, header, parentId, kind, level, variations, groups, answers) = groupData;
            Workspace = workspace;
            TopId = topId;
            PartitionKey = partitionKey;
            Id = id;
            Type = "group";
            Title = title;
            Link = link;
            Header = header ?? ""; 
            Kind = kind;
            ParentId = parentId;
            Level = (int)level!;
            Variations = variations ?? null;
            NumOfAnswers = answers == null ? 0 : answers.Count;
            HasSubGroups = groups != null && groups.Count > 0;
            AnswerRows = null;
            vectors = [];
            Doc1 = string.Empty;
        }

        public Group(GroupDto groupDto)
            :base(groupDto.Created, groupDto.Modified)
        {
            Type = "group";
            Workspace = groupDto.Workspace;
            Id = groupDto.Id;
            TopId = groupDto.TopId == "generateId" ? Id : groupDto.TopId;
            PartitionKey = groupDto.Workspace + "/" + TopId;
            Title = groupDto.Title;
            Link = groupDto.Link;
            Kind = groupDto.Kind;
            ParentId = groupDto.ParentId == "ROOT" ? null : groupDto.ParentId;
            Level = groupDto.Level;
            Variations = groupDto.Variations ?? null;
            AnswerRows = null;
            NumOfAnswers = 0;
            HasSubGroups = false;
            vectors = [];
        }

        //public Group(Group group)
        //   : base(group.Created, group.Modified, null)
        //{
        //    return JsonConvert.DeserializeObject(JsonConvert.SerializeObject(group));
        //}

        //public override string ToString() =>
        //    $"{PartitionKey}/{Id} : {Title}";


        public void Deconstruct(
            out string workspace,
            out string topId,
            out string partitionKey,
            out string id, 
            out string? parentId, 
            out string title,
            out string? link,
            out string header,
            out int level, 
            out int kind,
            out bool hasSubGroups,
            out List<GroupRow> subGroups,
            out bool? hasMoreAnswers,
            out int numOfAnswers,
            out List<AnswerRow>? answerRows,
            out List<string>? variations,
            out bool? isExpanded,
            out string doc1)
        {
            workspace = Workspace;
            topId = TopId;
            partitionKey = PartitionKey;
            id = Id;
            parentId = ParentId;
            title = Title;
            link = Link;
            header = Header;
            kind = Kind;
            level = Level;
            hasSubGroups = HasSubGroups;
            subGroups = SubGroups ?? [];
            numOfAnswers = NumOfAnswers;
            answerRows = AnswerRows;
            hasMoreAnswers = HasMoreAnswers;
            variations = Variations;
            isExpanded = IsExpanded;
            topId = TopId;
            doc1 = Doc1;
        }

        public static int Comparer(Group x, Group y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    // If x is null and y is null, they're
                    // equal.
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y
                    // is greater.
                    return -1;
                }
            }
            else
            {
                // If x is not null...
                //
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {
                    // ...and y is not null, compare the
                    // lengths of the two strings.
                    //
                    int retval = x.Title.CompareTo(y.Title);  // ASC
                    return retval;
                }
            }
        }

        public Group ShallowCopy()
        {
            return (Group)MemberwiseClone();
        }

        public Group DeepCopy()
        {
            return JsonConvert.DeserializeObject<Group>(JsonConvert.SerializeObject(this))!;
        }

        public void Dispose()
        {
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



