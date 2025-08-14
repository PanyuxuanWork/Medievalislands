/***************************************************************************
// File       : IInventoryView.cs
// Author     : panyuxuan
// CreateTime : 2025/08/13
// Description: 仅负责提供库存读数的轻量接口（IMGUI/UGUI 共用）
// ***************************************************************************/

public interface IInventoryView
{
    int Get(ResourceType type);
    int Total { get; }
}

/***************************************************************************
// File       : InventoryViewAdapter.cs
// Description: 将项目内 Inventory 适配为 IInventoryView
// ***************************************************************************/

public class InventoryViewAdapter : IInventoryView
{
    private readonly Inventory _inv;

    public InventoryViewAdapter(Inventory inv)
    {
        _inv = inv;
    }

    public int Get(ResourceType type)
    {
        return _inv != null ? _inv.Get(type) : 0;
    }

    public int Total
    {
        get
        {
            if (_inv == null) return 0;
            int sum = 0;
            System.Array vals = System.Enum.GetValues(typeof(ResourceType));
            for (int i = 0; i < vals.Length; i++)
            {
                sum += _inv.Get((ResourceType)vals.GetValue(i));
            }
            return sum;
        }
    }
}