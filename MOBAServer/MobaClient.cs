﻿using Photon.SocketServer;
using PhotonHostRuntimeInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOBAServer.Logic;
using MobaCommon.Code;

namespace MOBAServer
{
    public class MobaClient : ClientPeer
    {
        //账号逻辑
        IOpHandler account;
        PlayerHandler player;
        SelectHandler select;
        FightHandler fight;
        public MobaClient(InitRequest initRequest) : base(initRequest)
        {
            account = new AccountHandler();
            player = new PlayerHandler();
            select = new SelectHandler();
            fight = new FightHandler();

            player.StartSelectEvent = select.StartSelect;
            select.StartFightEvent = fight.StartFight;
        }
        /// <summary>
        /// 客户端断开连接
        /// </summary>
        /// <param name="reasonCode"></param>
        /// <param name="reasonDetail"></param>
        protected override void OnDisconnect(DisconnectReason reasonCode, string reasonDetail)
        {
            fight.OnDisconnect(this);
            select.OnDisconnect(this);
            player.OnDisconnect(this);
            account.OnDisconnect(this);
        }
        /// <summary>
        /// 客户端请求
        /// </summary>
        /// <param name="operationRequest"></param>
        /// <param name="sendParameters"></param>
        protected override void OnOperationRequest(OperationRequest request, SendParameters sendParameters)
        {
            byte opCode = request.OperationCode;
            byte subCode = (byte)request[80];
            switch(opCode)
            {
                case OpCode.AccountCode:
                    account.OnRequest(this, subCode, request);
                    break;
                case OpCode.PlayerCode:
                    player.OnRequest(this, subCode, request);
                    break;
                case OpCode.SelectCode:
                    select.OnRequest(this, subCode, request);
                    break;
                case OpCode.FightCode:
                    fight.OnRequest(this, subCode, request);
                    break;
            }
        }
    }
}
