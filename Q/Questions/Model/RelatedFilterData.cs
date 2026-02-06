using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using KnowledgeAPI.A.Answers.Model;
using KnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Net;

namespace KnowledgeAPI.Q.Questions.Model
{
    public class RelatedFilterData
    {
        public string Filter { get; set; }

        public RelatedFilterData()
        {
        }
    }
}
