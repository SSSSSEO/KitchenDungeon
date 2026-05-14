using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using KitchenDungeon.Models;

public class LobbyController : MonoBehaviour
{
    [Header("--- 상단 HUD UI ---")]
    [SerializeField] private TextMeshProUGUI nicknameText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI expText;     

    [Header("--- 스테이지 선택 UI ---")]
    [SerializeField] private TextMeshProUGUI stageNameText;  // 스테이지 이름 표시
    [SerializeField] private TextMeshProUGUI stagePriceText; // 가격 혹은 상태 표시
    [SerializeField] private Image stageIllustrationDisp;

    [Tooltip("스테이지별 일러스트 리스트 (서버의 unlock_order 순서대로 넣어주세요)")]
    [SerializeField] private List<Sprite> stageSprites = new List<Sprite>();

    [SerializeField] private Button prevButton;             // 이전 버튼
    [SerializeField] private Button nextButton;             // 다음 버튼

    [Header("--- 하단 액션 버튼 ---")]
    [SerializeField] private Button actionButton;           // 입장/구매 공용 버튼
    [SerializeField] private TextMeshProUGUI actionButtonText;

    // 서버에서 받은 데이터를 담아둘 바구니
    private List<StageData> stageList = new List<StageData>();
    private int currentStageIndex = 0; // 현재 보고 있는 스테이지 번호 (0부터 시작)

    private void Start()
    {
        // 1. 초기 UI 세팅 (닉네임, 골드, 레벨, 경험치 반영)
        UpdateUserStatsUI();

        // 2. 버튼 이벤트 연결
        if (prevButton != null) prevButton.onClick.AddListener(OnPrevButtonClicked);
        if (nextButton != null) nextButton.onClick.AddListener(OnNextButtonClicked);
        if (actionButton != null) actionButton.onClick.AddListener(OnActionButtonClicked);

        // 3. 서버에 스테이지 목록 요청
        StartCoroutine(RequestStageList());
    }

    /// <summary>
    /// 서버에서 스테이지 목록을 가져오는 코루틴
    /// </summary>
    private IEnumerator RequestStageList()
    {
        // NetworkManager의 주소를 사용하여 URL 조합
        string url = $"{NetworkManager.Instance.BaseUrl}/stages?user_id={NetworkManager.Instance.UserId}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                StageListResponse response = JsonUtility.FromJson<StageListResponse>(request.downloadHandler.text);

                if (response.status == "success" && response.data.Count > 0)
                {
                    stageList = response.data;
                    currentStageIndex = 0; // 첫 번째 스테이지부터 보여줌
                    UpdateStageDisplay();   // 화면 갱신
                }
            }
            else
            {
                Debug.LogError("스테이지 목록 로드 실패!");
            }
        }
    }

    /// <summary>
    /// 현재 인덱스에 맞는 스테이지 정보(텍스트, 일러스트)를 화면에 뿌려줌
    /// </summary>
    private void UpdateStageDisplay()
    {
        if (stageList.Count == 0) return;

        StageData current = stageList[currentStageIndex];

        // 1. 텍스트 정보 갱신
        stageNameText.text = current.name;

        // 2. 일러스트(이미지) 갱신
        // 리스트에 이미지가 등록되어 있고 인덱스가 유효할 때만 교체
        if (currentStageIndex < stageSprites.Count && stageSprites[currentStageIndex] != null)
        {
            stageIllustrationDisp.sprite = stageSprites[currentStageIndex];
        }

        // 3. 소유 여부에 따른 상태 표시 및 버튼 텍스트 변경
        if (current.is_owned)
        {
            stagePriceText.text = "도전 가능";
            actionButtonText.text = "던전 입장";
        }
        else
        {
            stagePriceText.text = $"{current.unlock_price} Gold";
            actionButtonText.text = "스테이지 해금";
        }

        // 4. 인덱스 범위에 따른 화살표 버튼 활성화/비활성화
        prevButton.interactable = (currentStageIndex > 0);
        nextButton.interactable = (currentStageIndex < stageList.Count - 1);
    }

    private void OnNextButtonClicked()
    {
        if (currentStageIndex < stageList.Count - 1)
        {
            currentStageIndex++;
            UpdateStageDisplay();
        }
    }

    private void OnPrevButtonClicked()
    {
        if (currentStageIndex > 0)
        {
            currentStageIndex--;
            UpdateStageDisplay();
        }
    }

    /// <summary>
    /// 하단 메인 버튼 클릭 시 실행 (입장 혹은 구매)
    /// </summary>
    private void OnActionButtonClicked()
    {
        StageData current = stageList[currentStageIndex];

        if (current.is_owned)
        {
            Debug.Log($"[Sprint 1] {current.name} 던전으로 입장합니다!");
            // TODO: 전투 씬 전환 로직
        }
        else
        {
            Debug.Log($"[Sprint 1] {current.name} 구매 통신을 시작합니다.");
            StartCoroutine(RequestPurchaseStage(current.stage_id));
        }
    }

    /// <summary>
    /// 상단 HUD 정보를 NetworkManager 데이터로 최신화함
    /// </summary>
    public void UpdateUserStatsUI()
    {
        // 닉네임과 경험치 정보까지 포함하여 갱신
        if (nicknameText != null) nicknameText.text = NetworkManager.Instance.Nickname;
        if (goldText != null) goldText.text = NetworkManager.Instance.TotalGold.ToString("N0");
        if (levelText != null) levelText.text = $"LV. {NetworkManager.Instance.UserLevel}";
        if (expText != null) expText.text = $"EXP: {NetworkManager.Instance.CurrentExp}";
    }

    /// <summary>
    /// 서버에 스테이지 구매 요청을 보내는 코루틴
    /// </summary>
    private IEnumerator RequestPurchaseStage(int stageId)
    {
        // 1. 버튼 중복 클릭 방지
        actionButton.interactable = false;

        // 2. 데이터 준비
        string url = $"{NetworkManager.Instance.BaseUrl}/stages/purchase";
        PurchaseRequest body = new PurchaseRequest
        {
            user_id = NetworkManager.Instance.UserId,
            stage_id = stageId
        };
        string json = JsonUtility.ToJson(body);

        // 3. POST 요청 설정
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            actionButton.interactable = true;

            if (request.result == UnityWebRequest.Result.Success)
            {
                PurchaseResponse response = JsonUtility.FromJson<PurchaseResponse>(request.downloadHandler.text);

                if (response.status == "success")
                {
                    Debug.Log($"구매 성공! 남은 골드: {response.data.remaining_gold}");

                    // [핵심] 1. 메모리 데이터 갱신 (NetworkManager 골드 업데이트)
                    NetworkManager.Instance.TotalGold = response.data.remaining_gold;

                    // [핵심] 2. 현재 스테이지 리스트 상태 갱신 (이걸 해야 버튼이 바뀜)
                    stageList[currentStageIndex].is_owned = true;

                    // [핵심] 3. UI 즉시 새로고침
                    UpdateUserStatsUI(); // 상단 골드 텍스트 갱신
                    UpdateStageDisplay(); // 버튼 "입장"으로 변경
                }
            }
            else
            {
                // 실패 처리 (골드 부족 등)
                try
                {
                    BaseResponse errorRes = JsonUtility.FromJson<BaseResponse>(request.downloadHandler.text);
                    Debug.LogError($"구매 실패: {errorRes.message}");
                    // TODO: 유저에게 팝업창 등으로 "골드가 부족합니다" 알려주기
                }
                catch
                {
                    Debug.LogError("서버 연결 에러");
                }
            }
        }
    }
}

    