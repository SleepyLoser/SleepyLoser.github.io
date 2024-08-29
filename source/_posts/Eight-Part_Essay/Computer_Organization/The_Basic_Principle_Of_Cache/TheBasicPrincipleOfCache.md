---
title: Cache的基本原理
top_img: '116763500_p1.jpg'
cover: '101048953_p0.png'
categories: 
    - 八股文
      - 计算机组成原理
tags: 
    - Cache
    - 计算机组成原理
---

## 为什么需要Cache

### 程序是如何运行起来的

* 程序是运行在 RAM（随机存储器） 之中，我们称之为 main memory（主存）。当我们需要运行一个进程的时候，首先会从磁盘设备（例如，eMMC、UFS、SSD等）中将可执行程序load到主存中，然后开始执行。
* 在CPU内部存在一堆的通用寄存器（register）。如果 CPU 需要将一个变量（假设地址是A）加1，一般分为以下3个步骤：
  1. CPU 从主存中读取地址 A 的数据到内部通用寄存器 x0（ARM64架构的通用寄存器之一）。
  2. 通用寄存器 x0 加1。
  3. CPU 将通用寄存器 x0 的值写入主存。

<img src="变量加一过程.png" alt="变量加一过程" style="zoom:100%;">

* 其实现实中，CPU通用寄存器的速度（< 1ns）和主存（~ 65ns）之间存在着太大的差异。
* 因此，上面举例的3个步骤中，步骤1和步骤3实际上速度很慢。当CPU试图从主存中 Load / Store 操作时，由于主存的速度限制，CPU不得不等待这漫长的65ns时间。
* 如果我们采用更快材料制作更快速度的主存，并且拥有几乎差不多的容量, 其成本将会大幅度上升。我们试图提升主存的速度和容量，又期望其成本很低，这就有点难为人了。
* 因此，我们有一种折中的方法，那就是制作一块速度极快但是容量极小的存储设备。那么其成本也不会太高。这块存储设备我们称之为**Cache Memory**。
* 在硬件上，我们将Cache放置在CPU和主存之间，作为主存数据的缓存。
* 当CPU试图从主存中 Load / Store数据的时候，CPU会首先从 Cache 中查找对应地址的数据是否缓存在 Cache 中。如果其数据缓存在 Cache 中，直接从 Cache 中拿到数据并返回给CPU。

<img src="变量加一过程(2).png" alt="变量加一过程(2)" style="zoom:100%;">

* CPU和主存之间直接数据传输的方式转变成 CPU 和 Cache 之间直接数据传输。Cache 负责和主存之间数据传输。

## 多级Cache存储结构

* 当cache中没有缓存我们想要的数据的时候，依然需要漫长的等待从主存中load数据。为了进一步提升性能，引入多级cache。
* 前面提到的cache，称之为L1 cache（第一级cache）。我们在L1 cache 后面连接L2 cache，在L2 cache 和主存之间连接L3 cache。等级越高，速度越慢，容量越大。但是速度相比较主存而言，依然很快。
* 不同等级cache速度之间关系如下：

<img src="不同等级cache速度.png" alt="不同等级cache速度" style="zoom:100%;">

* 经过3级cache的缓冲，各级cache和主存之间的速度差也逐级减小。

## 多级cache之间的配合工作

* 首先引入两个名词概念，`命中`和`缺失`。CPU要访问的数据在cache中有缓存，称为**命中** (hit)，反之则称为**缺失** (miss)。
* 多级cache之间是如何配合工作的呢？我们假设现在考虑的系统只有两级cache。
  1. 当CPU试图从某地址load数据时，首先从L1 cache中查询是否命中，如果命中则把数据返回给CPU。如果L1 cache缺失，则继续从L2 cache中查找。
  2. 当L2 cache命中时，数据会返回给L1 cache以及CPU。如果L2 cache也缺失，很不幸，我们需要从主存中load数据，将数据返回给L2 cache、L1 cache及CPU。

<img src="多级Cache.jpg" alt="多级Cache" style="zoom:100%;">

* 这种多级cache的工作方式称之为inclusive cache（包容性缓存），某一地址的数据可能存在多级缓存中。
* 与inclusive cache对应的是exclusive cache（独占缓存），这种cache保证某一地址的数据缓存只会存在于多级cache其中一级。也就是说，任意地址的数据不可能同时在 L1 和 L2 cache 中缓存。

## 直接映射缓存(Direct mapped cache)

* cache的大小称之为 `cache size`，代表cache可以缓存最大数据的大小。我们将cache平均分成相等的很多块，每一个块大小称之为 `cache line` ，其大小是 `cache line size` 。
* 例如一个64 Bytes大小的cache。如果我们将64 Bytes平均分成64块，那么cache line就是1字节，总共64行cache line。如果我们将64 Bytes平均分成8块，那么cache line就是8字节，总共8行cache line。
* 现在的硬件设计中，一般cache line的大小是4-128 Bytes。为什么没有1 byte呢？原因我们后面讨论。
* 注意，**cache line是cache和主存之间数据传输的最小单位**。什么意思呢？当CPU试图load一个字节数据的时候，如果cache缺失，那么cache控制器会从主存中**一次性**的load cache line大小的数据到cache中。
* 例如，cache line大小是8字节。CPU即使读取一个byte，在cache缺失后，cache会从主存中load 8字节填充整个cache line。又是因为什么呢？后面说完就懂了。
* 我们假设下面的讲解都是针对64 Bytes大小的cache，并且cache line大小是8字节。我们可以类似把这块cache想想成一个数组，数组总共8个元素，每个元素大小是8字节。就像下图这样。

<img src="Cache数组.jpg" alt="Cache数组" style="zoom:100%;">

* 一个地址访问要映射到Cache中，地址被分成三个字段：tag，set index，block offset。这样，通过一个物理地址就可以获取数据或指令在缓存中的位置(set, way, byte)
* 现在我们考虑一个问题，CPU从0x0654地址读取一个字节，cache控制器是如何判断数据是否在cache中命中呢？我们**如何根据地址在有限大小的cache中查找数据**呢？现在硬件采取的做法是对地址进行**散列**（可以理解成地址取模操作）。

<img src="在Cache中寻找数据.jpg" alt="在Cache中寻找数据" style="zoom:100%;">

* 我们一共有8行cache line，cache line大小是8 Bytes。所以我们可以利用地址低3 bits（如上图地址蓝色部分）用来寻址8 bytes中某一字节，我们称这部分bit组合为offset。
* 同理，8行cache line，为了覆盖所有行。我们需要3 bits（如上图地址黄色部分）查找某一行，这部分地址部分称之为index。
* 现在我们知道，如果两个不同的地址，其地址的bit3-bit5如果完全一样的话，那么这两个地址经过硬件散列之后都会找到同一个cache line。
* 所以，当我们找到cache line之后，只代表我们访问的地址对应的数据可能存在这个cache line中，但是也有可能是其他地址对应的数据。
* 为此，我们又引入tag array区域，tag array和data array一一对应。一个cache line都对应唯一一个tag，tag中保存的是整个地址位宽去除index和offset使用的bit剩余部分（如上图地址绿色部分）。
* tag、index和offset三者组合就可以唯一确定一个地址了。因此，当我们根据地址中index位找到cache line后，取出当前cache line对应的tag，然后和地址中的tag进行比较，如果相等，这说明**cache命中**。如果不相等，说明当前cache line存储的是其他地址的数据，这就是**cache缺失**。
* 因此解答了我们之前的一个疑问**为什么硬件cache line不做成一个字节？**。这样会导致硬件成本的上升，因为原本8个字节对应一个tag，现在需要8个tag，占用了很多内存。tag也是cache的一部分，但是我们谈到cache size的时候并不考虑tag占用的内存部分。
* 我们可以从图中看到tag旁边还有一个`valid bit`，这个bit用来表示cache line中数据**是否有效**（例如：1代表有效；0代表无效）。当系统刚启动时，cache中的数据都应该是无效的，因为还没有缓存任何数据。cache控制器可以根据valid bit确认当前cache line数据是否有效。所以，上述比较tag确认cache line是否命中之前还会检查valid bit是否有效。只有在有效的情况下，比较tag才有意义。如果无效，直接判定cache缺失。
* 上面的例子中，cache size是64 Bytes并且cache line size是8 bytes。offset、index和tag分别使用3 bits、3 bits和42 bits（假设地址宽度是48 bits）。我们现在再看一个例子：512 Bytes cache size，64 Bytes cache line size。根据之前的地址划分方法，offset、index和tag分别使用6 bits、3 bits和39 bits。如下图所示。

<img src="在Cache中寻找数据(2).jpg" alt="在Cache中寻找数据(2)" style="zoom:100%;">

## 直接映射缓存的优缺点

<img src="直接映射缓存的优缺点.png" alt="直接映射缓存" style="zoom:100%;">

* 我们可以看到，地址0x00-0x3f地址处对应的数据可以覆盖整个cache。0x40-0x7f地址的数据也同样是覆盖整个cache。
* 0x00、0x40、0x80地址中index部分是一样的。因此，这3个地址对应的cache line是同一个。
* 所以，当我们访问0x00地址时，cache会缺失，然后数据会从主存中加载到cache中第0行cache line。
* 当我们访问0x40地址时，依然索引到cache中第0行cache line，由于此时cache line中存储的是地址0x00地址对应的数据，所以此时依然会cache缺失。然后从主存中加载0x40地址数据到第一行cache line中。
* 同理，继续访问0x80地址，依然会cache缺失。
* 这就相当于每次访问数据都要从主存中读取，所以cache的存在并没有对性能有什么提升。访问0x40地址时，就会把0x00地址缓存的数据替换。
* 这种现象叫做**cache颠簸（cache thrashing）**。针对这个问题，我们引入**多路组相连缓存**。我们首先研究下最简单的两路组相连缓存的工作原理。

## 两路组相连缓存(Two-way set associative cache)
