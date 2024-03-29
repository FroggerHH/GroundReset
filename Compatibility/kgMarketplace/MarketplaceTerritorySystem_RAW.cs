﻿using Marketplace.Modules.TerritorySystem;

namespace GroundReset.Compatibility.kgMarketplace;

public static class MarketplaceTerritorySystem_RAW
{
    public static bool PointInTerritory(Vector3 pos)
    {
        foreach (var territory in TerritorySystem_DataTypes.SyncedTerritoriesData.Value)
        {
            var isInside = territory.IsInside(pos);
            if (isInside) return true;
        }

        return false;
    }
}