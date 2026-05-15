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

    [Header("--- 입력 모드 (Input Mode) UI ---")]
    [SerializeField] private TextMeshProUGUI guideTitleText; // 가이드 제목
    [SerializeField] private Image photoPreview;            // 찍은 사진 미리보기 칸
    [SerializeField] private Button attackButton;           // 최종 [공격하기] 버튼
    [SerializeField] private TextMeshProUGUI attackBtnText;  // 버튼 텍스트 (판정 중 시 텍스트 변경용)

    [Header("--- 결과 모드 (Result Mode) UI ---")]
    [SerializeField] private TextMeshProUGUI aiFeedbackText; // AI의 한마디 (ai_feedback)
    [SerializeField] private TextMeshProUGUI stepScoreText;  // 이번 단계 점수
    [SerializeField] private Button confirmBtn;             // 성공 시 [확인] 버튼
    [SerializeField] private Button retryBtn;               // 실패 시 [재시도] 버튼

    // 내부 통신용 데이터
    private int recipeId;
    private int stepOrder;
    private Texture2D capturedTexture; // 촬영된 이미지 저장용 (미리보기 및 전송용)

    [SerializeField] private Texture2D testTexture; // 인스펙터에서 아무 이미지나 넣어둬

    private void Start()
    {
        // 버튼 이벤트 바인딩
        attackButton.onClick.AddListener(OnVerifyRequest);
        retryBtn.onClick.AddListener(() => SwitchState(true)); // 재시도 시 다시 입력 모드로
        confirmBtn.onClick.AddListener(OnConfirmSuccess);

        // 씬 시작 시에는 비활성 상태
        gameObject.SetActive(false);
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

        if (isInput)
        {
            attackButton.interactable = false; // 사진 찍기 전에는 공격 버튼 비활성
            attackBtnText.text = "공격하기";
            photoPreview.sprite = null;       // 이전 사진 클리어
        }
    }

    #region [사진 촬영 및 업로드]
    /// <summary>
    /// (가상) 사진 촬영 혹은 갤러리 선택 완료 시 호출될 함수
    /// 실제 구현 시 NativeGallery 등의 플러그인 콜백에서 실행됩니다.
    /// </summary>
    public void OnPhotoCaptured(Texture2D tex)
    {
        capturedTexture = tex;
        // 미리보기 칸에 사진 표시
        photoPreview.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        attackButton.interactable = true; // 사진이 생겼으니 이제 공격 버튼 활성화
    }

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
                Debug.LogError($"[Verify] 통신 에러: {request.error}");
                attackButton.interactable = true;
                attackBtnText.text = "다시 시도";
            }
        }
    }
    #endregion

    private int savedNextStep; // 서버에서 받은 다음 단계 번호 임시 저장

    /// <summary>
    /// 서버로부터 받은 판정 결과(성공/실패, 점수, 피드백)를 결과 패널에 보여줍니다.
    /// </summary>
    private void ShowResult(CookingVerifyData data)
    {
        SwitchState(false); // 결과 모드로 전환

        // AI의 한마디 노출 (RichText 사용 가능)
        aiFeedbackText.text = $"<color=#FFD700>\"AI 명장\"</color>\n{data.feedback}";
        stepScoreText.text = $"이번 단계 점수: <b>{data.score}</b>점";

        savedNextStep = data.next_step; // 다음 단계를 미리 저장해둠

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
            battleCtrl.HandleNextStep(savedNextStep);
        }
        Debug.Log("[Verify] 정화 성공! 다음 단계로 이동합니다.");
    }

    /// <summary>
    /// 테스트용 버튼에 연결할 함수. 사진을 찍은 것처럼 꾸며줌.
    /// </summary>
    public void OnClickTestPhotoSelect()
    {
        if (testTexture != null)
        {
            // 실제 사진이 들어온 것처럼 처리
            OnPhotoCaptured(testTexture);
            Debug.Log("[Test] 가짜 사진이 선택되었습니다. 이제 '공격하기'를 누르세요.");
        }
        else
        {
            Debug.LogError("[Test] 인스펙터에서 testTexture에 이미지를 할당해줘!");
        }
    }
}

