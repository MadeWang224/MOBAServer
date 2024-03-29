﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobaCommon.Config
{
    /// <summary>
    /// 英雄数据
    /// </summary>
    public class HeroData
    {
        static Dictionary<int, HeroDataModel> idModelDict = new Dictionary<int, HeroDataModel>();
        static HeroData()
        {
            createHero(1, "战士", 60, 20, 300, 100, 10, 3, 50, 10, 4, new int[] { 1001, 1002, 1003, 1004 });
            createHero(2, "弓箭手", 50, 10, 200, 80, 15, 2, 30, 5, 8, new int[] { 2001, 2002, 2003, 2004 });

        }

        public static HeroDataModel GetHeroData(int heroId)
        {
            HeroDataModel model = null;
            idModelDict.TryGetValue(heroId, out model);
            return model;
        }

        /// <summary>
        /// 创建英雄
        /// </summary>
        /// <returns></returns>
        private static void createHero(int id, string name, int baseAttack, int baseDefense, int hp, int mp, int growAttack, int growDefense, int growHp, int growMp, double attackDistance, int[] skillIds)
        {
            HeroDataModel hero = new HeroDataModel(id, name, baseAttack, baseDefense, hp, mp, growAttack, growDefense, growHp, growMp, attackDistance, skillIds);
            //保存英雄数据
            idModelDict.Add(hero.TypeId, hero);
        }
    }

    /// <summary>
    /// 英雄的数据模型
    /// </summary>
    public class HeroDataModel
    {
        /// <summary>
        /// 英雄编号
        /// </summary>
        public int TypeId;
        /// <summary>
        /// 英雄名字
        /// </summary>
        public string Name;
        /// <summary>
        /// 基础攻击力
        /// </summary>
        public int BaseAttack;
        /// <summary>
        /// 基础防御力
        /// </summary>
        public int BaseDefense;
        /// <summary>
        /// 成长攻击力
        /// </summary>
        public int GrowAttack;
        /// <summary>
        /// 成长防御力
        /// </summary>
        public int GrowDefense;
        /// <summary>
        /// 生命值
        /// </summary>
        public int Hp;
        /// <summary>
        /// 成长生命值
        /// </summary>
        public int GrowHp;
        /// <summary>
        /// 魔法值
        /// </summary>
        public int Mp;
        /// <summary>
        /// 成长魔法
        /// </summary>
        public int GrowMp;
        /// <summary>
        /// 攻击距离
        /// </summary>
        public double AttackDistance;
        /// <summary>
        /// 技能ID
        /// </summary>
        public int[] SkillIds;
        public HeroDataModel()
        {

        }

        public HeroDataModel(int id,string name,int baseAttack,int baseDefense,int hp,int mp,int growAttack,int growDefense,int growHp,int growMp,double attackDistance,int[] skillIds)
        {
            this.TypeId = id;
            this.Name = name;
            this.BaseAttack = baseAttack;
            this.BaseDefense = baseDefense;
            this.Hp = hp;
            this.Mp = mp;
            this.GrowAttack = growAttack;
            this.GrowDefense = growDefense;
            this.GrowHp = growHp;
            this.GrowMp = growMp;
            this.AttackDistance = attackDistance;
            this.SkillIds = skillIds;
        }
    }
}
