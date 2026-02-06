using Microsoft.AspNetCore.OutputCaching;
using KnowledgeAPI.Common;
using KnowledgeAPI.A.Answers.Model;
using Newtonsoft.Json;
using System;

namespace KnowledgeAPI.A.Groups.Model
{
    public class GroupRowDto: IDisposable
    {
        public string Workspace { get; set; }
        public string TopId { get; set; }  // Top Group Id

        public string Id { get; set; }
        public string? ParentId { get; set; }

        public string Title { get; set; }

        public int Kind { get; set; }
        public int Level { get; set; }
        public bool HasSubGroups { get; set; }
        public List<GroupRowDto>? RowDtos { get; set; }
        public int NumOfAnswers { get; set; }
        public List<AnswerRowDto>? AnswerRowDtos { get; set; }

        public string? Link { get; set; }
        public string Header { get; set; }
        public List<string>? Variations { get; set; }

        public bool? IsExpanded { get; set; }
        public WhoWhenDto? Modified { get; set; }

        public GroupRowDto()
        {
        }

        public GroupRowDto(GroupRow groupRow)
        {
            var (_, topId, id, parentId, title, link, header, level, kind,
                hasSubGroups, subGroups,
                hasMoreAnswers, numOfAnswers, answerRows, variations, isExpanded) = groupRow;
            Workspace = null;
            TopId = topId;
            Id = id;
            Title = title;
            Kind = kind;
            ParentId = parentId;
            Level = level;
            HasSubGroups = hasSubGroups;
            RowDtos = subGroups != null ? subGroups.Select(c => new GroupRowDto(c)).ToList() : null;
            NumOfAnswers = numOfAnswers;
            AnswerRowDtos = answerRows != null ? answerRows.Select(q => new AnswerRowDto(q)).ToList(): null;
            Variations = variations;
            Link = link;
            Header = header;
            IsExpanded = isExpanded;
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



