using KnowledgeAPI.Common;
using Newtonsoft.Json;

namespace KnowledgeAPI.A.Workspaces.Model
{
    public class WorkspaceDto : RecordDto
    {
        public string? TenantId { get; set; }

        public string? Environment { get; set; }
        public string? DisplayName { get; set; }
        public string? Email { get; set; }

        public WorkspaceDto()
            : base()
        {
        }


        public WorkspaceDto(Workspace workspace)
        : base(workspace.Created, workspace.Modified)
        {
            Workspace = workspace.Id;
        }
    }
}



