using Azure.Search.Documents.Indexes;
using Microsoft.AspNetCore.OutputCaching;
using KnowledgeAPI.Common;
using KnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System;

namespace KnowledgeAPI.Q.Categories.Model
{
    public class CategoryRow : Record, IDisposable
    {
        [JsonProperty(PropertyName = "id")]
        [SearchableField()]
        public string Id { get; set; }
        public string? ParentId { get; set; } // Parent Category Id, it is null for Top Category Id

        public string Title { get; set; }
        public string? Link { get; set; }
        public string Header { get; set; }

        public int Kind { get; set; }
        public int Level { get; set; }
        public List<string>? Variations { get; set; }
        public int NumOfQuestions { get; set; }
        public bool HasSubCategories { get; set; }

        [JsonProperty(PropertyName = "SubCategories", NullValueHandling = NullValueHandling.Ignore)]
        public List<CategoryRow>? SubCategories {  get; set; }

        [JsonProperty(PropertyName = "Questions", NullValueHandling = NullValueHandling.Ignore)]
        public List<QuestionRow>? QuestionRows { get; set; }

        [JsonProperty(PropertyName = "HasMoreQuestions", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasMoreQuestions { get; set; }

        [JsonProperty(PropertyName = "IsExpanded", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsExpanded { get; set; }


        public CategoryRow()
            : base()
        {
        }

        public CategoryRow(Category category)
          : base(category.Created, category.Modified)
        {
            var (workspace, topId, partitionKey, id, parentId, title, link, header, level, kind,
                hasSubCategories, subCategories,
                hasMoreQuestions, numOfQuestions, questionRows, variations, isExpanded, doc1) = category;

            Workspace = workspace;
            TopId = topId;
            Id = id;
            ParentId = parentId;
            Title = title;
            Link = link;
            Header = header;
            Level = level;
            Kind = kind;
            HasSubCategories = hasSubCategories;
            SubCategories = subCategories; //.Select(c => new CategoryRow(c)).ToList();
            HasMoreQuestions = hasMoreQuestions;
            NumOfQuestions = numOfQuestions;
            QuestionRows = questionRows;
            Variations = variations;
            IsExpanded = isExpanded;
            TopId = topId;
        }

        public CategoryRow(Question question)
          : base()
        {
            Id = question.ParentId!;
            Workspace = question.Workspace;
            TopId = question.TopId;
        }


        //public Category(Category category)
        //   : base(category.Created, category.Modified, null)
        //{
        //    return JsonConvert.DeserializeObject(JsonConvert.SerializeObject(category));
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
            out bool hasSubCategories,
            out List<CategoryRow>? subCategories,
            out bool? hasMoreQuestions,
            out int numOfQuestions,
            out List<QuestionRow>? questionRows,
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
            hasSubCategories = HasSubCategories;
            subCategories = SubCategories;
            numOfQuestions = NumOfQuestions;
            questionRows = QuestionRows;
            hasMoreQuestions = HasMoreQuestions;
            variations = Variations;
            isExpanded = IsExpanded;
        }

        public static int Comparer(CategoryRow x, CategoryRow y)
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

        public CategoryRow ShallowCopy()
        {
            return (CategoryRow)MemberwiseClone();
        }

        public CategoryRow DeepCopy()
        {
            return JsonConvert.DeserializeObject<CategoryRow>(JsonConvert.SerializeObject(this))!;
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



