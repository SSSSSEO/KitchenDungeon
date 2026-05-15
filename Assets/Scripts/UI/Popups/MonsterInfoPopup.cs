using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using KitchenDungeon.Models;
using UnityEngine.SceneManagement;

namespace KitchenDungeon.UI.Popups
{
    /// <summary>
    /// 몬스터 리스트에서 특정 몬스터를 클릭했을 때 나타나는 '최종 확인 및 정보' 팝업입니다.
    /// 여기에서 서버에 전투 시작을 알리고 첫 번째 데이터를 받아옵니다.
    /// </summary>
    public class MonsterInfoPopup : MonoBehaviour
    {
        [Header("--- UI Panel ---")]
        [SerializeField] private GameObject popupPanel;     // 팝업 전체를 담고 있는 부모 오브젝트

        [Header("--- Buttons ---")]
        [SerializeField] private Button startButton;         // [정화 시작] 버튼
        [SerializeField] private Button cancelButton;        // [취소] 버튼

        [Header("--- Display UI ---")]
        [Tooltip("몬스터 이름과 시작 메시지를 보여주는 텍스트")]
        [SerializeField] private TextMeshProUGUI messageText;

        // 리스트에서 전달받은 현재 타겟 레시피 ID
        private int targetRecipeId;

        private void Start()
        {
            // 버튼 클릭 리스너 등록 (람다식 사용)
            if (startButton != null) startButton.onClick.AddListener(() => StartCoroutine(RequestStartCooking()));
            if (cancelButton != null) cancelButton.onClick.AddListener(ClosePopup);

            // 씬 시작 시에는 당연히 꺼져 있어야 함
            popupPanel.SetActive(false);
        }

        /// <summary>
        /// MonsterListController에서 몬스터를 클릭했을 때 이 함수를 호출하여 팝업을 엽니다.
        /// </summary>
        /// <param name="recipeId">서버와 통신할 레시피 고유 번호</param>
        /// <param name="recipeName">화면에 표시할 요리 이름</param>
        public void OpenPopup(int recipeId, string recipeName)
        {
            targetRecipeId = recipeId;
            if (messageText != null)
                messageText.text = $"<b>[{recipeName}]</b>\n오염을 정화하시겠습니까?";

            popupPanel.SetActive(true);
        }

        /// <summary>
        /// [POST] /api/v1/cooking/start API를 호출합니다.
        /// 서버는 여기서 유저의 기존 진행도를 체크하여 '이어하기' 혹은 '새로시작' 데이터를 줍니다.
        /// </summary>
        private IEnumerator RequestStartCooking()
        {
            // 1. 중복 요청 방지 (버튼 비활성화)
            startButton.interactable = false;

            string url = $"{NetworkManager.Instance.BaseUrl}/cooking/start";

            // 2. 서버 규격에 맞는 JSON 요청 객체 생성 (user_id, recipe_id)
            CookingStartRequest bodyData = new CookingStartRequest
            {
                user_id = NetworkManager.Instance.UserId,
                recipe_id = targetRecipeId
            };
            string json = JsonUtility.ToJson(bodyData);

            // 3. UnityWebRequest 설정 (POST 방식)
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                // 4. 응답 처리
                if (request.result == UnityWebRequest.Result.Success)
                {
                    // JSON 데이터를 CookingStepResponse 객체로 파싱
                    CookingStepResponse response = JsonUtility.FromJson<CookingStepResponse>(request.downloadHandler.text);

                    if (response.status == "success")
                    {
                        Debug.Log($"[CookingStart] 서버 메시지: {response.message}");

                        // [핵심] 받은 '전투 설계도' 데이터를 NetworkManager에 저장!
                        // 이렇게 해야 다음 씬(CookingBattleScene)에서 데이터를 꺼낼 수 있음.
                        NetworkManager.Instance.CurrentSessionData = response.data;

                        // 5. 실제 인게임 전투 씬으로 이동
                        SceneManager.LoadScene("CookingBattleScene");
                    }
                }
                else
                {
                    Debug.LogError($"[CookingStart] API 요청 실패: {request.error}");
                    startButton.interactable = true; // 실패 시 다시 버튼 활성화
                }
            }
        }

        public void ClosePopup()
        {
            popupPanel.SetActive(false);
        }
    }
}