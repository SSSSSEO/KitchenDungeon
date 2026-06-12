using System;
using System.Collections.Generic;

namespace KitchenDungeon.Models
{
    // [GET] /api/v1/monsters/<id>/brief 응답 매핑용
    [Serializable]
    public class MonsterBriefResponse
    {
        public string status;
        public string message;
        public MonsterBriefData data;
    }

    [Serializable]
    public class MonsterBriefData
    {
        public int recipe_id;
        public int stage_id;
        public string recipe_name;
        public int difficulty;
        public string quick_tip;
        public int reward_gold;
        public int reward_exp;
        public string ingredients;
        public string final_image_url; // 서버에서 변환해 준 이미지 Full URL
        public int total_steps;
        public List<StepBriefing> steps_briefing; // 단계 리스트
    }

    [Serializable]
    public class StepBriefing
    {
        public int step_order;
        public string step_summary; // 조리 단계 요약 설명
    }
}