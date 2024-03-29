﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobaCommon.Code
{
    /// <summary>
    /// 操作码
    /// </summary>
    public class OpCode
    {
        /// <summary>
        /// 账号
        /// </summary>
        public const byte AccountCode = 0;

        /// <summary>
        /// 玩家
        /// </summary>
        public const byte PlayerCode = 1;

        /// <summary>
        /// 选人
        /// </summary>
        public const byte SelectCode = 2;

        /// <summary>
        /// 战斗
        /// </summary>
        public const byte FightCode = 3;
    }
}
