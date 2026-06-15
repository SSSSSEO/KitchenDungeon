using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KitchenDungeon.Models;
using System;

namespace KitchenDungeon.UI.History
{
    /// <summary>
    /// 전적 스크롤 뷰 내부에 동적으로 생성될 리스트 한 칸의 UI와 이벤트를 제어합니다.
    /// prefab요소에 붙을 스크립트임
    /// </summary>
    public class HistoryItemUI : MonoBehaviour
    {
        [Header("--- 내부 UI 컴포넌트 ---")]
        [SerializeField] private TextMeshProUGUI recipeNameText; // 요리 이름
        [SerializeField] private TextMeshProUGUI scoreText;      // 최종 점수
        [SerializeField] private TextMeshProUGUI dateText;       // 클리어 날짜
        [SerializeField] private Button clickButton;             // 칸 전체를 감싸는 버튼 (상세 팝업 트리거)

        // 이 칸이 들고 있는 원본 데이터 백업
        private HistoryItemData myData;

        /// <summary>
        /// 생성 직후 컨트롤러에 의해 호출되어 화면을 그리고 버튼 이벤트를 연결합니다.
        /// </summary>
        /// <param name="data">서버에서 받은 이 칸의 전적 데이터</param>
        /// <param name="onClicked">클릭 시 부모 컨트롤러가 실행할 콜백 액션</param>
        public void Setup(HistoryItemData data, Action<HistoryItemData> onClicked)
        {
            myData = data;

            // 1. 요약 데이터 화면에 뿌리기
            if (recipeNameText != null) recipeNameText.text = data.recipe_name;
            if (scoreText != null) scoreText.text = $"최종 점수: <b>{data.final_score}</b>점";

            // 날짜 문자열이 너무 길면 보기 싫으니 앞부분(연-월-일)만 잘라서 쓰기 방어코드
            if (dateText != null && !string.IsNullOrEmpty(data.cleared_at))
            {
                if (data.cleared_at.Length >= 10)
                    dateText.text = data.cleared_at.Substring(0, 10);
                else
                    dateText.text = data.cleared_at;
            }

            // 2. 클릭 리스너 등록 (부모에게 내 데이터를 실어서 보냄)
            if (clickButton != null)
            {
                clickButton.onClick.RemoveAllListeners();
                clickButton.onClick.AddListener(() => onClicked?.Invoke(myData));

                clickButton.onClick.AddListener(() =>
                    {
                        Debug.Log("카드 클릭됨");
                        onClicked?.Invoke(myData);
                    });
            }
        }


    }
}