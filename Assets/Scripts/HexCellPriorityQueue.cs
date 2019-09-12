using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 名称：A*未访问边界优先级队列
/// 作用：用在寻路里
/// </summary>
public class HexCellPriorityQueue
{
    List<HexCell> list = new List<HexCell>();
    int count = 0;
    int minimum = int.MaxValue;     // 用于记录队列的最小的优先级引索

    public int Count
    {
        get
        {
            return count;
        }
    }

    /// <summary>
    /// 入队
    /// </summary>
    /// <returns></returns>
    public void Enqueue(HexCell cell)
    {
        count += 1;
        int priority = cell.SearchPriority;
        // 调整最低优先级
        if (priority < minimum)
        {
            minimum = priority;
        }
        // 扩容
        while (priority >= list.Count)
        {
            list.Add(null);
        }
        cell.NextWithSamePriority = list[priority];             // 从头部插入到邻接链表中
        list[priority] = cell;
    }

    /// <summary>
    /// 出队
    /// </summary>
    /// <returns></returns>
    public HexCell Dequeue()
    {
        count -= 1;
        // 从最小的优先级引索开始便利，返回第一个存在的格子
        for (; minimum < list.Count; minimum++)
        {
            HexCell cell = list[minimum];
            if (cell != null)
            {
                list[minimum] = cell.NextWithSamePriority;      // 对应优先级的邻接链表，头部出队，后面接上
                return cell;
            }
        }
        return null;
    }

    /// <summary>
    /// 修改一个格子的优先级
    /// </summary>
    /// <param name="cell">已赋值新优先级的格子</param>
    /// <param name="cell">旧的优先级</param>
    public void Change(HexCell cell, int oldPriority)
    {
        HexCell current = list[oldPriority];                // 旧的优先级对应的邻接链表头部
        HexCell next = current.NextWithSamePriority;        // 旧的优先级对应的邻接链表第二个节点
        // 判断当前节点是否是所需改变优先级的格子
        if (current == cell)
        {
            list[oldPriority] = next;                       // 第二个节点作为头部
        }
        else {
            // 遍历邻接链表，直到找到所需更改优先级的格子
            while (next != cell)
            {
                current = next;
                next = current.NextWithSamePriority;
            }
            current.NextWithSamePriority = cell.NextWithSamePriority;   // 把头部指向第二个节点的指针直接指向第三个节点
        }
        
        // 所需更改优先级的格子重新入队
        Enqueue(cell);
        count -= 1;
    }

    public void Clear()
    {
        list.Clear();
        count = 0;
        minimum = int.MaxValue;
    }
}
