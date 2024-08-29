---
title: AssetBundle内存、资源管理机制
top_img: '109951168299903051.jpg'
cover: '109951166522530307.jpg'
categories: 
    - Unity
      - 工具类
tags: 
    - AB包
---

<img src="48d2b7e150144a7b9bfa5b1e3b4e2e08.png" alt="AB包内存管理" style="zoom:50%;">
<img src="dc6960fad0f44c909f1ef4855917cd9a.png" alt="AB包内存管理" style="zoom:50%;">

## 管理已加载的Asset

* 在调用[AssetBundle.Unload]方法时，传入的 unloadAllLoadedObjects 参数（true或false）会导致不同的行为，这对于管理资源和AssetBundle来说非常重要。
* 例如，假设材质M加载自AssetBundle AB，并且M目前正在活动Scene中。

<img src="c5d5060e40b483213d61b1b86c6b1b55.jpg" alt="AB包内存管理" style="zoom:50%;">

* 如果调用了 AssetBundle.Unload(true)，那么M会被从Scene中移除、销毁并卸载。然而，如果调用了 AssetBundle.Unload(false)，AB的数据头信息会被卸载，但是M仍然会留在Scene中而且有效。调用 AssetBundle.Unload(false) 会破坏M和AB之间的链接关系。如果之后又加载了AB，会将AB中的Object的新的副本加载到内存中。

<img src="33857a99b94e524f8da873d2510e590a.jpg" alt="AB包内存管理" style="zoom:50%;">

* 如果之后又加载了AB，那么会重新加载一份新的AssetBundle数据头信息副本，但是不会在新的AB副本中加载M。Unity不会再新的AB和M之间建立任何链接关系。

<img src="d05c5311dcf219e5f1d043425cbcb080.jpg" alt="AB包内存管理" style="zoom:50%;">

* 如果调用 AssetBundle.LoadAsset() 重新加载M，Unity不会将已有的M副本作为AB中的数据的实例，因此，Unity会加载一个新的M的副本，这样，Scene中就会有两个不同的M的副本。

<img src="646eeff5fc5d3d30c01cf6f13de9475e.jpg" alt="AB包内存管理" style="zoom:50%;">

* 附AB包流程

<img src="AB包流程.png" alt="AB包流程" style="zoom:50%;">
