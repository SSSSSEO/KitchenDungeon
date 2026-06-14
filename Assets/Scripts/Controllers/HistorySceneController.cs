using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using KitchenDungeon.Models;
using KitchenDungeon.UI.History;
using KitchenDungeon.UI.Popups;
using UnityEngine.SceneManagement;

namespace KitchenDungeon.Controllers
{
    /// <summary>
    /// 전적 보관소 씬의 전체 로직을 총괄합니다. 
    /// 진입 시 전적 API를 비동기 호출하여 스크롤뷰 목록을 동적 생성합니다.
    /// </summary>
    public class HistorySceneController : MonoBehaviour
    {
        [Header("--- 스크롤 뷰 세팅 ---")]
        [Tooltip("Scroll View -> Viewport -> Content 오브젝트 연결")]
        [SerializeField] private Transform scrollContentParent;
        [Tooltip("HistoryItemUI 스크립트가 붙어있는 한 칸짜리 프리팹 오브젝트")]
        [SerializeField] private GameObject historyItemPrefab;

        [Header("--- 팝업 및 이동 버튼 ---")]
        [SerializeField] private HistoryDetailPopup detailPopup; // 상세 팝업 컴포넌트 연결
        [SerializeField] private Button backToLobbyButton;       // 로비로 귀환 버튼

        // 동적 생성된 오브젝트 청소용 리스트
        private List<GameObject> spawnedItems = new List<GameObject>();

        private void Start()
        {
            // 로비(몬스터 목록)로 돌아가는 뒤로가기 버튼 바인딩
            if (backToLobbyButton != null)
                backToLobbyButton.onClick.AddListener(OnBackToLobbyClicked);

            // 씬이 켜지자마자 현재 로그인된 유저 ID 기준 전적 요청 시동!
            int userId = NetworkManager.Instance.UserId;
            if (userId != -1)
            {
                StartCoroutine(RequestUserHistory(userId));
            }
            else
            {
                Debug.LogError("[History] 유저 세션 정보가 없어 전적을 로드할 수 없습니다.");
            }
        }

        /// <summary>
        /// [GET] /api/v1/users/<user_id>/history API를 호출하여 전체 전적 리스트를 받아옵니다.
        /// </summary>
        private IEnumerator RequestUserHistory(int userId)
        {
            string url = $"{NetworkManager.Instance.BaseUrl}/users/{userId}/history";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    HistoryResponse response = JsonUtility.FromJson<HistoryResponse>(request.downloadHandler.text);

                    if (response.status == "success" && response.data != null)
                    {
                        // 🌟 전적 데이터 리스트뷰 동적 생성 시작!
                        PopulateHistoryList(response.data.history_list);
                    }
                }
                else
                {
                    Debug.LogError($"[History API Error] 전적 리스트 요청 실패: {request.error}");
                }
            }
        }

        /// <summary>
        /// 서버에서 받아온 리스트 배열 데이터 개수만큼 루프를 돌며 프리팹을 인스턴스화합니다.
        /// </summary>
        private void PopulateHistoryList(List<HistoryItemData> list)
        {
            // 기존에 생성되어 혹시 남아있을 수 있는 잔상 오브젝트 제거 (리셋)
            foreach (var item in spawnedItems)
            {
                if (item != null) Destroy(item);
            }
            spawnedItems.Clear();

            if (scrollContentParent == null || historyItemPrefab == null) return;

            // 최신순 배열 루프 가동
            foreach (var data in list)
            {
                // 프리팹 생성 후 스크롤뷰 Content 자식으로 강제 이식
                GameObject go = Instantiate(historyItemPrefab, scrollContentParent);
                spawnedItems.Add(go);

                // 프리팹에서 UI 제어 컴포넌트 추출
                HistoryItemUI itemUi = go.GetComponent<HistoryItemUI>();
                if (itemUi != null)
                {
                    // 🌟 핵심: 한 칸짜리 UI를 세팅하면서 "나 클릭되면 이 람다식 실행해줘!" 하고 상세 팝업 오픈 함수를 콜백으로 전달!
                    itemUi.Setup(data, (selectedData) =>
                    {
                        if (detailPopup != null)
                        {
                            detailPopup.OpenPopup(selectedData);
                        }
                    });
                }
            }

            Debug.Log($"[History] 총 {list.Count}건의 전적 리스트 배치가 완료되었습니다.");
        }

        private void OnBackToLobbyClicked()
        {
            Debug.Log("[History] 로비(MonsterScene)로 귀환합니다.");
            SceneManager.LoadScene("MonsterScene"); // 몬스터 리스트 로비 씬 이름으로 변경 가능
        }
    }
}