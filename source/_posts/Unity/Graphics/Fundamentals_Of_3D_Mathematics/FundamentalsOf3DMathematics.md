---
title: 三维数学基础知识
top_img: '93990522_p0.png'
cover: '121986241_p0.jpg'
permalink: /Unity/Graphics/Fundamentals_Of_3D_Mathematics/
categories: 
    - Unity
      - 数学
tags: 
    - 数学
    - 图形学
---

## 欧拉角、矩阵、四元数表示旋转的区别和优缺点

* **欧拉角**：定义了绕着三个坐标轴的旋转角，来确定刚体的旋转位置的方式，包括俯仰角pitch，偏航角yaw和滚动角roll；
  * **优点**是：比较直观，而且单个维度上的角度也比较容易插值；
  * **缺点**是：它不能进行任意方向的插值，而且会导致**万向节死锁**的问题，旋转的次序对结果也有影响。
* **矩阵**：
  * **优点**是：不受万向节死锁的影响，可以独一无二的表达任意旋转，并且可以通过矩阵乘法来对点或矢量进行旋转变换；现在多数CPU以及所有GPU都有内置的硬件加速点积和矩阵乘法；
  * **缺点**是：不太直观，而且需要比较大的存储空间，也不太容易进行插值计算。
* **四元数**：
  * **好处**是：能够串接旋转，能把旋转直接作用于点或者矢量，而且能够进行旋转插值。另外它所占用的存储空间也比矩阵小，四元数可以解决万向节死锁的问题。
  * **缺点**是：抽象，不易理解，因为多了一个维度。

### 欧拉角

### 矩阵

#### 应用：模型矩阵、观察矩阵、投影矩阵的推导

* 基本概念
  1. **模型矩阵M(Model)**：将局部坐标变换到世界坐标；
  2. **观察矩阵V(View)**：将世界坐标转换为观察坐标，或者说，将物体的世界坐标，转换为在相机视角下的坐标；
  3. **投影矩阵P(Projection)**：将顶点坐标从观察空间变换到裁剪空间(clip space) ，后续的透视除法操作会将裁剪空间的坐标转换为标准化设备坐标系中（NDC）。

* 推导

### 四元数

## 向量点乘（Dot Product）

* 即相应元素的乘积的和：Vector1(x1, y1) · Vector2(x2, y2) = x1 · x2 + y1 · y2; **注意结果不是一个向量，而是一个标量（Scalar）**
* A · B = |A||B|Cos(θ)。θ是向量A和向量B之间的夹角，这里|A|我们称为向量A的模(norm)，也就是A的长度， 在二维空间中就是|A| = sqrt(x ^ 2 + y ^ 2)。这样我们就和容易计算两条线的夹角：Cos(θ) = A·B / (|A||B|)
* 这可以告诉我们如果点乘的结果（点积）**为0的话就表示这两个向量垂直**。当两向量平行时，点积有最大值。另外，点乘运算不仅限于2维空间，他可以推广到任意维空间。

## 叉乘（cross product）

<img src="叉乘.png" alt="叉乘" style="zoom:100%;">

* 二维空间中的叉乘是：Vector1(x1, y1) `x` Vector2(x2, y2) = x1 · y2 - y1 · x2。看起来像个标量，事实上叉乘的结果是个向量，方向在z轴上。**上述结果是它的模**。
* 在二维空间里，让我们暂时忽略它的方向，将结果看成一个向量，那么这个结果类似于上述的点积，我们有：A `x` B = |A||B|Sin(θ)
* 然而角度 θ 和上面点乘的角度有一点点不同，**它是有正负的**，是指从A到B的角度。
* 另外还有一个有用的特征那就是**叉积的绝对值就是A和B为两边所形成的平行四边形的面积**。也就是AB所包围三角形面积的两倍。
* 在**三维空间**里，A和B的叉乘公式为：

<img src="叉乘公式.png" alt="叉乘公式" style="zoom:100%;">

* 其中 i = (1, 0, 0); j = (0, 1, 0); k = (0, 0, 1)。
* 根据i、j、k间关系，有：

<img src="叉乘公式(2).png" alt="叉乘公式(2)" style="zoom:100%;">

* 向量A和向量B的外积结果是一个向量，有个更通俗易懂的叫法是**法向量**，该向量垂直于a和b向量构成的平面。
