using System;
using System.Collections.Generic;

namespace KitchenDungeon.Models
{
    [Serializable]
    public class MonsterData
    {
        public int recipe_id;
        public string recipe_name;
        public int difficulty;
        public int reward_gold;
        public int reward_exp;
        public int required_recipe_id;
        public string visual_state; // PURIFIED, CORRUPTED, LOCKED
    }

    [Serializable]
    public class MonsterListResponse : BaseResponse
    {
        public int stage_id;
        public List<MonsterData> data;
    }
}