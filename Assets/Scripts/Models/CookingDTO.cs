using System;

namespace KitchenDungeon.Models
{
    [Serializable]
    public class CookingStartRequest
    {
        public int user_id;
        public int recipe_id;
    }

    // DB: RecipeSteps 테이블 구조와 매칭
    [Serializable]
    public class StepDetail
    {
        public int step_id;             // 단계 고유 고유 PK
        public int recipe_id;           // 소속 레시피 ID
        public int step_order;          // 진행 순서 (1, 2, 3...)
        public string description;      // 메인 지시 사항
        public string step_image_path;  // 가이드 이미지 경로 (파일명)
        public string image_url;        // 서버에서 생성해준 완전한 URL
        public string step_tip;         // 팁 텍스트
        public int timer_seconds;       // 보조 타이머 설정 시간 (0이면 비활성)
        public int required_image_count;// 필요한 사진 개수
        public int is_ai_required;      // AI 검증 여부 (1: 참, 0: 거짓)
        public string ai_guide;         // AI 채점 기준 프롬프트
    }

    /// <summary>
    /// /cooking/start 혹은 /steps 요청 시 내려오는 요리 세션 응답 구조
    /// </summary>
    [Serializable]
    public class CookingStepResponse : BaseResponse
    {
        public CookingStepData data;
    }

    [Serializable]
    public class CookingStepData
    {
        public int user_id;
        public int recipe_id;
        public int current_step;        // 현재 진행해야 할 단계 번호
        public int total_steps;         // 체력바(HP) 최대치 설정을 위한 총 단계수
        public StepDetail step_detail;  // 현재 단계의 구체적 정보
    }

    /// <summary>
    /// AI 검증(/cooking/verify) 결과 응답 구조
    /// </summary>
    [Serializable]
    public class CookingVerifyResponse : BaseResponse
    {
        public CookingVerifyData data;
    }

    [Serializable]
    public class CookingVerifyData
    {
        public bool is_success;         // 검증 통과 여부
        public int score;               // 획득한 점수
        public string feedback;         // AI의 한마디 (ai_feedback)
        public int current_step;        // 방금 검증한 단계
        public int total_steps;         // 전체 단계 (HP바 동기화용)
        public int next_step;           // 성공 시 다음 단계 번호
    }
}