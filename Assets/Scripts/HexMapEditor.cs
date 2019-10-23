using UnityEngine;
using UnityEngine.EventSystems;
using System.IO;

public class HexMapEditor : MonoBehaviour
{
    public HexGrid hexGrid;

    public Material terrainMaterial;        // 地形材质

    int activeTerrainTypeIndex = -1;        // 当前地形类型引索

    int activeElevation;            // 高度

    int activeWaterLevel;           // 水平面高度

    int activeUrbanLevel = 0;       // 城市等级

    int activeFarmLevel = 0;        // 农场等级

    int activePlantLevel = 0;       // 植物等级

    int activeSpecialIndex = 0;     // 特殊特征物体下标，0表示没有

    bool applyElevation = true;     // 是否开启高度编辑

    bool applyWaterLevel = false;   // 是否开启水平面高度编辑

    bool applyUrbanLevel = false;   // 是否开启城市等级编辑

    bool applyFarmLevel = false;    // 是否开启农场等级编辑

    bool applyPlantLevel = false;   // 是否开启植物等级编辑

    bool applySpecialIndex = false; // 是否开启特殊特征物体下标编辑

    //bool editMode = true;          // 是否开启编辑模式

    int brushSize = 0;              // 画刷大小

    OptionalToggle riverMode;       // 河流添加模式

    OptionalToggle roadMode;        // 道路添加模式

    OptionalToggle walledMode;      // 围墙添加模式

    bool isDrag;                    // 鼠标是否在拖拽

    HexDirection dragDirection;     // 拖拽方向

    HexCell previousCell;           // 用于在拖拽中记录上一个的六边形

    HexCell searchFromCell;         // 寻路的起始六边形

    HexCell searchToCell;           // 寻路的目标六边形

    void Awake()
    {
        ShowGrid(false);            // 默认不显示网格
        SetEditMode(false);         // 默认不在编辑模式
    }

    void Update()
    {
        // 如果该次点击事件没有穿透GameObject
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            // 点击鼠标左键
            if (Input.GetMouseButton(0))
            {
                HandleInput();
                return;
            }

            // 创建或销毁移动单位
            if (Input.GetKeyDown(KeyCode.U))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    DestroyUnit();
                }
                else {
                    CreateUnit();
                }
                return;
            }
        }
        previousCell = null;        // 没有拖拽的时候把上一个的六边形置空
    }
    
    /// <summary>
    ///  设置地形类型引索
    /// </summary>
    /// <param name="index"></param>
    public void SetTerrainTypeIndex(int index)
    {
        activeTerrainTypeIndex = index;
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

    /// <summary>
    /// 是否更改特殊特征物体下标
    /// </summary>
    /// <param name="toggle"></param>
    public void SetApplySpecialIndex(bool toggle)
    {
        applySpecialIndex = toggle;
    }

    /// <summary>
    /// 更改特殊特征物体下标
    /// </summary>
    /// <param name="index"></param>
    public void SetSpecialIndex(float index)
    {
        activeSpecialIndex = (int)index;
    }

    /// <summary>
    /// 是否开启地图编辑模式
    /// </summary>
    /// <param name="toggle"></param>
    public void SetEditMode(bool toggle)
    {
        //editMode = toggle;

        //hexGrid.ShowUI(!toggle);
        enabled = toggle;

        hexGrid.ShowUI(!toggle);
        hexGrid.ClearPath();
        if (toggle)
        {
            Shader.EnableKeyword("HEX_MAP_EDIT_MODE");
        }
        else
        {
            Shader.DisableKeyword("HEX_MAP_EDIT_MODE");
        }
    }
 

    void HandleInput()
    {
        HexCell currentCell = GetCellUnderCursor();
        if (currentCell)
        {
            if (previousCell && previousCell != currentCell)
            {
                ValidateDrag(currentCell);
            }
            else
            {
                isDrag = false;
            }

            //if (editMode)
            //{
                EditCells(currentCell);
            //}
            //else if (Input.GetKey(KeyCode.LeftShift) && searchToCell != currentCell)
            //{
            //    if (searchFromCell)
            //    {
            //        searchFromCell.DisableHighlight();
            //    }
            //    // 设置起始点
            //    searchFromCell = currentCell;                                      
            //    searchFromCell.EnableHighlight(Color.blue);

            //    // 如果已经设置目标点则寻路
            //    if (searchToCell)
            //    {
            //        hexGrid.FindPath(searchFromCell, searchToCell, 24);
            //    }
            //}
            //else if(searchFromCell && searchFromCell != currentCell)
            //{
            //    if (searchToCell != currentCell)
            //    {
            //        // 设置目标点
            //        searchToCell = currentCell;
            //        // 寻路                                    
            //        hexGrid.FindPath(searchFromCell, searchToCell, 24); // 单回合移动成本暂时用24，现在默认一格移动成本是5
            //    }
            //}

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

    /// <summary>
    /// 获取当前鼠标触碰到的六边形
    /// </summary>
    /// <returns></returns>
    HexCell GetCellUnderCursor()
    {
        return hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
    }

    /// <summary>
    /// 实例化移动单位
    /// </summary>
    void CreateUnit()
    {
        HexCell cell = GetCellUnderCursor();
        if (cell && !cell.Unit)
        {
            hexGrid.AddUnit(Instantiate(HexUnit.unitPrefab), cell, Random.Range(0f, 360f));
        }
    }

    /// <summary>
    /// 销毁移动单位
    /// </summary>
    void DestroyUnit()
    {
        HexCell cell = GetCellUnderCursor();
        if (cell && cell.Unit)
        {
            hexGrid.RemoveUnit(cell.Unit);
        }
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
            // 设置地形类型引索
            if (activeTerrainTypeIndex >= 0)
            {
                cell.TerrainTypeIndex = activeTerrainTypeIndex;
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

            // 特殊特征物体下标
            if (applySpecialIndex)
            {
                cell.SpecialIndex = activeSpecialIndex;
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


    /// <summary>
    /// 是否显示网格
    /// </summary>
    /// <param name="visible"></param>
    /// <returns></returns>
    public void ShowGrid(bool visible)
    {
        if (visible)
        {
            terrainMaterial.EnableKeyword("GRID_ON");
        }
        else
        {
            terrainMaterial.DisableKeyword("GRID_ON");
        }

    }

}
