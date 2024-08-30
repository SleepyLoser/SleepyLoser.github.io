---
title: 继 渲染管线
top_img: ''
cover: ''
categories: 
    - Unity
      - 渲染管线
tags: 
    - 渲染管线
    - 图形学
---

## 渲染管线中各个阶段的可控性

| 阶段名 | 具体步骤 | 可控程度 | 由谁控制 |
| ----- | -------- | ------- | ------- |
| 应用阶段 |        | 完全可控 | 应用程序编程 |
| 几何阶段 | 顶点着色 | 完全可控 | Vertex Shader |
|         | 几何着色 | 完全可控 | Geometry Shader |
|         | 投影 | 完全可控 | Vertex Shader 中编程 |
|         | 视锥体裁剪 | 不可控 | 硬件中的 ViewPort Transform 组件负责 |
|         | 屏幕映射 | 不可控 | 硬件中的 ViewPort Transform 组件负责 |
|         | 背面剔除 | 可配置 | Shader 中的选项，cull back |
| 光栅化  | 三角形设置 | 不可控 | 硬件中的 Raster Engine 组件负责 |
|         | 三角形遍历 | 不可控 | 硬件中的 Raster Engine 组件负责 |
| 逐像素阶段 | 顶点属性插值 | 不可控 | 硬件中的 Attribute Setup 组件负责 |
|           | Early Z-Test（深度测试）| 不可控 | 硬件中的 Raster Engine 组件负责 |
|           | 片元着色 | 完全可控 | Fragment Shader |
|           | 混合 | 可配置 | Shader中的选项，blend命令等 |
