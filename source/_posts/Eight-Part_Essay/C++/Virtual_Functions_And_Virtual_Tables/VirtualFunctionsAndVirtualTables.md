---
title: 虚函数与虚表
top_img: '72125571_p0.jpg'
cover: '74182055_p0.jpg'
permalink: /Eight-Part_Essay/C++/Virtual_Functions_And_Virtual_Tables/
categories: 
    - 八股文
      - C++ 
tags: 
    - C++
---

## 虚函数与虚表

<img src="虚函数(1).jpg" alt="虚函数" style="zoom:100%;">
<img src="虚函数(2).jpg" alt="虚函数" style="zoom:100%;">

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

### 单继承下类的虚表布局

#### 虚表布局（按顺序）

1. `type_info`
2. `父类虚函数`：未被子类重写的虚函数，其地址保留在虚表中。
3. `​子类覆盖的虚函数`：子类重写的虚函数地址会替换父类虚表中的对应条目。
4. `新增虚函数`：子类新增的虚函数地址追加到虚表末尾。

* **在单继承中，派生类的虚表继承自基类**，并且派生类覆盖的虚函数地址会替换基类虚表中的对应条目。

#### type_info 布局

1. 辅助类地址，用来实现 `type_info` 的函数
2. 类名地址
3. 父类 `type_info` 地址

#### Dynamic Cast（RTTI）

* `dynamic_cast` 通过检查虚表中 `type_info` 的信息判断能否在运行时进行指针转型以及是否需要指针偏移，需要插入额外的操作，这也解释了 `dynamic_cast` 的开销问题。

### 多继承下类的虚表布局

费流版：[多继承的虚表](https://blog.csdn.net/jhdhdhehej/article/details/132629781)

* **当派生类同时继承多个包含虚函数的基类时，每个基类会维护独立的虚表指针（vptr）。派生类对象内存中会按继承顺序排列这些基类的虚表指针。**
* 以下为示例：

``` CPP
class Base1 { public: virtual void func1(); };
class Base2 { public: virtual void func2(); };
class Derived : public Base1, public Base2
{
    void func1() override;  // 覆盖Base1的func1
    void func2() override;  // 覆盖Base2的func2
};
```

* `Derived` 对象内存布局：
+-------------------+
| Base1虚表指针(vptr1)| → 指向Base1的虚表
| Base1成员变量      |
+-------------------+
| Base2虚表指针(vptr2)| → 指向Base2的虚表
| Base2成员变量      |
+-------------------+
| Derived成员变量    |
+-------------------+
* **虚表覆盖规则​：**
  派生类重写的虚函数会替换对应基类虚表中的条目，但索引位置保持与基类一致。例如：
  1. `vptr1` 指向的虚表中，`func1` 条目被替换为 `Derived::func1`
  2. `vptr2` 指向的虚表中，`func2` 条目被替换为 `Derived::func2`
* **新增虚函数的存储​**
  **派生类新增的虚函数会被追加到第一个基类虚表的末尾**。例如若 `Derived` 新增 `func3()` ，则 `vptr1` 指向的虚表末尾会新增该函数指针。
* **在多继承中，每个基类有自己独立的虚表**。派生类覆盖的虚函数会替换对应基类虚表中的函数地址。

#### 菱形继承

* 与上述（普通的多继承）相同

#### 菱形虚拟继承

``` CPP
class A
{
public:
    virtual void func1()
    {
      cout << "A::func1()" << endl;
    }
    int _a;
};
class B : virtual public A
{
public:
    int _b;
};
class C : virtual public A
{
public:
    int _c;
};
class D : public B, public C
{
public:
    int _d;
};
int main()
{
    D d;
    d.B::_a = 1;
    d.C::_a = 2;
    d._b = 3;
    d._c = 4;
    d._d = 5;
    return 0;
}
```

* 根据菱形虚拟继承的对象模型，不难得出以下的内存图

<img src="菱形虚拟继承内存图.png" alt="菱形虚拟继承内存图" style="zoom:100%;">

* `B` 和 `C` 均有 虚表指针（第一个）和虚基表指针（第二个）
* 对于虚基表里面的内容， 它里面存储的是 `偏移量` ，第一个是 `-4` ，第二个是 `18` ，可见，第一个值是为了找到该部分的起始位置，第二个值是为了找到 `A` 的部分
* 那么如果我们给 `D` 有自己单独的虚函数呢？`D` 会额外创建虚表吗？其实不会的，因为 `D` 完全可以存在已有的虚表里。我们可能会以为放入共享的 `A` 的虚表，不过如果按照多继承的角度去理解，也有可能会放入 `B` 的虚表。
* 菱形虚拟继承会将 `B` 和 `C` 中的 `A` 都放到了公共部分
* 此时的按照菱形虚拟继承的内存分配来看是没有什么大问题的，但是当我们 `B` 和 `C` 同时对 `A` 的虚函数进行了重写的时候，由于是菱形虚拟继承，所以都会让 `A` 给放到公共部分（ `B` 和 `C` 共享的 `A` ）。两个都一起重写，导致编译器不知道什么该听哪一个的，所以会报错
* 主要还是因为 `B` 和 `C` 都想要去重写这个 `A` ，才导致的问题。而如果只是一个菱形继承的话，就不会出现这个问题，因为各自重写各自的即可。
* 对于上面的情况，有两种方案去处理：
  1. 只保留一种（ `B` 或 `C` ）重写即可
  2. 让 `D` 再来一个重写，这样的话，无论 `B` 和 `C` 是否重写，都要听 `D` 的重写函数了。

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

#### 误用 RTTI 的例子

* 有些人对 `RTTI` 口诛笔伐，认为它是多余的，会导致程序效率低下和糟糕的编程方式。这里有一个需要尽量避免的例子。
* 在判断是否能调用某个方法时，尽量不要使用 `if-else` 和 `typeid` 的形式，因为这会使得代码冗长。
* 如果在扩展的 `if else` 语句系列中使用了 `typeid` ，则应该考虑是否应该使用 `虚函数` 和 `dynamic_cast`。
