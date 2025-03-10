---
title: 数字大写转换
top_img: '114477944_p0.png'
cover: '122650009_p0.png'
permalink: /Eight-Part_Essay/C++/Digital_Conversion/
categories: 
    - 八股文
      - C++
tags: 
    - C++
    - 算法
---

* **该算法未经过大量数据验证！！！**

``` CPP
#include <iostream>
#include <string>
using namespace std;

string chinese[] = {"零", "一", "二", "三", "四", "五", "六", "七", "八", "九"};
string bit[] = {"", "十", "百", "千"};

string GetChinese(string num)
{
    if (stoi(num) == 0) return "";

    string result;
    for (int i = 0; i < num.size(); i++)
    {
        if (num[i] == '0' && i != num.size() - 1 && num[i + 1] != '0')
        {
            result += "零";
        }
        else if (num[i] != '0')
        {
            result += chinese[num[i] - '0'] + bit[num.size() - i - 1];
        }
    }
    return result;
}

string NumToChinese(string str)
{
    if (str.size() <= 4)
    {
        string gc = GetChinese(str);
        if (gc == "") return "零";
        return gc;
    }
    string result;
    string thousand = str.substr(str.size() - 4, 4);
    result = GetChinese(thousand);
    if (str.size() <= 8)
    {
        string tt = str.substr(0, str.size() - 4);
        string gc = GetChinese(tt);
        if (gc != "") result = gc + "万" + result;
        return result;
    }
    else if (str.size() > 8)
    {
        string tt = str.substr(str.size() - 8, 4);
        string gc = GetChinese(tt);
        if (gc != "") result = gc + "万" + result;
        return NumToChinese(str.substr(0, str.size() - 8)) + "亿" + result;
    }
    return result;
}

int main()
{
    long long num;
    cin >> num;
    string result = NumToChinese(to_string(num));
    cout << result;
}
```
