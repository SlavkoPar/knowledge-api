using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using KnowledgeAPI.A.Answers.Model;
using Newtonsoft.Json;
using System.Text.Json;

namespace KnowledgeAPI.A.Groups.Model
{
    public class GroupKey
    {
        public string Workspace {  get; set; }
        public string TopId { get; set; }
        public string Id { get; set; }

        public string? ParentId { get; set; }


        public GroupKey()
        {
        }

        public GroupKey(GroupKey groupKey)
        {
            var (workspace, topId, _, id, parentId) = groupKey;
            Workspace = workspace;
            TopId = topId;
            Id = id;
            ParentId = parentId;
        }

        public GroupKey(string workspace, string topId, string id, string? parentId=null)
        {
            Workspace = workspace;
            TopId = topId;
            Id = id;
            ParentId = parentId;
        }

        public GroupKey(Group group)
        {
            Workspace = group.Workspace;
            TopId = group.TopId;
            Id = group.Id;
            ParentId = group.ParentId;
        }


        public GroupKey(GroupRowDto rowDto)
        {
            Workspace = rowDto.Workspace;
            TopId = rowDto.TopId;
            Id = rowDto.Id;
            ParentId = rowDto.ParentId;
        }

        public GroupKey(AnswerDto rowDto)
        {
            Workspace = rowDto.Workspace;
            TopId = rowDto.TopId;
            Id = rowDto.ParentId;
            ParentId = null;
        }

        public GroupKey(Answer answer)
        {
            Workspace = answer.Workspace;
            TopId = answer.TopId;
            Id = answer.ParentId!;
            ParentId = null;
        }

        public GroupKey(GroupDto groupDto)
        {
            Workspace = groupDto.Workspace;
            TopId = groupDto.TopId;
            Id = groupDto.Id;
            ParentId = groupDto.ParentId;
        }

       

        protected string PartitionKey {    
            get { 
                return Workspace + "/" + TopId;
            } 
        }

        public void Deconstruct(out string workspace, out string topId, out string partitionKey, out string id, out string? parentId)
        {
            workspace = Workspace;
            topId = TopId;
            partitionKey = Workspace + "/" + TopId;
            id = Id;
            parentId = ParentId;
        }

       
    }
 
}
