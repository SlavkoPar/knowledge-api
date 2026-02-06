using KnowledgeAPI.Q.Questions.Model;
using System.Diagnostics.Metrics;
using System.Net;


namespace KnowledgeAPI.Q.Categories.Model
{
    public class CategoryData
    {
        public string Workspace { get; set; }
        public string TopId { get; set; }
        public string PartitionKey {
            get {
                return Workspace + "/" + TopId;
            }
        }

        public string? ParentId { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }
        public string? Link { get; set; }
        public string? Header { get; set; }
        public int Kind { get; set; }
        public int? Level { get; set; }
        public List<string>? Variations { get; set; }
        public List<CategoryData>? Categories { get; set; }
        public List<QuestionData>? Questions { get; set; }

        public CategoryData() { 
        }

        public void Deconstruct(
            out string workspace,
            out string topId,
            out string partitionKey,
            out string id,
            out string title,
            out string? link,
            out string? header,
            out string? parentId,
            out int kind,
            out int? level,
            out List<string>? variations,
            out List<CategoryData>? categories,
            out List<QuestionData>? questions)
            {
                workspace = Workspace;
                topId = TopId;
                partitionKey = PartitionKey;
                id = Id;
                title = Title;
                link = Link;
                header = Header;
                parentId = ParentId;
                kind = Kind;
                level = Level;
                variations = Variations;
                categories = Categories;
                questions = Questions;
            }
        }
    

    public class CategoriesData
    {
        public string Workspace { get; set; }
        public List<CategoryData> Categories { get; set; }
    }
}
