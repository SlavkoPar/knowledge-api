using KnowledgeAPI.Common;
using Newtonsoft.Json;

namespace KnowledgeAPI.Hist.Model
{

    public class HistoryDtoListEx
    {
        public HistoryDtoListEx(List<HistoryDto> historyDtoList, string msg)
        {
            this.historyDtoList = historyDtoList;
            this.msg = msg;
        }

        public List<HistoryDto> historyDtoList { get; set; }
        public string msg { get; set; }

    }
}
