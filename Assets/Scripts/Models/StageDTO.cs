using System;
using System.Collections.Generic;

namespace KitchenDungeon.Models
{
    /// <summary>
    /// 개별 스테이지(던전)의 상세 정보
    /// </summary>
    [Serializable]
    public class StageData
    {
        public int stage_id;      // 스테이지 고유 번호
        public string name;       // 스테이지 이름 (예: 한식)
        public int unlock_price;  // 해금 비용 (골드)
        public int order;         // 정렬 순서
        public bool is_owned;     // 유저의 소유 여부
    }

    /// <summary>
    /// 스테이지 목록 조회의 전체 응답 양식
    /// </summary>
    [Serializable]
    public class StageListResponse : BaseResponse
    {
        // 서버에서 "data": [...] 형태로 리스트를 내려주므로 List로 받음
        public List<StageData> data;
    }
}