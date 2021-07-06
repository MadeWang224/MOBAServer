using MobaCommon.Config;
using MobaCommon.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOBAServer.Room
{
    /// <summary>
    /// 战斗房间
    /// </summary>
    public class FightRoom : RoomBase<MobaClient>
    {
        #region 队伍1
        //英雄
        Dictionary<int, HeroModel> team1HeroModel = new Dictionary<int, HeroModel>();
        //小兵
        Dictionary<int, DogModel> team1DogModel = new Dictionary<int, DogModel>();
        //炮塔
        Dictionary<int, BuildModel> team1BuilModel = new Dictionary<int, BuildModel>();
        #endregion

        #region 队伍2
        //英雄
        Dictionary<int, HeroModel> team2HeroModel = new Dictionary<int, HeroModel>();
        //小兵
        Dictionary<int, DogModel> team2DogModel = new Dictionary<int, DogModel>();
        //炮塔
        Dictionary<int, BuildModel> team2BuilModel = new Dictionary<int, BuildModel>();
        #endregion

        /// <summary>
        /// 逃跑的客户端
        /// </summary>
        public List<MobaClient> LeaveClient = new List<MobaClient>();

        #region Property

        /// <summary>
        /// 是否全部进入
        /// </summary>
        public bool IsAllEnter
        {
            get { return ClientList.Count >= Count; }
        }

        /// <summary>
        /// 是否全部退出
        /// </summary>
        public bool IsAllLeave
        {
            get { return ClientList.Count <= 0; }
        }

        /// <summary>
        /// 建筑
        /// </summary>
        public BuildModel[] Builds
        {
            get 
            {
                List<BuildModel> list = new List<BuildModel>();
                list.AddRange(team1BuilModel.Values);
                list.AddRange(team2BuilModel.Values);
                return list.ToArray();
            }
        }
        /// <summary>
        /// 英雄
        /// </summary>
        public HeroModel[] Heros
        {
            get 
            {
                List<HeroModel> list = new List<HeroModel>();
                list.AddRange(team1HeroModel.Values);
                list.AddRange(team2HeroModel.Values);
                return list.ToArray();
            }
        }

        #endregion

        public FightRoom(int id, int count) : base(id, count)
        {

        }

        /// <summary>
        /// 初始化房间
        /// </summary>
        /// <param name="team1"></param>
        /// <param name="team2"></param>
        public void Init(List<SelectModel> team1, List<SelectModel> team2)
        {
            //初始化英雄数据
            foreach (SelectModel item in team1)
            {
                team1HeroModel.Add(item.playerId, getHeroModel(item,1));
            }
            foreach (SelectModel item in team2)
            {
                team2HeroModel.Add(item.playerId, getHeroModel(item,2));
            }
            //初始化防御塔的数据
            //队伍1 防御塔ID:-10;队伍2 防御塔ID:-20
            team1BuilModel.Add(-10, getBuildModel(-10,(int)BuildType.Main,1));
            team1BuilModel.Add(-11, getBuildModel(-11,(int)BuildType.Camp,1));
            team1BuilModel.Add(-12, getBuildModel(-12, (int)BuildType.Turret, 1));

            team2BuilModel.Add(-20, getBuildModel(-20, (int)BuildType.Main, 2));
            team2BuilModel.Add(-21, getBuildModel(-21, (int)BuildType.Camp, 2));
            team2BuilModel.Add(-22, getBuildModel(-22, (int)BuildType.Turret, 2));
            
        }

        /// <summary>
        /// 小兵的ID
        /// </summary>
        private int dogId = -1000;
        public int DogId
        {
            get
            {
                dogId--;
                return dogId;
            }
        }

        /// <summary>
        /// 开启定时任务:30s之后产生小兵
        /// </summary>
        private void spawnDog()
        {
            this.StartSchedule(DateTime.UtcNow.AddSeconds(30),
                delegate
                {
                    List<DogModel> dogs = new List<DogModel>();
                    for (int i = 0; i < 5; i++)
                    {
                        //产生小兵
                        DogModel dog = new DogModel();
                        //TODO
                        //添加映射
                        team1DogModel.Add(dog.Id, dog);
                        dogs.Add(dog);

                        dog = new DogModel();
                        dog.ModelType = ModelType.DOG;
                        //TODO
                        //添加映射
                        team2DogModel.Add(dog.Id, dog);
                        dogs.Add(dog); 
                    }


                    //给客户端发送:出兵


                    spawnDog();
                });
        }

        /// <summary>
        /// 根据英雄ID获取英雄数据
        /// </summary>
        /// <param name="heroId"></param>
        /// <returns></returns>
        private HeroModel getHeroModel(SelectModel model,int team)
        {
            //TODO
            //从静态配置表中 获取 英雄数据
            HeroDataModel data = HeroData.GetHeroData(model.heroId);
            //英雄数据创建
            HeroModel hero = new HeroModel(model.playerId, data.TypeId, team, data.Hp, data.BaseAttack, data.BaseDefense, data.AttackDistance, data.Name, data.Mp, getSkillModel(data.SkillIds));

            hero.ModelType = ModelType.HERO;

            return hero;
        }

        /// <summary>
        /// 根据技能ID获取具体的技能的数据实体
        /// </summary>
        /// <param name="skillIds"></param>
        /// <returns></returns>
        public SkillModel[] getSkillModel(int[] skillIds)
        {
            SkillModel[] skillModels = new SkillModel[skillIds.Length];
            for (int i = 0; i < skillIds.Length; i++)
            {
                //获取技能数据
                SkillDataModel data = SkillData.GetSkillData(skillIds[i]);
                //初始化时,最低级
                SkillLevelDataModel lvData = data.LvModels[0];
                //赋值
                skillModels[i] = new SkillModel()
                {
                    Id = data.Id,
                    Level = 0,
                    LearnLevel = lvData.LearnLv,
                    CoolDown = lvData.CoolDown,
                    Name = data.Name,
                    Description = data.Description,
                    Distance = lvData.Distance
                };
            }
            return skillModels;
        }

        /// <summary>
        /// 获取防御塔的数据
        /// </summary>
        /// <param name="id">防御塔ID</param>
        /// <param name="typeId">防御塔类型</param>
        /// <param name="team">类型</param>
        /// <returns></returns>
        private BuildModel getBuildModel(int id,int typeId,int team)
        {
            //获取配置表里的数据
            BuildDataModel data = BuildData.GetBuildData(typeId);

            BuildModel model = new BuildModel(id, typeId, team, data.Hp, data.Attack, data.Defense, data.AttackDistance, data.Name, data.Agressire, data.Rebirth, data.RebirthTime);

            model.ModelType = ModelType.BUILD;

            return model;
        }

        /// <summary>
        /// 进入房间
        /// </summary>
        public void Enter(MobaClient client)
        {
            if(!ClientList.Contains(client))
                ClientList.Add(client);
        }

        /// <summary>
        ///  离开房间
        /// </summary>
        /// <param name="client"></param>
        public void Leave(MobaClient client)
        {
            if (ClientList.Contains(client))
                ClientList.Remove(client);

            if (LeaveClient.Contains(client))
                LeaveClient.Add(client);
        }

        /// <summary>
        /// 清空数据
        /// </summary>
        public void Clear()
        {
            team1BuilModel.Clear();
            team1DogModel.Clear();
            team1HeroModel.Clear();
            team2BuilModel.Clear();
            team2DogModel.Clear();
            team2HeroModel.Clear();
            LeaveClient.Clear();
            ClientList.Clear();
        }

        /// <summary>
        /// 获取英雄模型
        /// </summary>
        /// <returns></returns>
        public HeroModel GetHeroModel(int id)
        {
            HeroModel model = null;
            if (team1HeroModel.TryGetValue(id, out model))
                return model;
            if (team2HeroModel.TryGetValue(id, out model))
                return model;
            return null;
        }

        /// <summary>
        /// 获取英雄模型
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public BuildModel GetBuildModel(int id)
        {
            if (team1BuilModel.ContainsKey(id))
                return team1BuilModel[id];
            else if (team2BuilModel.ContainsKey(id))
                return team2BuilModel[id];

            return null;
        }

        /// <summary>
        /// 移除小兵在房间内的数据模型
        /// </summary>
        /// <param name="dog"></param>
        public void RemoveDog(DogModel dog)
        {
            if (dog.Team == 1)
                team1DogModel.Remove(dog.Id);
            else if (dog.Team == 2)
                team2DogModel.Remove(dog.Id);
        }

        /// <summary>
        /// 移除塔在房间内的数据模型
        /// </summary>
        /// <param name="dog"></param>
        public void RemoveBuild(BuildModel build)
        {
            if (build.Team == 1)
                team1BuilModel.Remove(build.Id);
            else if (build.Team == 2)
                team1BuilModel.Remove(build.Id);
        }

    }
}
