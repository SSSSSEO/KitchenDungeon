using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking; // 서버와 통신(HTTP)을 하기 위한 엔진
using UnityEngine.UI;         // 버튼(Button) 컴포넌트 제어용
using TMPro;                  // 텍스트메시프로(TextMeshPro) 제어용
using UnityEngine.SceneManagement; // 로그인 성공 후 화면 전환용
using KitchenDungeon.Models;  // 우리가 만든 UserDto 데이터 모델 사용

/// <summary>
/// 로그인 화면의 로직을 담당하는 컨트롤러 클래스
/// </summary>
public class LoginController : MonoBehaviour
{
    [Header("--- UI 연결 (인스펙터에서 드래그) ---")]
    [Tooltip("유저 아이디를 입력받는 인풋 필드")]
    [SerializeField] private TMP_InputField usernameInput;

    [Tooltip("비밀번호를 입력받는 인풋 필드")]
    [SerializeField] private TMP_InputField passwordInput;

    [Tooltip("로그인 실행 버튼")]
    [SerializeField] private Button loginButton;

    [Tooltip("로그인 결과 메시지를 보여줄 하단 텍스트")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("--- 서버 주소 ---")]
    [Tooltip("서버의 로그인 엔드포인트 경로")]
    [SerializeField] private string loginEndpoint = "/login"; // 뒷부분만 적어둠

    /// <summary>
    /// 게임 오브젝트가 활성화될 때 한 번 실행됨
    /// </summary>
    private void Start()
    {
        // 버튼이 정상적으로 할당되었다면, 클릭 이벤트를 등록함
        if (loginButton != null)
        {
            // AddListener를 통해 버튼 클릭 시 OnLoginButtonClicked 함수가 실행되게 함
            loginButton.onClick.AddListener(OnLoginButtonClicked);
        }
    }

    /// <summary>
    /// [1단계] 로그인 버튼 클릭 시 호출 (입력값 검증)
    /// </summary>
    private void OnLoginButtonClicked()
    {
        // 입력창의 양 끝 공백을 제거한 뒤 텍스트를 가져옴
        string id = usernameInput.text.Trim();
        string pw = passwordInput.text.Trim();

        // [방어 코드] 아이디나 비밀번호가 비어있으면 서버에 보내지도 않고 컷!
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
        {
            ShowStatusText("아이디와 비밀번호를 모두 입력해주세요.", Color.red);
            return;
        }

        // [2단계] 실제 통신을 수행하는 코루틴 실행
        StartCoroutine(RequestLogin(id, pw));
    }

    /// <summary>
    /// [3단계] 실제 서버와 데이터를 주고받는 비동기 처리 과정 (코루틴)
    /// </summary>
    private IEnumerator RequestLogin(string id, string pw)
    {
        // 통신 중에는 유저가 버튼을 또 누르지 못하게 비활성화 (중복 요청 방지)
        loginButton.interactable = false;
        ShowStatusText("서버에 접속 중입니다...", Color.yellow);

        // [중요] NetworkManager에서 베이스 URL을 가져와서 엔드포인트와 합침
        string fullUrl = $"{NetworkManager.Instance.BaseUrl}{loginEndpoint}";

        // 1. 서버에 보낼 데이터를 DTO 객체에 담음
        LoginRequest requestBody = new LoginRequest { username = id, password = pw };

        // 2. DTO 객체를 JSON 문자열로 변환 (직렬화)
        string json = JsonUtility.ToJson(requestBody);

        // 3. HTTP POST 요청 설정 (합쳐진 fullUrl 사용)
        using (UnityWebRequest request = new UnityWebRequest(fullUrl, "POST"))
        {
            // JSON 데이터를 바이트 배열로 변환하여 업로드 핸들러에 할당
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);

            // 서버로부터 응답을 받아올 다운로드 핸들러 설정
            request.downloadHandler = new DownloadHandlerBuffer();

            // HTTP 헤더 설정: "우리가 보내는 건 JSON 데이터다"라고 서버에 알려줌
            request.SetRequestHeader("Content-Type", "application/json");

            // 서버의 응답이 올 때까지 여기서 대기 (yield return)
            yield return request.SendWebRequest();

            // 통신 끝났으므로 버튼 다시 활성화
            loginButton.interactable = true;

            // 4. 응답 결과에 따른 처리
            if (request.result == UnityWebRequest.Result.Success)
            {
                // [성공] 서버 응답 코드 200번대
                // 서버가 보내준 JSON 텍스트를 LoginResponse 객체로 변환 (역직렬화)
                LoginResponse response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);

                if (response.status == "success")
                {
                    ShowStatusText(response.message, Color.green);

                    // 1. 서버가 준 유저 데이터(ID, 닉네임, 골드 등)를 NetworkManager 싱글톤에 저장
                    // 이제 기기에 ID가 기록되어 앱을 껐다 켜도 자동 로그인이 가능해짐!
                    NetworkManager.Instance.SaveLoginData(response.data);

                    Debug.Log($"[Login] 세션 저장 완료: {response.data.nickname} (ID: {response.data.user_id})");

                    // 2. 1초 뒤에 로비 화면으로 이동
                    Invoke("GoToLobby", 1.0f);
                }
            }
            else
            {
                // [실패] 서버 응답 코드 400~500번대 혹은 네트워크 에러
                HandleErrorResponse(request);
            }
        }
    }

    /// <summary>
    /// 서버 에러 발생 시 응답 JSON을 파싱하여 메시지를 띄워줌
    /// </summary>
    private void HandleErrorResponse(UnityWebRequest request)
    {
        try
        {
            // 서버가 준 에러 JSON에서 message 필드만 추출 시도
            BaseResponse errorRes = JsonUtility.FromJson<BaseResponse>(request.downloadHandler.text);
            ShowStatusText(errorRes.message, Color.red);
        }
        catch
        {
            // JSON 파싱 자체가 실패했을 때 (서버가 아예 죽어있는 경우 등)
            ShowStatusText("서버와의 연결이 원활하지 않습니다.", Color.red);
        }
    }

    /// <summary>
    /// UI 텍스트에 메시지를 표시하고 색상을 변경함
    /// </summary>
    private void ShowStatusText(string msg, Color col)
    {
        if (statusText != null)
        {
            statusText.text = msg;
            statusText.color = col;
        }
    }

    /// <summary>
    /// 다음 씬으로 이동 (팀원이 씬 이름을 정하면 그때 연동)
    /// </summary>
    private void GoToLobby()
    {
        Debug.Log("[Login] 로비 씬으로 이동합니다.");

        // 씬 매니저를 통해 실제 게임의 메인 로비로 이동
        // 주의: 유니티 Build Settings에 이 씬이 등록되어 있어야 해!
        SceneManager.LoadScene("LobbyScene");
    }
}