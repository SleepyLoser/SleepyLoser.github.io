---
title: C++ 的几种类型转换
top_img: '119083349_p0.png'
cover: '115480546_p0.png'
categories: 
    - 八股文
      - C++
tags: 
    - C++
---

## C++ 的几种类型转换

* 在 C 语言中，我们大多数是用 `(type_name) expression` 这种方式来做强制类型转换，但是在 C++ 中，更推荐使用四个转换操作符来实现显式类型转换：
  1. **static_cast**
  2. **dynamic_cast**
  3. **const_cast**
  4. **reinterpret_cast**

### static_cast

* 用法: `static_cast <new_type> (expression)` 。其实 `static_cast` 和 C 语言 `()` 做强制类型转换基本是等价的。主要用于以下场景:

#### 基本类型之间的转换

* 将一个基本类型转换为另一个基本类型，例如将整数转换为浮点数或将字符转换为整数。

``` CPP
int a = 42;
double b = static_cast<double>(a); // 将整数a转换为双精度浮点数b
```

#### 指针类型之间的转换

* 将一个指针类型转换为另一个指针类型，尤其是在类层次结构中从基类指针转换为派生类指针。**这种转换不执行运行时类型检查，可能不安全，要自己保证指针确实可以互相转换**。

``` CPP
class Base {};
class Derived : public Base {};

Base* base_ptr = new Derived();
Derived* derived_ptr = static_cast<Derived*>(base_ptr); // 将基类指针base_ptr转换为派生类指针derived_ptr
```

#### 引用类型之间的转换

* 类似于指针类型之间的转换，可以将一个引用类型转换为另一个引用类型。**在这种情况下，也应注意安全性**。

``` CPP
Derived derived_obj;
Base& base_ref = derived_obj;
Derived& derived_ref = static_cast<Derived&>(base_ref); // 将基类引用base_ref转换为派生类引用derived_ref
```

* `static_cast` **在编译时执行类型转换**，在进行指针或引用类型转换时，需要自己保证合法性。如果想要运行时类型检查，可以使用 `dynamic_cast` 进行安全的向下类型转换。

### dynamic_cast

* 用法: `dynamic_cast <new_type> (expression)` 。`dynamic_cast` 在 C++ 中主要应用于父子类层次结构中的安全类型转换。它在运行时执行类型检查，因此相比于 `static_cast` ，它更加安全。`dynamic_cast` 的主要应用场景：

#### 向下类型转换

* 当需要将基类指针或引用转换为派生类指针或引用时，`dynamic_cast` 可以确保类型兼容性。如果转换失败，`dynamic_cast` 将**返回空指针（对于指针类型）或抛出异常（对于引用类型）**。

``` CPP
class Base { virtual void dummy() {} };
class Derived : public Base { int a; };

Base* base_ptr = new Derived();
Derived* derived_ptr = dynamic_cast<Derived*>(base_ptr); // 将基类指针base_ptr转换为派生类指针derived_ptr，如果类型兼容，则成功
```

#### 用于多态类型检查

* 处理多态对象时，`dynamic_cast` 可以用来确定对象的实际类型，例如：

``` CPP
class Animal { public: virtual ~Animal() {} };
class Dog : public Animal { public: void bark() { /* ... */ } };
class Cat : public Animal { public: void meow() { /* ... */ } };

Animal* animal_ptr = /* ... */;

// 尝试将Animal指针转换为Dog指针
Dog* dog_ptr = dynamic_cast<Dog*>(animal_ptr);
if (dog_ptr) 
{
    dog_ptr->bark();
}

// 尝试将Animal指针转换为Cat指针
Cat* cat_ptr = dynamic_cast<Cat*>(animal_ptr);
if (cat_ptr) 
{
    cat_ptr->meow();
}
```

* 另外，要使 `dynamic_cast` 有效，**基类至少需要一个虚函数**。因为，`dynamic_cast` **只有在基类存在虚函数(虚函数表)的情况下才有可能将基类指针转化为子类**。

#### dynamic_cast 底层原理

* `dynamic_cast` 的底层原理依赖于**运行时类型信息（RTTI, Runtime Type Information）**。C++ 编译器在编译时为支持多态的类生成RTTI，它包含了类的类型信息和类层次结构。
* 我们都知道当使用虚函数时，编译器会为每个类生成一个虚函数表（vtable），并在其中存储指向虚函数的指针。伴随虚函数表的还有**RTTI(运行时类型信息)**，这些辅助的信息可以用来帮助我们运行时识别对象的类型信息。
* 《深度探索C++对象模型》中有个例子：

``` CPP
class Point
{
public:
    Point(float xval);
    virtual ~Point();

    float x() const;
    static int PointCount();

protected:
    virtual ostream& print(ostream& os) const;

    float _x;
    static int _point_count;
};
```

<img src="C++对象模型.png" alt="C++对象模型" style="zoom:50%;">

* 首先，每个多态对象都有一个指向其 `vtable` 的指针，称为 `vptr` 。`RTTI`（就是上面图中的 `type_info` 结构）通常与 `vtable` 关联。`dynamic_cast` 就是利用 `RTTI` 来执行运行时类型检查和安全类型转换。
* 以下是 `dynamic_cast` 的工作原理的简化描述：
  1. 首先，`dynamic_cast` 通过查询对象的 `vptr` 来获取其 `RTTI`（这也是为什么 `dynamic_cast` 要求对象有虚函数）
  2. 然后，`dynamic_cast` 比较请求的目标类型与从 `RTTI` 获得的实际类型。如果目标类型是实际类型或其基类，则转换成功。
  3. 如果目标类型是派生类，`dynamic_cast` 会检查**类层次结构**，以确定转换是否合法。如果在**类层次结构**中找到了目标类型，则转换成功；否则，转换失败。
  4. **转换成功时，`dynamic_cast` 返回转换后的指针或引用**。
  5. **转换失败时，对于指针类型，`dynamic_cast` 返回空指针；对于引用类型，它会抛出一个 `std::bad_cast` 异常**。
* 因为 `dynamic_cast` 依赖于运行时类型信息，**它的性能可能低于其他类型转换操作**（如 `static_cast` ），**`static_cast` 是编译器静态转换，编译时期就完成了**。

### const_cast

* 用法: `const_cast <new_type> (expression)`, **`new_type` 必须是一个指针、引用或者指向对象类型成员的指针**。

#### 修改const对象

* 当需要修改 `const` 对象时，可以使用 `const_cast` 来删除 `const` 属性。

``` CPP
const int a = 42;
int* mutable_ptr = const_cast<int*>(&a); // 删除 const 属性，使得可以修改a的值
*mutable_ptr = 43; // 修改a的值
```

##### 修改局部变量

* 程序能正常运行，且常量被修改了，但是有一个问题：输出 `a` 的值和 `*mutable_ptr` 的值并不相同，`a` 的值还是 `42` ，而 `*mutable_ptr` 的值是 `43` , 而且 `mutable_ptr` 确实指向 `a` 所在的地址空间。
* 这是什么原因呢？难道一个地址空间可以存储不同的俩个值？当然不能。这就是 C++ 中的**常量折叠**：`const` 变量（即常量）值放在编译器的符号表中，计算时编译器直接从表中取值，省去了访问内存的时间，这是编译器进行的优化。`a` 是 `const` 变量，编译器对 `a` 在预处理的时候就进行了替换。编译器只对 `const` 变量的值读取一次。所以打印的是 `42` 。`a` 实际存储的值被指针 `mutable_ptr` 所改变。但是为什么能改变呢，从其存储地址可以看出来，其存储在堆栈中。

##### 修改全局变量

* 程序编译通过，但运行时错误。编译器提示 `a` 存储的空间不可写，也就是没有写权限，不能修改其值。原因是 `a` 是全局变量，全局变量存储在静态存储区，且只有可读属性，无法修改其值。

##### 使用 volatile 关键字

* [碎玉零珠 —— C++](https://sleepyloser.github.io/2024/08/23/Eight-Part_Essay/C++/Broken_Jade_Beads/BrokenJadeBeads/) 中有详细介绍，就不赘述了。

#### const对象调用非const成员函数

* 当需要使用 `const` 对象调用非 `const` 成员函数时，可以使用 `const_cast` 删除对象的 `const` 属性。

``` CPP
class MyClass 
{
public:
    void non_const_function() { /* ... */ }
};

const MyClass my_const_obj;
MyClass* mutable_obj_ptr = const_cast<MyClass*>(&my_const_obj); // 删除const属性，使得可以调用非const成员函数
mutable_obj_ptr->non_const_function(); // 调用非const成员函数
```

* 不过上述行为都不是很安全，可能导致未定义的行为，因此应谨慎使用。

### reinterpret_cast

* 用法: `reinterpret_cast <new_type> (expression)`, `reinterpret_cast` 用于在不同类型之间进行低级别的转换。
* 首先从英文字面的意思理解，`interpret` 是 `“解释，诠释”` 的意思，加上前缀 `“re”` ，就是 `“重新诠释”` 的意思；`cast` 在这里可以翻译成 `“转型”`（在侯捷大大翻译的《深度探索C++对象模型》、《Effective C++（第三版）》中，`cast` 都被翻译成了转型），这样整个词顺下来就是 `“重新诠释的转型”` 。**它仅仅是重新解释底层比特（也就是对指针所指的那片比特位换个类型做解释），而不进行任何类型检查**。因此，`reinterpret_cast` 可能导致未定义的行为，应谨慎使用。

#### reinterpret_cast 底层原理

* 一个指向字符串的指针是如何地与一个指向整数的指针或一个指向其他自定义类型对象的指针有所不同呢？从内存需求的观点来说，没有什么不同！它们三个都需要足够的内存（并且是相同大小的内存）来放置一个机器地址。指向不同类型的各指针之间的差异，既不在其指针表示法不同，也不在其内容（代表一个地址）不同，而是在其所寻址出来的对象类型不同。也就是说，指针类型会教导编译器如何解释某个特定地址中的内存内容及其大小。

``` CPP
#include <iostream>
using namespace std;
int main(int argc, char** argv)
{
    int num = 0x00636261; //用16进制表示32位int，0x61是字符'a'的ASCII码
    int * pnum = &num;
    char * pstr = reinterpret_cast<char*>(pnum);
    cout << "pnum指针的值: " << pnum << endl;
    cout << "pstr指针的值: " << static_cast<void*>(pstr) << endl; //直接输出pstr会输出其指向的字符串，这里的类型转换是为了保证输出pstr的值
    cout << "pnum指向的内容: " << hex << *pnum << endl;
    cout << "pstr指向的内容: " << pstr << endl;
    return 0;
    /*
    输出：
        pnum指针的值: 0x61fe0c
        pstr指针的值: 0x61fe0c
        pnum指向的内容: 636261
        pstr指向的内容: abc
    */
}
```

* 使用 `reinterpret_cast` 运算符把 `pnum` 从 `int*` 转变成 `char*` 类型并用于初始化 `pstr` 后，`pstr` 也指向 `num` 的内存区域，但是由于 `pstr` 是 `char*` 类型的，通过 `pstr` 读写 `num` 内存区域将不再按照整型变量的规则，而是按照 `char` 型变量规则。一个 `char` 型变量占用一个 `Byte` ，对 `pstr` 解引用得到的将是一个字符，也就是 `a` 。而在使用输出流输出 `pstr` 时，将输出 `pstr` 指向的内存区域的字符，那 `pstr` 指向的是一个的字符，那为什么输出三个字符呢？这是由于在输出 `char*` 指针时，输出流会把它当做输出一个字符串来处理，直至遇到 `\0` 才表示字符串结束。对代码稍做改动，就会得到不一样的输出结果，例如将 `num` 的值改为 `0x63006261` , 输出的字符串就变为 `ab` 。
* 上面的例子融合了一些巧妙的设计，我们在pstr指向的内存区域中故意地设置了结束符 `\0` 。假如将 `num` 的值改为 `0x64636261` ，运行结果会是怎样的呢？

<img src="地址.png" alt="内存示意图" style="zoom:100%;">
<img src="运行结果.png" alt="运行结果" style="zoom:100%;">

* 参考上面的内存示意图，思考一下为什么在输出”abcd”之后又输出了6个字符才结束。

#### 指针类型间的转换

* 在某些情况下，需要在不同指针类型之间进行转换，如将一个 `int` 指针转换为 `char` 指针。这在 C 语言中用的非常多，C语言中就是直接使用 `()` 进行强制类型转换

``` CPP
int a = 42;
int* int_ptr = &a;
char* char_ptr = reinterpret_cast<char*>(int_ptr); // 将int指针转换为char指针
```
