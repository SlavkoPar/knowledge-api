namespace KnowledgeAPI.Q.Questions.Model
{
    public class QuestionQueryParams
    {
            public string TopId { get; set; }
            public string ParentId { get; set; }

            public int StartCursor { get; set; }
            public int PageSize { get; set; }

            public QuestionQueryParams()
            {
            }

            public void Deconstruct(out string topId, out string parentId, out int startCursor, out int pageSize)
            {
                topId = TopId;
                parentId = ParentId;
                startCursor = StartCursor;
                pageSize = PageSize;
            }

        }
}
