---
title: 左值、右值、纯右值、将亡值
top_img: '114291323_p0.png'
cover: '107162316_p0.png'
categories: 
    - 八股文
      - C++ 
tags: 
    - C++
---

## 左值、右值、纯右值、将亡值

* C++11使用下面两种独立的性质来区别类别：
    1. **拥有身份**：指代某个非临时对象。
    2. **可被移动**：可被右值引用类型匹配。
* 每个C++表达式只属于三种基本值类别中的一种：`左值 (lvalue)`、`纯右值 (prvalue)`、`将亡值 (xvalue)`
    1. 拥有身份且不可被移动的表达式被称作 `左值 (lvalue)` 表达式，指持久存在的对象或类型为左值引用类型的返还值。
    2. 拥有身份且可被移动的表达式被称作 `将亡值 (xvalue)` 表达式，一般是指类型为右值引用类型的返还值。
    3. 不拥有身份且可被移动的表达式被称作 `纯右值 (prvalue)` 表达式，也就是指纯粹的临时值（即使指代的对象是持久存在的）。
    4. 不拥有身份且不可被移动的表达式无法使用。
* 如此分类是因为移动语义的出现，需要对类别重新规范说明。例如不能简单定义说右值就是临时值（因为也可能是 `std::move` 过的对象，该代指对象并不一定是临时值）。

### 左值

  1. 左值是一个数据的表达式（如变量名或引用的指针），我们**可以获取到它的地址**，正常情况下是可以能够对它赋值
  2. **定义const修饰后的左值，不能给它赋值**，但是可以取出它的地址
  3. 左值可以出现在赋值符号`（ " = " ）`的左边，也可以出现在赋值符号`（" = "）`的右边
  4. 左值具有持久的状态

``` CPP
int lValue = 5;
int* lValueP = new int();
const int lValueC = 10;
```

### 左值引用

1. 左值引用是对左值的一种引用，相当于给左值取别名
2. 普通的左值引用不能引用右值，**但是const的左值引用可以引用右值**
3. 引用方法: `类型+&` ，例如： `int& lvalueReferenceP = lValueP` ;

``` CPP
int& lValueReference = lValue;
int*& lValueReferenceP = lValueP;
const int& lValueReferenceC_1 = 10; // const的左值引用，引用右值
const int& lValueReferenceC_2 = 10 + 20; // const的左值引用，引用右值
```

### 右值

1. 右值也是一个数据表达式，右值是**字面常量**或者是求值过程中创建的**临时对象**
2. 右值的生命周期是短暂的，如：`字面常量`，`表达式返回值`，`函数返回值`（不是左值引用的返回值），`临时变量`，`匿名对象`等等
3. 右值不能出现在赋值符号的左边，右值也**不能取出地址**，更**不能对它赋值**

``` CPP
// 以下是常见的右值
x + y; // 表达式返回值
function(x, y); // 函数返回值
10; // 常量
```

### 右值引用

1. 右值引用是给右值取别名，**所有的右值引用是不能引用左值**
2. **右值是不能取出地址的**，但是当右值**取别名后，这个右值会被存到特定的位置，且可以取到该值的地址**，也就是说右值引用值是一个左值
3. 右值引用会开辟一块空间去存右值，其中普通的右值引用是可以被修改这块空间的，const的右值引用时不可以被修改的

``` CPP
int&& rValueReference = x + y; // 正确
rValueReference = 20; // 正确，普通的右值引用可以被修改
int&& rValueReference = x; // 错误，右值引用不能引用左值
const int rValueReferenceC = 10;
rValueReferenceC = 30; // 错误，rValueReferenceC是const右值引用，不能被修改
// 标准库中的move函数可以将一个左值强制转换为右值
int&& rValueReference = move(x); // 正确，move将x转化为右值
```

#### 右值引用作为函数参数

``` CPP
// 类似这样的函数可以接受右值参数
void foo(int&& x) 
{
    // ...
}
```

#### 右值引用和移动语义

* 右值引用与移动语义密切相关。通过使用右值引用，可以实现资源的所有权转移而不进行深层拷贝。这可以通过移动构造函数和移动赋值运算符来实现。右值引用还为实现`完美转发（perfect forwarding）`提供了支持，这在泛型编程和模板元编程中非常有用。

``` CPP
#include <iostream>

class MyClass 
{
private:
    int* data;

public:
    // 默认构造函数
    MyClass() : data(nullptr) 
    {
        std::cout << "Default constructor called" << std::endl;
    }

    // 构造函数
    MyClass(int value) : data(new int(value)) 
    {
        std::cout << "Constructor called" << std::endl;
    }

    // 移动构造函数
    MyClass(MyClass&& other) noexcept : data(other.data) 
    {
        std::cout << "Move constructor called" << std::endl;
        other.data = nullptr; // 避免其他对象释放资源时重复释放
    }

    // 移动赋值运算符
    MyClass& operator=(MyClass&& other) noexcept 
    {
        std::cout << "Move assignment operator called" << std::endl;
        if (this != &other) 
        {
            delete data; // 释放当前对象的资源
            data = other.data; // 转移资源所有权
            other.data = nullptr; // 避免其他对象释放资源时重复释放
        }
        return *this;
    }

    // 析构函数
    ~MyClass() 
    {
        delete data;
        std::cout << "Destructor called" << std::endl;
    }

    // 打印数据
    void printData() const 
    {
        if (data) 
        {
            std::cout << "Data: " << *data << std::endl;
        } 
        else 
        {
            std::cout << "Data: nullptr" << std::endl;
        }
    }
};

int main() 
{
    // 创建对象
    MyClass obj1(10);
    obj1.printData(); // 打印: Data: 10

    // 使用移动构造函数转移资源
    MyClass obj2 = std::move(obj1);
    obj2.printData(); // 打印: Data: 10
    obj1.printData(); // 打印: Data: nullptr

    // 使用移动赋值运算符转移资源
    MyClass obj3;
    obj3 = std::move(obj2);
    obj3.printData(); // 打印: Data: 10
    obj2.printData(); // 打印: Data: nullptr

    return 0;
}
```

#### 右值引用注意点

* 当你传递一个临时对象（右值）给接受右值引用参数的函数时，该临时对象的生命周期将与函数调用的生命周期绑定在一起。这意味着在函数调用结束后，临时对象将被销毁，因此你不能再安全地访问它。

### std::move() 函数

* `std::move()` 是一个模板函数，它将一个左值转换为对应的右值引用。这对于支持移动语义很有用。

``` CPP
int x = 5;
int&& rx = std::move(x); // 将 x 转换为右值引用
```

* `std::move()` 是一个 C++ 标准库中的函数模板，位于 `<utility>` 头文件中。它的作用是将一个 `左值（lvalue）`转换为对应的 `右值引用（rvalue reference）`，从而允许移动语义的使用。

``` CPP
template<typename T>
typename std::remove_reference<T>::type&& move(T&& arg) noexcept;
```

* `std::move()` 函数的定义使用了模板元编程技术，通过参数推导来接受任意类型的参数，并返回对应类型的右值引用。具体来说，`std::move()` 接受一个参数 `arg` ，并将其转换为对应类型的右值引用。这个参数可以是任何类型，包括用户定义的类型、标准库类型或者内置类型。
* 使用 `std::move()` 的主要目的是为了支持移动语义。当我们需要将资源从一个对象转移至另一个对象时，通常需要使用移动语义来避免不必要的深层拷贝。`std::move()` 提供了一种简单的方法来显式表示我们正在进行资源的转移而不是拷贝。
* 使用 `std::move()` 的一般步骤如下：
  1. 定义一个对象，它包含某种资源（如内存、文件句柄等）;
  2. 当我们确定不再需要原始对象中的资源，并且想要将资源转移到另一个对象时，使用 `std::move()` 将原始对象转换为右值引用;
  3. 将右值引用传递给接受右值引用参数的构造函数、赋值运算符或者其他函数。

``` CPP
#include <iostream>
#include <utility>

class MyClass 
{
public:
    MyClass() { std::cout << "Constructor" << std::endl; }
    MyClass(const MyClass& other) { std::cout << "Copy constructor" << std::endl; }
    MyClass(MyClass&& other) { std::cout << "Move constructor" << std::endl; }
};

int main() 
{
    MyClass original;
    MyClass moved = std::move(original); // 使用 std::move() 将 original 转移为右值
    return 0;
}
```

* 在这个示例中，当我们使用 `std::move(original)` 时，`original` 被显式转换为右值引用，从而调用了移动构造函数。这样，资源可以从 `original` 对象转移到 `moved` 对象，而不需要执行深层拷贝。
* **需要注意的是，`std::move()` 本身并不执行任何移动操作，它只是将其参数转换为对应的右值引用，实际的资源转移操作是由接受右值引用的构造函数或者赋值运算符执行的**。

### 完美转发（Perfect Forwarding）

* 为解决 `引用折叠` 问题，必须写一个任意参数的函数模板，并转发到其他函数。比如当右值引用作为参数时，虽然名义上接收的是右值，但是向下传递时，已经改变为了左值。我们希望左值转发之后还是左值，右值转发后还是右值，我们想让它保持原有的属性。

#### 引用折叠

* 引用折叠就是，如果间接创建一个引用的引用， 那么这些引用就会折叠。
* 规则：
  1. `&& + && -> &&`：右值的右值引用是右值
  2. `&& + & -> &`：右值的左值引用是左值
  3. `& + && -> &`：左值的右值引用是左值
  4. `& + & -> &`：左值的左值引用是左值

## 总结

* **左值（lvalue）** 指持久存在（有变量名）的对象或返还值类型为左值引用的返还值，是不可移动的。
* **右值（rvalue）** 包含了 `将亡值`、`纯右值`，**是可移动（可被右值引用类型匹配）的值**。
* 实际上C++ `std::move` 函数的实现原理就是**强转成右值引用类型并返还之**，因此该返还值会被判断为将亡值，更宽泛的说是被判定为右值。

``` CPP
Vector& func1();
Vector&& func2();
Vector func3();

int main()
{
    Vector v;

    v;              //左值表达式
    func1();        //左值表达式，返还值是临时的，返还类型是左值引用，因此被认为不可移动。
    func2();        //将亡值表达式，返还值是临时的，返还类型是右值引用，因此指代的对象即使非临时也会被认为可移动。
    func3();        //纯右值表达式，返还值为临时值。
    std::move(v)；  //将亡值表达式，std::move本质也是个函数，同上。
    Vector();       //纯右值表达式
}
```
