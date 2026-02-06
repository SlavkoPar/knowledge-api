using KnowledgeAPI.A.Answers.Model;
using KnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Net;

namespace KnowledgeAPI.A.Groups.Model
{
    public class GroupDto : RecordDto
    {
        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        public string Title { get; set; }
        public string? Link { get; set; }
        public string Header { get; set; }

        public int Kind { get; set; }
        public int Level { get; set; }
        public List<string>? Variations { get; set; }
        public bool HasSubGroups { get; set; }
        public List<GroupRowDto>? SubGroups { get; set; }
        public int NumOfAnswers { get; set; }
        public List<AnswerRowDto>? AnswerRowDtos { get; set; }
        public bool? HasMoreAnswers { get; set; }

        public bool? IsExpanded { get; set; }
        public string Doc1 { get; set; } // just to differentiate IGroupRow and IGroup ad FrontEnd
      
        public GroupDto()
            : base()
        {
        }
      

        public GroupDto(GroupKey groupKey, AnswersMore answersMore)
            : base() // TODO
            //: base(null, null, null) // TODO prosledi 
        {
            var (workspace, topId, partitionKey, id, _) = groupKey;
            Id = id;
            Title = "deca";
            Link = null;
            Header = "peca";
            Kind = 1;
            Level = 1;
            Variations = [];

            //Console.WriteLine("pitanja {0}", answersMore.answers.Count);
            //if (answersMore.answers.Count > 0) {
            //    Answer q = answersMore.answers.First();
            //}
            AnswerRowDtos = Answers2Dto(answersMore.AnswerRows/*.Select(row => new Answer(row))*/.ToList());
            HasMoreAnswers = answersMore.HasMoreAnswers;
            Doc1 = string.Empty;
        }

        public GroupDto(Group group)
            : base(group.Created, group.Modified)
        {
            var (workspace, topId, partitionKey, id, parentId, title, link, header, level, kind,
                hasSubGroups, subGroups,
                hasMoreAnswers, numOfAnswers, answerRows, variations, isExpanded, doc1) = group;
            Id = id;
            Title = title;
            Link = link;
            Header = header;   
            Kind = kind;
            TopId = topId;
            ParentId = parentId;
            Level = level;
            Variations = variations;
            HasSubGroups = hasSubGroups;
            SubGroups = subGroups != null 
                ? subGroups.Select(row => new GroupRowDto(row)).ToList()
                : [];
            NumOfAnswers = numOfAnswers; //answers == null ? 0 : answers.Count;
            IsExpanded = isExpanded;
            Doc1 = doc1;
            if (answerRows == null)
            {
                AnswerRowDtos = [];
                HasMoreAnswers = false;
            }
            else
            {
                //IList<AnswerDto> answers = new List<AnswerDto>();
                //foreach (var answer in group.answers)
                //    answers.Add(new AnswerDto(answer));
                AnswerRowDtos = Answers2Dto(answerRows!);
                HasMoreAnswers = hasMoreAnswers;
            }
        }

        public List<AnswerRowDto> Answers2Dto(List<AnswerRow> answerRows)
        {
            List<AnswerRowDto> list = [];
            foreach (var answerRow in answerRows)
            {
                //Console.WriteLine(JsonConvert.SerializeObject(answer));
                list.Add(new AnswerRowDto(answerRow));
            }
            return list;
        }

        public List<AnswerDto> Answers2Dto(List<Answer> answers)
        {
            List<AnswerDto> list = [];
            foreach (var answer in answers)
            {
                //Console.WriteLine(JsonConvert.SerializeObject(answer));
                list.Add(new AnswerDto(answer));
            }
            return list;
        }


        public void Deconstruct(out string workspace, out string topId, out string partitionKey, 
            out string id, out string? parentId, 
                out string title, out string? link, 
                out int level, out int kind, out List<string>? variations,
                out WhoWhenDto? modified,
                out string doc1 )
        {
            workspace = Workspace;
            topId = TopId;
            partitionKey = PartitionKey;    
            id = Id;
            parentId = ParentId;
            title = Title;
            link = Link;
            kind = Kind;
            level = Level;
            variations = Variations;
            modified = Modified;
            doc1 = Doc1;
        }

    }
}



