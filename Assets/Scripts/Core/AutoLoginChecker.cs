using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 앱 시작 시 저장된 로그인 정보가 있는지 확인하는 스크립트
/// </summary>
public class AutoLoginChecker : MonoBehaviour
{
    private void Start()
    {
        // 1. 기기 수첩(PlayerPrefs)에서 저장된 UserID가 있는지 확인
        // 만약 저장된 게 없다면 기본값으로 -1을 가져옴
        int savedId = PlayerPrefs.GetInt("SavedUserID", -1);

        if (savedId != -1)
        {
            Debug.Log("저장된 유저 정보를 발견했습니다! 로비로 바로 이동합니다.");

            // [실제 구현 시] 여기서 서버에 "이 ID 유효해?"라고 한 번 물어보는 게 좋지만,
            // 일단은 바로 로비로 보내는 걸로 짬.
            NetworkManager.Instance.UserId = savedId;

            // 로비 씬으로 이동 (씬 이름은 팀원과 상의해서 결정)
            SceneManager.LoadScene("LobbyScene");
        }
        else
        {
            Debug.Log("저장된 정보가 없습니다. 로그인 화면으로 보냅니다.");
            // 로그인/타이틀 화면으로 이동
            SceneManager.LoadScene("LoginScene");
        }
    }
}