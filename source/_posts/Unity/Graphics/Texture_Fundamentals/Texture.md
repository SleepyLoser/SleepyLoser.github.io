---
title: Unity Texture
top_img: '114291391_p0.png'
cover: '109115574_p0.png'
categories: 
    - Unity
      - Texture
tags: 
    - 图形学
    - 纹理
---

## Unity Texture 基础

### 纹理

* 纹理 Texture 用于覆盖在 3D 物体上, 其本质是一张图片, 用于替代物体上渲染的颜色。

* 它可以用来添加物体的细节，我们可以在一张图片上插入非常多的细节，这样就可以让物体非常精细而不用指定额外的顶点。

* 为了能够把纹理映射(Map)到三角形上，我们需要指定三角形的每个顶点各自对应纹理的哪个部分，这样每个顶点就会关联着一个**纹理坐标(Texture Coordinate)**，用来标明该从纹理图像的哪个部分采样，之后在图形的其它片段上进行**片段插值(Fragment Interpolation)**。

* 纹理坐标在x和y轴上，范围为0到1之间（注意我们现在使用的是2D纹理图像），使用纹理坐标获取纹理颜色叫做**采样(Sampling)**，{% emp 纹理坐标起始于(0, 0)，也就是纹理图片的左下角，结束于(1, 1)，即纹理图片的右上角 %}。

* 下图是把纹理坐标映射到三角形上
<img src="TextureEffect.png" alt="纹理坐标起始于(0, 0)，也就是纹理图片的左下角，结束于(1, 1)，即纹理图片的右上角" style="zoom:50%;">

1. **网格 Mesh**只能表示 3D 模型的形状
2. **材质 Material**只能进行 3D 模型的纯色渲染
3. **纹理 Texture** 可以进行 3D 模型的图片渲染
  
* 纹理贴图是在建模软件中制作完成的, 是建模的相关工作

### 为 3D 模型设置纹理贴图

* 在 Project 文件窗口中的 Assets 目录下, 创建`Texture`目录（文件夹名称随意）, 将图片拖动到该目录下 , 可以直接从文件系统中拖动到 Unity 编辑器的 Project 窗口或者手动复制到相应的文件夹下。
<img src="TextureFolder.png" alt="创建 Texture 目录并加入图片" style="zoom:50%;">

* 选中 Project 文件窗口中的 Assets/Materials 目录下的`Texture_Material`材质文件（材质名称随意）
<img src="Texture_Material.png" alt="选中材质" style="zoom:50%;">

* 在 Inspector 检查器窗口会显示该材质的属性, 然后直接将 Textures 中的材质图片拖动到 Inspector 检查器窗口中的 Albedo 左侧的方框中即可, 操作完成后的效果如下：
<img src="Albedo.png" alt="将纹理挂载到材质上后为Sphere挂载材质" style="zoom:50%;">

<img src="TextureSphere.png" alt="将纹理挂载到材质上后为Sphere挂载材质" style="zoom:50%;">

* 附纹理图片：
<img src="Weapon.png" alt="纹理图片" style="zoom:50%;">

### 纹理类型

* [Unity官方文档](https://docs.unity.cn/cn/2020.3/Manual/TextureTypes.html)

### 材质、纹理、Shader三者之间的关系

* **(个人理解，缺乏权威性)** Shader决定纹理渲染的方式，材质是Shader的实例化

* **以下是Unity官方解释：**

* `网格`是 Unity 的主要图形基元。网格定义了对象的形状。

* `材质`通过包含对所用纹理的引用、平铺信息、颜色色调等来定义表面应使用的渲染方式。材质的可用选项取决于材质使用的着色器。

* `着色器`是一些包含数学计算和算法的小脚本，根据光照输入和材质配置来计算每个像素渲染的颜色。

* `纹理`是位图图像。材质可包含对纹理的引用，因此材质的着色器可在计算游戏对象的表面颜色时使用纹理。除了游戏对象表面的基本颜色（反照率）之外，纹理还可表示材质表面的许多其他方面，例如其反射率或粗糙度。

* **材质指定了要使用的一种特定着色器**，而**使用的着色器确定材质中可用的选项**。着色器指定期望使用的**一个或多个纹理变量**，而 Unity 中的材质检视面板 (Material Inspector) 允许您将自己的纹理资源分配给这些纹理变量。
