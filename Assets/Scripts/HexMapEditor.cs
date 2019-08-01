using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{
    public Color[] colors;

    public HexGrid hexGrid;

    private Color activeColor;      // 当前选中的颜色

    int activeElevation;            // 高度

    int activeWaterLevel;           // 水平面高度

    int activeUrbanLevel = 0;       // 城市等级

    int activeFarmLevel = 0;        // 农场等级

    int activePlantLevel = 0;       // 植物等级

    bool applyColor = false;        // 是否开启颜色编辑

    bool applyElevation = true;     // 是否开启高度编辑

    bool applyWaterLevel = false;   // 是否开启水平面高度编辑

    bool applyUrbanLevel = false;   // 是否开启城市等级编辑

    bool applyFarmLevel = false;    // 是否开启农场等级编辑

    bool applyPlantLevel = false;   // 是否开启植物等级编辑

    int brushSize = 0;              // 画刷大小

    OptionalToggle riverMode;       // 河流添加模式

    OptionalToggle roadMode;        // 道路添加模式

    OptionalToggle walledMode;      // 围墙添加模式

    bool isDrag;                    // 鼠标是否在拖拽

    HexDirection dragDirection;     // 拖拽方向

    HexCell previousCell;           // 用于在拖拽中记录上一个的六边形

    void Awake()
    {
        SelectColor(-1);
    }

    void Update()
    {
        // 点击鼠标左键，并且该次点击事件没有穿透GameObject
        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            HandleInput();
        }
        else
        {
            previousCell = null;        // 没有拖拽的时候把上一个的六边形置空
        }
    }

    /// <summary>
    /// 选择颜色，UI上的toggle调用
    /// </summary>
    /// <param name="index"></param>
    public void SelectColor(int index)
    {
        applyColor = index >= 0;
        if(applyColor)
            activeColor = colors[index];
    }

    /// <summary>
    /// 设置高度，UI上的Slider调用
    /// </summary>
    /// <param name="elevation">高度</param>
    public void SetElevation(float elevation)
    {
        activeElevation = (int)elevation;
    }

    /// <summary>
    /// 设置水平面高度，UI上的Slider调用
    /// </summary>
    /// <param name="waterLevel"></param>
    public void SetWaterLevel(float waterLevel)
    {
        activeWaterLevel = (int)waterLevel;
    }

    /// <summary>
    /// 是否更变高度
    /// </summary>
    /// <param name="toggle"></param>
    public void SetApplyElevation(bool toggle)
    {
        applyElevation = toggle;
    }

    /// <summary>
    /// 是否更变水平面
    /// </summary>
    /// <param name="toggle"></param>
    public void SetApplyWaterLevel(bool toggle)
    {
        applyWaterLevel = toggle;
    }

    /// <summary>
    /// 是否更变城市等级
    /// </summary>
    /// <param name="toggle"></param>
    public void SetApplyUrbanLevel(bool toggle)
    {
        applyUrbanLevel = toggle;
    }

    /// <summary>
    /// 更改城市等级
    /// </summary>
    /// <param name="urbanLevel"></param>
    public void SetUrbanLevel(float urbanLevel)
    {
        activeUrbanLevel = (int)urbanLevel;
    }
    /// <summary>
    /// 是否更变农场等级
    /// </summary>
    /// <param name="toggle"></param>
    public void SetApplyFarmLevel(bool toggle)
    {
        applyFarmLevel = toggle;
    }

    /// <summary>
    /// 更改农场等级
    /// </summary>
    /// <param name="urbanLevel"></param>
    public void SetFarmLevel(float urbanLevel)
    {
        activeFarmLevel = (int)urbanLevel;
    }
    /// <summary>
    /// 是否更变植物等级
    /// </summary>
    /// <param name="toggle"></param>
    public void SetApplyPlantLevel(bool toggle)
    {
        applyPlantLevel = toggle;
    }

    /// <summary>
    /// 更改植物等级
    /// </summary>
    /// <param name="urbanLevel"></param>
    public void SetPlantLevel(float urbanLevel)
    {
        activePlantLevel = (int)urbanLevel;
    }

    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {
            HexCell currentCell = hexGrid.GetCell(hit.point);
            if (previousCell && previousCell != currentCell)
            {
                ValidateDrag(currentCell);
            }
            else
            {
                isDrag = false;
            }
            EditCells(hexGrid.GetCell(hit.point));
            previousCell = currentCell;
            isDrag = true;
        }
        else
        {
            previousCell = null;        // 如果没有点击到六边形上，则把上一个的六边形置空
        }
    }

    // 拖拽
    void ValidateDrag(HexCell currentCell)
    {
        for (dragDirection = HexDirection.NE ; dragDirection <= HexDirection.NW ; dragDirection++)
        {
            if (previousCell.GetNeighbor(dragDirection) == currentCell)
            {
                isDrag = true;
                return;
            }
        }
        isDrag = false;
    }

    void EditCells(HexCell center)
    {
        int centerX = center.Coordinates.X;
        int centerZ = center.Coordinates.Z;
        
        for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++)
        {
            for (int x = centerX - r; x <= centerX + brushSize; x++)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
        for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++)
        {
            for (int x = centerX - brushSize; x <= centerX + r; x++)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
    }

    void EditCell(HexCell cell)
    {
        if (cell)
        {
            // 单元那颜色
            if (applyColor)
            {
                cell.Color = activeColor;
            }
            // 单元高度
            if (applyElevation)
            {
                cell.Elevation = activeElevation;
            }

            // 单元水平面高度
            if (applyWaterLevel)
            {
                cell.WaterLevel = activeWaterLevel;
            }

            // 单元城市密度
            if (applyUrbanLevel)
            {
                cell.UrbanLevel = activeUrbanLevel;
            }

            // 单元农场密度
            if (applyFarmLevel)
            {
                cell.FarmLevel = activeFarmLevel;
            }

            // 单元植物密度
            if (applyPlantLevel)
            {
                cell.PlantLevel = activePlantLevel;
            }

            // 移除河流
            if (riverMode == OptionalToggle.No)
            {
                cell.RemoveRiver();
            }
            // 移除道路
            if (roadMode == OptionalToggle.No)
            {
                cell.RemoveRoads();
            }

            // 添加或移除围墙
            if (walledMode != OptionalToggle.Ignore)
            {
                cell.Walled = walledMode == OptionalToggle.Yes;
            }

            // 添加河流
            else if (isDrag)
            {
                // 获得当前六边形拖拽方向反方向的六边形，使画刷也能批量添加河流、道路
                HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
                if (otherCell)
                {
                    if (riverMode == OptionalToggle.Yes)
                    {
                        otherCell.SetOutgoingRiver(dragDirection);
                    }
                    if (roadMode == OptionalToggle.Yes)
                    {
                        otherCell.AddRoad(dragDirection);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 设置画刷大小
    /// </summary>
    /// <param name="size"></param>
    public void SetBrushSize(float size)
    {
        brushSize = (int)size;
    }
    public void SetRiverMode(int mode)
    {
        riverMode = (OptionalToggle)mode;
    }

    /// <summary>
    /// 设置道路编辑模式（忽略、添加、移除）
    /// </summary>
    /// <param name="mode"></param>
    public void SetRoadMode(int mode)
    {
        roadMode = (OptionalToggle)mode;
    }

    /// <summary>
    /// 设置围墙编辑模式（忽略、添加、移除）
    /// </summary>
    /// <param name="mode"></param>
    public void SetWalledMode(int mode)
    {
        walledMode = (OptionalToggle)mode;
    }
}
