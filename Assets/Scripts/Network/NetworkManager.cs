using UnityEngine;
using KitchenDungeon.Models;

/// <summary>
/// 게임 전체의 통신과 유저 데이터를 관리하는 중앙 통제실
/// </summary>
public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("--- 서버 설정 ---")]
    [Tooltip("서버의 기본 주소. 모든 컨트롤러는 이 주소를 참조하게 함")]
    public string BaseUrl = "http://192.168.219.113:5000/api/v1";

    [Header("--- 유저 세션 정보 ---")]
    public int UserId = -1;
    public string Nickname;
    public int TotalGold;
    public int UserLevel;
    public int CurrentExp;

    private void Awake()
    {
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
    /// 로그인 성공 시 서버 데이터를 메모리와 기기(PlayerPrefs)에 저장
    /// </summary>
    public void SaveLoginData(LoginResponseData data)
    {
        UserId = data.user_id;
        Nickname = data.nickname;
        TotalGold = data.total_gold;
        UserLevel = data.user_level;
        CurrentExp = data.current_exp;

        PlayerPrefs.SetInt("SavedUserID", UserId);
        PlayerPrefs.Save();

        Debug.Log($"[Network] {Nickname} 세션 데이터 저장 완료.");
    }

    /// <summary>
    /// 로그아웃 시 저장된 모든 데이터를 삭제 및 초기화
    /// </summary>
    public void ClearLoginData()
    {
        // 기기 저장 데이터 삭제
        PlayerPrefs.DeleteKey("SavedUserID");
        PlayerPrefs.Save();

        // 메모리 변수들 초기화 (깨끗하게 청소!)
        UserId = -1;
        Nickname = "";
        TotalGold = 0;
        UserLevel = 0;
        CurrentExp = 0;

        Debug.Log("[Network] 로그아웃: 모든 유저 데이터를 초기화했습니다.");
    }
}