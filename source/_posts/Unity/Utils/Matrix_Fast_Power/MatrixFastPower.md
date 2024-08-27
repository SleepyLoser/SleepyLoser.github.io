---
title: 矩阵快速幂
top_img: '71463839_p0.png'
cover: '68296718_p0.png'
categories: 
    - Unity
      - 算法
tags: 
    - 算法
---

## 快速幂

* 快速幂是一种高效的指数运算方法，通过指数折半或二进制位运算减少计算次数。
* 如下：快速计算base的power次方

``` CPP
// base：底数，power：指数
long long FastPower(int base, int power)
{
    long long result = 1;
    while (power)
    {
        // 表示按位与操作，相当于power % 2
        if (power & 1) result *= base;
        // 右移运算符，相当于 power /= 2
        power >>= 1;
        base *= base;
    }
    return result;
}

// 快速幂取模运算
long long FastPower(int base, int power, int model)
{
    long long result = 1;
    while (power)
    {
        if (power & 1) result = (result * base) % model;
        power >>= 1;
        base = (base * base) % model;
    }
    return result;
}
```

## 矩阵快速幂

* 例：降低斐波那契（Fibonacci）数列的时间、空间复杂度

<img src="juzhen.png" alt="推导过程" style="zoom:50%;">

``` CPP
// 矩阵相乘运算
// x，y：需要计算的两个矩阵
vector<vector<int>> MatrixMul(vector<vector<int>>& x, vector<vector<int>>& y)
{
    int sizeARow = x.size();
    int sizeAColumn = x[0].size();
    int sizeBColumn = y[0].size();
    vector<vector<int>> result(sizeARow, vector<int>(sizeBColumn, 0));
    for (int i = 0; i < sizeARow; ++i)
    {
        for (int j = 0; j < sizeBColumn; ++j)
        {
            for (int k = 0; k < sizeAColumn; ++k)
            {
                result[i][j] += x[i][k] * y[k][j];
            }
        }
    }
    return result;
}

// 矩阵相乘取模运算
vector<vector<int>> MatrixMul(vector<vector<int>>& x, vector<vector<int>>& y, int model)
{
    int sizeARow = x.size();
    int sizeAColumn = x[0].size();
    int sizeBColumn = y[0].size();
    vector<vector<int>> result(sizeARow, vector<int>(sizeBColumn, 0));
    for (int i = 0; i < sizeARow; ++i)
    {
        for (int j = 0; j < sizeBColumn; ++j)
        {
            for (int k = 0; k < sizeAColumn; ++k)
            {
                result[i][j] = (result[i][j] + (x[i][k] % model) * (y[k][j] % model)) % model;
            }
        }
    }
    return result;
}

// 计算一个2 * 2矩阵的n次方
vector<vector<int>> MatrixFastPower(int n)
{
    --n;
    // 初始矩阵
    vector<vector<int>> matrix = {{1, 1}, {1, 0}};
    // 初始矩阵的n - 1次方
    vector<vector<int>> result = {{1, 0}, {0, 1}};
    while (n)
    {
        if (n & 1) result = MatrixMul(result, matrix);
        n >>= 1;
        matrix = MatrixMul(matrix, matrix);
    }
    return result;
}
```
