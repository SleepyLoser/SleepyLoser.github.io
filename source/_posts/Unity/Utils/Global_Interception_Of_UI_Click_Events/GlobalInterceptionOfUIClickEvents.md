---
title: Unity 全局拦截 UI 点击事件
top_img: ''
cover: ''
categories: 
    - Unity
      - 工具类
tags: 
    - 实用工具
---

## 思路

* Unity 点击事件都是基于 `IPointerClickHandler` 接口，凡是实现了该接口的对象都会触发点击事件

``` CSharp
public interface IPointerClickHandler : IEventSystemHandler
{
    void OnPointerClick(PointerEventData eventData);
}
```

* 继续查在哪调用的，从 `EventSystem` 里面找到了 `BaseInputModule`

``` CSharp
private BaseInputModule m_CurrentInputModule;
```

* 它就是跟 `EventSystem` 挂载一起的 `StandaloneInputModule` 继承的基类
* 然后看 `StandaloneInputModule` 里面的代码，全局查找 `IPointerClickHandler`

``` CSharp
private void ReleaseMouse(PointerEventData pointerEvent, GameObject currentOverGo)
{
    ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);
    GameObject eventHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);
    if (pointerEvent.pointerClick == eventHandler && pointerEvent.eligibleForClick)
    {
        ExecuteEvents.Execute(pointerEvent.pointerClick, pointerEvent, ExecuteEvents.pointerClickHandler);
    }

    if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
    {
        ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
    }

    pointerEvent.eligibleForClick = false;
    pointerEvent.pointerPress = null;
    pointerEvent.rawPointerPress = null;
    pointerEvent.pointerClick = null;
    if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
    {
        ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);
    }

    pointerEvent.dragging = false;
    pointerEvent.pointerDrag = null;
    if (currentOverGo != pointerEvent.pointerEnter)
    {
        HandlePointerExitAndEnter(pointerEvent, null);
        HandlePointerExitAndEnter(pointerEvent, currentOverGo);
    }

    m_InputPointerEvent = pointerEvent;
}
```

* 大概意思就是在释放鼠标的时候会检查一次点击事件，核心代码就一句

``` CSharp
ExecuteEvents.Execute(pointerEvent.pointerClick, pointerEvent, ExecuteEvents.pointerClickHandler);
```

* `Execute` 内部代码

``` CSharp
public static bool Execute<T>(GameObject target, BaseEventData eventData, EventFunction<T> functor) where T : IEventSystemHandler
{
    List<IEventSystemHandler> list = s_HandlerListPool.Get();
    GetEventList<T>(target, list);
    int count = list.Count;
    for (int i = 0; i < count; i++)
    {
        T handler;
        try
        {
            handler = (T)list[i];
        }
        catch (Exception innerException)
        {
            IEventSystemHandler eventSystemHandler = list[i];
            Debug.LogException(new Exception($"Type {typeof(T).Name} expected {eventSystemHandler.GetType().Name} received.", innerException));
            continue;
        }

        try
        {
            functor(handler, eventData);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    int count2 = list.Count;
    s_HandlerListPool.Release(list);
    return count2 > 0;
}
```

* 也就是说最后会执行 `functor(handler, eventData)` ，它是从前面传过来的，也就是 `ExecuteEvents.pointerClickHandler` ，继续看它的内部实现

``` CSharp
public static EventFunction<IPointerClickHandler> pointerClickHandler => s_PointerClickHandler;
```

* 是一个委托，指向了 `s_PointerClickHandler`

``` CSharp
private static readonly EventFunction<IPointerClickHandler> s_PointerClickHandler = Execute;
private static void Execute(IPointerClickHandler handler, BaseEventData eventData)
{
    handler.OnPointerClick(ValidateEventData<PointerEventData>(eventData));
}
```

* 至此就明了了，所有的点击事件都是通过 invoke 这个 `s_PointerClickHandler` 委托实现的。那我们就重写覆盖这个委托，因为是 `private` 的，所以需要用到反射。核心代码如下

``` CSharp
System.Type type = typeof(ExecuteEvents);
FieldInfo field = type.GetField("s_PointerClickHandler", BindingFlags.NonPublic | BindingFlags.Static);
field.SetValue(null, new ExecuteEvents.EventFunction<IPointerClickHandler>(OnPointerClick));

/// <summary>
/// 重写所有点击事件
/// </summary>
/// <param name="handler">点击事件处理器</param>
/// <param name="eventData">事件数据基类</param>
private void OnPointerClick(IPointerClickHandler handler, BaseEventData eventData)
{
    PointerEventData pointerEventData = ExecuteEvents.ValidateEventData<PointerEventData>(eventData);
    handler.OnPointerClick(pointerEventData);
}
```

## 拓展
