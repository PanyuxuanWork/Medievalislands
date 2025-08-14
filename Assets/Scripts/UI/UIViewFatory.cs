/***************************************************************************
// File       : UIViewFatory.cs
// Author     : Panyuxuan
// Created    : 2025/08/
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/
/***************************************************************************
// File       : UIViewFactory.cs
// Description: 视图工厂（从选中对象创建 IResidentView / IBuildingView）
// ***************************************************************************/

using UnityEngine;

public static class UIViewFactory
{
    public static IResidentView CreateResidentView(Resident r)
    {
        return r != null ? new ResidentViewAdapter(r) : null;
    }

    public static IBuildingView CreateBuildingView(BuildingBase b)
    {
        if (b == null) return null;

        ProductionBuilding pb = b.GetComponent<ProductionBuilding>();
        if (pb != null) return new ProductionBuildingViewAdapter(pb);

        WarehouseBuilding wh = b.GetComponent<WarehouseBuilding>();
        if (wh != null) return new WarehouseBuildingViewAdapter(wh);

        return null;
    }
}
