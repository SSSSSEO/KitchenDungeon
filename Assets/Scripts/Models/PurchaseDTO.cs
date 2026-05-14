using System;

namespace KitchenDungeon.Models
{
    // [POST] /api/v1/stages/purchase 요청용 데이터
    [Serializable]
    public class PurchaseRequest
    {
        public int user_id;
        public int stage_id;
    }

    // 구매 성공 시 서버가 주는 응답 데이터
    [Serializable]
    public class PurchaseResponse : BaseResponse
    {
        public PurchaseResponseData data;
    }

    [Serializable]
    public class PurchaseResponseData
    {
        public int remaining_gold;
        public int purchased_stage;
    }
}