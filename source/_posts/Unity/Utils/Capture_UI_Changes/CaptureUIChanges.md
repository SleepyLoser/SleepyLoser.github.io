---
title: 自动捕获UI变化
top_img: '121074821_p0.jpg'
cover: '120811718_p0.jpg'
categories: 
    - Unity
      - 工具类
tags: 
    - 实用工具
---

## 自动捕获UI变化

* 在UI组件发生变化的时候（布局、内容的变化，例如：某个Image组件被开启、Image组件里的Source Image发生改变、Image组件位置发生改变、Image组件大小发生改变等）会**自动捕获该组件**

* UI变化的本质是**网格重建**

``` C#
public class CaptureUIChanges : MonoBehaviour
{
    // 保存待重建布局元素（如：RectTransform变化）
    IList<ICanvasElement> m_LayoutRebuildQueue;
    // 保存待重建渲染元素（如：Image变化）
    IList<ICanvasElement> m_GraphicRebuildQueue;
 
    // 通过C#反射获取dll文件里的变量
    private void Awake()
    {
        System.Type type = typeof(CanvasUpdateRegistry);
        FieldInfo field = type.GetField("m_LayoutRebuildQueue", BindingFlags.NonPublic | BindingFlags.Instance);
        m_LayoutRebuildQueue = (IList<ICanvasElement>)field.GetValue(CanvasUpdateRegistry.instance);
        field = type.GetField("m_GraphicRebuildQueue", BindingFlags.NonPublic | BindingFlags.Instance);
        m_GraphicRebuildQueue = (IList<ICanvasElement>)field.GetValue(CanvasUpdateRegistry.instance);
        StartCoroutine(Check());
    }
    
    private IEnumerator Check()
    {
        while (true)
        {
            for (int j = 0; j < m_LayoutRebuildQueue.Count; j++)
            {
                ICanvasElement layout = m_LayoutRebuildQueue[j];
                if (ObjectValidForUpdate(layout))
                {
                    /*
                        对发生变化的UI组件（layout）进行处理
                    */
                    Debug.LogFormat("{0}引起{1}网格重建(UI布局发生变化)", GetUIPath(layout), layout.transform.GetComponent<Graphic>().canvas.name);
                }
            }

            for (int j = 0; j < m_GraphicRebuildQueue.Count; j++)
            {
                ICanvasElement graphic = m_GraphicRebuildQueue[j];
                if (ObjectValidForUpdate(graphic))
                {
                    /*
                        对发生变化的UI组件（graphic）进行处理
                    */
                    Debug.LogFormat("{0}引起{1}网格重建(UI图形发生变化)", GetUIPath(graphic), graphic.transform.GetComponent<Graphic>().canvas.name);
                }
            }
            yield return null;
        }
    }

    /// <summary>
    /// 该UI组件是否有效更新
    /// </summary>
    /// <param name="element">UI组件</param>
    /// <returns>判断结果(True/False)</returns>
    private bool ObjectValidForUpdate(ICanvasElement element)
    {
        bool valid = element != null;
 
        bool isUnityObject = element is UnityEngine.Object;

        if (isUnityObject) valid = (element as UnityEngine.Object) != null; 
 
        return valid;
    }

    /// <summary>
    /// 获取UI组件在Hierarchy中的路径（高版本Unity API : SearchUtils.GetHierarchyPath(GameObject gameObject, bool includeScene) ）
    /// </summary>
    /// <param name="element">UI组件</param>
    /// <returns>该UI组件在Hierarchy中的路径</returns>
    private string GetUIPath(ICanvasElement element)
    {
        StringBuilder path = new StringBuilder('/' + element.transform.name);
        Transform parent = element.transform.parent;
        while (parent != null)
        {
            path.Insert(0, '/' + parent.name);
            parent = parent.parent;
        }
        path.Remove(0, 1);
        return path.ToString();
    }
}
```
