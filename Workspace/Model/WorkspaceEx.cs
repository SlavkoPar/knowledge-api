namespace KnowledgeAPI.A.Workspaces.Model
{
 
    public class WorkspaceEx
    {
        public WorkspaceEx(Workspace? workspace, string msg)
        {
            this.workspace = workspace;
            this.msg = msg;
        }

        public void Deconstruct(out Workspace? workspace, out string msg)
        {
            workspace = this.workspace;
            msg = this.msg;
        }

        public Workspace? workspace { get; set; }
        public string msg { get; set; }    
    }
}
