using KitchenDungeon.Models;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 요리 전투 씬의 전체 로직을 총괄하는 컨트롤러입니다.
/// 상단 HUD, 중앙 몬스터, 하단 정보 패널 및 타이머를 관리합니다.
/// </summary>
public class CookingBattleController : MonoBehaviour
{
    [Header("--- 상단 HUD UI (진척도) ---")]
    [Tooltip("전체 단계 중 몇 단계를 통과했는지 보여주는 슬라이더 (몬스터의 피통)")]
    [SerializeField] private Slider hpBar;
    [Tooltip("현재까지 획득한 누적 점수 표시")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("--- 하단 정보 패널 (미션) ---")]
    [Tooltip("현재 단계에서 해야 할 일 (description)")]
    [SerializeField] private TextMeshProUGUI descText;
    [Tooltip("성공을 위한 힌트 (step_tip)")]
    [SerializeField] private TextMeshProUGUI tipText;
    [Tooltip("요리 가이드 예시 이미지")]
    [SerializeField] private Image guideImage;
    [Tooltip("검증하기 혹은 공격하기 버튼")]
    [SerializeField] private Button actionButton;
    [Tooltip("버튼에 적힐 텍스트")]
    [SerializeField] private TextMeshProUGUI actionBtnText;

    [Header("--- 보조 도구 (타이머) ---")]
    [Tooltip("타이머 UI 전체 그룹 (0초면 비활성화됨)")]
    [SerializeField] private GameObject timerGroup;
    [Tooltip("남은 시간 표시 (00:00)")]
    [SerializeField] private TextMeshProUGUI timerText;
    [Tooltip("타이머 시작/정지 버튼")]
    [SerializeField] private Button timerControlBtn;
    [Tooltip("타이머 버튼 텍스트 (시작/일시정지/재개)")]
    [SerializeField] private TextMeshProUGUI timerBtnText;

    [Header("--- 팝업 연결 ---")]
    [SerializeField] private CookingVerifyPopup verifyPopup; // 사진 제출용 부분 팝업

    [Header("--- 승리 연출용 UI ---")]
    [Tooltip("중앙의 몬스터 일러스트 이미지")]
    [SerializeField] private Image monsterImage;
    [Tooltip("정화가 완료되었을 때 바뀔 깨끗한 요리 이미지")]
    [SerializeField] private Sprite purifiedMonsterSprite;

    [Header("--- 승리 결과 팝업 ---")]
    [Tooltip("승리 시 나타날 결과창 부모 오브젝트")]
    [SerializeField] private GameObject victoryPopupGroup;
    [SerializeField] private TextMeshProUGUI totalScoreText;
    [SerializeField] private TextMeshProUGUI rewardGoldText;
    [SerializeField] private TextMeshProUGUI rewardExpText;
    [SerializeField] private Button lobbyExitButton; // 로비로 돌아가기 버튼

    [Header("--- 레시피 고정 데이터 (보상) ---")]
    private int rewardGold;
    private int rewardExp;
    private string finalImageUrl;

    // 내부 관리 변수
    private int recipeId;
    private int currentStepOrder;
    private int totalSteps;
    private int accumulatedScore = 0;

    // 타이머 구동용 변수
    private float remainingTime;
    private bool isTimerRunning = false;
    private Coroutine timerCoroutine;

    private void Start()
    {
        // 씬이 시작되면 NetworkManager가 들고 있는 세션 데이터를 확인합니다.
        if (NetworkManager.Instance.CurrentSessionData != null)
        {
            // 저장된 데이터를 사용해 UI와 시스템을 초기화합니다.
            InitializeBattle(NetworkManager.Instance.CurrentSessionData);
        }
        else
        {
            Debug.LogError("[Battle] 서버에서 전달받은 세션 데이터가 없습니다!");
        }
    }

    /// <summary>
    /// 서버에서 받아온 초기 혹은 다음 단계 데이터를 UI에 바인딩합니다.
    /// </summary>
    public void InitializeBattle(CookingStepData data)
    {
        // [추가] 서버에서 새로 받은 데이터를 NetworkManager에도 동기화한다!
        // 이렇게 해야 전역적으로 현재 진행 상황이 최신화됨.
        NetworkManager.Instance.CurrentSessionData = data;

        recipeId = data.recipe_id;
        currentStepOrder = data.current_step;
        totalSteps = data.total_steps;

        Debug.Log($"<color=yellow>[HP 검문소]</color> 서버가 준 현재 단계: {currentStepOrder} / 총 단계: {totalSteps}");

        hpBar.maxValue = totalSteps;
        hpBar.value = totalSteps - (currentStepOrder - 1);

        StepDetail detail = data.step_detail;

        // 1. HP바 연출: (총 단계 수)를 Max로, (남은 단계 수)를 Value로 설정
        // 예: 총 5단계 중 1단계라면 5-0 = 5칸(꽉 참) / 5단계라면 5-4 = 1칸(빈사)
        hpBar.maxValue = totalSteps;
        hpBar.value = totalSteps - (currentStepOrder - 1);

        scoreText.text = accumulatedScore.ToString("N0");

        // 2. 미션 정보 텍스트 주입
        descText.text = detail.description;
        tipText.text = $"<color=#FFD700>Tip:</color> {detail.step_tip}";

        // [추가] 가이드 이미지 로드 로직
        // DTO의 image_url이 비어있지 않다면 서버에서 이미지를 가져옴
        if (guideImage != null && !string.IsNullOrEmpty(detail.image_url))
        {
            StartCoroutine(LoadGuideImage(detail.image_url));
        }

        // 3. 타이머 설정 (timer_seconds가 0이면 UI를 숨김)
        SetupTimer(detail.timer_seconds);

        // 4. 버튼 모드 분기 (is_ai_required가 1이면 AI 검증 모드)
        actionButton.onClick.RemoveAllListeners();
        if (detail.is_ai_required == 1)
        {
            actionBtnText.text = "검증하기";
            // 검증하기 클릭 시 하단 부분 팝업(사진 제출창)을 엶
            actionButton.onClick.AddListener(() => verifyPopup.OpenVerifyMode(recipeId, currentStepOrder, detail));
        }
        else
        {
            actionBtnText.text = "공격하기";
            // AI가 필요 없는 단계는 즉시 타격 로직 실행
            actionButton.onClick.AddListener(OnDirectAttack);
        }
    }

    #region [타이머 핵심 로직]
    /// <summary>
    /// 지시 사항에 명시된 타이머 시간을 세팅합니다.
    /// </summary>
    private void SetupTimer(int seconds)
    {
        if (seconds <= 0)
        {
            timerGroup.SetActive(false);
            return;
        }

        timerGroup.SetActive(true);
        remainingTime = seconds;
        UpdateTimerUI();

        timerControlBtn.onClick.RemoveAllListeners();
        timerControlBtn.onClick.AddListener(ToggleTimer);
        timerBtnText.text = "시작";
        isTimerRunning = false;
    }

    /// <summary>
    /// 유저가 버튼을 누를 때마다 타이머를 시작하거나 일시정지합니다.
    /// </summary>
    private void ToggleTimer()
    {
        if (isTimerRunning)
        {
            if (timerCoroutine != null) StopCoroutine(timerCoroutine);
            timerBtnText.text = "재개";
        }
        else
        {
            timerCoroutine = StartCoroutine(TimerRoutine());
            timerBtnText.text = "일시정지";
        }
        isTimerRunning = !isTimerRunning;
    }

    private IEnumerator TimerRoutine()
    {
        while (remainingTime > 0)
        {
            // Time.deltaTime을 사용해 프레임과 상관없이 정확한 시간을 뺌
            remainingTime -= Time.deltaTime;
            UpdateTimerUI();
            yield return null; // 다음 프레임까지 대기
        }

        remainingTime = 0;
        UpdateTimerUI();
        timerBtnText.text = "완료";
        isTimerRunning = false;
        // TODO: 여기서 요리가 완료되었다는 띵동! 소리를 재생하면 좋음
    }

    private void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    #endregion

    /// <summary>
    /// 사진 검증 없이 바로 다음 단계로 넘어가는 단순 타격 로직
    /// </summary>
    private void OnDirectAttack()
    {
        Debug.Log("[Battle] 사진 검증 없는 일반 공격! (서버에 통과 보고 로직 필요)");
        // TODO: 단순 공격 성공 후 다음 단계 불러오기 API 연동
    }

    /// <summary>
    /// [핵심] 검증 성공 후 팝업에서 호출되는 함수입니다.
    /// 다음 단계로 넘어갈지, 전투를 종료할지 결정합니다.
    /// </summary>
    /// <param name="nextStepOrder">서버가 알려준 다음 단계 번호</param>
    public void HandleNextStep(int nextStepOrder)
    {
        // 1. 만약 다음 단계 번호가 총 단계 수보다 크다면? -> 모든 요리 완료(승리)!
        if (nextStepOrder > totalSteps)
        {
            ShowVictoryResult();
            return;
        }

        // 2. 아직 단계가 남았다면, 서버에서 해당 단계의 상세 정보를 가져옵니다.
        StartCoroutine(RequestNextStepData(nextStepOrder));
    }

    /// <summary>
    /// [GET] /api/v1/monsters/<recipe_id>/steps/<step_order>
    /// 서버에서 새로운 단계의 지시문, 팁, 타이머 정보를 가져와 UI를 갈아끼웁니다.
    /// </summary>
    private IEnumerator RequestNextStepData(int nextStepOrder)
    {
        // 로딩 연출을 여기에 넣으면 좋습니다 (예: 화면 암전 혹은 대기 UI)
        Debug.Log($"[Battle] {nextStepOrder}단계 정보를 가져오는 중...");

        string url = $"{NetworkManager.Instance.BaseUrl}/monsters/{recipeId}/steps/{nextStepOrder}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // CookingStepResponse 규격으로 파싱 (우리가 수정한 total_steps 포함 버전)
                CookingStepResponse response = JsonUtility.FromJson<CookingStepResponse>(request.downloadHandler.text);

                if (response.status == "success")
                {
                    // [재사용] 기존에 만들어둔 초기화 함수를 호출하여 UI를 새 데이터로 갱신!
                    // response.data 안에는 current_step, total_steps, step_detail이 모두 들어있습니다.
                    InitializeBattle(response.data);

                    Debug.Log($"[Battle] {nextStepOrder}단계 전환 완료!");
                }
            }
            else
            {
                Debug.LogError($"[Battle] 단계 전환 실패: {request.error}");
            }
        }
    }

    /// <summary>
    /// 모든 단계를 클리어했을 때 호출되는 승리 연출 함수
    /// </summary>
    private void ShowVictoryResult()
    {
        Debug.Log("<color=cyan>[Victory] 클리어 성공! 몬스터를 성공적으로 정화했습니다! </color>");

        // 1. 체력바를 0으로 만듦 (마지막 타격 확인)
        hpBar.value = 0;

        // 2. 몬스터 일러스트를 정화된 일러스트로 교체
        if (monsterImage != null && purifiedMonsterSprite != null)
        {
            monsterImage.sprite = purifiedMonsterSprite;
            // 약간의 연출: 정화되었다는 느낌을 주도록 하얀색으로 깜빡이는 연출 등을 추가하면 좋음
            monsterImage.color = Color.white;
        }

        // 3. 서버 세션 데이터에서 고정 보상(Gold, Exp)을 가져와 UI에 세팅
        if (NetworkManager.Instance.CurrentSessionData != null)
        {
            var data = NetworkManager.Instance.CurrentSessionData;

            // 결과창 텍스트 업데이트
            totalScoreText.text = $"최종 점수: {accumulatedScore:N0}";
            rewardGoldText.text = $"+ {data.reward_gold:N0} GOLD";
            rewardExpText.text = $"+ {data.reward_exp:N0} EXP";
        }

        // 4. 결과 팝업 활성화
        if (victoryPopupGroup != null)
        {
            victoryPopupGroup.SetActive(true);
            
            // 버튼 연결 (이전 리스너 제거 후 새로 등록)
            lobbyExitButton.onClick.RemoveAllListeners();
            lobbyExitButton.onClick.AddListener(ReturnToLobby);
        }
    }

    /// <summary>
    /// 모든 정화가 끝나고 로비 씬으로 돌아감.
    /// </summary>
    private void ReturnToLobby()
    {
        // NetworkManager에 저장된 세션 데이터를 비워줌 (전투 종료)
        NetworkManager.Instance.CurrentSessionData = null;

        Debug.Log("[Battle] 로비로 귀환합니다.");
        SceneManager.LoadScene("LobbyScene"); // 로비 씬 이름에 맞춰 수정
    }

    public void AddScore(int scoreToAdd)
    {
        accumulatedScore += scoreToAdd;
        scoreText.text = accumulatedScore.ToString("N0"); // 1,000 단위 콤마 찍기
        Debug.Log($"[Battle] 점수 획득! 현재 총점: {accumulatedScore}");
    }

    /// <summary>
    /// 서버 URL로부터 이미지를 다운로드하여 가이드 이미지 UI에 적용합니다.
    /// </summary>
    private IEnumerator LoadGuideImage(string url)
    {
        // 텍스처(Texture) 전용 WebRequest 사용
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // 내려받은 데이터를 Texture2D로 변환
                Texture2D texture = DownloadHandlerTexture.GetContent(request);

                // Texture2D를 UI Image 컴포넌트에 쓸 수 있는 Sprite로 변환
                // (중심점은 중앙 0.5, 0.5)
                Sprite newSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                guideImage.sprite = newSprite;

                // 이미지 비율이 깨지지 않게 하고 싶다면 아래 옵션 사용 (선택)
                // guideImage.preserveAspect = true;

                Debug.Log($"[Battle] 가이드 이미지 로드 완료: {url}");
            }
            else
            {
                Debug.LogError($"[Battle] 이미지 로드 실패 ({url}): {request.error}");
                // 실패 시 기본 이미지로 대체하거나 guideImage를 비활성화하는 처리 추가 가능
            }
        }
    }
}
