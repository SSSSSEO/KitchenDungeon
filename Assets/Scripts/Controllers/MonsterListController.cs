using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using KitchenDungeon.Models;
using KitchenDungeon.UI.Popups;
using UnityEngine.SceneManagement;

/// <summary>
/// 특정 스테이지 내의 몬스터(요리) 목록을 버튼으로 내비게이션하고, 
/// 서버 상태에 따라 일러스트와 UI를 갱신하는 클래스.
/// </summary>
public class MonsterListController : MonoBehaviour
{
    [Header("--- 상단 HUD UI (유저 스탯) ---")]
    [Tooltip("NetworkManager에 저장된 유저 닉네임을 표시")]
    [SerializeField] private TextMeshProUGUI nicknameText;
    [Tooltip("보유 골드를 표시")]
    [SerializeField] private TextMeshProUGUI goldText;
    [Tooltip("유저의 현재 레벨 표시")]
    [SerializeField] private TextMeshProUGUI levelText;
    [Tooltip("유저의 현재 경험치 양 표시")]
    [SerializeField] private TextMeshProUGUI expText;

    [Header("--- 몬스터 선택 UI 요소 ---")]
    [Tooltip("현재 선택된 레시피(몬스터)의 이름 표시")]
    [SerializeField] private TextMeshProUGUI monsterNameText;
    [Tooltip("중앙에 표시될 몬스터 일러스트 이미지 컴포넌트")]
    [SerializeField] private Image monsterIllustrationDisp;
    [Tooltip("상태(정화/오염/잠김)를 텍스트로 안내하는 영역")]
    [SerializeField] private TextMeshProUGUI monsterStatusText;

    [Header("--- 몬스터 일러스트 데이터 (Sprite) ---")]
    [Tooltip("정화 완료(PURIFIED) 상태일 때 보여줄 깨끗한 요리 이미지 리스트")]
    [SerializeField] private List<Sprite> purifiedSprites = new List<Sprite>();

    [Tooltip("오염 상태(CORRUPTED/LOCKED)일 때 보여줄 기괴한 요리 이미지 리스트")]
    [SerializeField] private List<Sprite> corruptedSprites = new List<Sprite>();

    [Header("--- 리스트 내비게이션 버튼 ---")]
    [Tooltip("상위 단계(Index +)의 몬스터로 이동하는 버튼")]
    [SerializeField] private Button upButton;
    [Tooltip("하위 단계(Index -)의 몬스터로 이동하는 버튼")]
    [SerializeField] private Button downButton;

    [Header("--- 하단 상호작용 버튼 ---")]
    [Tooltip("몬스터의 상세 정보(팝업)를 확인하는 버튼")]
    [SerializeField] private Button infoButton;
    [Tooltip("버튼 내 텍스트 (상태에 따라 '정화하기' 등으로 변경)")]
    [SerializeField] private TextMeshProUGUI infoButtonText;

    [Header("--- 팝업 연결 ---")]
    [SerializeField] private MonsterInfoPopup infoPopup; // 새로 만든 팝업 연결

    // 👇 [스프린트 2 추가] 레벨업 연출용 팝업 (아직 제작 전이므로 하이어라키의 비활성 GameObject를 연결할 예정)
    [Header("--- 스프린트 2 신규 팝업 ---")]
    [Tooltip("전투 종료 후 레벨업 발생 시 로비에서 활성화할 축하 팝업 오브젝트")]
    [SerializeField] private GameObject levelUpPopup;

    // 서버에서 받아온 몬스터 원본 데이터를 보관하는 리스트
    private List<MonsterData> monsterList = new List<MonsterData>();
    // 현재 유저가 보고 있는 몬스터의 리스트 인덱스 (0부터 시작)
    private int currentMonsterIndex = 0;
    // 현재 진입한 스테이지 고유 번호 (추후 씬 전환 시 전달받아야 함)
    private int currentStageId = 1;

    private void Start()
    {
        // [수정] NetworkManager에서 선택된 스테이지 ID를 가져옴
        currentStageId = NetworkManager.Instance.SelectedStageId;

        // 1. 씬 시작 시 NetworkManager에 저장된 최신 유저 정보로 상단 UI 갱신
        UpdateUserStatsUI();

        // 2. 버튼 클릭 리스너 등록 (Delegate 방식)
        if (upButton != null) upButton.onClick.AddListener(OnUpButtonClicked);
        if (downButton != null) downButton.onClick.AddListener(OnDownButtonClicked);
        if (infoButton != null) infoButton.onClick.AddListener(OnInfoButtonClicked);

        // 3. 서버에 해당 스테이지의 몬스터 리스트 요청 시작
        StartCoroutine(RequestMonsterList());

        // 👇 [스프린트 2 추가] 유저 스탯이 갱신된 직후, 전투 씬에서 넘어온 레벨업 예약이 있는지 체크!
        CheckAndTriggerLevelUp();
    }

    /// <summary>
    /// [API 통신] 서버로부터 해당 스테이지의 몬스터 데이터와 유저의 해금 상태를 가져옴
    /// </summary>
    private IEnumerator RequestMonsterList()
    {
        // 서버 엔드포인트: /api/v1/stages/{stage_id}/monsters?user_id={user_id}
        string url = $"{NetworkManager.Instance.BaseUrl}/stages/{currentStageId}/monsters?user_id={NetworkManager.Instance.UserId}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // JSON 데이터를 MonsterListResponse 객체로 역직렬화(Parsing)
                MonsterListResponse response = JsonUtility.FromJson<MonsterListResponse>(request.downloadHandler.text);

                if (response.status == "success" && response.data != null)
                {
                    monsterList = response.data;
                    currentMonsterIndex = 0; // 목록을 새로 받았으니 첫 번째 몬스터부터 노출
                    UpdateMonsterDisplay();   // 화면 그리기
                }
            }
            else
            {
                Debug.LogError($"[MonsterAPI] 몬스터 목록 조회 실패: {request.error}");
            }
        }
    }

    /// <summary>
    /// 서버에서 내려준 visual_state를 기반으로 이미지, 텍스트, 버튼의 활성화 상태를 결정함
    /// </summary>
    private void UpdateMonsterDisplay()
    {
        // 데이터가 없는 경우를 대비한 방어 코드
        if (monsterList.Count == 0 || currentMonsterIndex >= monsterList.Count) return;

        MonsterData current = monsterList[currentMonsterIndex];
        monsterNameText.text = current.recipe_name;

        // [핵심] 리스트 인덱스가 아니라 recipe_id로 이미지를 직접 로드!
        string stateSuffix = (current.visual_state == "PURIFIED") ? "purified" : "corrupted";
        string spritePath = $"Monsters/monster_{current.recipe_id}_{stateSuffix}";

        Sprite loadedSprite = Resources.Load<Sprite>(spritePath);
        if (loadedSprite != null)
        {
            monsterIllustrationDisp.sprite = loadedSprite;
        }

        // --- 상태별 UI 처리 ---
        switch (current.visual_state)
        {
            case "PURIFIED":
                monsterStatusText.text = "<color=green>정화 완료</color>";
                monsterIllustrationDisp.color = Color.white;
                infoButton.interactable = true;
                infoButtonText.text = "재도전";
                break;

            case "CORRUPTED":
                monsterStatusText.text = "<color=red>오염 상태</color>";
                monsterIllustrationDisp.color = Color.white;
                infoButton.interactable = true;
                infoButtonText.text = "정찰하기";
                break;

            case "LOCKED":
                monsterStatusText.text = "잠김";
                monsterIllustrationDisp.color = Color.black; // 실루엣 처리
                monsterNameText.text = "???";
                infoButton.interactable = false;
                infoButtonText.text = "접근불가";
                break;
        }

        // --- 화살표 버튼의 활성화 상태 제어 (범위 밖으로 나가지 않게) ---
        // 위로(상위) 버튼: 현재 인덱스가 리스트 끝보다 작을 때만 활성화
        upButton.interactable = (currentMonsterIndex < monsterList.Count - 1);
        // 아래로(하위) 버튼: 현재 인덱스가 0보다 클 때만 활성화
        downButton.interactable = (currentMonsterIndex > 0);
    }

    // --- 버튼 이벤트 핸들러 ---

    private void OnUpButtonClicked()
    {
        // 리스트의 마지막 원소 전까지만 인덱스 증가 가능
        if (currentMonsterIndex < monsterList.Count - 1)
        {
            currentMonsterIndex++;
            UpdateMonsterDisplay();
        }
    }

    private void OnDownButtonClicked()
    {
        // 0번(첫 번째) 몬스터 전까지만 인덱스 감소 가능
        if (currentMonsterIndex > 0)
        {
            currentMonsterIndex--;
            UpdateMonsterDisplay();
        }
    }

    /// <summary>
    /// 하단 메인 버튼 클릭 시 호출 (PURIFIED나 CORRUPTED 상태일 때만 눌림)
    /// </summary>
    private void OnInfoButtonClicked()
    {
        // 현재 리스트에서 보고 있는 몬스터 데이터를 가져옴
        MonsterData currentMonster = monsterList[currentMonsterIndex];

        // 팝업 스크립트에 데이터를 넘겨주며 팝업을 열라고 명령함
        if (infoPopup != null)
        {
            MonsterData current = monsterList[currentMonsterIndex];
            infoPopup.OpenPopup(current.recipe_id, current.recipe_name);
        }
        else
        {
            Debug.LogWarning("MonsterInfoPopup 스크립트가 연결되지 않았습니다!");
        }
    }

    /// <summary>
    /// 로비와 동일하게 상단 HUD 정보를 실시간으로 갱신하는 함수
    /// </summary>
    public void UpdateUserStatsUI()
    {
        var net = NetworkManager.Instance;
        if (nicknameText != null) nicknameText.text = net.Nickname;
        if (goldText != null) goldText.text = net.TotalGold.ToString("N0"); // 숫자에 쉼표 추가
        if (levelText != null) levelText.text = $"{net.UserLevel}";
        if (expText != null) expText.text = $"{net.CurrentExp}";
    }

    public void OnBackButtonClicked()
    {
        Debug.Log("로비 화면으로 돌아갑니다!");
        SceneManager.LoadScene("LobbyScene");
    }

    /// <summary>
    /// [스프린트 2 추가] NetworkManager를 검사하여 레벨업 예약이 걸려있다면 축하 팝업을 화면에 노출합니다.
    /// </summary>
    private void CheckAndTriggerLevelUp()
    {
        // 우체통에 레벨업 편지가 와 있다면?
        if (NetworkManager.Instance.IsLevelUpPending)
        {
            // 1. 다음 로비 진입 때 또 뜨면 안 되므로 플래그부터 즉시 꺼버림 (중복 차단 안전장치)
            NetworkManager.Instance.IsLevelUpPending = false;

            // 2. 연결된 레벨업 팝업 오브젝트 강제 활성화!
            if (levelUpPopup != null)
            {
                levelUpPopup.SetActive(true);

                // 💡 [팁] 나중에 레벨업 팝업 전역 스크립트(LevelUpPopup.cs)를 따로 커스텀하게 만들면 
                // 아래처럼 컴포넌트를 찾아 최신 레벨 정보를 파라미터로 넘겨주며 열어주면 완벽해!
                // levelUpPopup.GetComponent<LevelUpPopup>().OpenLevelUpWindow(NetworkManager.Instance.UserLevel);

                Debug.Log("<color=cyan>🎉 [Lobby] 유저 레벨업을 감지하여 축하 팝업 연출을 가동합니다!</color>");
            }
            else
            {
                Debug.LogWarning("[Lobby] 레벨업 데이터는 넘어왔으나, 인펙터에 levelUpPopup 오브젝트가 연결되지 않았습니다!");
            }
        }
    }
}