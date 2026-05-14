using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using KitchenDungeon.Models;
using UnityEngine.SceneManagement;

namespace KitchenDungeon.UI.Popups
{
    public class MonsterInfoPopup : MonoBehaviour
    {
        [Header("--- UI Panel ---")]
        [SerializeField] private GameObject popupPanel;

        [Header("--- Buttons ---")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button cancelButton;

        [Header("--- Display (Sprint 1 간소화) ---")]
        [SerializeField] private TextMeshProUGUI messageText;

        private int targetRecipeId;

        private void Start()
        {
            // 버튼 클릭 리스너 연결
            if (startButton != null) startButton.onClick.AddListener(OnStartButtonClicked);
            if (cancelButton != null) cancelButton.onClick.AddListener(ClosePopup);

            popupPanel.SetActive(false);
        }

        /// <summary>
        /// 몬스터 리스트에서 호출하여 팝업을 활성화함
        /// </summary>
        public void OpenPopup(int recipeId, string recipeName)
        {
            targetRecipeId = recipeId;
            if (messageText != null)
                messageText.text = $"[{recipeName}]\n정화를 시작하시겠습니까?";

            popupPanel.SetActive(true);
        }

        /// <summary>
        /// 시작 버튼 클릭 시 호출 (서버에 요리 세션 시작 요청)
        /// </summary>
        private void OnStartButtonClicked()
        {
            StartCoroutine(RequestStartCooking());
        }

        private IEnumerator RequestStartCooking()
        {
            // 통신 중 중복 클릭 방지
            startButton.interactable = false;

            string url = $"{NetworkManager.Instance.BaseUrl}/cooking/start";
            CookingStartRequest body = new CookingStartRequest
            {
                user_id = NetworkManager.Instance.UserId,
                recipe_id = targetRecipeId
            };
            string json = JsonUtility.ToJson(body);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    CookingStartResponse response = JsonUtility.FromJson<CookingStartResponse>(request.downloadHandler.text);

                    if (response.status == "success")
                    {
                        Debug.Log($"[CookingStart] {response.message}");

                        // TODO: response.data.step_detail 정보를 NetworkManager 등에 캐싱하여 인게임에서 활용
                        // NetworkManager.Instance.CurrentCookingStep = response.data.step_detail;

                        // 인게임(전투) 씬으로 전환
                        SceneManager.LoadScene("CookingBattleScene");
                    }
                }
                else
                {
                    Debug.LogError($"[CookingStart] API 요청 실패: {request.error}");
                    startButton.interactable = true;
                }
            }
        }

        public void ClosePopup()
        {
            popupPanel.SetActive(false);
        }
    }
}