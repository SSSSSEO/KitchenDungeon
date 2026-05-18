using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using KitchenDungeon.Models; 

/// <summary>
/// 전투 화면 하단에 위치하여 사진 촬영 및 AI 검증 결과를 담당하는 부분 팝업입니다.
/// 한 영역 안에서 [사진 입력] -> [검증 중] -> [결과 확인] 상태를 전환하며 보여줍니다.
/// </summary>
public class CookingVerifyPopup : MonoBehaviour
{
    [Header("--- UI 상태 그룹 (State Groups) ---")]
    [Tooltip("사진을 찍거나 선택하는 입력 화면 그룹")]
    [SerializeField] private GameObject inputGroup;
    [Tooltip("AI의 판정 결과(피드백/점수)를 보여주는 결과 화면 그룹")]
    [SerializeField] private GameObject resultGroup;
    [Tooltip("촬영/갤러리 선택 창")]
    [SerializeField] private GameObject uploadSelectionGroup; 

    [Header("--- 입력 모드 (Input Mode) UI ---")]
    [SerializeField] private Button uploadMainButton;    // 메인 업로드 버튼
    [SerializeField] private Button selectCameraButton;  // 선택창 내 촬영 버튼
    [SerializeField] private Button selectGalleryButton; // 선택창 내 갤러리 버튼
    [SerializeField] private Button closeSelectionBtn;   // 선택창 닫기 버튼

    [SerializeField] private TextMeshProUGUI guideTitleText; // 가이드 제목
    [SerializeField] private Image photoPreview;            // 찍은 사진 미리보기 칸
    [SerializeField] private Button attackButton;           // 최종 [공격하기] 버튼
    [SerializeField] private TextMeshProUGUI attackBtnText;  // 버튼 텍스트 (판정 중 시 텍스트 변경용)

    [Header("--- 결과 모드 (Result Mode) UI ---")]
    [SerializeField] private TextMeshProUGUI successStatusText; // [추가] 성공 여부 상태 텍스트
    [SerializeField] private TextMeshProUGUI aiFeedbackText; // AI의 한마디 (ai_feedback)
    [SerializeField] private TextMeshProUGUI stepScoreText;  // 이번 단계 점수
    [SerializeField] private Button confirmBtn;             // 성공 시 [확인] 버튼
    [SerializeField] private Button retryBtn;               // 실패 시 [재시도] 버튼

    [Header("--- 데이터 및 시연용 설정 ---")]
    [SerializeField] private Texture2D testTexture; // 에디터 시연용 더미 이미지
    [SerializeField] private TextMeshProUGUI debugScreenText; // 핸드폰 디버그 용

    // 내부 통신용 데이터
    private int recipeId;
    private int stepOrder;
    private Texture2D capturedTexture; // 촬영된 이미지 저장용 (미리보기 및 전송용)

    private void Start()
    {
        LogToScreen("시작");
        // 메인 업로드 버튼 누르면 선택 팝업 띄움
        uploadMainButton.onClick.AddListener(() => uploadSelectionGroup.SetActive(true));
        closeSelectionBtn.onClick.AddListener(() => uploadSelectionGroup.SetActive(false));
        
        // 선택창 내 버튼들
        selectCameraButton.onClick.AddListener(OnCameraClick);
        selectGalleryButton.onClick.AddListener(OnGalleryClick);

        // 버튼 이벤트 바인딩
        attackButton.onClick.AddListener(OnVerifyRequest);
        retryBtn.onClick.AddListener(() => SwitchState(true)); // 재시도 시 다시 입력 모드로
        confirmBtn.onClick.AddListener(OnConfirmSuccess);

        // 씬 시작 시에는 비활성 상태
        uploadSelectionGroup.SetActive(false);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// [핵심] 시연 환경(에디터 vs 모바일)에 따라 사진 선택 방식을 결정합니다.
    /// </summary>
    private void OnCameraClick()
    {
        // 👇 [추가] 에디터 테스트 시에도 카메라 버튼 누르면 선택창이 바로 닫히도록 설정
        if (uploadSelectionGroup != null) 
        {
            uploadSelectionGroup.SetActive(false);
        }

#if UNITY_EDITOR
        // 1. 유니티 에디터 시연: 인스펙터에 넣은 테스트 이미지를 즉시 사용
        Debug.Log("<color=orange>[Demo] 에디터 모드: 테스트 이미지를 로드합니다.</color>");
        if (testTexture != null) OnPhotoCaptured(testTexture);
        else Debug.LogError("인스펙터의 Test Texture가 비어있습니다!");
#else
        // 2. 실제 모바일 기기: 카메라 실행 (NativeCamera 플러그인 필요)
        TakePhoto();
#endif
    }

    private void OnGalleryClick()
    {
        uploadSelectionGroup.SetActive(false);
#if UNITY_EDITOR
        if (testTexture != null) OnPhotoCaptured(testTexture);
#else
        PickGalleryImage();
#endif
    }

    private void TakePhoto()
    {
        LogToScreen("카메라 실행 시도...");
        NativeCamera.TakePicture((path) =>
        {
            LogToScreen($"카메라 콜백 응답 - 경로: {path}");
            if (path != null)
            {
                // 텍스처 로드 시도
                Texture2D tex = NativeCamera.LoadImageAtPath(path, 1024, markTextureNonReadable: false);
                if (tex != null)
                {
                    LogToScreen($"카메라 사진 로드 성공: {tex.width}x{tex.height}");
                    OnPhotoCaptured(tex);
                }
                else LogToScreen("에러: 카메라 텍스처 로드 실패 (null)");
            }
            else LogToScreen("취소: 사진 촬영 안 함");
        }, 1024);
    }

    private void PickGalleryImage()
    {
        LogToScreen("갤러리 실행 시도...");
        NativeGallery.GetImageFromGallery((path) =>
        {
            LogToScreen($"갤러리 콜백 응답 - 경로: {path}");
            if (path != null)
            {
                // NativeGallery용 로드 함수 사용 (NativeCamera와 혼용해도 되지만 안전하게)
                Texture2D tex = NativeGallery.LoadImageAtPath(path, 1024, markTextureNonReadable: false);
                if (tex != null)
                {
                    LogToScreen($"갤러리 사진 로드 성공: {tex.width}x{tex.height}");
                    OnPhotoCaptured(tex);
                }
                else LogToScreen("에러: 갤러리 텍스처 로드 실패 (null)");
            }
            else LogToScreen("취소: 이미지 선택 안 함");
        }, "사진 선택", "image/*");
    }

    public void OnPhotoCaptured(Texture2D tex)
    {
        capturedTexture = tex;

        if (photoPreview != null)
        {
            photoPreview.gameObject.SetActive(true);
            photoPreview.color = Color.white;
            photoPreview.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            photoPreview.preserveAspect = true;
        
        // 레이아웃 강제 갱신 (이미지가 안 나타날 때 특효약)
        Canvas.ForceUpdateCanvases();

        LogToScreen("UI 프리뷰 할당 완료"); 
        }
        else
        {
            LogToScreen("에러: photoPreview(Image) 컴포넌트 연결 안 됨");
        }

        attackButton.interactable = true;
        LogToScreen($"이미지 로드 완료: {tex.width}x{tex.height}");


        // 👇 [추가] 사진 가져오기 성공했으니 선택 창을 닫아서 이전 팝업이 보이게 합니다!
        if (uploadSelectionGroup != null) 
        {
            uploadSelectionGroup.SetActive(false);
        }
    }

    /// <summary>
    /// BattleController에서 호출하여 검증 창을 엶
    /// </summary>
    public void OpenVerifyMode(int rId, int sOrder, StepDetail detail)
    {
        recipeId = rId;
        stepOrder = sOrder;
        guideTitleText.text = $"<b>미션:</b> {detail.description}";
        SwitchState(true); // 입력 모드로 초기화
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 입력 모드와 결과 모드 사이의 UI 레이아웃을 전환합니다.
    /// </summary>
    private void SwitchState(bool isInput)
    {
        inputGroup.SetActive(isInput);
        resultGroup.SetActive(!isInput);
        uploadSelectionGroup.SetActive(false);

        if (isInput)
        {
            attackButton.interactable = false; // 사진 찍기 전에는 공격 버튼 비활성
            attackBtnText.text = "공격하기";
            photoPreview.sprite = null;       // 이전 사진 클리어
            capturedTexture = null;
        }
    }

    #region [사진 촬영 및 업로드]
    /*
    /// <summary>
    /// (가상) 사진 촬영 혹은 갤러리 선택 완료 시 호출될 함수
    /// 실제 구현 시 NativeGallery 등의 플러그인 콜백에서 실행됩니다.
    /// </summary>
    public void OnPhotoCaptured(Texture2D tex)
    {
        if (tex == null)
        {
            LogToScreen("로드된 텍스처가 null입니다!");
            return;
        }
        // [핵심] 유니티 메인 스레드에서만 UI를 수정하도록 보장
        // 사실 NativeCamera는 메인 스레드 콜백을 지원하지만, 
        // 혹시 모를 상황을 대비해 Rect와 Apply를 확실히 해줍니다.
        capturedTexture = tex;
        capturedTexture.filterMode = FilterMode.Bilinear;
        capturedTexture.Apply(); // 데이터 적용 확정

        // 텍스처 정보 로그 (폰에서도 확인 가능하게)
        LogToScreen($"사진 로드 성공: {tex.width}x{tex.height}");

        // 스프라이트 생성 시 Rect 정보를 명확히 전달
        Rect rect = new Rect(0, 0, tex.width, tex.height);
        photoPreview.sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f));

        // 버튼 활성화
        attackButton.interactable = true;
    } */

    /// <summary>
    /// [공격하기] 버튼 클릭 시 서버로 사진과 데이터를 쏩니다.
    /// </summary>
    private void OnVerifyRequest()
    {
        StartCoroutine(VerifyRoutine());
    }

    private IEnumerator VerifyRoutine()
    {
        attackButton.interactable = false;
        attackBtnText.text = "AI 판정 중...";
        Debug.Log($"[Verify Request] ID: {recipeId}, Step: {stepOrder}");

        string url = $"{NetworkManager.Instance.BaseUrl}/cooking/verify";

        // 1. MultipartForm 데이터 생성 (텍스트 데이터와 이미지 바이너리 혼합)
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormDataSection("user_id", NetworkManager.Instance.UserId.ToString()));
        formData.Add(new MultipartFormDataSection("recipe_id", recipeId.ToString()));
        formData.Add(new MultipartFormDataSection("step_order", stepOrder.ToString()));

        // 2. 이미지 데이터 압축 및 첨부 (CapturedTexture가 있다고 가정)
        if (capturedTexture != null)
        {
            byte[] imageBytes = capturedTexture.EncodeToJPG(75); // 용량 절약을 위해 75% 퀄리티로 압축
            formData.Add(new MultipartFormFileSection("image", imageBytes, "upload.jpg", "image/jpeg"));
        }

        // 3. 통신 시작
        using (UnityWebRequest request = UnityWebRequest.Post(url, formData))
        {
            // [중요] AI 서버 지연 대비 20초 타임아웃 설정
            request.timeout = 20;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // 응답 데이터를 CookingVerifyResponse 규격으로 파싱
                CookingVerifyResponse res = JsonUtility.FromJson<CookingVerifyResponse>(request.downloadHandler.text);

                if (res.status == "success")
                {
                    // 결과 모드로 화면 전환 및 데이터 뿌리기
                    ShowResult(res.data);
                }
            }
            else
            {
                HandleNetworkError(request);
            }
        }
    }

    private void HandleNetworkError(UnityWebRequest request)
    {
        SwitchState(false);
        successStatusText.text = "ERROR";
        successStatusText.color = Color.gray;

        string errorMsg = "네트워크 오류가 발생했습니다.";
        if (request.result == UnityWebRequest.Result.ConnectionError || request.error.Contains("timeout"))
        {
            errorMsg = "서버 응답 시간 초과!\n다시 시도해 주세요. (Timeout)";
        }

        aiFeedbackText.text = $"<color=red>{errorMsg}</color>\n{request.error}";
        stepScoreText.text = "점수 측정 불가";

        confirmBtn.gameObject.SetActive(false);
        retryBtn.gameObject.SetActive(true);
    }

    private int savedNextStep; // 서버에서 받은 다음 단계 번호 임시 저장
    private int savedScore;

    /// <summary>
    /// 서버로부터 받은 판정 결과(성공/실패, 점수, 피드백)를 결과 패널에 보여줍니다.
    /// </summary>
    private void ShowResult(CookingVerifyData data)
    {
        SwitchState(false); // 결과 모드로 전환

        // 1. 성공 여부에 따른 상태 텍스트 및 색상 변경
        if (data.is_success)
        {
            successStatusText.text = "<size=120%>SUCCESS</size>";
            successStatusText.color = Color.green; // 성공은 초록색!
        }
        else
        {
            successStatusText.text = "<size=120%>FAIL</size>";
            successStatusText.color = Color.red;   // 실패는 빨간색!
        }

        // AI의 한마디 노출 (RichText 사용 가능)
        aiFeedbackText.text = $"<color=#FFD700>\"AI의 피드백 :\"</color>\n{data.feedback}";
        stepScoreText.text = $"이번 단계 점수: <b>{data.score}</b>점";

        savedNextStep = data.next_step; // 다음 단계를 미리 저장해둠
        savedScore = data.score;

        // 성공 여부에 따라 [확인] 혹은 [재시도] 버튼 노출 분기
        confirmBtn.gameObject.SetActive(data.is_success);
        retryBtn.gameObject.SetActive(!data.is_success);
    }

    /// <summary>
    /// [성공 확인] 버튼 클릭 시. 팝업을 닫고 메인 전투 화면에 다음 단계를 요청함.
    /// </summary>
    private void OnConfirmSuccess()
    {
        gameObject.SetActive(false);

        // 메인 컨트롤러를 찾아 다음 단계 처리를 맡김
        CookingBattleController battleCtrl = Object.FindFirstObjectByType<CookingBattleController>();
        if (battleCtrl != null)
        {
            battleCtrl.AddScore(savedScore);
            battleCtrl.HandleNextStep(savedNextStep);
        }
        Debug.Log($"[Verify Request] ID: {recipeId}, Step: {stepOrder}");
        Debug.Log("[Verify] 공격 성공! 다음 단계로 이동합니다.");
    }
    #endregion

    // 로그 찍고 싶을 때마다 사용
    private void LogToScreen(string msg)
    {
        debugScreenText.text += $"\n{msg}";
    }
}

