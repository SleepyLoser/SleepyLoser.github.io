---
title: 进程内存分布
top_img: '100700212_p0.jpg'
cover: '92607177_p0.png'
categories: 
    - 八股文
      - 计算机操作系统
tags: 
    - 计算机操作系统
---

## 内存分布

* 以32位系统为例，共有4G的寻址能力，进程在内存中的分布如下图所示。Linux默认将高地址的1G空间分配给内核，称为内核空间，剩下的3G空间分配给进程使用，称为用户空间。

| 操作系统内核区 | 用户不可见 |
| ---           | ---       |
| 用户栈        | 栈指针，向下扩展 |
| 动态堆        | 向上扩展 |
| 全局区（静态区）| .data初始化 .bss未初始化 |
| 文字常量区（只读数据）| 常量字符串 |
| 程序代码区 | 栈指针，向下扩展 |

<img src="进程内存分布.png" alt="进程内存分布" style="zoom:100%;">

### 栈区

* 由编译器自动分配释放，速度较快
* 用来存储函数调用时的临时信息的结构，存放为运行时函数分配的**局部变量**、**函数参数**、**返回数据**、**返回地址**等。这些局部变量等空间都会被释放
* 程序运行过程中函数调用时参数的传递也在栈上进行，如递归调用栈
* 当栈过多的时候，就是导致栈溢出（比如大量的递归调用或者大量的内存分配）
* 栈是向低地址扩展的数据结构，是一块连续的内存的区域，空间有限

### 堆区

* 由程序员分配 (new,malloc) 释放 (delete,free) ，并指明大小，速度较慢
* 若程序员不释放，导致内存泄漏，new 完没有 delete
* 不过在整个程序结束时由操作系统回收,但是这样无疑增加了操作系统的负担
* 高地址扩展的数据结构，是不连续的内存区域，空间很大，比较灵活
* 频繁地分配和释放不同大小的堆空间容易产生内存碎片

**注：当 堆区 数据地址 和 栈区 数据地址相同时（碰面了）就代表，数据已用完。**

### 全局区（静态区static）

* 全局变量和静态变量是放在一起的
* 初始化的全局变量和静态变量（static修饰的）放在一块区域.data节
* 未初始化的全局变量和未初始化的静态变量在相邻的另一块区域.bss（不占磁盘空间，不做具体解释）
* 程序结束后由系统释放

### 文字常量区

* 常量字符串就是放在这里，程序结束后由系统释放

### 程序代码区

* 存放函数体的二进制代码
