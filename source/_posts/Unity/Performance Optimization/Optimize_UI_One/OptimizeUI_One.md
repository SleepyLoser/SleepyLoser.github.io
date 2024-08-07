---
title: UGUI的优化方案(1)
top_img: '109951169086186923.jpg'
cover: '109951165311290979.jpg'
categories: 
    - Unity
      - 性能优化
tags: 
    - UGUI
    - 性能优化
---

## DrawCall优化方案

* 在优化UGUI的DrawCall时，我们可以采取以下几种方法:

### 合并UI元素

* 将多个相邻的UI元素合并成一个元素，这样可以减少DrawCall的数量。可以使用Unity的Sprite Packer工具来自动合并UI元素
* 将多个UI元素的纹理合并成一个Atlas，这样可以减少DrawCall的数量。可以使用Unity的Sprite Atlas功能来实现。

### 使用合批技术

> 将多个UI元素合并成一个批次，这样可以减少DrawCall的数量。可以使用Unity的Batching功能来实现合批。

**静态合批:**

* 静态合批是将静态（不移动）GameObjects组合成大网格，然后进行绘制。
* 静态合批使用比较简单，PlayerSettings中开启static batching，然后对需要静态合批物体的Static打钩即可，unity会自动合并被标记为static的对象，前提它们共享相同的材质，并且不移动，被标记为static的物体不能在游戏中移动，旋转或缩放。
* 静态批处理需要额外的内存来存储合并的几何体。
* 注意如果多个GameObject在静态批处理之前共享相同的几何体，则会在编辑器或运行时为每个GameObject创建几何体的副本，这会增大内存的开销。例如，在密集的森林级别将树标记为静态可能会产生严重的内存影响。此时就必须去权衡利弊，为了更少的内存占用，可能需要避免某些GameObjects的静态批处理，尽管这必须要牺牲一定的渲染性能。

**动态合批:**

> 动态合批是将一些足够小的网格，在CPU上转换它们的顶点，将许多相似的顶点组合在一起，并一次性绘制它们。
> **无论静态还是动态合批都要求使用相同的材质**，动态合批有以下限制：

* 动态合批处理动态的GameObjects的每个顶点都有一定的开销，因此动态合批处理仅应用于包含不超过900个顶点属性和不超过300个顶点的网格。(`如果shader中使用Vertex Position, Normal和single UV，可以批量处理最多300个顶点，而如果shader中使用Vertex Position, Normal, UV0, UV1和Tangent，则只能使用180个顶点。`注意：将来可能会更改属性计数限制。)
* 如果GameObjects在Transform上包含镜像，则不会对其进行动态合批处理（例如，scale 为1的GameObject A和scale为-1的GameObject B无法一起动态合批处理）
* 使用不同的Material实例会导致GameObjects不能一起批处理，即使它们基本相同。阴影渲染(shadow caster)是一个例外
* 带有光照贴图的GameObjects有额外的渲染器参数：保存光照贴图的索引和偏移/缩放。一般来说，动态光照贴图的GameObjects应指向完全相同的光照贴图位置才能被动态合批处理
* 使用多个pass的shader不会被动态合批处理。

**Github上Unity官方总结了25种不能被合批处理的情况 :** <https://github.com/Unity-Technologies/BatchBreakingCause>

### 使用UI Canvas

* 添加Canvas将会打断和之前元素DrawCall的合并，每个Canvas都会开始一个全新的DrawCall(动静分离)。

## 大量同屏玩家头顶UI的优化案例: {% psw 《华夏》手游 头顶UI性能优化 %}

<img src="bf947f9535b549c6a54357977b2137e4.png" alt="优化案例" style="zoom:50%;">

### 图文混排
