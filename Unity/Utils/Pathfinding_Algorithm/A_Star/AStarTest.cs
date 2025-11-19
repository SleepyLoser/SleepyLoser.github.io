using System.Collections.Generic;
using UnityEngine;

public class PathfindingAlgorithmTest : MonoBehaviour
{
    void Start()
    {
        Vector2 start = new Vector2(0, 0);
        Vector2 end = new Vector2(100, 100);
        AStarNode[ , ] map = InitMap(101, 101);
        map[100, 100].nodeStatu = GlobalEnum.NodeStatu.Walk;
        float n1 = Time.realtimeSinceStartup;
        List<AStarNode> path = new AStar().FindPath2D(start, end, map);

        if (path != null)
        {
            Debug.Log("寻路耗时：" + (Time.realtimeSinceStartup - n1));
            Debug.Log("找到路径！路径长度: " + path.Count);
            for (int i = 0; i < map.GetLength(0); ++i)
            {
                for (int j = 0; j < map.GetLength(1); ++j)
                {
                    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    float x = map[i, j].x;
                    float y = map[i, j].y;
                    go.transform.position = new Vector3(x, y);
                    if (map[i, j].nodeStatu == GlobalEnum.NodeStatu.Stop)
                    {
                        Renderer cubeRenderer = go.GetComponent<Renderer>();
                        Material newMaterial = new Material(Shader.Find("Unlit/Color"));
                        newMaterial.color = Color.red;
                        cubeRenderer.material = newMaterial;
                    }
                }
            }
            for (int i = 0; i < path.Count; i++)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                float x = path[i].x;
                float y = path[i].y;
                go.transform.position = new Vector3(x, y);
                Renderer cubeRenderer = go.GetComponent<Renderer>();
                Material newMaterial = new Material(Shader.Find("Unlit/Color"));
                newMaterial.color = Color.green;
                cubeRenderer.material = newMaterial;
            }
        }
        else
        {
            Start();
        }
    }

    /// <summary>
    /// 初始化地图信息
    /// </summary>
    /// <param name="width">地图的宽</param>
    /// <param name="height">地图的高</param>
    private AStarNode[ , ] InitMap(int width, int height)
    {
        AStarNode[ , ] map = new AStarNode[height, width];
        // 随机创建地图障碍物
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                AStarNode node = new AStarNode(i, j, Random.Range(0, 100) < 40 ? GlobalEnum.NodeStatu.Stop : GlobalEnum.NodeStatu.Walk);
                map[i, j] = node;
            }
        }
        return map;
    }
}