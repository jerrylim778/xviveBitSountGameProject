using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Commons.Helpers;

public class TestAnyFunc_PhycisRefer : MonoBehaviour
{
    #region 여러 경로가 포함되어있는 네비 메쉬 로직

    public List<NavMeshSurface> navMeshSurfaces; // 모든 NavMeshSurface 리스트
    public Transform target; // 목표 지점
    public float speed = 3.5f; // 이동 속도
    public float stoppingDistance = 0.5f; // 멈춤 거리

    private NavMeshPath finalPath; // 통합된 최종 경로
    private Vector3[] pathCorners; // 경로 코너
    private int currentCornerIndex = 0; // 현재 경로 코너 인덱스

    void Start_1()
    {
        finalPath = new NavMeshPath();
    }

    void Update_1()
    {
        // 모든 NavMeshSurface를 실시간 베이킹 (성능 최적화를 위해 적절한 조건 추가)
        foreach (var surface in navMeshSurfaces)
        {
            surface.BuildNavMesh();
        }

        // 최단 경로 계산
        CalculateShortestPath();

        // 경로를 따라 이동
        if (pathCorners != null && pathCorners.Length > 0)
        {
            FollowPath();
        }
    }

    

    void CalculateShortestPath()
    {
        float shortestDistance = float.MaxValue;
        Vector3[] bestPath = null;

        foreach (var surface in navMeshSurfaces)
        {
            NavMeshPath tempPath = new NavMeshPath();
            if (NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, tempPath))
            {
                // 유효한 경로가 있는 경우 경로 길이를 계산
                float pathDistance = CalculatePathDistance(tempPath.corners);
                if (pathDistance < shortestDistance)
                {
                    shortestDistance = pathDistance;
                    bestPath = tempPath.corners;
                }
            }
        }

        // 최단 경로 설정
        if (bestPath != null)
        {
            pathCorners = bestPath;
            currentCornerIndex = 0;
        }
    }

    float CalculatePathDistance(Vector3[] corners)
    {
        float distance = 0f;
        for (int i = 0; i < corners.Length - 1; i++)
        {
            distance += Vector3.Distance(corners[i], corners[i + 1]);
        }
        return distance;
    }

    void FollowPath()
    {
        if (currentCornerIndex < pathCorners.Length)
        {
            Vector3 currentCorner = pathCorners[currentCornerIndex];
            Vector3 direction = (currentCorner - transform.position).normalized;

            // 이동
            transform.position += direction * speed * Time.deltaTime;

            // 회전
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);

            // 현재 코너에 도달했는지 확인
            if (Vector3.Distance(transform.position, currentCorner) <= stoppingDistance)
            {
                currentCornerIndex++; // 다음 코너로 이동
            }
        }
    }
    #endregion

    //이어진 경로를 확인만 하면 된다
    //따라서 모든 경로를 임의로 가져온다음
    //목표치까지의 위치에서 pathCorners를 전부 긁어 모은뒤 
    //해당 배열의 첫번째 구간의 vector를 최종적으로 반환하여 
    //거리를 계산을 하면되겠다
    [SerializeField] private List<NavMeshSurface> m_GetNavSurfaces = new();

    private void Initialization()
    {
        //베이킹되어있는것만 긁어온다
        var SurfaceHaveDataList =
        FindObjectsOfType<NavMeshSurface>().ToList().FindAll(x => x.navMeshData != null);
        m_GetNavSurfaces.AddRange(SurfaceHaveDataList);
    }

    public void PointTarget(Vector3 _GetTarget)
    {
        NavMeshPath GetNavPath = new NavMeshPath();
        //NavMesh.CalculatePath(transform.position, _GetTarget, NavMesh.AllAreas, GetNavPath) &&
        //GetNavPath.status == NavMeshPathStatus.PathComplete;
        //if (m_GetNavSurfaces.ISFindCondition(x =>
        //, out NavMeshSurface _GetSurface)
        //_GetTarget
        //if (
        
    }

    private void Start()
    {
        Initialization();
    }

}
