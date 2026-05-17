using UnityEngine;
using KitchenDungeon.Models;

/// <summary>
/// 게임 전체의 서버 통신 기본 정보와 현재 로그인된 유저의 상태, 
/// 그리고 씬 간 이동 시 데이터를 전달하는 '데이터 브릿지' 역할을 수행하는 클래스.
/// </summary>
public class NetworkManager : MonoBehaviour
{
    // 어디서든 NetworkManager.Instance로 접근 가능한 싱글톤
    public static NetworkManager Instance { get; private set; }

    [Header("--- 서버 연결 설정 ---")]
    [Tooltip("서버 API의 기본 URL. 로컬 테스트 및 실서버 주소 전환 시 여기만 수정")]
    public string BaseUrl = "http://192.168.219.113:5000/api/v1";

    [Header("--- 유저 세션 정보 (로그인 시 채워짐) ---")]
    public int UserId = -1;
    public string Nickname;
    public int TotalGold;
    public int UserLevel;
    public int CurrentExp;

    [Header("--- 스테이지 ID 보관용 ---")] 
    public int SelectedStageId { get; set; } // 선택된 스테이지 ID를 임시 보관

    [Header("--- 요리 전투 세션 정보 (씬 이동 데이터 브릿지) ---")]
    [Tooltip("팝업에서 시작 버튼을 눌렀을 때 서버에서 받은 첫 단계 정보를 저장. 인게임 씬에서 이를 참조함.")]
    public CookingStepData CurrentSessionData;

    private void Awake()
    {
        // 싱글톤 패턴: 단 하나의 인스턴스만 유지하고 씬이 바뀌어도 파괴되지 않게 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 로그인 성공 시 서버에서 받은 유저 프로필 데이터를 메모리와 기기(PlayerPrefs)에 저장.
    /// </summary>
    public void SaveLoginData(LoginResponseData data)
    {
        UserId = data.user_id;
        Nickname = data.nickname;
        TotalGold = data.total_gold;
        UserLevel = data.user_level;
        CurrentExp = data.current_exp;

        // 앱을 재시작해도 로그인 상태를 유지할 수 있게 유저 ID 저장
        PlayerPrefs.SetInt("SavedUserID", UserId);
        PlayerPrefs.Save();

        Debug.Log($"[Network] {Nickname} 셰프님의 로그인 정보가 저장되었습니다.");
    }

    /// <summary>
    /// 로그아웃 혹은 세션 만료 시 메모리와 기기에 저장된 유저 정보를 초기화.
    /// </summary>
    public void ClearLoginData()
    {
        // 기기 저장 데이터 삭제
        PlayerPrefs.DeleteKey("SavedUserID");
        PlayerPrefs.Save();

        // 모든 변수 초기 상태로 회복
        UserId = -1;
        Nickname = "";
        TotalGold = 0;
        UserLevel = 0;
        CurrentExp = 0;

        // 전투 세션 데이터도 함께 클리어
        CurrentSessionData = null;

        Debug.Log("[Network] 로그아웃: 모든 세션 데이터가 성공적으로 초기화되었습니다.");
    }
}