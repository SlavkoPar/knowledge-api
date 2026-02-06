namespace KnowledgeAPI.A.Groups.Model
{
 
    public class GroupRowEx

    {
        public GroupRowEx(GroupRow? row, string msg)
        {
            groupRow = row;
            message = msg;
        }

        public GroupRowEx(Group? group, string msg)
        {
            groupRow = group != null ? new GroupRow(group) : null;
            message = msg;
        }


        public void Deconstruct(out GroupRow? groupRow, out string msg)
        {
            groupRow = this.groupRow;
            msg = this.message;
        }

        public GroupRow? groupRow { get; set; }
        public string message { get; set; }    
    }
}
