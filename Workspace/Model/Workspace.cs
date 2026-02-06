using Azure.Search.Documents.Indexes;
using KnowledgeAPI.Common;
using Newtonsoft.Json;

namespace KnowledgeAPI.A.Workspaces.Model
{
    public class Workspace : Record, IDisposable
    {
        [SearchableField()]
        [JsonProperty(PropertyName = "partitionKey")]
        public string PartitionKey { get; set; }

        public string Environment { get; set; }

        [JsonProperty(PropertyName = "TenantId")]
        public string TenantId { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

                
        [SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public string DisplayName { get; set; }

        public Workspace()
            : base()
        {
            Type = "workspace";
            TopId = "";
        }


        public Workspace(WorkspaceDto workspaceDto)
            :base(workspaceDto.Created!, workspaceDto.Modified!)
        {
            PartitionKey = "workspace";
            Type = "workspace";
            TopId = "";
            Id = workspaceDto.Email!; //Guid.NewGuid().ToString();
            Workspace = workspaceDto.Email!;
            Environment = workspaceDto.Environment!;
            DisplayName = workspaceDto.DisplayName!;
            TenantId = workspaceDto.TenantId!;
        }

        //public Workspace(Workspace workspace)
        //   : base(workspace.Created, workspace.Modified, null)
        //{
        //    return JsonConvert.DeserializeObject(JsonConvert.SerializeObject(workspace));
        //}

        //public override string ToString() =>
        //    $"{PartitionKey}/{Id} : {Title}";


        public void Deconstruct(
            out string partitionKey,
            out string id, 
            out string displayName)
        {
            partitionKey = PartitionKey;
            id = Id;
            displayName = DisplayName;
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



