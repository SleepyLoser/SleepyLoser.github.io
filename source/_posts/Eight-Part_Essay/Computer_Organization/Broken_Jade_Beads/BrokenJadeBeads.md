---
title: 碎玉零珠————计算机组成原理
top_img: '116763500_p0.jpg'
cover: '101137838_p0.png'
categories: 
    - 八股文
      - 计算机组成原理
tags: 
    - 计算机组成原理
---

## 什么是缓存(Cache)？为什么需要缓存？如何提高缓存的命中率？缓存是不是最快的？

* Cache即CPU的高速缓冲存储器，是一种是用于减少处理器访问内存所需平均时间的部件；
* 由于CPU的计算速度远远大于从CPU向内存取数据的速度，如果每次都让CPU去内存取数据，会导致CPU计算能力的浪费，所以人们设计了缓存，CPU通过读写缓存来获取操作数，结果也通过缓存写入内存；
* 注意程序的**局部性原理**，在遍历数组时按照内存顺序访问；充分利用**CPU分支预测功能**，将预测的指令放到缓存中执行；此外缓存的容量和块长是影响缓存效率的重要因素。
* 缓存不是最快的，寄存器更快。
* 附 [Cache 的基本原理](https://sleepyloser.github.io/2024/08/27/Eight-Part_Essay/Computer_Organization/The_Basic_Principle_Of_Cache/TheBasicPrincipleOfCache/)
