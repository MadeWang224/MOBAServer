using MobaCommon.Code;
using MobaCommon.Dto;
using MOBAServer.Cache;
using MOBAServer.Room;
using Photon.SocketServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LitJson;
using MOBAServer.Model;

namespace MOBAServer.Logic
{
    public class SelectHandler :SingeSend, IOpHandler
    {
        //开始战斗的事件
        public Action<List<SelectModel>, List<SelectModel>> StartFightEvent;

        /// <summary>
        /// 选人缓存
        /// </summary>
        private SelectCache selectCache = Caches.Select;

        /// <summary>
        /// 角色缓存
        /// </summary>
        private PlayerCache playerCache = Caches.Player;

        /// <summary>
        /// 开始选人
        /// </summary>
        public void StartSelect(List<int> team1,List<int> team2)
        {
            //创建一个房间
            selectCache.CreateRoom(team1, team2);

        }

        public void OnDisconnect(MobaClient client)
        {
            selectCache.Offline(client, playerCache.GetId(client));
        }

        public void OnRequest(MobaClient client, byte subCode, OperationRequest request)
        {
            switch (subCode)
            {
                case OpSelect.Enter:
                    onEnter(client);
                    break;
                case OpSelect.Select:
                    onSelect(client, (int)request[0]);
                    break;
                case OpSelect.Ready:
                    onReady(client);
                    break;
                case OpSelect.Talk:
                    onTalk(client, request[0].ToString());
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 聊天
        /// </summary>
        /// <param name="client"></param>
        /// <param name="text"></param>
        private void onTalk(MobaClient client, string text)
        {
            //给当前客户端所在的房间内的所有人发一条消息
            PlayerModel player = playerCache.GetModel(client);
            if (player == null)
                return;
            SelectRoom room = selectCache.GetRoom(player.Id);
            string str = string.Format("{0}:{1}", player.Name, text);
            room.Brocast(OpCode.SelectCode, OpSelect.Talk, 0, "有玩家发言", null, str);
        }

        /// <summary>
        /// 玩家确认选择
        /// </summary>
        /// <param name="client"></param>
        private void onReady(MobaClient client)
        {
            int playerId = playerCache.GetId(client);
            SelectRoom room = selectCache.Ready(playerId);
            if(room==null)
            {
                //确认失败
                this.Send(client, OpCode.SelectCode, OpSelect.Ready, -1, "确认失败");
                return;
            }
            //告诉房间所有人,已确认选择
            room.Brocast(OpCode.SelectCode, OpSelect.Ready, 0, "有人确认选择", null, playerId);
            //是否全部准备.如果全部准备了,就开始战斗
            if(room.IsAllReady)
            {
                //开始战斗事件
                this.StartFightEvent(room.team1Dict.Values.ToList(), room.team2Dict.Values.ToList());
                //给客户端发送消息
                room.Brocast(OpCode.SelectCode, OpSelect.StartFight, 0, "准备进入战斗场景", null);
                //销毁房间
                selectCache.Destroy(room.Id);
            }
        }

        /// <summary>
        /// 玩家选择英雄
        /// </summary>
        /// <param name="client"></param>
        /// <param name="heroId"></param>
        private void onSelect(MobaClient client,int heroId)
        {
            //给房间里面的所有人发一条消息:谁选了谁(playerId,heroId)
            int playerId = playerCache.GetId(client);
            SelectRoom room = selectCache.Select(playerId, heroId);
            if(room==null)
            {
                //选择失败
                this.Send(client, OpCode.SelectCode, OpSelect.Select, -1, "选择失败");
                return;
            }
            //给房间里面所有人发送一条消息
            room.Brocast(OpCode.SelectCode, OpSelect.Select, 0, "有人选择英雄了", null, playerId, heroId);
        }

        /// <summary>
        /// 进入选人
        /// </summary>
        /// <param name="client"></param>
        private void onEnter(MobaClient client)
        {
            SelectRoom room = selectCache.Enter(playerCache.GetId(client), client);
            if (room == null)
                return;
            //给此客户端发一个房间模型
            this.Send(client, OpCode.SelectCode, OpSelect.GetInfo, 0, "获取房间模型",
                JsonMapper.ToJson(room.team1Dict.Values.ToArray()), JsonMapper.ToJson(room.team2Dict.Values.ToArray()));
            //再给房间内的客户端发一条消息:有人进入房间
            room.Brocast(OpCode.SelectCode, OpSelect.Enter, 0, "有玩家进入", client, playerCache.GetId(client));

        }

    }
}
