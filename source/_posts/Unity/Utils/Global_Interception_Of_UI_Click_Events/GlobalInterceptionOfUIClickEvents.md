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
