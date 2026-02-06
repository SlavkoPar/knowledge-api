using Microsoft.AspNetCore.OutputCaching;
using KnowledgeAPI.Common;
using KnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System;

namespace KnowledgeAPI.Q.Categories.Model
{
    public class CategoryRowDto: IDisposable
    {
        public string Workspace { get; set; }
        public string TopId { get; set; }  // Top Category Id

        public string Id { get; set; }
        public string? ParentId { get; set; }

        public string Title { get; set; }

        public int Kind { get; set; }
        public int Level { get; set; }
        public bool HasSubCategories { get; set; }
        public List<CategoryRowDto>? RowDtos { get; set; }
        public int NumOfQuestions { get; set; }
        public List<QuestionRowDto>? QuestionRowDtos { get; set; }

        public string? Link { get; set; }
        public string Header { get; set; }
        public List<string>? Variations { get; set; }

        public bool? IsExpanded { get; set; }
        public WhoWhenDto? Modified { get; set; }

        public CategoryRowDto()
        {
        }

        public CategoryRowDto(CategoryRow categoryRow)
        {
            var(_, topId, id, parentId, title, link, header, level, kind, 
                hasSubCategories, subCategories,
                hasMoreQuestions, numOfQuestions, questionRows,
                variations,
                isExpanded) = categoryRow;
            Workspace = null;
            TopId = topId;
            Id = id;
            Title = title;
            Kind = kind;
            ParentId = parentId;
            Level = level;
            HasSubCategories = hasSubCategories;
            RowDtos = subCategories != null ? subCategories.Select(c => new CategoryRowDto(c)).ToList() : null;
            NumOfQuestions = numOfQuestions;
            QuestionRowDtos = questionRows != null ? questionRows.Select(q => new QuestionRowDto(q)).ToList() : null;
            Variations = variations; // ?? [];
            Link = link;
            Header = header;
            IsExpanded = isExpanded;
        }

        private CategoryRow var()
        {
            throw new NotImplementedException();
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



