---
title: 手撕队列（queue）
top_img: '66974323_p0.jpg'
cover: '72027189_p0.png'
categories: 
    - 八股文
      - C++ 
tags: 
    - C++
    - 数据结构
---

## 手撕队列（基于数组）

* 关键点在于头指针和尾指针在队列清空时的调整（MyQueue::pop()），不当的操作可能导致清空后的空间无法重复使用，不断扩容。
* 可对扩容操作（MyQueue::push()）做进一步优化（数据前移）。

``` CPP
#include<iostream>
using namespace std;

template <typename T>
class MyQueue
{
private:
    T* _data;
    int _front;
    int _rear;
    int _size;
    int _capacity;

public:
    MyQueue();
    MyQueue(int capacity);
    ~MyQueue();
    T front();
    T back();
    T size();
    T pop();
    bool empty();
    void push(T num);
};

template <typename T>
MyQueue<T>::MyQueue() : _front(0), _rear(-1), _size(0), _capacity(4)
{
    _data = new T[4];
}

template <typename T>
MyQueue<T>::MyQueue(int capacity) : _front(0), _rear(-1), _size(0), _capacity(capacity)
{
    _data = new T[capacity];
}

template <typename T>
MyQueue<T>::~MyQueue()
{
    delete[] _data;
    _data = nullptr;
}

template <typename T>
T MyQueue<T>::front()
{
    if (_size == 0) throw "队列为空";
    return _data[_front];
}

template <typename T>
T MyQueue<T>::back()
{
    if (_size == 0) throw "队列为空";
    return _data[_rear];
}

template <typename T>
T MyQueue<T>::size()
{
    return _size;
}

template <typename T>
T MyQueue<T>::pop()
{
    if (_size == 0) throw "队列为空";
    T value = _data[_front];
    _front = (_front + 1) % _capacity;
    if (_front == 0) _rear = 0;
    --_size;
    return value;
}

template <typename T>
bool MyQueue<T>::empty()
{
    return _size == 0;
}

template <typename T>
void MyQueue<T>::push(T element)
{
    if (_size == _capacity)
    {
        _capacity *= 2;
        T* newData = new T[_capacity];
        for (int i = 0; i < _size; ++i)
        {
            newData[i] = _data[i];
        }
        delete[] _data;
        _data = newData;
        newData = nullptr;
    }
    ++_rear;
    _data[_rear] = element;
    ++_size;
}
```

## 手撕队列（基于链表）

``` CPP
待更新
```
