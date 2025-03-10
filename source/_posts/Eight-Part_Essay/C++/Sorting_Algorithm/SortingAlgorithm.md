---
title: 排序算法
top_img: '100723659_p0.jpg'
cover: '74182068_p0.jpg'
permalink: /Eight-Part_Essay/C++/Sorting_Algorithm/
categories: 
    - 八股文
      - C++
tags: 
    - C++
    - 算法
---

## 各种排序算法的原理和时间复杂度

* **快速排序**：一轮划分，选择一个基准值，小于该基准值的元素放到左边，大于的放在右边，此时该基准值在整个序列中的位置就确定了，接着递归地对左边子序列和右边子序列进行划分。时间复杂度O(nlogn)，最坏的时间复杂度是O(n^2)。**需要排序的对象越有序，快速排序的退化程度越高，即时间复杂度越趋向于O(n ^ 2)**。通过随机选择枢轴，可以有效地避免这种情况。随机选择使得每次分区都有较大概率是平衡的，从而保持了快速排序的平均时间复杂度 O(n * logn)；

``` CPP
void QuickSort(vector<int>& nums, int left, int right)
{
    if (left >= right)
        return;

    int pivot = rand() % (right - left + 1) + left;
    swap(nums[pivot], nums[right]);

    pivot = nums[right];
    int i = left;
    for (int j = left; j < right; ++j)
    {
        if (nums[j] < pivot)
        {
            swap(nums[i++], nums[j]);
        }
    }
    swap(nums[i], nums[right]);

    QuickSort(nums, left, i - 1);
    QuickSort(nums, i + 1, right);
}

vector<int> SortArray(vector<int>& nums) 
{
    srand((unsigned)time(NULL));
    QuickSort(nums, 0, nums.size() - 1);
    return nums;
}

int main()
{
    vector<int> nums = {1, 3, 10, 32, 32, 2, 5, 6, 0, 4, -1, 100};
    SortArray(nums);
    for (int num : nums) cout << num << " ";
}
```

* **堆排序**：利用完全二叉树性质构造的一个一维数组，用数组下标代表结点，则一个结点的左孩子下标为2i+1,右孩子为2i+2，一个结点的父节点为(i-1)/2。堆排序的思想就是，构造一个最大堆或者最小堆，以最大堆为例，那么最大的值就是根节点，把这个最大值和最后一个结点交换，然后在从前n-1个结点中构造一个最大堆，再重复上述的操作，即每次将现有序列的最大值放在现有数组的最后一位，最后就会形成一个有序数组；求升序用最大堆，降序用最小堆。时间复杂度O(nlogn)；
* **冒泡排序**：从前往后两两比较，逆序则交换，不断重复直到有序；时间复杂度O(n^2)，最好情况O(n)；
* **插入排序**：类似打牌，从第二个元素开始，把每个元素插入前面有序的序列中；时间复杂度O(n^2)，最好情况O(n)；
* **选择排序**：每次选择待排序列中的最小值和未排序列中的首元素交换；时间复杂度O(n^2);
* **归并排序**：将整个序列划分成最小的>=2的等长序列，排序后再合并，再排序再合并，最后合成一个完整序列。时间复杂度O(nlogn)。
* **希尔排序**：是插入排序的改进版，取一个步长划分为多个子序列进行排序，再合并（如135一个序列，246一个序列），时间复杂度O(n1.3)，最好O(n)，最坏O(n^2)；
* **桶排序**：将数组分到有限数量的桶里。每个桶再个别排序，最后依次把各个桶中的记录列出来记得到有序序列。桶排序的平均时间复杂度为线性的O(N+C)，其中C = N * (logN - logM)，M为桶的数量。最好的情况下为O(N)。

| 排序算法       | 平均时间复杂度 | 最坏时间复杂度 | 最好时间复杂度 | 空间复杂度 | 稳定性 |
|----------------|----------------|----------------|----------------|------------|--------|
| 冒泡排序       | O(n²)          | O(n²)          | O(n)           | O(1)       | 稳定   |
| 选择排序       | O(n²)          | O(n²)          | O(n²)          | O(1)       | 不稳定 |
| 插入排序       | O(n²)          | O(n²)          | O(n)           | O(1)       | 稳定   |
| 快速排序       | O(n log n)     | O(n²)          | O(n log n)     | O(log n)   | 不稳定 |
| 堆排序         | O(n log n)     | O(n log n)     | O(n log n)     | O(1)       | 不稳定 |
| 希尔排序       | O(n log n)     | O(n²)          | O(n)           | O(1)       | 不稳定 |
| 归并排序       | O(n log n)     | O(n log n)     | O(n log n)     | O(n)       | 稳定   |
| 基数排序       | O(nk)          | O(nk)          | O(nk)          | O(n + k)   | 稳定   |
