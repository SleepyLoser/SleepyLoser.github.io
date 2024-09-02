---
title: 虚函数与虚表
top_img: '72125571_p0.jpg'
cover: '74182055_p0.jpg'
categories: 
    - 八股文
      - C++ 
tags: 
    - C++
---

## 虚函数与虚表

<img src="虚函数(1).jpg" alt="虚函数与虚表" style="zoom:100%;">
<img src="虚函数(2).jpg" alt="虚函数与虚表" style="zoom:100%;">

### 普通类的内存布局和带虚函数类的内存布局

``` CPP
#include <iostream>
using namespace std;

class NonVirtualClass 
{
public:
    void foo(){}
};

class VirtualClass 
{
public:
    virtual void foo(){}
};

int main() 
{
    cout << "Size of NonVirtualClass: " << sizeof(NonVirtualClass) << endl;
    cout << "Size of VirtualClass: " << sizeof(VirtualClass) << endl;
}

```

* 这里 `NonVirtualClass` 的大小为1，而 `VirtualClass` 的大小为8（64位情况），有两个原因造成两者的不同：
  1. C++中类的大小不能为0，所以一个空类的大小为 `1` , 如果对一个空类对象取地址，如果大小为0，这个地址就没法取了。
  2. 如果一个空类有虚函数，那其内存布局中只有一个虚表指针，其大小为 `sizeof(void*)` , 即 `8`。

### 单继承下类的虚表布局和type_info布局

#### 虚表布局

* 单继承下的虚表比较简单，虚表指针总是指向虚表偏移 +16（`2 * sizeof(void*)`）的地址，这个地址是第一个虚函数的入口地址（指向类自己的 type_info (8字节) 指针 与 指向第一个虚函数的指针 (8字节) ）。

#### type_info布局

1. 辅助类地址，用来实现 `type_info` 的函数
2. 类名地址
3. 父类 `type_info` 地址

#### Dynamic Cast(RTTI)

* `dynamic_cast` 通过检查虚表中 `type_info` 的信息判断能否在运行时进行指针转型以及是否需要指针偏移，需要插入额外的操作，这也解释了 `dynamic_cast` 的开销问题。

### RTTI 有什么用？怎么用？

#### RTTI 的用途

* 假设有一个类层次结构，其中的类都是从一个基类派生而来的，则可以让基类指针指向其中任何一个类的对象。
* 有时候我们会想要知道指针具体指向的是哪个类的对象。因为：
  1. 可能希望调用类方法的正确版本，而有时候派生对象可能包含不是继承而来的方法，此时，只有某些类的对象可以使用这种方法。
  2. 也可能是出于调试目的，想跟踪生成的对象的类型。

#### RTTI 的工作原理

* C++有3个支持RTTI的元素：
  1. 如果可能的话，`dynamic_cast` 运算符将使用一个指向基类的指针来生成一个指向派生类的指针；否则，该运算符返回 0（空指针）；
  2. `typeid` 运算符返回一个指出对象的类型的值；
  3. `type_info` 结构存储了有关特定类型的信息。
* **注意：RTTI 只适用于包含虚函数的类**。因为只有对于这种类层次结构，才应该将派生类的地址赋给基类指针。
* RTTI的初衷就是运行阶段类型判断，以便debug或者类型判断后调用派生类与基类的非公有类。可以考虑把派生类成员函数实现放在基类作为虚函数，派生类中实现，直接利用基类调用虚函数以调用到派生类的虚函数实现。

#### dynamic_cast 运算符

* 这是最常用的 RTTI 组件，它不能回答 `指针指向的是哪类对象` 这样的问题，但能够回答 `是否可以安全地将对象的地址赋给特定类型的指针` 这样的问题。说白了，就是看看**这个对象指针能不能转换为目标指针**。
* 通常，如果指向的对象（*pt）的类型为Type或者是从Type直接或简介派生而来的类型，则下面的表达式将指针pt转换为Type类型的指针：

``` CPP
dynamic_cast<Type *>(pt)
```

* 否则，结果为0，即空指针。
* **注意**：即使编译器支持RTTI，在默认情况下，它也可能关闭该特性。如果该特性被关闭，程序可能仍能够通过编译，但将出现运行阶段错误。在这种情况下，应该查看文档或菜单选项。
* 也可以将dynamic_cast用于引用，用法稍微有点不同。
* 没有与空指针对应的引用值，因此无法使用特殊的引用值来指示失败。当请求不正确时，`dynamic_cast` 将引发类型为 `bad_cast` 的异常，这种异常是从 `exception` 类派生而来的，它是在头文件 `typeinfo` 中定义的。

``` CPP
#include <typeinfo> // for bad_cast
...
try 
{
    Basic & rs = dynamic_cast<Basic &>(rt);
    ...
}
catch(bad_cast &)
{
    ...
};
```

#### typeid运算符和type_info类

* `typeid` 运算符能够用于确定两个对象是否为同种类型。它与sizeof有些想象，可以接受两种参数：
  1. 类名
  2. 结果为对象的表达式
* 返回一个对 `type_info` 对象的引用，其中，`type_info` 是在头文件 `typeinfo` 中定义的一个类，这个类重载了 `==` 和 `!=` 运算符，以便可以用于对类型进行比较。

``` CPP
// 判断pg指向的是否是ClassName类的对象
typeid(ClassName) == typeid(*pg)
```

* 如果 `pg` 是一个空指针，程序将引发 `bad_typeid` 异常，该异常是从 `exception` 类派生而来的，它是在头文件 `typeinfo` 中声明的。
* `type_info` 类的实现随厂商而异，但包含一个 `name()` 成员，该函数返回一个随实现而异的字符串，通常（但并非一定）是类的名称。可以这样显示：

``` CPP
std::cout << "Now processing type is " << typeid(*pg).name() << ".\\n";
```

* 其实，`typeid` 运算符就是指出或判断具体的类型，而 `dynamic_cast` 运算符主要用于判断是否能够转换，并进行类型转换（指针或引用）。

#### 误用RTTI的例子

* 有些人对RTTI口诛笔伐，认为它是多余的，会导致程序效率低下和糟糕的编程方式。这里有一个需要尽量避免的例子。
* 在判断是否能调用某个方法时，尽量不要使用 `if-else` 和 `typeid` 的形式，因为这会使得代码冗长。
* 如果在扩展的 `if else` 语句系列中使用了 `typeid` ，则应该考虑是否应该使用 `虚函数` 和 `dynamic_cast`。
