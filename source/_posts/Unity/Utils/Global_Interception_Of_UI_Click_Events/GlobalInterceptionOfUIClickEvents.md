---
title: Unity 全局控制 UI 点击事件（拓展：全局控制其它涉及到 EventSystem 的事件（输入、射线投射和发送事件））
top_img: '111102699_p0.jpg'
cover: '123068644_p0.png'
categories: 
    - Unity
      - 工具类
tags: 
    - 实用工具
---

## 全局控制 UI 点击事件

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

* 继续看 `ExecuteEvents` 静态类的内部，可以发现所有的输入事件调用都在这里执行

``` CSharp
public delegate void EventFunction<T1>(T1 handler, BaseEventData eventData);

private static readonly EventFunction<IPointerEnterHandler> s_PointerEnterHandler = Execute;

private static readonly EventFunction<IPointerExitHandler> s_PointerExitHandler = Execute;

private static readonly EventFunction<IPointerDownHandler> s_PointerDownHandler = Execute;

private static readonly EventFunction<IPointerUpHandler> s_PointerUpHandler = Execute;

private static readonly EventFunction<IPointerClickHandler> s_PointerClickHandler = Execute;

private static readonly EventFunction<IInitializePotentialDragHandler> s_InitializePotentialDragHandler = Execute;

private static readonly EventFunction<IBeginDragHandler> s_BeginDragHandler = Execute;

private static readonly EventFunction<IDragHandler> s_DragHandler = Execute;

private static readonly EventFunction<IEndDragHandler> s_EndDragHandler = Execute;

private static readonly EventFunction<IDropHandler> s_DropHandler = Execute;

private static readonly EventFunction<IScrollHandler> s_ScrollHandler = Execute;

private static readonly EventFunction<IUpdateSelectedHandler> s_UpdateSelectedHandler = Execute;

private static readonly EventFunction<ISelectHandler> s_SelectHandler = Execute;

private static readonly EventFunction<IDeselectHandler> s_DeselectHandler = Execute;

private static readonly EventFunction<IMoveHandler> s_MoveHandler = Execute;

private static readonly EventFunction<ISubmitHandler> s_SubmitHandler = Execute;

private static readonly EventFunction<ICancelHandler> s_CancelHandler = Execute;

private static readonly ObjectPool<List<IEventSystemHandler>> s_HandlerListPool = new ObjectPool<List<IEventSystemHandler>>(null, delegate (List<IEventSystemHandler> l)
{
    l.Clear();
});
```

* 同理，通过反射获取相应委托并进行重写即可达到控制相应事件的效果

## Unity UGUI 事件的底层原理

* 在 Unity 场景中创建一个 `Canvas` ，可以发现编辑器自动创建了一个叫 `EventSystem` 的东西，可以发现这个 `EventSystem` 中默认包含两个组件：`EventSystem` 和 `StandaloneInputModule` 。这便是 UGUI 事件系统。
* 在游戏运行过程中一个 UI 元素捕获用户的输入和操作从而驱动应用程序作出反应，这其中涉及到 UI 的显示，事件调度、UI 捕获、事件处理。

### 事件调度

* 事件调度发生在 `EventSystem` 的 `Update` 中，找到当前的 `Module` 并处理，处理中包含了对移动点击拖动事件的处理。首先来看下事件数据有哪几种。

#### 事件数据

<img src="事件数据.webp" alt="事件数据" style="zoom:100%;">

* **BaseEventData**：是事件数据类的父类，其中包括 `EventSystem` 、`InputModule` 和当前选中 `GameObject` 的引用。
* **AxisEventData**：滚轮事件数据，只记录滚动的方向数据。
* **PointerEventData**：点位事件数据，其中包含当前位置，滑动距离，点击时间以及不同状态下 GameObject 的引用。
* 当点击事件发生时，UGUI 可以获得点位事件数据，这是后续处理该事件重要的依据，在整个事件处理流程中进行传递。

#### 事件类型

<img src="事件类型.webp" alt="事件类型" style="zoom:100%;">

* 输入检测模块规定了对事件的处理逻辑和细节，如处理鼠标点击事件，拖拽和移动等，其中 `TouchInputModule` 主要是面向触摸平台和移动设备的输入检测模块，`StandaloneInputModule` 主要是面向标准鼠标键盘的。
* 除了 Unity 提供的 `StandaloneInputModule` 和 `TouchInputModule` 之外，我们也可以通过泛化 `BaseInputModule` 来自定义 `InputModule` 。处理的过程其实就是重写父类的 `Process` 函数，在其内部对鼠标光标的各种状态进行计算和标记。

#### UI 捕获

* 在获取一个点位事件数据时，会计算当前点位事件对应的 GameObject 。
* **UI 捕获通过射线检测**，从摄像机出发出射线穿过当前 `Pointer` 或者 `Touch` 所在位置，获得碰撞的 GameObject 列表。
* **射线碰撞检测模块的主要工作是从摄像机的屏幕位置上进行射线碰撞检测并获取碰撞结果，将结果返回给事件处理逻辑类，交由事件处理模块处理**。UI 是分层的，在顶层的当然是先被照射的，当然免不了有些 UI 控件是忽略射线检测的。
* 射线检测在 Unity 中分为 `PhysicsRaycaster` ，`Physics2DRaycaster` 以及 `GraphicRaycaster` 。2D 射线碰撞检测、3D 射线碰撞检测相对比较简单，采用射线的形式进行碰撞检测。
* 区别在于 2D 射线碰撞检测结果里预留了 2D 的层级次序，以便**在后面的碰撞结果排序时，以这个层级次序为依据进行排序**，而 3D 射线碰撞检测结果则是**以距离大小为依据进行排序的**。
* `GraphicRaycaster` 类为 UGUI 元素点位检测的类，它被放在 `Core` 渲染块里。它主要针对 `ScreenSpaceOverlay` 模式下的输入点位进行碰撞检测，因为这个模式下的检测并不依赖于射线碰撞，而是通过遍历所有可点击的 UGUI 元素来进行检测比较，从而判断该响应哪个 UI 元素的。因此 `GraphicRaycaster` 类是比较特殊的。

#### 事件处理

* 对于发生的事件，会由 `ExecuteEvents` 中的 `Execute` 方法找到合适的方法处理。
* 现在我们知道 Unity 里通过 `InputModule` 把用户的输入处理完之后包装到 `BaseEventData` 里，而且也通过射线检测捕获到了对应的 UI ，那么我们就可以调用该 GameObject 所实现的任意一个接口的接口函数。
* 例如鼠标光标事件有很多种，比如：`光标按下-Down` 、`抬起-Up` 、`离开-Exit` ，如果按下和抬起的时间很短又可以产生 `点击事件-Click` ，对于每一种事件都可以定义为一个接口，例如：`IPointerDownHandler` 、`IPointerUpHandler` 、`IPointerEnterHandler` 、`IPointerExitHandler` 、`IPointerClickHandler` ，为了统一操作这些接口，它们都继承自 `IEventSystemHandler` 。
* 不同的 UI 组件可以选择性的支持这些鼠标事件，比如有的控件我们就是不希望它响应点击事件，那么不让它实现 `IPointerClickHandler` 的接口就可以。
* 既然 Button 实现了 `IPointerClickHandler` 接口，那么对于一个按钮控件来说，当光标点击事件到来时，就可以调用到它 Button 组件的 `OnPointerClick` 函数。
* UGUI 把封装好的数据传递给 UI 组件，那么这些组件在调用其对应类型的接口函数时，便会触发相应事件。对于 `IPointerClickHandler` 按钮来说，鼠标点击一个按钮时，便会调用它的 `OnPointerClick` 接口，在 `OnPointerClick` 中触发 `onClick` 事件。接下来便是监听 `onClick` 去编写业务逻辑了。
