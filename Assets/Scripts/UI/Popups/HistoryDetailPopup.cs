using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using KitchenDungeon.Models;

namespace KitchenDungeon.UI.Popups
{
    /// <summary>
    /// 전적 요약 칸을 클릭했을 때 나타나는 'AI 한마디 및 실제 촬영 사진' 상세 팝업창입니다.
    /// [스프린트 2 고도화] 골드, 경험치, 점수 텍스트가 UI 레이아웃을 위해 각각 분리되었습니다.
    /// </summary>
    public class HistoryDetailPopup : MonoBehaviour
    {
        [Header("--- UI Panel ---")]
        [SerializeField] private GameObject popupPanel;       // 팝업 부모 오브젝트

        [Header("--- 상세 정보 UI 요소 (텍스트 분리 버전) ---")]
        [SerializeField] private TextMeshProUGUI titleText;       // 요리 이름 타이틀
        [SerializeField] private TextMeshProUGUI finalScoreText;  // 최종 점수 표시 text
        [SerializeField] private TextMeshProUGUI rewardGoldText;  // 획득 골드 표시 text
        [SerializeField] private TextMeshProUGUI rewardExpText;   // 획득 경험치 표시 text

        [Tooltip("🌟 핵심: AI의 종합 한마디 피드백")]
        [SerializeField] private TextMeshProUGUI feedbackText;
        [Tooltip("🌟 핵심: 유저가 실제 촬영했던 사진")]
        [SerializeField] private RawImage userPhotoPreview;

        // 👇 [스프린트 2 추가] 정화된 몬스터 프리팹이 위치할 앵커와 생성된 객체 기억 공간
        [Header("--- 몬스터 프리팹 연동 ---")]
        [Tooltip("팝업창 내부에 몬스터가 서 있을 위치 (부모 오브젝트)")]
        [SerializeField] private Transform monsterAnchor;
        private GameObject currentMonsterInstance; // 잔상 제거 및 청소용 변수

        [Header("--- Buttons ---")]
        [SerializeField] private Button closeButton;          // 팝업 닫기 버튼

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(ClosePopup);
            popupPanel.SetActive(false);
        }

        /// <summary>
        /// 요약 리스트 칸을 누르면 데이터를 이어받아 상세창을 활성화합니다.
        /// </summary>
        public void OpenPopup(HistoryItemData data)
        {
            popupPanel.SetActive(true);

            // 1. 기존에 생성되어 있던 몬스터 잔상이 있다면 깔끔하게 파괴 (청소)
            if (currentMonsterInstance != null) Destroy(currentMonsterInstance);

            // 2. [핵심] 이 전적의 recipe_id를 활용해 정화된(Good) 몬스터 프리팹 로드 및 생성
            // 경로 예시: Assets/Resources/Monsters/Monster_1_Good.prefab
            GameObject monsterPrefab = Resources.Load<GameObject>($"Monsters/Monster_{data.recipe_id}_Good");
            if (monsterPrefab != null && monsterAnchor != null)
            {
                currentMonsterInstance = Instantiate(monsterPrefab, monsterAnchor);
                currentMonsterInstance.SetActive(true); // 화면에 등장!
            }
            else
            {
                Debug.LogWarning($"[HistoryDetail] Monsters/Monster_{data.recipe_id}_Good 프리팹을 찾을 수 없거나 앵커가 비어있습니다.");
            }

            // 3. 분리된 텍스트 컴포넌트에 각각 데이터 세팅
            if (titleText != null)
                titleText.text = $"<b>[{data.recipe_name}]</b>";

            if (finalScoreText != null)
                finalScoreText.text = $"<b>{data.final_score}</b> 점";

            if (rewardGoldText != null)
                rewardGoldText.text = $"+ {data.earned_gold:N0}";

            if (rewardExpText != null)
                rewardExpText.text = $"+ {data.earned_exp:N0}";

            if (feedbackText != null)
            {
                feedbackText.text = $"<color=#FFD700><b>[Gemini Judge의 종합 평가]</b></color>\n\"{data.final_ai_feedback}\"";
            }

            // 4. 유저가 찍은 실제 요리 이미지 실시간 웹 다운로드
            if (userPhotoPreview != null)
            {
                // 로딩 전 리셋 (흰색 투명)
                userPhotoPreview.texture = null;
                userPhotoPreview.color = new Color(1, 1, 1, 0);

                if (!string.IsNullOrEmpty(data.final_image_url))
                {
                    StartCoroutine(DownloadUserPhoto(data.final_image_url));
                }
            }
        }

        /// <summary>
        /// 서버에 보관된 유저의 실제 촬영 사진 URL을 다운로드하여 RawImage에 바인딩합니다.
        /// </summary>
        private IEnumerator DownloadUserPhoto(string url)
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    if (userPhotoPreview != null)
                    {
                        userPhotoPreview.texture = texture;
                        userPhotoPreview.color = Color.white; // 불투명 활성화
                    }
                }
                else
                {
                    Debug.LogError($"[HistoryDetail] 사진 다운로드 실패: {request.error}");
                }
            }
        }

        public void ClosePopup()
        {
            // 👇 [스프린트 2 추가] 창 닫을 때 생성된 몬스터 오브젝트를 확실하게 파괴
            if (currentMonsterInstance != null)
            {
                Destroy(currentMonsterInstance);
            }

            popupPanel.SetActive(false);
        }
    }
}