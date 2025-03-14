---
title: const 与 constexpr
top_img: '82910218_p0.png'
cover: '128002675_p0.jpg'
permalink: /Eight-Part_Essay/C++/Const_And_Constexpr/
categories: 
    - 八股文
      - C++
tags: 
    - C++
---

## const 与 constexpr 的区别

* 如下图：

<img src="区别.png" alt="const 与 constexpr 的区别" style="zoom:100%;">

* 主要是用于区分**只读变量**和**常量**
  1. **只读变量**：运行时确定
  2. **常量**：编译时确定
  3. 性能方面：常量 > 只读变量

### 常见的常量表达式

1. 字面值（如 42）
2. 用常量表达式初始化的对象

* 一个对象（或表达式）是否是常量表达式取决于类型和初始值，如下：

``` CPP
int i1 = 42;           // i1 不是常量表达式：初始值 42 是字面值，但 i1 不是 const / constexpr 类型
const int i2 = i1;     // i2 不是常量表达式：初始值 i1 不是常量表达式
const int i3 = 42;     // i3 是常量表达式：用字面值 42 初始化的 const 对象
const int i4 = i3 + 1; // i4 是常量表达式：用常量表达式 i3 + 1 初始化的 const 对象
const int i5 = getValue(); // 如果 getValue() 是普通函数，则 i5 值要到运行时才能确定，则不是常量表达式。相反，如果 getValue() 是 const 或 constexpr 类型，则 i5 是常量表达式，编译时即可确定
```

### constexpr 变量

* 上面的例子可以看出，不能直接判断一个 const 对象是否是常量表达式：例如 i4 是否是常量表达式取决于 i3 是否是常量表达式，而 i4 又可能用来初始化其他常量表达式。在复杂的系统中，很难一眼看出某个 const 对象是否是常量表达式。

* C++11 允许把变量声明为 constexpr 类型，此时编译器会保证 constexpr 变量是常量表达式（否则编译报错）。换句话说，只要看到 constexpr 类型的变量，则一定能够在编译期取得结果，可以用在需要常量表达式的场景。

``` CPP
int i1 = 42;
constexpr int i2 = i1; // constexpr 变量 'i2' 必须由常量表达式初始化。不允许在常量表达式中读取非 const / constexpr 变量 'i1'
constexpr int i3 = 42; // i3 是常量表达式
constexpr int i4 = i3 + 1; // i4 是常量表达式
constexpr int i5 = getValue(); // 只有 getValue() 是 constexpr 函数时才可以，否则编译报错
```

### constexpr 函数

* constexpr 函数是指能用于常量表达式的函数。需要强调的是，constexpr 函数既能用于要求常量表达式 / 编译期常量的语境，也可以作为普通函数使用。
* **注意：constexpr 函数不一定返回常量表达式**！只有 constexpr 的所有实参都是常量表达式 / 编译期常量时，constexpr 函数的结果才是常量表达式/编译期常量。只要有一个参数在编译期未知，那就和普通函数一样，在运行时计算。

``` CPP
constexpr int sum(int a, int b) { return a + b; }

constexpr int i1 = 42;
constexpr int i2 = sum(i1, 52); // 所有参数都是常量表达式，sum 的结果也是常量表达式，在编译期求值

int AddThree(int i) { return sum(i, 3); } // i 不是常量表达式，此时 sum 作为普通函数使用
```

### constexpr 限制

* 因为需要在编译期求值，所以 constexpr 函数有一些限制：返回类型和所有形参的类型必须是字面值类型（literal type）。除了内置类型，用户自定义的类也可以是字面值类型，因为它的构造函数和成员函数也可以是 constexpr 函数。

### 使用 constexpr 的好处

* 编译器可以保证 constexpr 对象是常量表达式（能够在编译期取得结果），而 const 对象不能保证。如果一个 const 变量能够在编译期求值，将其改为 constexpr 能够让代码更清晰易读
* constexpr 函数可以把运行期计算迁移至编译期，使得程序运行更快（但会增加编译时间）

## 小结

* 修饰对象的时候，可以把 constexpr 当作加强版的 const。**const 对象只表明值不会改变，不一定能够在编译期取得结果**。
* **constexpr 对象不仅值不会改变，而且保证能够在编译期取得结果**。constexpr 函数既可以用于编译期计算，也可以作为普通函数在运行期使用。
