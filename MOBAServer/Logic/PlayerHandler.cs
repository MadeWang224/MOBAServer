using MobaCommon.Code;
using Photon.SocketServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOBAServer.Cache;
using MobaCommon.Dto;
using MOBAServer.Model;
using LitJson;

namespace MOBAServer.Logic
{
    public class PlayerHandler : SingeSend, IOpHandler
    {
        public Action<List<int>, List<int>> StartSelectEvent;

        /// <summary>
        /// 账号缓存
        /// </summary>
        private AccountCache accountCache = Caches.Account;

        /// <summary>
        /// 角色缓存
        /// </summary>
        private PlayerCache playerCache = Caches.Player;

        /// <summary>
        /// 匹配缓存
        /// </summary>
        private MatchCache matchCache = Caches.Match;

        public void OnDisconnect(MobaClient client)
        {
            #region 每次下线时,通知好友,显示离线状态
            PlayerModel model = playerCache.GetModel(client);
            if (model != null)
            {
                MobaClient tempClient = null;
                string[] friends = model.FriendIdList.Split(',');
                foreach (string item in friends)
                {
                    if (string.IsNullOrEmpty(item))
                        continue;
                    int id = int.Parse(item);
                    if (!playerCache.IsOnline(id))
                        continue;
                    tempClient = playerCache.GetClient(id);
                    Send(tempClient, OpCode.PlayerCode, OpPlayer.FriendOffline, 0, "此玩家下线", model.Id);
                }
            } 
            #endregion

            matchCache.Offline(client, playerCache.GetId(client));
            playerCache.Offline(client);
        }

        public void OnRequest(MobaClient client, byte subCode, OperationRequest request)
        {
            switch (subCode)
            {
                case OpPlayer.GetInfo:
                    onGetInfo(client);
                    break;
                case OpPlayer.Create:
                    string name = request[0].ToString();
                    onCreate(client, name);
                    break;
                case OpPlayer.Online:
                    onOnline(client);
                    break;
                case OpPlayer.RequestAdd:
                    string addName = request[0].ToString();
                    onAdd(client, addName);
                    break;
                case OpPlayer.ToClientAdd:
                    onToClientAdd(client,(bool)request[0],(int)request[1]);
                    break;
                case OpPlayer.StartMatch:
                    onStartMatch(client, (int)request[0]);
                    break;
                case OpPlayer.StopMatch:
                    onStopMatch(client);
                    break;
            }
        }

        /// <summary>
        /// 离开匹配
        /// </summary>
        /// <param name="client"></param>
        private void onStopMatch(MobaClient client)
        {
            if(matchCache.StopMatch(client, playerCache.GetId(client)))
            {
                Send(client, OpCode.PlayerCode, OpPlayer.StopMatch, 0, "离开成功");
            }

        }

        /// <summary>
        /// 开始匹配
        /// </summary>
        /// <param name="client"></param>
        /// <param name="playerId"></param>
        private void onStartMatch(MobaClient client,int playerId)
        {
            //非法操作
            if (playerCache.GetId(client) != playerId)
                return;
            //获取匹配结果
            Room.MatchRoom room = matchCache.StartMatch(client, playerId);
            Send(client, OpCode.PlayerCode, OpPlayer.StartMatch, 0, "开始匹配");
            //如果房间满了,开始选人
            if (room.IsFull)
            {
                //开始选人
                StartSelectEvent(room.Team1IdList, room.Team2IdList);
                //发起是否进入选人界面请求
                room.Brocast(OpCode.PlayerCode, OpPlayer.MatchComplete, 0, "是否进入选人界面(10s内)",null);
                //摧毁房间
                matchCache.DestroyRoom(room);
            }
        }

        /// <summary>
        /// 添加结果
        /// </summary>
        /// <param name="result"></param>
        private void onToClientAdd(MobaClient client,bool result,int requestId)
        {
            MobaClient requestClient = playerCache.GetClient(requestId);
            if (result==true)
            {
                int playerId = playerCache.GetId(client);
                //同意, 双向保存数据
                playerCache.AddFriend(playerId, requestId);
                Send(client, OpCode.PlayerCode, OpPlayer.ToClientAdd, 1, " 添加成功",JsonMapper.ToJson(toDto(playerCache.GetModel(playerId))));
                Send(requestClient, OpCode.PlayerCode, OpPlayer.ToClientAdd, 1, " 添加成功", JsonMapper.ToJson(toDto(playerCache.GetModel(requestId))));
                return;
            }
            //拒绝了,回传给原来的客户端,不同意
            Send(requestClient, OpCode.PlayerCode, OpPlayer.ToClientAdd, -1, "此玩家拒绝你的请求");
        }

        /// <summary>
        /// 添加好友的处理
        /// </summary>
        /// <param name="addName"></param>
        private void onAdd(MobaClient client,string addName)
        {
            //获取添加好友的数据模型
            PlayerModel toModel = playerCache.GetModel(addName);
            if(toModel==null)
            {
                Send(client, OpCode.PlayerCode, OpPlayer.RequestAdd, -1, "没有此角色");
                return;
            }
            //如果添加的是自身,那么不能添加
            if(playerCache.GetModel(client).Id==toModel.Id)
            {
                Send(client, OpCode.PlayerCode, OpPlayer.RequestAdd, -3, "不能添加自身");
                return;
            }

            //如果已经是好友了,也不可以添加
            string[] friends = playerCache.GetModel(client).FriendIdList.Split(',');
            foreach (var item in friends)
            {
                if (string.IsNullOrEmpty(item))
                    continue;
                if (int.Parse(item) == toModel.Id)
                {
                    Send(client, OpCode.PlayerCode, OpPlayer.RequestAdd, -4, "该玩家已经是好友了");
                    return;
                }
            }
            
            //如果能获取数据模型,先判断他是否在线
            bool isOnline = playerCache.IsOnline(toModel.Id);
            //不在线,回传不在线
            if(!isOnline)
            {
                Send(client, OpCode.PlayerCode, OpPlayer.RequestAdd, -2, "此玩家不在线");
                return;
            }
            //在线 给模型对应的客户端发信息:有人向他加好友,同不同意
            MobaClient toClient = playerCache.GetClient(toModel.Id);
            //获取当前玩家的数据模型
            PlayerModel model = playerCache.GetModel(client);
            Send(toClient, OpCode.PlayerCode, OpPlayer.ToClientAdd, 0, "是否添加好友",JsonMapper.ToJson(toDto(model)));

        }

        /// <summary>
        /// 上线处理
        /// </summary>
        private void onOnline(MobaClient client)
        {
            int accId = accountCache.GetId(client);
            int playerId = playerCache.GetId(accId);
            if (playerCache.IsOnline(client))
                return;
            //上线
            playerCache.Online(client, playerId);

            #region 每次上线时,通知好友,显示在线状态
            PlayerModel model = playerCache.GetModel(client);
            if (model != null)
            {
                MobaClient tempClient = null;
                string[] friends = model.FriendIdList.Split(',');
                foreach (string item in friends)
                {
                    if (string.IsNullOrEmpty(item))
                        continue;
                    int id = int.Parse(item);
                    if (!playerCache.IsOnline(id))
                        continue;//直接进行下一次循环
                    tempClient = playerCache.GetClient(id);
                    Send(tempClient, OpCode.PlayerCode, OpPlayer.FriendOnline, 0, "此玩家上线", model.Id);
                }
            } 
            #endregion


            PlayerDto dto = toDto(playerCache.GetModel(playerId));
            
            //发送
            Send(client, OpCode.PlayerCode, OpPlayer.Online, 0, "上线成功", JsonMapper.ToJson(dto));
        }

        /// <summary>
        /// 将model转换成dto数据
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private PlayerDto toDto(PlayerModel model)
        {
            PlayerDto dto = new PlayerDto()
            {
                id = model.Id,
                name = model.Name,
                lv = model.Lv,
                exp = model.Exp,
                power = model.Power,
                winCount = model.WinCount,
                loseCount = model.LoseCount,
                runCount = model.RunCount,

                heroIds=new int[model.HeroIdList.Length],
                friends=new Friend[model.FriendIdList.Length],
            };

            //赋值英雄ID列表
            string[] heros = model.HeroIdList.Split(',');
            dto.heroIds = new int[heros.Length];
            for (int i = 0; i < heros.Length; i++)
            {
                dto.heroIds[i] = int.Parse(heros[i]);
            }

            //赋值好友列表
            string[] friends = model.FriendIdList.Split(',');
            dto.friends = new Friend[friends.Length];
            for (int i = 0; i < friends.Length; i++)
            {
                if (string.IsNullOrEmpty(friends[i]))
                    continue;
                int id = int.Parse(friends[i]);
                string name = playerCache.GetModel(id).Name;
                bool online = playerCache.IsOnline(id);
                dto.friends[i] = new Friend(id, name, online);
            }
            return dto;
        }

        /// <summary>
        /// 创建角色
        /// </summary>
        /// <param name="client"></param>
        /// <param name="name"></param>
        private void onCreate(MobaClient client,string name)
        {
            int accId = accountCache.GetId(client);
            if (playerCache.Has(accId))
                return;
            //验证时候开始创建
            playerCache.Create(name, accId);
            Send(client, OpCode.PlayerCode, OpPlayer.Create, 0, "创建成功",accId);
        }

        /// <summary>
        /// 获取角色信息
        /// </summary>
        private void onGetInfo(MobaClient client)
        {
            int accId = accountCache.GetId(client);
            if(accId==-1)
            {
                Send(client, OpCode.PlayerCode, OpPlayer.GetInfo, -1, "非法登录");
                return;
            }
            if(playerCache.Has(accId))
            {
                Send(client, OpCode.PlayerCode, OpPlayer.GetInfo, 0, "存在角色");
                return;
            }
            else
            {
                Send(client, OpCode.PlayerCode, OpPlayer.GetInfo, -2, "没有角色");
                return;
            }
        }
    }
}
