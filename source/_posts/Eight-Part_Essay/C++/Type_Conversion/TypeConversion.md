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

### const_cast

### reinterpret_cast
