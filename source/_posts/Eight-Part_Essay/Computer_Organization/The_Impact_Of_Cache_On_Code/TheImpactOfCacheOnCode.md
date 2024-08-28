---
title: Cache对代码的影响
top_img: '119683453_p2.jpg'
cover: '101091270_p0.png'
categories: 
    - 八股文
      - 计算机组成原理
tags: 
    - Cache
    - 计算机组成原理
---

## 问题背景

* 代码片段一

``` CPP
int array[10][128];

for (i = 0; i < 10; i++)
    for (j = 0; j < 128; j++)
        array[i][j] = 1;
```

* 代码片段二

``` CPP
int array[10][128];

for (i = 0; i < 128; i++)
    for (j = 0; j < 10; j++)
        array[j][i] = 1;
```

* 我们假设使用的L1 Cache Line大小是64字节，采用写分配及写回策略。继续假设数组 array 内存首地址是64字节对齐。

## 问题分析

* 在有了以上背景假设后，我们先分析下`片段1`导致的Cache Miss / Hit情况。
* 当执行 `array[0][0]` = 1 时，Cache 控制器发现 `array[0][0]` 的值不在Cache中，此时发生一次 Cache Miss。然后从主存中读取 `array[0][0]` 到 `array[0][15]` 的内存值到 Cache 中。
* 当执行访问 `array[0][1]` = 1 时会发生一次 Cache Hit。此时内存访问速度极快。接着继续往下执行，会一直 Cache Hit。
* 直到执行 `array[0][16]` = 1，此时会 Cache Miss。总结来说就是访问内存每发生一次 Cache Miss。接下来会发生15次 Cache Hit。
* 因此这种初始化方法**Cache命中率很高**。

***

* 我们再来分析下`片段2`。
* 当执行 `array[0][0]` = 1 时，Cache 控制器发现 `array[0][0]` 的值不在 Cache 中，此时发生一次 Cache Miss。然后从主存中读取 `array[0][0]` 到 `array[0][15]` 的内存值到 Cache 中。
* 当执行访问 `array[1][0]` = 1 时依然发生一次 Cache Miss。一直执行到 `array[9][0]` = 1 依然是一次Cache Miss。
* 现在思考下，访问 `array[0][1]` 会是怎么情况呢？此时就需要考虑 Cache 的大小了。如果**Cache大小大于数组 array 大小**，Cache 此时相当于缓存了整个 array 数组的内容。那么后续访问其他元素，确实是 Cache Hit。似乎和片段1代码分析结果差不多。
* 但是如果 Cache 的大小很小，例如只有数组一半大小，那么 Cache 命中率就很明显会降低。同样的 Cache 大小，`片段1` 的代码依然会获得很高的cache命中率。

## 总结

* 在大多数情况下，`片段1` 代码的性能比 `片段2` 好。因此我们倾向 `片段1` 代码的写法。
* 附 [Cache 的基本原理](https://sleepyloser.github.io/2024/08/27/Eight-Part_Essay/Computer_Organization/The_Basic_Principle_Of_Cache/TheBasicPrincipleOfCache/)
