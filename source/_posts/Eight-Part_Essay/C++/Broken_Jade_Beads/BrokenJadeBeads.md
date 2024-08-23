---
title: 碎玉零珠———— C++
top_img: '111303012_p0.jpg'
cover: '120131650_p0.png'
categories: 
    - 八股文
      - C++
tags: 
    - C++
---

## const 修饰的函数能否重载？

* **const修饰的函数可以重载**。const成员函数既不能改变类内的数据成员，也无法调用非const的成员函数；const类对象只能调用const成员函数。非const对象无论是否是const成员函数都能调用，但是如果有重载的非const函数，非const对象会优先调用重载后的非const函数。

## const 与 static 关键字
