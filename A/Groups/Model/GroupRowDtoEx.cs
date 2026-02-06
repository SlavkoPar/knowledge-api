using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using KnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Net;

namespace KnowledgeAPI.A.Groups.Model
{
       public class GroupRowDtoEx
    {
        //public GroupDtoEx(GroupDto? groupDto, string msg)
        //{
        //    this.groupDto = groupDto;
        //    this.msg = msg;
        //}
        public GroupRowDtoEx(GroupRowEx groupRowEx)
        {

            groupRowDto = groupRowEx.groupRow != null ? new GroupRowDto(groupRowEx.groupRow!) : null;
            msg = groupRowEx.message!;
        }


        public GroupRowDtoEx(GroupRowDto groupRowDto, string msg)
        {
            this.groupRowDto = groupRowDto;
            this.msg = msg;
        }

        public GroupRowDtoEx(string msg)
        {
            groupRowDto = null;
            this.msg = msg;
        }


        public GroupRowDto? groupRowDto { get; set; }
        public string msg { get; set; }
    }

}



