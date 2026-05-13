using System.Collections;
using System.Text;
using TMPro;                // 유니티의 고해상도 텍스트 시스템 (TextMeshPro)
using UnityEngine;
using UnityEngine.Networking; // 서버와 HTTP 통신을 하기 위한 모듈
using UnityEngine.UI;         // 버튼 등 기본 UI 컴포넌트 제어용
using KitchenDungeon.Models;  // 우리가 만든 데이터 구조(UserDto)를 가져옴
using UnityEngine.SceneManagement; // 씬 전환이 필요할 경우 사용

/// <summary>
/// 신규 요리사 등록(회원가입) 기능을 담당하는 클래스
/// </summary>
public class SignupController : MonoBehaviour
{
    [Header("--- UI 연결 (팀원이 할당할 곳) ---")]
    [Tooltip("로그인 아이디 입력창")]
    [SerializeField] private TMP_InputField usernameInput;

    [Tooltip("비밀번호 입력창")]
    [SerializeField] private TMP_InputField passwordInput;

    [Tooltip("게임 내에서 사용할 이름 입력창")]
    [SerializeField] private TMP_InputField nicknameInput;

    [Tooltip("가입 완료 버튼")]
    [SerializeField] private Button signupButton;

    [Tooltip("상태 메시지(성공/오류) 출력용 텍스트")]
    [SerializeField] private TextMeshProUGUI statusText;

    // [추가] 취소 버튼 변수
    [Tooltip("로그인 화면으로 돌아가는 취소 버튼")]
    [SerializeField] private Button cancelButton;

    [Header("--- 화면 관리 (패널 전환 시 사용) ---")]
    [Tooltip("회원가입 창 오브젝트 (현재 창)")]
    [SerializeField] private GameObject signupPanel;

    [Tooltip("로그인 창 오브젝트 (돌아갈 창)")]
    [SerializeField] private GameObject loginPanel;

    [Header("--- 서버 설정 ---")]
    [Tooltip("서버의 회원가입 엔드포인트 경로")]
    [SerializeField] private string signupEndpoint = "/register"; // 뒷부분 경로만 적어둠

    /// <summary>
    /// 스크립트가 시작될 때 버튼에 이벤트를 바인딩함
    /// </summary>
    private void Start()
    {
        // 버튼 오브젝트가 인스펙터에 연결되어 있다면 클릭 리스너 등록
        if (signupButton != null) signupButton.onClick.AddListener(OnSignupButtonClicked);


        // [추가] 취소 버튼 이벤트 연결
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelButtonClicked);
    }

    /// <summary>
    /// [추가] 취소 버튼 클릭 시 실행되는 함수
    /// </summary>
    private void OnCancelButtonClicked()
    {
        Debug.Log("회원가입을 취소하고 초기 화면으로 돌아갑니다.");

        // 방법 1: 만약 로그인과 회원가입이 '다른 씬(Scene)'이라면?
        // SceneManager.LoadScene("LoginScene"); 

        // 방법 2: 만약 한 씬 안에서 '패널(Panel)'만 껐다 켰다 하는 거라면? (더 많이 쓰임)
        if (signupPanel != null && loginPanel != null)
        {
            signupPanel.SetActive(false); // 회원가입 창 끄기
            loginPanel.SetActive(true);   // 로그인 창 켜기
        }
        else
        {
            // 팀원이 패널을 안 꽂아줬을 때를 대비한 경고
            Debug.LogWarning("연결된 UI 패널이 없어 화면을 전환할 수 없습니다!");
        }
    }

    /// <summary>
    /// [1단계] 회원가입 버튼 클릭 시 호출되는 함수
    /// </summary>
    private void OnSignupButtonClicked()
    {
        // 입력받은 값들의 앞뒤 공백을 제거하고 변수에 저장
        string id = usernameInput.text.Trim();
        string pw = passwordInput.text.Trim();
        string nick = nicknameInput.text.Trim();

        // [유효성 검사] 하나라도 비어있으면 서버 요청을 보내지 않음
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw) || string.IsNullOrEmpty(nick))
        {
            ShowStatusMessage("모든 필드를 입력해주세요!", Color.red);
            return;
        }

        // [통신 준비] 버튼을 비활성화하여 중복 클릭(더블 요청)을 방지
        signupButton.interactable = false;
        ShowStatusMessage("서버와 통신 중...", Color.yellow);

        // [2단계] 코루틴을 통해 비동기 서버 통신 시작
        StartCoroutine(SendSignupRequest(id, pw, nick));
    }

    /// <summary>
    /// [3단계] 실제 서버 API에 데이터를 전송하는 과정
    /// </summary>
    private IEnumerator SendSignupRequest(string username, string password, string nickname)
    {
        // [중요] NetworkManager에서 베이스 URL을 가져와서 엔드포인트와 합침
        string url = $"{NetworkManager.Instance.BaseUrl}{signupEndpoint}";

        // 1. 서버 전용 데이터 객체(DTO) 생성 및 데이터 채우기
        SignupRequest requestData = new SignupRequest
        {
            username = username,
            password = password,
            nickname = nickname
        };

        // 2. 객체를 JSON 형태의 문자열로 변환 (직렬화)
        string jsonData = JsonUtility.ToJson(requestData);

        // 3. UnityWebRequest를 이용한 POST 요청 설정 (합쳐진 url 사용)
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            // 전송할 JSON 데이터를 바이트 배열로 인코딩
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            // 전송 처리 핸들러 설정
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            // 응답 데이터 처리 핸들러 설정
            request.downloadHandler = new DownloadHandlerBuffer();

            // HTTP 헤더 설정: 서버에게 우리가 보내는 데이터가 JSON임을 알림
            request.SetRequestHeader("Content-Type", "application/json");

            // 서버 응답이 올 때까지 프로그램이 멈추지 않고 여기서 대기 (비동기)
            yield return request.SendWebRequest();

            // 통신 완료 후 버튼 다시 활성화
            signupButton.interactable = true;

            // 4. 서버 응답 결과 분석
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                // 네트워크가 끊겼거나 서버 주소가 잘못된 경우
                ShowStatusMessage("서버 연결에 실패했습니다.", Color.red);
            }
            else
            {
                // 서버로부터 응답 문자열을 가져옴
                string responseText = request.downloadHandler.text;

                // JSON 문자열을 C# 객체로 다시 변환 (역직렬화)
                SignupResponse response = JsonUtility.FromJson<SignupResponse>(responseText);

                // HTTP 상태 코드 201 (Created) 및 서버 성공 메시지 확인
                if (request.responseCode == 201 && response.status == "success")
                {
                    // 유저에게 성공 메시지를 보여주고 잠시 뒤 화면을 전환함
                    ShowStatusMessage($"가입 성공! {nickname} 요리사님, 환영합니다.", Color.green);

                    // 1.5초 뒤에 GoToLogin 함수를 실행해라!
                    Invoke("GoToLogin", 1.5f);
                }
                else
                {
                    // 중복 아이디(409)나 기타 서버 에러 메시지 출력
                    ShowStatusMessage(response.message, Color.red);
                }
            }
        }
    }

    /// <summary>
    /// 회원가입 완료 후 로그인 화면으로 유도하는 함수
    /// </summary>
    private void GoToLogin()
    {
        Debug.Log("[Signup] 로그인 화면으로 이동합니다.");

        // [방법 1] 씬을 통째로 바꿀 때 (SceneManager 사용)
        // SceneManager.LoadScene("LoginScene");

        // [방법 2] 한 씬 안에서 UI 창(Panel)만 바꿀 때
        if (loginPanel != null && signupPanel != null)
        {
            signupPanel.SetActive(false); // 가입창 끄고
            loginPanel.SetActive(true);   // 로그인창 켜기
        }
    }

    /// <summary>
    /// UI 텍스트에 결과 메시지를 표시하는 헬퍼 함수
    /// </summary>
    private void ShowStatusMessage(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
        // 개발 중 확인을 위해 콘솔에도 기록
        Debug.Log($"[Signup System] {message}");
    }
}