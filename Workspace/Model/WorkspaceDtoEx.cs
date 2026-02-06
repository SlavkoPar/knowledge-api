using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using KnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Net;

namespace KnowledgeAPI.A.Workspaces.Model
{
       public class WorkspaceDtoEx
    {
        //public WorkspaceDtoEx(WorkspaceDto? workspaceDto, string msg)
        //{
        //    this.workspaceDto = workspaceDto;
        //    this.msg = msg;
        //}
        public WorkspaceDtoEx(WorkspaceEx workspaceEx)
        {
            workspaceDto = workspaceEx.workspace != null ? new WorkspaceDto(workspaceEx.workspace!) : null;
            msg = workspaceEx.msg!;
        }


        public WorkspaceDtoEx(WorkspaceDto workspaceDto, string msg)
        {
            this.workspaceDto = workspaceDto;
            this.msg = msg;
        }

        public WorkspaceDtoEx(string msg)
        {
            workspaceDto = null;
            this.msg = msg;
        }



        public WorkspaceDto? workspaceDto { get; set; }
        public string msg { get; set; }
    }

}



