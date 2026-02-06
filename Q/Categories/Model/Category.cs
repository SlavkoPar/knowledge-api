using Azure.Search.Documents.Indexes;
using Microsoft.AspNetCore.OutputCaching;
using KnowledgeAPI.Common;
using KnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System;

namespace KnowledgeAPI.Q.Categories.Model
{
    public class Category : Record, IDisposable
    {
        [SearchableField()]
        [JsonProperty(PropertyName = "partitionKey")]
        public string PartitionKey { get; set; }
        public string? ParentId { get; set; } // Parent Category Id, it is null for Top Category Id

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
        public int NumOfQuestions { get; set; }
        public bool HasSubCategories { get; set; }

        [JsonProperty(PropertyName = "SubCategories", NullValueHandling = NullValueHandling.Ignore)]
        public List<CategoryRow>? SubCategories { get; set; }

        [JsonProperty(PropertyName = "Questions", NullValueHandling = NullValueHandling.Ignore)]
        public List<QuestionRow>? QuestionRows { get; set; }

        [JsonProperty(PropertyName = "HasMoreQuestions", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasMoreQuestions { get; set; }

        [JsonProperty(PropertyName = "IsExpanded", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsExpanded { get; set; }

        public string Doc1 { get; set; } // just to differentiate ICategoryRow and ICategory ad FrontEnd


        public Category()
            : base()
        {
            Type = "category";
        }

        //public Category(CategoryRow row)
        //{
        //    var (partitionKey, id, parentId, title, link, header, level, kind,
        //                   hasSubCategories, subCategories,
        //                   hasMoreQuestions, numOfQuestions, questionRows, variations, isExpanded, topId) = row;
        //    PartitionKey = partitionKey;
        //    Id = id;
        //    ParentId = parentId;
        //    Title = title;
        //    Link = link;
        //    Header = header;
        //    Level = level!;
        //    Kind = kind;
        //    HasSubCategories = hasSubCategories;
        //    SubCategories = subCategories; //.Select(c => new CategoryRow(c)).ToList();
        //    HasMoreQuestions = hasMoreQuestions;
        //    NumOfQuestions = numOfQuestions;
        //    QuestionRows = questionRows;
        //    Variations = [];
        //    IsExpanded = false;
        //    TopId = topId;
        //}

        public Category(Question question)
          : base()
        {
            Id = question.ParentId!;
            PartitionKey = question.PartitionKey;
        }


        public Category(CategoryData categoryData)
            : base(new WhoWhen("Admin"), null)
        {
            var (workspace, topId, partitionKey, id, title, link, header, parentId, kind, level, variations, categories, questions) = categoryData;
            Workspace = workspace;
            TopId = topId;
            PartitionKey = partitionKey;
            Id = id;
            Type = "category";
            Title = title;
            Link = link;
            Header = header ?? ""; 
            Kind = kind;
            ParentId = parentId;
            Level = (int)level!;
            Variations = variations ?? null;
            NumOfQuestions = questions == null ? 0 : questions.Count;
            HasSubCategories = categories != null && categories.Count > 0;
            QuestionRows = null;
            vectors = [];
            Doc1 = string.Empty;
        }

        public Category(CategoryDto categoryDto)
            :base(categoryDto.Created, categoryDto.Modified)
        {
            Type = "category";
            Workspace = categoryDto.Workspace;
            Id = categoryDto.Id;
            TopId = categoryDto.TopId == "generateId" ? Id : categoryDto.TopId;
            PartitionKey = categoryDto.Workspace + "/" + TopId;
            Title = categoryDto.Title;
            Link = categoryDto.Link;
            Kind = categoryDto.Kind;
            ParentId = categoryDto.ParentId == "ROOT" ? null : categoryDto.ParentId;
            Level = categoryDto.Level;
            Variations = categoryDto.Variations ?? null;
            QuestionRows = null;
            NumOfQuestions = 0;
            HasSubCategories = false;
            vectors = [];
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
            out string partitionKey,
            out string id, 
            out string? parentId, 
            out string title,
            out string? link,
            out string header,
            out int level, 
            out int kind,
            out bool hasSubCategories,
            out List<CategoryRow> subCategories,
            out bool? hasMoreQuestions,
            out int numOfQuestions,
            out List<QuestionRow>? questionRows,
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
            hasSubCategories = HasSubCategories;
            subCategories = SubCategories ?? [];
            numOfQuestions = NumOfQuestions;
            questionRows = QuestionRows;
            hasMoreQuestions = HasMoreQuestions;
            variations = Variations;
            isExpanded = IsExpanded;
            topId = TopId;
            doc1 = Doc1;
        }

        public static int Comparer(Category x, Category y)
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

        public Category ShallowCopy()
        {
            return (Category)MemberwiseClone();
        }

        public Category DeepCopy()
        {
            return JsonConvert.DeserializeObject<Category>(JsonConvert.SerializeObject(this))!;
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



