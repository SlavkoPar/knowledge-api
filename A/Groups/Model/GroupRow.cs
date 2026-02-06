using Azure.Search.Documents.Indexes;
using Microsoft.AspNetCore.OutputCaching;
using KnowledgeAPI.Common;
using KnowledgeAPI.A.Answers.Model;
using Newtonsoft.Json;
using System;

namespace KnowledgeAPI.A.Groups.Model
{
    public class GroupRow : Record, IDisposable
    {
        [JsonProperty(PropertyName = "id")]
        [SearchableField()]
        public string Id { get; set; }
        public string? ParentId { get; set; } // Parent Group Id, it is null for Top Group Id

        public string Title { get; set; }
        public string? Link { get; set; }
        public string Header { get; set; }

        public int Kind { get; set; }
        public int Level { get; set; }
        public List<string>? Variations { get; set; }
        public int NumOfAnswers { get; set; }
        public bool HasSubGroups { get; set; }

        [JsonProperty(PropertyName = "SubGroups", NullValueHandling = NullValueHandling.Ignore)]
        public List<GroupRow>? SubGroups {  get; set; }

        [JsonProperty(PropertyName = "Answers", NullValueHandling = NullValueHandling.Ignore)]
        public List<AnswerRow>? AnswerRows { get; set; }

        [JsonProperty(PropertyName = "HasMoreAnswers", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasMoreAnswers { get; set; }

        [JsonProperty(PropertyName = "IsExpanded", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsExpanded { get; set; }


        public GroupRow()
            : base()
        {
        }

        public GroupRow(Group group)
          : base(group.Created, group.Modified)
        {
            var (workspace, topId, partitionKey, id, parentId, title, link, header, level, kind,
                hasSubGroups, subGroups,
                hasMoreAnswers, numOfAnswers, answerRows, variations, isExpanded, doc1) = group;

            Workspace = workspace;
            TopId = topId;
            Id = id;
            ParentId = parentId;
            Title = title;
            Link = link;
            Header = header;
            Level = level;
            Kind = kind;
            HasSubGroups = hasSubGroups;
            SubGroups = subGroups; //.Select(c => new GroupRow(c)).ToList();
            HasMoreAnswers = hasMoreAnswers;
            NumOfAnswers = numOfAnswers;
            AnswerRows = answerRows;
            Variations = variations;
            IsExpanded = false;
            TopId = topId;
        }

        public GroupRow(Answer answer)
          : base()
        {
            Id = answer.ParentId!;
            Workspace = answer.Workspace;
            TopId = answer.TopId;
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
            out string id, 
            out string? parentId, 
            out string title,
            out string? link,
            out string header,
            out int level, 
            out int kind,
            out bool hasSubGroups,
            out List<GroupRow>? subGroups,
            out bool? hasMoreAnswers,
            out int numOfAnswers,
            out List<AnswerRow>? answerRows,
            out List<string>? variations,
            out bool? isExpanded)
        {
            workspace = Workspace;
            topId = TopId;
            id = Id;
            parentId = ParentId;
            title = Title;
            link = Link;
            header = Header;
            kind = Kind;
            level = Level;
            hasSubGroups = HasSubGroups;
            subGroups = SubGroups;
            numOfAnswers = NumOfAnswers;
            answerRows = AnswerRows;
            hasMoreAnswers = HasMoreAnswers;
            variations = Variations;
            isExpanded = IsExpanded;
        }

        public static int Comparer(GroupRow x, GroupRow y)
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

        public GroupRow ShallowCopy()
        {
            return (GroupRow)MemberwiseClone();
        }

        public GroupRow DeepCopy()
        {
            return JsonConvert.DeserializeObject<GroupRow>(JsonConvert.SerializeObject(this))!;
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



