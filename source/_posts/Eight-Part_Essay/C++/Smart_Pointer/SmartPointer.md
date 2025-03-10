---
title: 智能指针
top_img: '111557774_p0.png'
cover: '101188292_p0.png'
permalink: /Eight-Part_Essay/C++/Smart_Pointer/
categories: 
    - 八股文
      - C++
tags: 
    - C++
---

## unique_ptr

* 独占资源所有权的指针。由于没有引用计数，因此性能较好。
* 离开 `unique_ptr` 对象的作用域时，会自动释放资源。
* `unique_ptr` 本质是一个类，将复制构造函数和赋值构造函数声明为 `delete` 就可以实现独占式，
只允许移动构造和移动赋值。`unique_ptr` 所持有的对象只能通过 `转移语义(move)` 将所有权转移
到另外一个 `unique_ptr` 。

``` CPP
// 自定义实现 unique_ptr
UniquePtr(UniquePtr<T> const &) = delete;
UniquePtr & operator=(UniquePtr<T> const &) = delete;
```

``` CPP
std::unique_ptr<int> uptr = std::make_unique<int>(200);
// ...
// 离开 uptr 的作用域的时候自动释放内存
```

* `std::unique_ptr` 是 `move-only` 的。

``` CPP
std::unique_ptr<int> uptr = std::make_unique<int>(200);
std::unique_ptr<int> uptr1 = uptr;  // 编译错误，std::unique_ptr<T> 是 move-only 的

std::unique_ptr<int> uptr2 = std::move(uptr);
assert(uptr == nullptr);
```

* `std::unique_ptr` 可以指向一个数组。

``` CPP
std::unique_ptr<int[]> uptr = std::make_unique<int[]>(10);
for (int i = 0; i < 10; i++) 
{
    uptr[i] = i * i;
}   
for (int i = 0; i < 10; i++) 
{
    std::cout << uptr[i] << std::endl;
}
```

## shared_ptr

* `std::shared_ptr` 其实就是对资源做引用计数——当引用计数为 `0` 的时候，自动释放资源。

### C++ 智能指针的实现（手撕 shared_ptr）

``` CPP
#include <iostream>
#include <vector>
#include <unordered_set>

template <typename T>
class shared_ptr 
{
private:
    T* ptr;
    int* ref_count;
    // 释放方法为内置方法
    void release() 
    {
        if (ref_count) 
        {
            --(*ref_count);
            if (*ref_count == 0) 
            {
                delete ptr;
                delete ref_count;
            }
            ptr = nullptr;
            ref_count = nullptr;
        }
    }
public:
    // shared_ptr
    shared_ptr() : ptr(nullptr), ref_count(nullptr) {}
    // 用初始指针,new
    shared_ptr(T* p) : ptr(p), ref_count(new int(1)) {}
    // 拷贝构造
    shared_ptr(const shared_ptr& other) : ptr(other.ptr), ref_count(other.ref_count) 
    {
        if (ref_count) { ++(*ref_count); }
    }
    ~shared_ptr() { release(); }
    shared_ptr& operator=(const shared_ptr& other) 
    {
        if (this != &other) 
        {
            release();
            ptr = other.ptr;
            ref_count = other.ref_count;
            if (ref_count) { ++(*ref_count); }
        }
        return *this;
    }
    T* get() const { return ptr; }
    int use_count() const { return ref_count ? *ref_count : 0; }
    void reset() { release(); }
    void reset(T* p) 
    {
        release();
        ptr = p;
        ref_count = new int(1);
    }
    T& operator * () const { return *ptr; }
    T* operator -> () const { return ptr; }
};
```

### std::shared_ptr 的实现原理

* `shared_ptr` 需要维护的信息有两部分：
  1. 指向共享资源的指针
  2. 引用计数等共享资源的控制信息——实现上是维护一个指向控制信息的指针。
* 所以，`shared_ptr` 对象**需要保存两个指针（shared_ptr 大小为 16 ）**。`shared_ptr` 的 `deleter` 是保存在控制信息中，所以，是否有自定义 `deleter` 不影响 `shared_ptr` 对象的大小。
* 当我们创建一个 shared_ptr 时，其实现一般如下：

``` CPP
std::shared_ptr<T> sptr1(new T);
```

<img src="智能指针1.jpg" alt="智能指针1" style="zoom:100%;">

* 复制一个 shared_ptr：

``` CPP
std::shared_ptr<T> sptr2 = sptr1;
```

<img src="智能指针2.jpg" alt="智能指针2" style="zoom:100%;">

* 为什么控制信息和每个 `shared_ptr` 对象都需要保存指向共享资源的指针？可不可以去掉 `shared_ptr` 对象中指向共享资源的指针，以节省内存开销？
* 答案是：不能。 因为 `shared_ptr` 对象中的指针指向的对象不一定和控制块中的指针指向的对象一样。
* 来看一个例子:

``` CPP
struct Fruit 
{
    int juice;
};

struct Vegetable 
{
    int fiber;
};

struct Tomato : public Fruit, Vegetable 
{
    int sauce;
};

 // 由于继承的存在，shared_ptr 可能指向基类对象
std::shared_ptr<Tomato> tomato = std::make_shared<Tomato>();
std::shared_ptr<Fruit> fruit = tomato;
std::shared_ptr<Vegetable> vegetable = tomato;
```

<img src="智能指针3.jpg" alt="智能指针3" style="zoom:100%;">

* 另外，`std::shared_ptr` 支持 `aliasing constructor`

``` CPP
template< class Y >
shared_ptr( const shared_ptr<Y>& r, element_type* ptr ) noexcept;
```

* `Aliasing Constructor` : 简单说就是构造出来的 `shared_ptr` 对象和参数 `r` 指向同一个控制块（会影响 `r` 指向的资源的生命周期），但是指向共享资源的指针是参数 `ptr` 。看下面这个例子。

``` CPP
using Vec = std::vector<int>;
std::shared_ptr<int> GetSPtr() 
{
    auto elts = {0, 1, 2, 3, 4};
    std::shared_ptr<Vec> pvec = std::make_shared<Vec>(elts);
    return std::shared_ptr<int>(pvec, &(*pvec)[2]);
}

std::shared_ptr<int> sptr = GetSPtr();
for (int i = -2; i < 3; ++i) 
{
    printf("%d\n", sptr.get()[i]);
}
```

<img src="智能指针4.jpg" alt="智能指针4" style="zoom:100%;">

* 看上面的例子，使用 `std::shared_ptr` 时，会涉及两次内存分配：一次分配共享资源对象；一次分配控制块。C++ 标准库提供了 `std::make_shared` 函数来创建一个 `shared_ptr` 对象，只需要一次内存分配。

<img src="智能指针5.jpg" alt="智能指针5" style="zoom:100%;">

* 这种情况下，不用通过控制块中的指针，我们也能知道共享资源的位置——这个指针也可以省略掉。

<img src="智能指针6.jpg" alt="智能指针6" style="zoom:100%;">

### shared_ptr 是线程安全的吗

* **`shared_ptr` 在 C++ 中是部分线程安全的**, 这意味着它不是在所有情况下都是安全的

#### 线程安全的部分

* **`std::shared_ptr` 的引用计数（即管理对象的共享所有权的计数）是线程安全的**。因为`std::shared_ptr` 的内部引用计数是原子的，这意味着多个线程可以安全地对同一个 `std::shared_ptr` 对象进行引用计数的操作（如 `shared_ptr` 的拷贝构造和赋值）。这些操作不会导致数据竞争。

``` CPP
std::shared_ptr<int> p = std::make_shared<int>(0);
constexpr int N = 10000;
std::vector<std::shared_ptr<int>> sp_arr1(N);
std::vector<std::shared_ptr<int>> sp_arr2(N);
void increment_count(std::vector<std::shared_ptr<int>>& sp_arr) 
{    
    for (int i = 0; i < N; i++) 
    {        
        sp_arr[i] = p;    
    }
}
int main()
{    
    std::thread t1(increment_count, std::ref(sp_arr1));    
    std::thread t2(increment_count, std::ref(sp_arr2));    
    t1.join();    
    t2.join();    
    std::cout << p.use_count() << std::endl; // always 20001
    return 0;
}
```

* **初始引用计数**：`p` 初始时有一个引用计数，因为它本身就是一个 `std::shared_ptr` 。因此，初始的引用计数是 `1` 。
* **线程 `t1` 和 `t2` 的操作**：每个线程将 `p` 赋值给一个包含 `10000` 个元素的向量中的每个元素。每次赋值操作都会增加 `p` 的引用计数。由于有两个线程，每个线程都会增加 `10000` 次引用计数。因此，总的引用计数增加量是 `10000 + 10000 = 20000`
* **最终引用计数**：初始引用计数 `1` 加上两个线程增加的引用计数 `20000`，总引用计数为 `1 + 20000 = 20001`
* 这里的关键在于**每次赋值操作都会原子地增加引用计数**。因此，即使两个线程同时执行 `sp_arr[i] = p;` ，也不会导致数据竞争或未定义行为。

#### 线程不安全的部分

##### 对象的访问

* 尽管 `std::shared_ptr` 的引用计数是线程安全的，但对所管理对象的访问并不是线程安全的。如果多个线程同时访问同一个 `shared_ptr` 管理的对象，并且至少有一个线程在修改该对象，那么就需要额外的同步机制（如互斥锁）来确保线程安全。

``` CPP
std::shared_ptr<int> p1 = std::make_shared<int>(0);

void modify_memory() 
{    
    for (int i = 0; i < 10000; i++) 
    {        
        (*p1)++;    
    }
}

int main()
{    
    std::thread t1(modify_memory);    
    std::thread t2(modify_memory);    
    t1.join();    
    t2.join();    
    std::cout << "Final value of p: " << *p1 << std::endl; 
    return 0;
}
```

* 上面的代码运行，输出的结果不是预想的 `20000` ，每次运行输出的结果都会发生变化。因此同时修改 `shared_ptr` 指向的对象不是线程安全的。

##### 直接修改 shared_ptr 对象本身的指向

* 如果多个线程同时修改同一个 `std::shared_ptr` 对象的指向（例如，使用赋值操作或重置操作），这将导致**数据竞争**。
* 数据竞争可能导致以下问题：
  1. **引用计数的损坏**：如果一个线程在修改 shared_ptr 的指向时，另一个线程也在修改它，可能会导致引用计数不一致，从而导致内存泄漏或双重释放。
  2. **未定义行为**：访问已释放的内存或访问无效的指针。

``` CPP
#include <iostream>
#include <memory>
#include <thread>

std::shared_ptr<int> sharedPtr = std::make_shared<int>(42);  // 创建一个 shared_ptr

void modifySharedPtr() 
{   
    // 直接修改 shared_ptr 的指向    
    sharedPtr = std::make_shared<int>(100);  // 不安全的操作
}

int main() 
{   
    std::thread t1(modifySharedPtr);    
    std::thread t2(modifySharedPtr);
    t1.join();
    t2.join();
    std::cout << "Value: " << *sharedPtr << std::endl;  // 可能导致未定义行为
    return 0;
}
```

* 多次运行上面的代码会发现，输出的 `value` 值有时候会是一个乱码数字，不是预期的 `100` 。为了避免这些问题，需要使用`互斥锁（std::mutex）`来同步对 `sharedPtr` 的修改。

``` CPP
std::shared_ptr<int> sharedPtr = std::make_shared<int>(42);  // 创建一个 shared_ptr
std::mutex mtx;  // 互斥锁

void modifySharedPtr() 
{    
    std::lock_guard<std::mutex> lock(mtx);  // 加锁    
    sharedPtr = std::make_shared<int>(100);  // 安全地修改指向
}
```

## weak_ptr

* `std::weak_ptr` 要与 `std::shared_ptr` 一起使用。 一个 `std::weak_ptr` 对象看做是 `std::shared_ptr` 对象管理的资源的观察者，它不影响共享资源的生命周期：
  1. 如果需要使用 `weak_ptr` 正在观察的资源，可以将 `weak_ptr` 提升为 `shared_ptr`
  2. 当 `shared_ptr` 管理的资源被释放时，`weak_ptr` 会自动变成 `nullptr`

``` CPP
void Observe(std::weak_ptr<int> wptr) 
{
    if (auto sptr = wptr.lock()) 
    {
        std::cout << "value: " << *sptr << std::endl;
    } 
    else 
    {
        std::cout << "wptr lock fail" << std::endl;
    }
}

std::weak_ptr<int> wptr;
{
    auto sptr = std::make_shared<int>(111);
    wptr = sptr;
    Observe(wptr);  // sptr 指向的资源没被释放，wptr 可以成功提升为 shared_ptr
}
Observe(wptr);  // sptr 指向的资源已被释放，wptr 无法提升为 shared_ptr
```

<img src="智能指针7.jpg" alt="智能指针7" style="zoom:100%;">

* 当 `shared_ptr` 析构并释放共享资源的时候，只要 `weak_ptr` 对象还存在，控制块就会保留，`weak_ptr` 可以通过控制块观察到对象是否存活。

<img src="智能指针8.jpg" alt="智能指针8" style="zoom:100%;">

### enable_shared_from_this

* 一个类的成员函数如何获得指向自身（ `this` ）的 `shared_ptr` ？

``` CPP
class Foo 
{
    public:
    std::shared_ptr<Foo> GetSPtr() 
    {
        return std::shared_ptr<Foo>(this);
    }
};

auto sptr1 = std::make_shared<Foo>();
assert(sptr1.use_count() == 1);
auto sptr2 = sptr1->GetSPtr();
assert(sptr1.use_count() == 1);
assert(sptr2.use_count() == 1);
```

* 上面的代码其实会生成两个独立的 `shared_ptr` ，他们的控制块是独立的，最终导致一个 `Foo` 对象会被 `delete` 两次。

<img src="智能指针9.jpg" alt="智能指针9" style="zoom:100%;">

* 成员函数获取 `this` 的 `shared_ptr` 的正确的做法是继承 `std::enable_shared_from_this` 。

``` CPP
class Bar : public std::enable_shared_from_this<Bar> 
{
    public:
    std::shared_ptr<Bar> GetSPtr() 
    {
        return shared_from_this();
    }
};

auto sptr1 = std::make_shared<Bar>();
assert(sptr1.use_count() == 1);
auto sptr2 = sptr1->GetSPtr();
assert(sptr1.use_count() == 2);
assert(sptr2.use_count() == 2);
```

* 一般情况下，继承了 `std::enable_shared_from_this` 的子类，成员变量中会增加一个指向 `this` 的 `weak_ptr` 。这个 `weak_ptr` 在第一次创建 `shared_ptr` 的时候会被初始化，指向 `this` 。

<img src="智能指针10.jpg" alt="智能指针10" style="zoom:100%;">
