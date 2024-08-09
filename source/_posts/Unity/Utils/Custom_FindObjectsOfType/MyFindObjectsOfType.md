---
title: 自定义 UnityEngine.Object.FindObjectsOfType<T>()
top_img: '101733334_p0.png'
cover: '118310262_p0.png'
categories: 
    - Unity
      - 工具类
tags: 
    - 实用工具
---

## MyFindObjectsOfType

该函数的遍历顺序为（以Hierarchy窗口为基准）`从上到下，由外向内`依次遍历。

* 手动遍历对于某些需求（例如获取所有的顶层组件`T`）在一定程度上能够降低时间复杂度（剪枝），不过需要自己添加限制条件（例如：使用Dictionary记录已经获取过的组件）
* 同样的，MyFindObjectsOfType的返回值根据项目实际情况自行更改

``` C#
public static T[] MyFindObjectsOfType<T>() where T : Component
{
    Scene scene = SceneManager.GetActiveScene();
    GameObject[] roots = scene.GetRootGameObjects();
    List<T> objects = new List<T>();
    for (int i = 0; i < roots.Length; i++)
    {
        /*
            如果之前获取过该组件，就不再获取
        */
        if (roots[i].TryGetComponent(out T component))
        {
            objects.Add(component);
        }
        GetObjectsByType<T>(roots[i].transform, ref objects);
    }
    return objects as T[];
}
static void GetObjectsByType<T>(Transform transform, ref List<T> objects) where T : Component
{
    if (transform.childCount == 0) return;
    Transform curChild;
    for (int i = 0; i < transform.childCount; i++)
    {
        curChild = transform.GetChild(i);
        /*
            如果之前获取过该组件，就不再获取
        */
        if (curChild.TryGetComponent(out T component))
        {
            objects.Add(component);
        }
        GetObjectsByType<T>(curChild, ref objects);
    }
}
```
