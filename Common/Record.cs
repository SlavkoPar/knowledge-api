using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace KnowledgeAPI.Common
{
    
    public class Record 
    {
        public string Type { get; set; }
        public string Workspace { get; set; }
        public string TopId { get; set; }  // Top Category/Group Id

        public WhoWhen? Created { get; set; }
        public WhoWhen? Modified { get; set; }

        public Record(WhoWhen? created, WhoWhen? modified)
        {
            Created = created;
            Modified = modified;
        }

        public Record(WhoWhenDto created, WhoWhenDto modified)
        {
            Created = created != null ? new WhoWhen(created) : null;
            Modified = modified != null ? new WhoWhen(modified) : null;
        }

        public Record()
        {
        }

    }
}
