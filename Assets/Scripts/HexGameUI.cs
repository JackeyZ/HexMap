using UnityEngine.EventSystems;
using UnityEngine;
using System;

/// <summary>
/// 名称：游戏模式下的UI脚本
/// 作用：用于控制游戏模式下的六边形地图UI
/// </summary>
public class HexGameUI : MonoBehaviour
{
    public HexGrid grid;

    HexUnit selectedUnit;                                   // 当前选中的移动单位

    HexCell currentCell;                                    // 当前鼠标所在的六边形格子

    /// <summary>
    /// 设置是否是编辑模式，如果是编辑模式则停用该脚本
    /// </summary>
    /// <param name="toggle"></param>
    public void SetEditMode(bool toggle)
    {
        enabled = !toggle;
        grid.ShowUI(!toggle);
        grid.ClearPath();
    }

    /// <summary>
    /// 更新当前鼠标所在的六边形
    /// </summary>
    /// <returns></returns>
    bool UpdateCurrentCell()
    {
        HexCell cell = grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
        if (cell != currentCell)
        {
            currentCell = cell;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 选择单位
    /// </summary>
    void DoSelection()
    {
        grid.ClearPath();
        UpdateCurrentCell();
        if (currentCell)
        {
            selectedUnit = currentCell.Unit;
        }
    }

    void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.GetMouseButtonDown(0))
            {
                DoSelection();
            }
            else if (selectedUnit)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    DoMove();
                }
                else
                {
                    DoPathfinding();
                }
            }
        }
    }

    /// <summary>
    /// 查找选中单位所在格子与当前鼠标所在的格子之间的路径
    /// </summary>
    private void DoPathfinding()
    {
        if (UpdateCurrentCell())
        {
            if (currentCell && selectedUnit.IsValidDestination(currentCell))
            {
                grid.FindPath(selectedUnit.Location, currentCell, 24);
            }
            else
            {
                grid.ClearPath();           // 若没有当前格子（鼠标位于地图外），则清除显示的路径
            }
        }
    }

    /// <summary>
    /// 移动地图上的单位
    /// </summary>
    void DoMove()
    {
        if (grid.HasPath)
        {
            selectedUnit.Travel(grid.GetPath());
            grid.ClearPath();
        }
    }
}
