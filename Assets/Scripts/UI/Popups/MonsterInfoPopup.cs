using System.Collections;
using System.Collections.Generic;
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
    /// [Sprint 2 고도화] 여기에서 서버에 브리핑 정보를 GET으로 먼저 받아와 UI를 채우고,
    /// 최종적으로 전투 시작을 알리고 첫 번째 데이터를 받아옵니다.
    /// </summary>
    public class MonsterInfoPopup : MonoBehaviour
    {
        [Header("--- UI Panel ---")]
        [SerializeField] private GameObject popupPanel;     // 팝업 전체를 담고 있는 부모 오브젝트

        [Header("--- Buttons ---")]
        [SerializeField] private Button startButton;         // [정화 시작] 버튼
        [SerializeField] private Button cancelButton;        // [취소] 버튼

        [Header("--- Display UI ---")]
        [Tooltip("몬스터 이름과 시작 메시지를 보여주는 텍스트 (기존 messageText에서 역할 확장)")]
        [SerializeField] private TextMeshProUGUI messageText; // 몬스터 이름 표시용

        [Header("--- Sprint 2 고도화 UI 요소 ---")]
        [SerializeField] private TextMeshProUGUI difficultyText;  // 난이도 별 표시 텍스트
        [SerializeField] private RawImage finalImagePreview;      // 완성 요리 이미지 (서버 URL 다운로드)
        [SerializeField] private TextMeshProUGUI rewardGoldText;  // 보상 골드 text
        [SerializeField] private TextMeshProUGUI rewardExpText;   // 보상 경험치 text
        [SerializeField] private TextMeshProUGUI ingredientsText; // 주요 재료/공략 정보 text
        [SerializeField] private TextMeshProUGUI quickTipText;    // 한줄 공략 팁 text

        [Header("--- 동적 스크롤 뷰 세팅 ---")]
        [Tooltip("Scroll View -> Viewport -> Content 오브젝트 연결")]
        [SerializeField] private Transform stepContentParent;     // 프리팹들이 동적으로 생성되어 붙을 부모 Content
        [Tooltip("조리 흐름 한 칸을 담당할 TextMeshPro가 포함된 프리팹 오브젝트")]
        [SerializeField] private GameObject stepItemPrefab;       // 동적 생성할 단계 칸 프리팹

        // 리스트에서 전달받은 현재 타겟 레시피 ID
        private int targetRecipeId;

        // 새로 생성된 스크롤뷰 아이템들을 추적하고 청소하기 위한 리스트 기억 공간
        private List<GameObject> spawnedStepItems = new List<GameObject>();

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

            // 새로운 데이터를 받기 전에 기존에 남아있던 스크롤뷰 아이템들과 이전 이미지 잔상을 깨끗이 청소
            ClearBeforeData();

            // 팝업 레이아웃 가시화
            popupPanel.SetActive(true);

            if (messageText != null)
                messageText.text = $"<b>[{recipeName}]</b> 정찰 중...";

            // [Sprint 2 추가] 팝업이 열리자마자 서버에서 통합 브리핑 데이터를 받아옴 (올인원)
            StartCoroutine(RequestMonsterBriefing(recipeId));
        }

        /// <summary>
        /// [GET] /api/v1/monsters/<int:recipe_id>/brief API를 호출합니다.
        /// 서버로부터 보상, 재료, 팁, 완성 이미지 URL, 전체 단계 흐름을 한 번에 땡겨옵니다.
        /// </summary>
        private IEnumerator RequestMonsterBriefing(int recipeId)
        {
            string url = $"{NetworkManager.Instance.BaseUrl}/monsters/{recipeId}/brief";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // JSON 데이터를 MonsterBriefResponse 객체로 파싱
                    MonsterBriefResponse response = JsonUtility.FromJson<MonsterBriefResponse>(request.downloadHandler.text);

                    if (response.status == "success")
                    {
                        // 받아온 올인원 데이터를 기반으로 UI 컴포넌트들 세팅 시작
                        UpdateBriefingUI(response.data);
                    }
                }
                else
                {
                    Debug.LogError($"[MonsterBrief] API 요청 실패: {request.error}");
                    if (messageText != null) messageText.text = "<color=red>브리핑 데이터 로드 실패!</color>";
                }
            }
        }

        /// <summary>
        /// 서버에서 성공적으로 받아온 브리핑 데이터를 각 UI 텍스트 및 스크롤뷰에 매핑합니다.
        /// </summary>
        private void UpdateBriefingUI(MonsterBriefData data)
        {
            // 1. 마스터 정보 및 텍스트 데이터 세팅
            if (messageText != null) messageText.text = $"<b>[{data.recipe_name}]</b>";
            if (difficultyText != null) difficultyText.text = new string('★', data.difficulty);
            if (rewardGoldText != null) rewardGoldText.text = $"{data.reward_gold:N0} G";
            if (rewardExpText != null) rewardExpText.text = $"{data.reward_exp:N0} EXP";
            if (ingredientsText != null) ingredientsText.text = $"<b>주요 재료:</b> {data.ingredients}";
            if (quickTipText != null) quickTipText.text = $"Tip: {data.quick_tip}";

            // 2. 완성 사진 URL 주소가 존재한다면 다운로드 코루틴 시동
            if (!string.IsNullOrEmpty(data.final_image_url))
            {
                StartCoroutine(DownloadFinalImage(data.final_image_url));
            }

            // 3. 요리 단계 리스트뷰(스크롤뷰) 동적 밀어 넣기
            // 서버에서 내려준 steps_briefing 배열의 개수만큼 루프를 돌며 프리팹을 찍어냅니다.
            if (stepContentParent != null && stepItemPrefab != null)
            {
                foreach (var step in data.steps_briefing)
                {
                    // 프리팹 인스턴스화 및 스크롤 뷰 Content의 자식으로 등록
                    GameObject item = Instantiate(stepItemPrefab, stepContentParent);
                    spawnedStepItems.Add(item); // 추후 청소를 위해 리스트에 수집

                    // 생성된 아이템의 자식 TMP 컴포넌트를 찾아 페이즈 및 요약 설명 주입
                    TextMeshProUGUI itemText = item.GetComponentInChildren<TextMeshProUGUI>();
                    if (itemText != null)
                    {
                        itemText.text = $"<color=#FFD700>Phase {step.step_order}.</color> {step.step_summary}";
                    }
                }
            }

            // 모든 브리핑 데이터 세팅이 완벽히 끝났으므로 최종 [정화 시작] 버튼 잠금 해제
            startButton.interactable = true;
        }

        /// <summary>
        /// 서버 웹 서버에 호스팅된 완성 요리 이미지를 실시간으로 다운로드하여 RawImage에 렌더링합니다.
        /// </summary>
        private IEnumerator DownloadFinalImage(string imageUrl)
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // 다운로드된 텍스처 데이터를 꺼내와 RawImage에 할당
                    Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(request);
                    if (finalImagePreview != null)
                    {
                        finalImagePreview.texture = downloadedTexture;
                        finalImagePreview.color = Color.white; // 투명 상태 해제 (불투명하게 활성화)
                    }
                }
                else
                {
                    Debug.LogError($"[MonsterBrief] 이미지 다운로드 실패: {request.error}");
                }
            }
        }

        /// <summary>
        /// 팝업이 다시 열릴 때 이전 몬스터의 데이터(스크롤뷰 리스트, 이미지) 잔상이 남아있는 것을 방지하는 클리어 함수입니다.
        /// </summary>
        private void ClearBeforeData()
        {
            // 1. 기존에 생성되어 쌓여있던 조리 단계 오브젝트들을 전부 파괴(Destroy)
            foreach (var item in spawnedStepItems)
            {
                if (item != null) Destroy(item);
            }
            spawnedStepItems.Clear();

            // 2. 이미지 미리보기 컴포넌트 초기화 및 투명화 (리셋 상태)
            if (finalImagePreview != null)
            {
                finalImagePreview.texture = null;
                finalImagePreview.color = new Color(1, 1, 1, 0); // 알파값을 0으로 만들어 흰색 빈칸이 깜빡이는 현상 방지
            }

            // 3. 데이터 로드가 비동기로 도는 동안 유저가 버튼을 먼저 누르지 못하도록 시작 버튼 일시 잠금
            startButton.interactable = false;
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