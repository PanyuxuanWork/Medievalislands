/***************************************************************************
// File       : ProductionBuildingViewAdapter.cs
// Author     : Panyuxuan
// Created    : 2025/08/13
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

/***************************************************************************
// File       : IBuildingView.cs
// Description: 建筑视图接口（生产 / 仓库 统一对外）
// ***************************************************************************/

public interface IBuildingView
{
    string DisplayName { get; }
    BuildingState State { get; }
    IInventoryView InputInventory { get; }    // 仅生产有；仓库为 null
    IInventoryView OutputInventory { get; }   // 仅生产有；仓库为 null
    IInventoryView StorageInventory { get; }  // 仅仓库有；生产为 null
    float CycleSeconds { get; }               // 仅生产；仓库返回 0
}

/***************************************************************************
// File       : BuildingViewAdapters.cs
// Description: ProductionBuilding / WarehouseBuilding 的适配器
// ***************************************************************************/

/***************************************************************************
// File       : ProductionBuildingViewAdapter.cs
// Description: ProductionBuilding -> IBuildingView 适配器（无 None 枚举的兼容）
// ***************************************************************************/

public class ProductionBuildingViewAdapter : IBuildingView
{
    private readonly ProductionBuilding _pb;
    private readonly IInventoryView _inView;
    private readonly IInventoryView _outView;

    public ProductionBuildingViewAdapter(ProductionBuilding pb)
    {
        _pb = pb;
        _inView = new InventoryViewAdapter(pb != null ? pb.inputInv : null);
        _outView = new InventoryViewAdapter(pb != null ? pb.outputInv : null);
    }

    public string DisplayName { get { return _pb != null ? _pb.name : "(null)"; } }

    // 注意：你的 BuildingState 没有 None；当 _pb 为空时，回退到 Placed 以避免编译错误。
    public BuildingState State { get { return _pb != null ? _pb.state : BuildingState.Placed; } }

    public IInventoryView InputInventory { get { return _inView; } }
    public IInventoryView OutputInventory { get { return _outView; } }
    public IInventoryView StorageInventory { get { return null; } }

    public float CycleSeconds
    {
        get
        {
            if (_pb == null) return 0f;
            if (_pb.cycleSecondsOverride > 0f) return _pb.cycleSecondsOverride;
            return _pb.recipe != null ? _pb.recipe.cycleSeconds : 0f;
        }
    }
}


public class WarehouseBuildingViewAdapter : IBuildingView
{
    private readonly WarehouseBuilding _wh;
    private readonly IInventoryView _storeView;

    public WarehouseBuildingViewAdapter(WarehouseBuilding wh)
    {
        _wh = wh;
        _storeView = new WarehouseInventoryViewAdapter(wh);
    }

    public string DisplayName { get { return _wh != null ? _wh.name : "(null)"; } }
    public BuildingState State { get { return _wh != null ? _wh.state : BuildingState.Disabled; } }
    public IInventoryView InputInventory { get { return null; } }
    public IInventoryView OutputInventory { get { return null; } }
    public IInventoryView StorageInventory { get { return _storeView; } }
    public float CycleSeconds { get { return 0f; } }
}

/***************************************************************************
// File       : WarehouseInventoryViewAdapter.cs
// Description: 用 IStorage 的 Get 实现库存视图（适配 WarehouseBuilding）
// ***************************************************************************/

public class WarehouseInventoryViewAdapter : IInventoryView
{
    private readonly WarehouseBuilding _wh;

    public WarehouseInventoryViewAdapter(WarehouseBuilding wh)
    {
        _wh = wh;
    }

    public int Get(ResourceType type)
    {
        return _wh != null ? _wh.Get(type) : 0;
    }

    public int Total
    {
        get
        {
            if (_wh == null) return 0;
            int sum = 0;
            System.Array vals = System.Enum.GetValues(typeof(ResourceType));
            for (int i = 0; i < vals.Length; i++)
            {
                sum += _wh.Get((ResourceType)vals.GetValue(i));
            }
            return sum;
        }
    }
}
