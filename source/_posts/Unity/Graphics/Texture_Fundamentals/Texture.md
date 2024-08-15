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

## 什么是 RenderTexture ？

* RenderTexture是Unity定义的一种特殊的Texture类型,它是连接着一个FrameBufferObject的存在于GPU端的Texture(Server-Side Texture)。

### 什么是 server-side texture ？

* 在渲染过程中，贴图最开始是存在CPU这边的内存中的，这个贴图我们通常称为client-side的texture，它最终要被送到GPU的存储里，GPU才能使用它进行渲染，送到GPU里的那一份被称为server-side的texture。这个texture在CPU和GPU之间拷贝要考虑到一定的带宽瓶颈。

### 什么是 FrameBufferObject ？

* FrameBuffer就是GPU里渲染结果的目的地，我们绘制的**所有结果**（包括color depth stencil等）都最终存在这个这里，有一个默认的FBO它直接连着我们的显示器窗口区域，就是把我们的绘制物体绘制到显示器的窗口区域。但是**现代GPU通常可以创建很多其他的FBO**，这些FBO不连接窗口区域，这种我们创建的FBO的存在目的就是**允许我们将渲染结果保存在GPU的一块存储区域**，待之后使用。
* 当渲染的结果被渲染到一个FBO上后，就有很多种方法得到这些结果，我们能想想的使用方式就是把这个结果作为一个Texture的形式得到，通常有这样几种方式得到这个贴图：
  * 将这个FBO上的结果传回CPU这边的贴图，在GLES中的实现一般是ReadPixels（）这样的函数，这个函数是将当前设为可读的FBO拷贝到CPU这边的一个存储Buffer，没错如果当前设为可读的FBO是那个默认FBO，那这个函数就是在截屏，如果是你自己创建的FBO，那就把刚刚绘制到上面的结果从GPU存储拿回内存。
  * 将这个FBO上的结果拷贝到一个gpu上的texture，在GLES中的实现一般是CopyTexImage2D（），它一般是将可读的FBO的一部分拷贝到存在于gpu上的一个texture对象中，直接考到server-sider就意味着可以马上被gpu渲染使用
  * 将这个FBO直接关联一个gpu上的texture对象，这样就等于在绘制时就直接绘制到这个texure上，这样也省去了拷贝时间，GLES中一般是使用FramebufferTexture2D（）这样的接口

### 渲染到RenderTexture的几种方式

* 在Assets里创建一个RenderTexture，然后将其附给一个摄像机，这样这个摄像机实时渲染的结果就都在这个RenderTexture上了。
* 有的时候我们想人为的控制每一次渲染，你可以将这个摄像机disable掉，然后手动的调用一次render。
* 有的时候我们想用一个特殊的Shader去渲染这个RenderTexture，那可以调用camera的**RenderWithShader**这个函数，它将使用你指定的Shader去渲染场景，这时候场景物体上原有的Shader都将被自动替换成这个Shader，而参数会按名字传递。这有什么用？比如我想得到当前场景某个视角的黑白图，那你就可以写个渲染黑白图的Shader，调用这个函数。
* 我们还可以不用自己在Assets下创建RenderTexture，直接使用 **Graphics.Blit(src, target, mat)** 这个函数来渲染到RenderTexture上，这里的的target就是你要绘制的RenderTexture，src是这个mat中需要使用的_mainTex,可以是普通Texture2d，也可以是另一个RenderTexture，这个函数的本质是，绘制一个四方块，然后使用mat这个材质，用src做maintex，然后先Clear为Black，然后渲染到target上。这个是一个快速的用于图像处理的方式。我们可以看到Unity的很多后处理的一效果就是一连串的Graphics.Blit操作来完成一重重对图像的处理，如果在CPU上做那几乎是会卡死的。

### 从RenderTexture获取结果

* 大部分情况我们渲染到RenderTexture就是为了将其作为Texture继续给其他Material使用。这时候我们只需在那个Material上调用SetTexture传入这个RenderTexture就行，这完全是在GPU上的操作。
* 但有的时候我们想把它拷贝回CPU这边的内存，比如你想保存成图像，你想看看这个图什么样，因为直接拿着RenderTexture你并不能得到它的每个Pixel的信息，因为他没有内存这一侧的信息。Texture2d之所以有，是因为对于选择了Read/Write属性的Texture2D，它会保留一个内存这边的镜像。**注意这个操作效率不是很高**。以下是复制的方法：

``` C#
Texture2D uvtexRead = new Texture2D()

RennderTexture currentActiveRT = RenderTexture.active;
// Set the supplied RenderTexture as the active one
RenderTexture.active = uvTex;
uvtexRead.ReadPixels(new Rect(0, 0, uvTexReadWidth, uvTexReadWidth), 0, 0);
RenderTexture.active = currentActiveRT;
```

* 上面这段代码就是等于先把当前的FBO设为可读的对象，然后调用相关操作将其读回内存。

### 其它的一些问题

* **RenderTexture的格式**：RenderTexture的格式和普通的Texture2D的格式并不是一回事，我们查阅文档，看到RenderTexture的格式支持的有很多种，最基本的ARGB32是肯定支持的，很多机器支持ARRBHALF或者ARGBFLOAT这样的格式，这种浮点格式是很有用的，想象一下你想把场景的uv信息保存在一张图上，你要保存的就不是256的颜色，而是一个个浮点数。但是使用前一定要查询当前的GPU是否支持这种格式
* 如果你想**从RenderTexture拷贝回到内存**，那么RenderTexture和拷贝回来的Texture的格式必须匹配，且必须是RGBA32或者RGBA24这种基本类型，你把float拷贝回来应该是不行的
* **RenderTexture的分配和销毁**：如果你频繁的要new一个RenderTexture出来，那么不要直接new，而是使用RenderTexture提供的**GetTemporary**和**ReleaseTemporary**，它将在内部维护一个池，反复重用一些**大小格式一样**的RenderTexture资源，因为让GPU为你分配一个新的Texture其实是要耗时间的。更重要的这里还会调用DiscardContents
* **DiscardContents**：这个RenderTexture的接口非常重要，好的习惯是你应该尽量在每次往一个已经有内容的RenderTexture上绘制之前总是调用它的这个DiscardContents函数，大致得到的优化是，在一些基于Tile的GPU上，RenderTexture和一些Tile的内存之间要存在着各种同步， 如果你准备往一个已经有内容的RenderTexture上绘制，将触发到这种同步，而这个函数告诉GPU这块RenderTexture的内容不用管他了，我反正是要重新绘制，这样就避免了这个同步而产生的巨大开销。总之还是尽量用**GetTemporary**这个接口吧，它会自动为你处理这个事情

## 纹理采样

* 在着色器里**采集纹理样本**是最常见的 **GPU 像素数据处理操作**。要想**修改**这段数据，可以**复制修改后的纹理**，或用**着色器**把修改渲染到一张纹理上。
* 附 [Unity 渲染管线](https://sleepyloser.github.io/2024/08/05/Unity/Graphics/Rendering_Pipeline/RenderingPipeline/) 和 [纹理采样代码示例](https://sleepyloser.github.io/2024/08/15/Unity/Utils/TextureUtils/TextureUtil/) 供理解参考
