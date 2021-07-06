using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobaCommon.Dto
{
    /// <summary>
    /// 玩家信息传输模型
    /// </summary>
    public class PlayerDto
    {
        public int id;
        public string name;
        public int lv;
        public int exp;
        public int power;
        public int winCount;
        public int loseCount;
        public int runCount;
        public int[] heroIds;
        public Friend[] friends;

        public PlayerDto()
        {

        }
    }
}
