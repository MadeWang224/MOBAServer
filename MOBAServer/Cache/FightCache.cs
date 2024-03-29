﻿using MobaCommon.Dto;
using MOBAServer.Room;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOBAServer.Cache
{
    public class FightCache:RoomCacheBase<FightRoom>
    {
        private int id = 0;
        /// <summary>
        /// 创建战斗房间
        /// </summary>
        /// <param name="team1"></param>
        /// <param name="team2"></param>
        public void CreatRoom(List<SelectModel> team1, List<SelectModel> team2)
        {
            FightRoom room = null;
            //检测有没有重用的房间
            if (!roomQue.TryDequeue(out room))
                room = new FightRoom(id++, team1.Count + team2.Count);
            //初始化房间数据
            room.Init(team1, team2);
            //添加映射关系
            foreach (SelectModel item in team1)
            {
                playerRoomDict.TryAdd(item.playerId, room.Id);
            }
            foreach (SelectModel item in team2)
            {
                playerRoomDict.TryAdd(item.playerId, room.Id);
            }
            idRoomDict.TryAdd(room.Id, room);
        }

        /// <summary>
        ///  进入战斗
        /// </summary>
        /// <returns></returns>
        public FightRoom Enter(int playerId,MobaClient client)
        {
            FightRoom room = this.GetRoom(playerId);

            room.Enter(client);
            return room;
        }

        /// <summary>
        /// 玩家下线处理
        /// </summary>
        /// <param name="client">客户端</param>
        /// <param name="playerId">玩家ID</param>
        public void Offline(MobaClient client,int playerId)
        {
            //最好先验证一下有没有当前玩家
            int roomId = -1;
            if (!playerRoomDict.TryGetValue(playerId, out roomId))
                return;
            FightRoom room = null;
            if (!idRoomDict.TryGetValue(roomId, out room))
                return;
            //离开方法
            room.Leave(client);
            //判断房间内有没有人
            if (!room.IsAllLeave)
                return;
            //销毁房间
            Destroy(room.Id);

        }

        /// <summary>
        /// 获取房间
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public FightRoom GetRoom(int playerId)
        {
            int roomId = -1;
            if (!playerRoomDict.TryGetValue(playerId, out roomId))
                return null;
            FightRoom room = null;
            if (!idRoomDict.TryGetValue(roomId, out room))
                return null;

            return room;
        }

        /// <summary>
        /// 销毁房间
        /// </summary>
        /// <param name="roomId"></param>
        public void Destroy(int roomId)
        {
            FightRoom room = null;
            if (!idRoomDict.TryRemove(roomId, out room))
                return;

            //移除玩家ID和房间ID的关系
            foreach (HeroModel item in room.Heros)
            {
                playerRoomDict.TryRemove(item.Id, out roomId);
            }
            //清空房间内的数据
            room.Clear();
            //入队列
            roomQue.Enqueue(room);
        }
    }
}
