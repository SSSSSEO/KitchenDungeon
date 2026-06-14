using System;
using System.Collections.Generic;

namespace KitchenDungeon.Models
{
    /// <summary>
    /// [GET] /api/v1/users/<id>/history API의 최상위 응답 매핑 클래스
    /// </summary>
    [Serializable]
    public class HistoryResponse
    {
        public string status;
        public string message;
        public HistoryContainer data; // 중간 데이터 컨테이너로 진입
    }

    /// <summary>
    /// 서버 응답 내부의 통계 및 리스트를 담고 있는 컨테이너 클래스
    /// </summary>
    [Serializable]
    public class HistoryContainer
    {
        public int user_id;
        public int total_count;
        public List<HistoryItemData> history_list; // 🌟 개별 전적들이 담긴 핵심 배열!
    }

    /// <summary>
    /// CookingHistory 테이블과 Recipes 테이블이 JOIN된 개별 전적 데이터 클래스
    /// </summary>
    [Serializable]
    public class HistoryItemData
    {
        public int history_id;
        public int recipe_id;
        public string recipe_name;       // Recipes 테이블에서 JOIN해온 요리 명
        public int earned_gold;          // 이번 판에서 실제 획득한 골드 (재도전 시 0)
        public int earned_exp;           // 이번 판에서 실제 획득한 경험치 (재도전 시 0)
        public int final_score;          // 이번 판 최종 누적 점수
        public string final_ai_feedback; // 이번 판 마지막 단계의 AI 한마디 피드백
        public string final_image_url;   // 유저가 찍은 실제 조리 사진의 웹 다운로드 URL
        public string cleared_at;        // 정산 완료된 날짜 및 시간 문자열
    }
}