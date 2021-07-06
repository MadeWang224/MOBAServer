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
using MobaCommon.Dto.Skill;
using MobaCommon.Config;
using MOBAServer.Model;

namespace MOBAServer.Logic
{
    public class FightHandler : SingeSend,IOpHandler
    {
        #region 缓存层

        public FightCache fightCache
        {
            get { return Caches.Fight; }
        }

        public PlayerCache playerCache
        {
            get { return Caches.Player; }
        }

        #endregion
        /// <summary>
        /// 开始战斗
        /// </summary>
        /// <param name="team1"></param>
        /// <param name="team2"></param>
        public void StartFight(List<SelectModel> team1,List<SelectModel> team2)
        {
            fightCache.CreatRoom(team1, team2);
        }

        public void OnDisconnect(MobaClient client)
        {
            fightCache.Offline(client, playerCache.GetId(client));
        }

        public void OnRequest(MobaClient client, byte subCode, OperationRequest request)
        {
            switch (subCode)
            {
                case OpFight.Enter:
                    onEnter(client, (int)request[0]);
                    break;
                case OpFight.Walk:
                    onWalk(client, (float)request[0], (float)request[1], (float)request[2]);
                    break;
                case OpFight.Skill:
                    onSkill(client, (int)request[0], (int)request[1], (int)request[2],(int)request[3], (int)request[4], (int)request[5]);
                    break;
                case OpFight.Damage:
                    onDamage(client,(int)request[0],(int)request[1],(int[])request[2]);
                    break;
                case OpFight.Buy:
                    onBuy(client, (int)request[0]);
                    break;
                case OpFight.Sale:
                    onSale(client, (int)request[0]);
                    break;
                case OpFight.SkillUp:
                    onSkillUp(client, (int)request[0]);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 技能升级
        /// </summary>
        /// <param name="client"></param>
        /// <param name="skillId"></param>
        private void onSkillUp(MobaClient client, int skillId)
        {
            //获取房间模型
            int playerId = playerCache.GetId(client);
            FightRoom room = fightCache.GetRoom(playerId);
            if (room == null)
                return;
            //获取英雄数据模型
            HeroModel hero = room.GetHeroModel(playerId);
            if (hero == null)
                return;
            //没有技能点
            if (hero.Points <= 0)
                return;
            //加点
            foreach (var item in hero.Skills)
            {
                //找到技能
                if (item.Id != skillId)
                    continue;
                //等级不够或等级已满
                if (item.LearnLevel > hero.Level||item.LearnLevel==-1)
                    return;
                //扣点数
                hero.Points--;
                //先获取技能下一级的数据
                SkillLevelDataModel data = SkillData.GetSkillData(skillId).LvModels[++item.Level];
                //修改技能
                item.LearnLevel = data.LearnLv;
                item.Distance = data.Distance;
                item.CoolDown = data.CoolDown;
                //广播,谁更新了什么技能
                room.Brocast(OpCode.FightCode, OpFight.SkillUp, 0, "有人点了技能", null, playerId, JsonMapper.ToJson(item));
                break;
            }
        }

        /// <summary>
        /// 卖装备
        /// </summary>
        /// <param name="client"></param>
        /// <param name="itemId"></param>
        private void onSale(MobaClient client, int itemId)
        {
            //检测装备是否存在
            ItemModel item = ItemData.GetItem(itemId);
            if (item == null)
                return;
            //获取房间模型
            int playerId = playerCache.GetId(client);
            FightRoom room = fightCache.GetRoom(playerId);
            if (room == null)
                return;
            //获取英雄数据模型
            HeroModel hero = room.GetHeroModel(playerId);
            for (int i = 0; i < hero.Equipments.Length; i++)
            {
                if (hero.Equipments[i] ==itemId)
                {
                    //开始出售
                    hero.Money += item.Price;
                    //赋值
                    hero.Equipments[i] = -1;
                    //增加属性
                    hero.Attack -= item.Attack;
                    hero.Defense -= item.Defense;
                    hero.MaxHp -= item.Hp;
                    //给房间内所有客户端发消息
                    room.Brocast(OpCode.FightCode, OpFight.Sale, 0, "有人卖出装备了", null, JsonMapper.ToJson(hero));
                    return;
                }
            }
            Send(client, OpCode.FightCode, OpFight.Sale, -1, "出售失败");
            return;
        }

        /// <summary>
        /// 买装备
        /// </summary>
        /// <param name="client"></param>
        /// <param name="v"></param>
        private void onBuy(MobaClient client, int itemId)
        {
            //检测装备是否存在
            ItemModel item = ItemData.GetItem(itemId);
            if (item == null)
                return;
            //获取房间模型
            int playerId = playerCache.GetId(client);
            FightRoom room = fightCache.GetRoom(playerId);
            if (room == null)
                return;
            //获取英雄数据模型
            HeroModel hero = room.GetHeroModel(playerId);
            //检测钱够不够
            if(hero.Money<item.Price)
            {
                Send(client, OpCode.FightCode, OpFight.Buy, -1, "金币不足");
                return;
            }
            //添加装备
            for (int i = 0; i < hero.Equipments.Length; i++)
            {
                //没有装备
                if(hero.Equipments[i]==-1)
                {
                    //开始购买
                    hero.Money -= item.Price;
                    //赋值
                    hero.Equipments[i] = item.Id;
                    //增加属性
                    hero.Attack += item.Attack;
                    hero.Defense += item.Defense;
                    hero.MaxHp += item.Hp;
                    //给房间内所有客户端发消息
                    room.Brocast(OpCode.FightCode, OpFight.Buy, 0, "有人购买装备了", null, JsonMapper.ToJson(hero));
                    return;
                }
            }
            Send(client, OpCode.FightCode, OpFight.Buy, -2, "装备已满");
            return;

        }

        /// <summary>
        /// 计算伤害
        /// </summary>
        private void onDamage(MobaClient client, int attackId, int skillId, int[] targetId)
        {
            //获取房间模型
            int playerId = playerCache.GetId(client);
            FightRoom room = fightCache.GetRoom(playerId);

            //判断是谁攻击 谁被攻击
            //攻击者的数据模型
            DogModel attackModel = null;
            if (attackId >= 0)
            {
                //攻击者的id大于0,英雄攻击
                attackModel = room.GetHeroModel(attackId);
            }
            else if (attackId <= -10 && attackId >= -30)
            {
                //塔攻击
                attackModel = room.GetBuildModel(attackId);
            }
            else if (attackId <= -1000)
            {
                //小兵攻击
            }
            //被攻击者的数据模型
            DogModel[] targetModels = new DogModel[targetId.Length];
            for (int i = 0; i < targetId.Length; i++)
            {
                if (targetId[i] >= 0)
                {
                    //英雄
                    targetModels[i] = room.GetHeroModel(targetId[i]);
                }
                else if (targetId[i] <= -10 && targetId[i] >= -30)
                {
                    //塔
                    targetModels[i] = room.GetBuildModel(targetId[i]);
                }
                else if (targetId[i] <= -1000)
                {
                    //小兵
                }
            }

            //根据技能id 判断是普通攻击还是技能
            //根据伤害表 根据技能id 获取ISkill,调用damage,计算伤害
            ISkill skill = null;
            List<DamageModel> damages = null;
            skill = DamageData.GetSkill(skillId);
            //计算伤害
            damages = skill.Damage(skillId,0, attackModel, targetModels);
            //给房间内客户端广播数据模型
            room.Brocast(OpCode.FightCode, OpFight.Damage, 0, "有伤害产生", null, JsonMapper.ToJson(damages.ToArray()));
            //结算
            foreach (DogModel item in targetModels)
            {
                if (item.CurrHp == 0)
                {
                    switch (item.ModelType)
                    {
                        case ModelType.DOG:
                            if (attackModel.Id >= 0)
                            {
                                //钱
                                ((HeroModel)attackModel).Money += 20;
                                //经验
                                ((HeroModel)attackModel).Exp += 10;
                                if(((HeroModel)attackModel).Exp> ((HeroModel)attackModel).Level*100)
                                {
                                    //等级
                                    ((HeroModel)attackModel).Level++;
                                    //技能点
                                    ((HeroModel)attackModel).Points++;
                                    ((HeroModel)attackModel).Exp = 0;

                                    HeroDataModel data = HeroData.GetHeroData(attackModel.Id);
                                    //属性成长
                                    ((HeroModel)attackModel).Attack += data.GrowAttack;
                                    ((HeroModel)attackModel).Defense += data.GrowDefense;
                                    ((HeroModel)attackModel).MaxHp += data.GrowHp;
                                    ((HeroModel)attackModel).MaxMp += data.GrowMp;
                                }
                                //给客户端发送信息attackModel
                                room.Brocast(OpCode.FightCode, OpFight.UpdateModel, 0, "更新数据模型", null, JsonMapper.ToJson((HeroModel)attackModel));

                            }
                            room.RemoveDog(item);
                            break;
                        case ModelType.BUILD:
                            if(attackModel.Id >= 0)
                            {
                                ((HeroModel)attackModel).Money += 150;
                                //给客户端发信息attackModel
                                room.Brocast(OpCode.FightCode, OpFight.UpdateModel, 0, "更新数据模型", null, JsonMapper.ToJson((HeroModel)attackModel));

                            }
                            //能否重生
                            if (((BuildModel)item).Rebirth)
                            {
                                //开启定时任务
                                room.StartSchedule(DateTime.UtcNow.AddSeconds((double)((BuildModel)item).RebirthTime), () =>
                                 {
                                     ((BuildModel)item).CurrHp = ((BuildModel)item).MaxHp;
                                     //给客户端发送一个复活的信息,item
                                     room.Brocast(OpCode.FightCode, OpFight.Resurge, 1, "有模型复活了", null, JsonMapper.ToJson((BuildModel)item));

                                 });
                            }
                            else
                            {
                                room.RemoveBuild((BuildModel)item);
                            }
                            if(item.Id==-10)
                            {
                                //队伍1输,队伍2赢
                                onGameOver(room, 2);
                            }
                            else if(item.Id==-20)
                            {
                                //队伍2输,队伍1赢
                                onGameOver(room, 1);
                            }
                            break;
                        case ModelType.HERO:
                            if (attackModel.Id >= 0)
                            {
                                //加杀人数
                                ((HeroModel)attackModel).Kill++;
                                //加钱
                                ((HeroModel)attackModel).Money += 300;
                                //加经验
                                ((HeroModel)attackModel).Exp += 50;
                                //检测是否升级
                                if (((HeroModel)attackModel).Exp > ((HeroModel)attackModel).Level * 100)
                                {
                                    //升级
                                    ((HeroModel)attackModel).Level++;
                                    //技能点数
                                    ((HeroModel)attackModel).Points++;
                                    //重置经验值
                                    ((HeroModel)attackModel).Exp = 0;

                                    HeroDataModel data = HeroData.GetHeroData(attackModel.Id);
                                    //英雄成长属性 增加
                                    ((HeroModel)attackModel).Attack += data.GrowAttack;
                                    ((HeroModel)attackModel).Defense += data.GrowDefense;
                                    ((HeroModel)attackModel).MaxHp += data.GrowHp;
                                    ((HeroModel)attackModel).MaxMp += data.GrowMp;
                                }
                                //给客户端发送 这个数据模型 attackModel 客户端更新就行了
                                room.Brocast(OpCode.FightCode, OpFight.UpdateModel, 0, "更新数据模型", null, JsonMapper.ToJson((HeroModel)attackModel));
                            }

                            //目标英雄死亡
                            //加死亡数
                            ((HeroModel)item).Dead++;
                            //开启一个定时任务 在指定的时间之后 复活
                            room.StartSchedule(DateTime.UtcNow.AddSeconds(((HeroModel)attackModel).Level * 5),
                                () =>
                                {
                                    //满状态
                                    ((HeroModel)item).CurrHp = ((HeroModel)item).MaxHp;
                                    //给客户端发送一个复活的消息 参数 item
                                    room.Brocast(OpCode.FightCode, OpFight.Resurge, 0, "有模型复活了", null, JsonMapper.ToJson((HeroModel)item));
                                });
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 游戏结束
        /// </summary>
        /// <param name="room"></param>
        /// <param name="winTeam"></param>
        private void onGameOver(FightRoom room, int winTeam)
        {
            //广播游戏结束的消息 参数：胜利的队伍
            room.Brocast(OpCode.FightCode, OpFight.GameOver, 0, "游戏结束", null, winTeam);

            //更新玩家的数据
            foreach (MobaClient client in room.ClientList)
            {
                //获取玩家数据模型
                PlayerModel model = playerCache.GetModel(client);
                //检测是否逃跑
                if (room.LeaveClient.Contains(client))
                {
                    //更新逃跑场次
                    playerCache.UpdateModel(model, 2);
                }
                HeroModel hero = room.GetHeroModel(model.Id);
                if (hero.Team == winTeam)
                {
                    //赢了
                    playerCache.UpdateModel(model, 0);
                }
                else
                {
                    //输了
                    playerCache.UpdateModel(model, 1);
                }
            }

            //销毁房间
            fightCache.Destroy(room.Id);
        }

        /// <summary>
        /// 使用技能攻击
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        private void onSkill(MobaClient client, int skillId, int attackId,int targetId,float x,float y,float z)
        {
            int playerId = playerCache.GetId(client);
            FightRoom room = fightCache.GetRoom(playerId);
            //先判断是不是普功
            if (skillId==1)
            {
                //攻击者,被攻击者
                room.Brocast(OpCode.FightCode, OpFight.Skill, 0, "有人普通攻击", null, attackId, targetId);
            }
            //是技能,从技能配置表中获取信息,再广播
            else
            {
                //TODO
                if(targetId==-1)
                {
                    //无目标
                    room.Brocast(OpCode.FightCode, OpFight.Skill, 1, "有人释放技能", null, skillId,attackId, -1, x, y, z);
                }
                else
                {
                    //有目标
                    room.Brocast(OpCode.FightCode, OpFight.Skill, 1, "有人释放技能", null, skillId,attackId, targetId);
                }
            }
        }

        /// <summary>
        /// 移动
        /// </summary>
        /// <param name="client"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        private void onWalk(MobaClient client,float x,float y,float z)
        {
            int playerId = playerCache.GetId(client);
            FightRoom room = fightCache.GetRoom(playerId);
            if (room == null)
                return;
            //发送 谁 移动的位置
            room.Brocast(OpCode.FightCode, OpFight.Walk, 0, "有玩家移动", null, playerId, x, y, z);
        }

        /// <summary>
        ///  玩家进入
        /// </summary>
        /// <param name="client"></param>
        /// <param name="playerId"></param>
        private void onEnter(MobaClient client,int playerId)
        {
            FightRoom room = fightCache.Enter(playerId, client);
            if (room == null)
                return;
            //首先判断是否全部进入:保证竞技游戏的公平
            if (!room.IsAllEnter)
                return;
            //给每一个客户端发送战斗房间信息
            room.Brocast(OpCode.FightCode, OpFight.GetInfo, 0, "加载战斗场景数据", null,
                JsonMapper.ToJson(room.Heros), 
                JsonMapper.ToJson(room.Builds));

        }

    }
}
