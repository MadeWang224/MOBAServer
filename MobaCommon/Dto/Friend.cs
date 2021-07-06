using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobaCommon.Dto
{
    /// <summary>
    /// 好友信息
    /// </summary>
    public class Friend
    {
        public int Id;
        public string Name;
        public bool IsOnline;

        public Friend()
        {

        }
        public Friend(int id,string name,bool online)
        {
            this.Id = id;
            this.Name = name;
            this.IsOnline = online;
        }
    }
}
