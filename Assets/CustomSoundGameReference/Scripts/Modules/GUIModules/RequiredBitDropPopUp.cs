using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Commons.Helpers;
using DG.Tweening;

public class RequiredBitDropPopUp : SystemBaseGUIPopUpRequired, I_PopUpPush, I_PopUpPop
{
    // 그리드 셀을 표현하는 클래스
    [System.Serializable]
    public class GridCell
    {
        public GameObject cellObject;
        public GameObject emptyUI;
        public GameObject activeUI;
    }

    [Header("그리드 설정")]
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 9;
    [SerializeField] private Transform gridContainer; // 그리드 UI들의 부모 객체

    [Header("노트 설정")]
    [SerializeField] private float noteSpeed = 1.0f;
    [SerializeField] private float spawnInterval = 0.5f;
    [SerializeField] private bool avoidRecentColumns = true; // 최근 사용된 열 피하기
    [SerializeField] private int columnsToAvoid = 2; // 피할 최근 열 개수

    [Header("방향 설정")]
    [SerializeField] private bool bottomToTop = true; // true: 아래에서 위로, false: 위에서 아래로

    // 그리드 셀 참조를 저장할 2차원 배열
    private GridCell[,] gridCells;
    private bool isPlaying = false;

    // 최근에 사용된 열을 추적하는 큐
    private Queue<int> recentColumns = new Queue<int>();

    // 현재 활성화된 노트들의 열 정보
    private HashSet<int> activeColumns = new HashSet<int>();

    public override void Initlization()
    {
        InitializeGrid();
    }

    public void PushOutEvent(System.Action _AddCallBack = null)
    {
        StartRhythmGame();
    }

    public void PopOutEvent(System.Action _EndCallBack = null)
    {
        var GetCGroup =
        this.gameObject.CheckComnectComponent<CanvasGroup>();
        DOTween.To(() => GetCGroup.alpha, x => GetCGroup.alpha = x, 0f, 0.65f).
        SetEase(Ease.InSine).OnComplete(() => 
        {
            _EndCallBack?.Invoke();
            StopRhythmGame();
        });
    }

    #region 비트 역행 로직

    // 그리드를 초기화하고 셀 객체 참조 저장
    private void InitializeGrid()
    {
        if (gridContainer == null)
        {
            Debug.LogError("그리드 컨테이너가 할당되지 않았습니다!");
            return;
        }

        gridCells = new GridCell[columns, rows];

        // 그리드 컨테이너의 모든 자식을 순회하며 셀 정보 저장
        for (int i = 0; i < gridContainer.childCount; i++)
        {
            // 그리드 셀의 위치를 행과 열 인덱스로 변환
            int col = i % columns;
            int row = i / columns;

            if (row < rows && col < columns)
            {
                Transform cellTransform = gridContainer.GetChild(i);
                GridCell cell = new GridCell
                {
                    cellObject = cellTransform.gameObject
                };

                // 빈 UI와 활성 UI 찾기
                Transform emptyUI = cellTransform.GetChild(0);
                Transform activeUI = cellTransform.GetChild(1);

                if (emptyUI != null) cell.emptyUI = emptyUI.gameObject;
                if (activeUI != null) cell.activeUI = activeUI.gameObject;

                // 모든 셀의 활성 UI를 초기에 비활성화
                if (cell.activeUI != null)
                {
                    cell.activeUI.SetActive(false);
                }

                gridCells[col, row] = cell;
            }
        }

        // 모든 그리드 초기화
        ResetAllCells();
    }

    // 모든 셀 초기화
    public void ResetAllCells()
    {
        if (gridCells == null) return;

        for (int col = 0; col < columns; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                if (gridCells[col, row] != null && gridCells[col, row].activeUI != null)
                {
                    gridCells[col, row].activeUI.SetActive(false);
                }
            }
        }

        // 추적 데이터 초기화
        recentColumns.Clear();
        activeColumns.Clear();
    }

    // 게임 시작
    public void StartRhythmGame()
    {
        if (isPlaying) return;

        isPlaying = true;
        ResetAllCells();
        StartCoroutine(SpawnNotes());
    }

    // 게임 중지
    public void StopRhythmGame()
    {
        isPlaying = false;
        StopAllCoroutines();
        ResetAllCells();
    }

    // 노트 생성 코루틴
    private IEnumerator SpawnNotes()
    {
        while (isPlaying)
        {
            // 사용 가능한 열(현재 활성화되지 않은 열)에서 하나를 선택
            int selectedColumn = GetNextColumn();

            // 유효한 열이 선택되었다면 노트 생성
            if (selectedColumn >= 0)
            {
                // 선택된 열을 활성 열로 표시
                activeColumns.Add(selectedColumn);

                // 최근 사용 열 큐에 추가
                recentColumns.Enqueue(selectedColumn);

                // 최대 피할 열 수를 초과하면 가장 오래된 열 제거
                while (recentColumns.Count > columnsToAvoid)
                {
                    recentColumns.Dequeue();
                }

                // 노트 이동 시작
                StartCoroutine(MoveNoteInColumn(selectedColumn));
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    // 다음 사용할 열 선택 (중복 없이)
    private int GetNextColumn()
    {
        // 사용 가능한 열 목록 생성
        List<int> availableColumns = new List<int>();

        for (int col = 0; col < columns; col++)
        {
            // 현재 활성화된 열이 아니고
            if (!activeColumns.Contains(col))
            {
                // 최근 사용 내역 피하기가 활성화되었을 경우 최근에 사용된 열인지 체크
                bool isRecentColumn = avoidRecentColumns && recentColumns.Contains(col);

                // 최근 사용된 열이 아니면 사용 가능 목록에 추가
                if (!isRecentColumn)
                {
                    availableColumns.Add(col);
                }
            }
        }

        // 사용 가능한 열이 없으면 -1 반환
        if (availableColumns.Count == 0)
        {
            // 모든 열이 활성화 상태인 경우, 최근 사용 내역 무시
            if (activeColumns.Count == columns)
            {
                return -1; // 모든 열이 사용 중이면 이번 턴은 생성하지 않음
            }

            // 최근 사용 내역만 문제라면 그 중에서 선택
            if (avoidRecentColumns)
            {
                // 활성화되지 않은 열 중에서 선택 (최근 사용 내역은 무시)
                for (int col = 0; col < columns; col++)
                {
                    if (!activeColumns.Contains(col))
                    {
                        availableColumns.Add(col);
                    }
                }
            }

            // 여전히 사용 가능한 열이 없으면 -1 반환
            if (availableColumns.Count == 0)
            {
                return -1;
            }
        }

        // 사용 가능한 열 중에서 랜덤하게 하나 선택
        int randomIndex = Random.Range(0, availableColumns.Count);
        return availableColumns[randomIndex];
    }

    // 특정 열에서 노트를 이동시키는 코루틴
    private IEnumerator MoveNoteInColumn(int column)
    {
        int startRow, endRow, direction;

        // 아래에서 위로 이동
        if (bottomToTop)
        {
            startRow = 0;           // 맨 아래 행에서 시작
            endRow = rows;          // 맨 위 행까지
            direction = 1;          // 위로 이동 (인덱스 증가)
        }
        // 위에서 아래로 이동 (필요한 경우)
        else
        {
            startRow = rows - 1;    // 맨 위 행에서 시작
            endRow = -1;            // 맨 아래 행까지
            direction = -1;         // 아래로 이동 (인덱스 감소)
        }

        int currentRow = startRow;

        // 시작 셀의 활성 UI 켜기
        if (gridCells[column, currentRow] != null && gridCells[column, currentRow].activeUI != null)
        {
            gridCells[column, currentRow].activeUI.SetActive(true);
        }

        float waitTime = noteSpeed / rows;

        // 노트가 끝에 도달할 때까지 이동
        while (currentRow != endRow && isPlaying)
        {
            yield return new WaitForSeconds(waitTime);

            // 현재 셀 비활성화
            if (gridCells[column, currentRow] != null && gridCells[column, currentRow].activeUI != null)
            {
                gridCells[column, currentRow].activeUI.SetActive(false);
            }

            // 다음 행으로 이동
            currentRow += direction;

            // 새 위치가 유효한지 확인
            bool isValidRow = bottomToTop ? (currentRow < rows) : (currentRow >= 0);

            // 새 위치가 유효하면 해당 셀 활성화
            if (isValidRow && gridCells[column, currentRow] != null && gridCells[column, currentRow].activeUI != null)
            {
                gridCells[column, currentRow].activeUI.SetActive(true);
            }
        }

        // 이동이 완료되면 활성 열 목록에서 제거
        activeColumns.Remove(column);
    }

    // 사용자 입력 처리 (필요한 경우 구현)
    public void HandleUserInput(int column)
    {
        // 판정 행 (아래에서 위로인 경우 최상단, 위에서 아래로인 경우 최하단)
        int judgmentRow = bottomToTop ? rows - 1 : 0;

        if (gridCells[column, judgmentRow] != null && gridCells[column, judgmentRow].activeUI != null &&
            gridCells[column, judgmentRow].activeUI.activeSelf)
        {
            // 노트 판정 성공 - 점수 증가 등의 로직 추가
            Debug.Log($"Column {column} 노트 성공!");
            gridCells[column, judgmentRow].activeUI.SetActive(false);

            // 이 열의 노트가 판정되었으므로 활성 열 목록에서 제거
            activeColumns.Remove(column);
        }
        else
        {
            // 노트 판정 실패 로직
            Debug.Log($"Column {column} 노트 실패!");
        }
    }

    // 속도 조절 메서드
    public void SetNoteSpeed(float speed)
    {
        noteSpeed = Mathf.Max(0.1f, speed);
    }

    public void SetSpawnInterval(float interval)
    {
        spawnInterval = Mathf.Max(0.1f, interval);
    }

    // 방향 전환 메서드
    public void SetDirection(bool isBottomToTop)
    {
        bottomToTop = isBottomToTop;
    }

    // 최근 열 피하기 설정
    public void SetAvoidRecentColumns(bool avoid, int count = 2)
    {
        avoidRecentColumns = avoid;
        columnsToAvoid = Mathf.Clamp(count, 1, columns - 1);
    }
    #endregion
}