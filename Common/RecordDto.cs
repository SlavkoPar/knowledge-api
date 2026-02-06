
using System.Text.Json.Serialization;

namespace KnowledgeAPI.Common
{
    public class RecordDto
    {
        public string Workspace { get; set; }
        public string TopId { get; set; }  // Top Category/Group Id
        public string? ParentId { get; set; }  // Top Category/Group Id

        public WhoWhenDto? Created { get; set; }
        public WhoWhenDto? Modified { get; set; }

        [JsonIgnore]
        public string PartitionKey
        {
            get
            {
                return Workspace + "/" + TopId;
            }
        }

        public RecordDto()
        {
        }

        public RecordDto(WhoWhenDto? Created, WhoWhenDto? Modified) //, WhoWhenDto? Archived)
        {
            this.Created = Created;
            this.Modified = Modified;
            //this.Archived = Archived;
        }


        public RecordDto(WhoWhen Created, WhoWhen Modified) //, WhoWhen Archived)
        {
            if (Created != null)
                this.Created = new WhoWhenDto(Created);
            if (Modified != null)
                this.Modified = new WhoWhenDto(Modified);
            //if (Archived != null)
            //    this.Archived = new WhoWhenDto(Archived);
        }


    }
}