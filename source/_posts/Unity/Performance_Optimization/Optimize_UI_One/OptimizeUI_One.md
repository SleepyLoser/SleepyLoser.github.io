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

#### 静态合批

* 静态合批是将静态（不移动）GameObjects组合成大网格，然后进行绘制。
* 静态合批使用比较简单，PlayerSettings中开启static batching，然后对需要静态合批物体的Static打钩即可，unity会自动合并被标记为static的对象，前提它们共享相同的材质，并且不移动，被标记为static的物体不能在游戏中移动，旋转或缩放。
* 静态批处理需要额外的内存来存储合并的几何体。
* 注意如果多个GameObject在静态批处理之前共享相同的几何体，则会在编辑器或运行时为每个GameObject创建几何体的副本，这会增大内存的开销。例如，在密集的森林级别将树标记为静态可能会产生严重的内存影响。此时就必须去权衡利弊，为了更少的内存占用，可能需要避免某些GameObjects的静态批处理，尽管这必须要牺牲一定的渲染性能。

#### 动态合批

> 动态合批是将一些足够小的网格，在CPU上转换它们的顶点，将许多相似的顶点组合在一起，并一次性绘制它们。
> **无论静态还是动态合批都要求使用相同的材质**，动态合批有以下限制：

* 动态合批处理动态的GameObjects的每个顶点都有一定的开销，因此动态合批处理仅应用于包含不超过900个顶点属性和不超过300个顶点的网格。(`如果shader中使用Vertex Position, Normal和single UV，可以批量处理最多300个顶点，而如果shader中使用Vertex Position, Normal, UV0, UV1和Tangent，则只能使用180个顶点。`注意：将来可能会更改属性计数限制。)
* 如果GameObjects在Transform上包含镜像，则不会对其进行动态合批处理（例如，scale 为1的GameObject A和scale为-1的GameObject B无法一起动态合批处理）
* 使用不同的Material实例会导致GameObjects不能一起批处理，即使它们基本相同。阴影渲染(shadow caster)是一个例外
* 带有光照贴图的GameObjects有额外的渲染器参数：保存光照贴图的索引和偏移/缩放。一般来说，动态光照贴图的GameObjects应指向完全相同的光照贴图位置才能被动态合批处理
* 使用多个pass的shader不会被动态合批处理。

**Github上Unity官方总结了25种不能被合批处理的情况 :** <https://github.com/Unity-Technologies/BatchBreakingCause>

#### 合批条件

##### 模型静态合批条件

* `Gameobject` 标记为 `static`
* `Mesh` 开启 `read/write`
* 材质一致

##### UI合批条件

* 无需标记为 `static`
* `Depth` 值一致
* 材质一致：小图标采用图集，未设置材质将默认为UI/Default视为同一材质
* 贴图一致：文字引用的贴图为Font Texture，视为同一贴图

### 使用UI Canvas

* 添加Canvas将会打断和之前元素DrawCall的合并，每个Canvas都会开始一个全新的DrawCall(动静分离)。

## 大量同屏玩家头顶UI的优化案例: {% psw 《华夏》手游 头顶UI性能优化 %}

<img src="bf947f9535b549c6a54357977b2137e4.png" alt="优化案例" style="zoom:50%;">

### 图文混排

* 图文混排的方案有很多，我们采用了UGUI支持富文本的特性，使用Quad关键字占位，在需要加载Image的时候，在对应ImageHolder下创建Image。
* 这也是我们能将所有头顶UI的**Batch降到3个**的基础：所有角色的Text在一个Canvas下，ImageHolder在另一个Canvas下，变化较大的血条在第三个Canvas下。发生变更时，由于**UGUI合并Batch是以Canvas为单位的**，同一个Canvas下的所有物体会被合并到一个Mesh中。这样一来可以减少血条这种变化较大的物体的变化导致整个Canvas的刷新。

### UI图集采用2048*2048

* 图集采用2048 * 2048。一张图集容纳所有常用资源。由于UGUI的Batch规则是**相同材质（Shader、层级等也要相同）的物体会被Batch到一起**。所以我们直接将所有常用资源合并到一张图集中。此处的常用资源的概念是指主界面及常驻界面的资源，以及头顶的Image资源。这样做的好处是主界面、常驻界面以及头顶UI的Image只要在Hierarchy中是相邻的，就能100%在一个Batch中完成。对于所有界面来说95%以上的Image资源都在同一个图集中。对于Batch优化，或者是UI开发，都是非常方便的。

### Outline描边优化

* 详情见[一种UGUI的Outline描边优化方案](https://sleepyloser.github.io/2024/08/09/Unity/Performance_Optimization/Outline_Stroke_Optimization/OutlineStrokeOptimization/)
* 实际上在后续进一步优化中，我们将描边Shader中原始纹理与描边融合的部分写到了一个Pass中，所以此处性能得到了进一步的提升。数据如下：

<img src="Outline.png" alt="进一步优化Outline" style="zoom:50%;">

* 可以看到进一步优化后三角形数量与无效果时的三角形数量一致。顶点数量仅增加了2个（增加的2个顶点数量是因为UGUI顶点帮助函数将共享顶点全部拆分导致）

### 预加载字体

* 视野中进入新的角色时常常会观察到一次明显的CPU Spike，继续看调用栈发现最耗时的是Font.cacheFontForText,实际上这是动态字体Dynamic Text在扩充Texture。在新的文字进入时，如果文字纹理的Texture剩余空间不足，Unity会新开辟一个原来纹理2倍的新纹理，并将原来纹理上的所有文字都要Copy到新的纹理上。这个过程是相当耗时的，而且完全随机可能会造成游戏卡顿影响体验。
* 我们的做法是在游戏进程刚开始的时候，将3500个常用汉字和ASCII字符全部RequestCharactersInTexture, 相当于缓存所有常用汉字及字符。（此处省略3628字的常用汉字+ASCII字符）。
* 这样操作以后解决了两个问题：第一点是Font.cacheFontForText基本消失不见了，游戏的一开始已经分配了2048*1024的纹理，足够很长一段时间的文字使用了。第二点就是字体花屏的问题，准确的说这是非常偶现的一个Bug，怀疑与Unity在重新分配一张大的纹理的操作有关。这样修改以后文字花屏的Bug再也没有收到报告了。

### 分帧处理

* 同一时刻视野内可能出现大量的角色，为了避免卡顿，这里可以采用分帧处理的方式，现将需要处理的角色ID缓存起来，虽然处理总量不变，但是可以1帧处理几个甚至几帧处理1个以减少单帧的压力。对于平顺帧率有非常大的帮助。

### 缓冲池

* UI的资源加载耗时不容小觑，既然部分UI（例如这里的头顶UI）是可以复用的，就可以使用缓存池预先缓存一定数量的UI。另外，Enable和Disable其实对性能的影响同样不小，缓存池这里可以不必真正的执行SetActive操作，而是逻辑标记一下就可以了。在缓存池中缓存的节点直接挪到很远很远的地方，让UICamera看不到就好了（也可以加上CanvasGroup让UI隐形）。
* 但是这里需要注意的一点是，逻辑标记Deactive的节点上的脚本一定要注意停掉主逻辑。

### 减少刷新次数

* 头顶UI的信息是分很多层的，不同的事件驱动不同层的刷新，其他没有变更的层的信息缓存即可。在同一帧中如果收到了多个刷新事件，要求多次刷新同一层的信息或者同时刷新多层的信息（事实上这种情况经常发生），可以模仿UGUI的做法SetDirty()，将对应的层置脏，其他层继续使用缓存的信息。在这一帧的末尾甚至下一帧的末尾执行一次刷新。这样可以显著减少头顶UI的刷新次数。

### 动态元素和静态元素分离

* 在头顶UI中变化比较频繁的是血条，文字和图片变化相对较少。这部分就可以采用血条在一个Canvas中，文字在一个Canvas中，图片在另一个Canvas中的方案。尽量减少同一个Canvas下变化的物体的数量。
