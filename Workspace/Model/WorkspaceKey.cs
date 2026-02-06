using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using KnowledgeAPI.A.Answers.Model;
using Newtonsoft.Json;
using System.Text.Json;

namespace KnowledgeAPI.A.Workspaces.Model
{
    public class WorkspaceKey
    {
        public string Email { get; set; }


        public WorkspaceKey()
        {
        }

        public WorkspaceKey(WorkspaceKey workspaceKey)
        {
            Email = workspaceKey.Email;
        }

        public WorkspaceKey(string email)
        {
            Email = email;
        }


        protected string PartitionKey {    
            get { 
                return "workspace";
            } 
        }

        public void Deconstruct(out string email, out string partitionKey)
        {
            email = Email;
            partitionKey = PartitionKey;
        }

       
    }
 
}
