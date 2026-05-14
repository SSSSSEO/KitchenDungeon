using System;

namespace KitchenDungeon.Models
{
    // [POST] /api/v1/cooking/start 요청용
    [Serializable]
    public class CookingStartRequest
    {
        public int user_id;
        public int recipe_id;
    }

    // 서버 응답 데이터 전체 구조
    [Serializable]
    public class CookingStartResponse : BaseResponse
    {
        public CookingStartData data;
    }

    [Serializable]
    public class CookingStartData
    {
        public int user_id;
        public int recipe_id;
        public int current_step;
        public StepDetail step_detail; // 현재 진행해야 할 단계의 상세 정보
    }

    [Serializable]
    public class StepDetail
    {
        public int step_id;
        public int recipe_id;
        public int step_order;
        public string instruction;     // 요리 지시문 (예: 양파를 써세요)
        public string step_image_path;
        public string image_url;       // 서버에서 변환해준 이미지 주소
        // 추가적인 필드(시간 제한 등)가 있다면 여기에 더 추가 가능
    }
}