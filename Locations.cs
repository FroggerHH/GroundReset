﻿using System.Threading.Tasks;

namespace GroundReset;

public static class Locations
{
    public static async Task<int> ResetLocations(bool checkWards)
    {
        watch.Restart();
        // var test = ZoneSystem.instance.m_locations.Where(x => !ZoneSystem.instance.m_locationsByHash.ContainsValue(x)
        // ).ToList();
        // DebugWarning($"Found {test.Count} locations which are not in m_locationsByHash, but in m_locations\n{
        //     test.Select(x => x.m_prefabName).GetString()}");

        var zdos = await ZoneSystem.instance.GetWorldObjectsAsync(
            ZoneSystem.instance.m_locationProxyPrefab.GetPrefabName());
        var resets = 0;
        if (zdos.Count > 0)
        {
            foreach (var zdo in zdos)
            {
                //To test it only in Eikthyrnir location
                if (zdo.GetInt(ZDOVars.s_location) != "Eikthyrnir".GetStableHashCode()) continue;

                await ResetLocation(zdo, checkWards);
                resets++;
            }
        }

        var totalSeconds = TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds).TotalSeconds;
        DebugWarning($"{resets} locations been reset. Took {totalSeconds} seconds");
        watch.Restart();

        return resets;
    }

    private static async Task<bool> ResetLocation(ZDO locationZdo, bool checkWards)
    {
        var locationNameHash = locationZdo.GetInt(ZDOVars.s_location);
        var location =
            ZoneSystem.instance.m_locations.Find(x => x.m_prefabName.GetStableHashCode() == locationNameHash);

        if (location == null)
        {
            DebugError($"Unknown location '{locationNameHash}' detected");
            return false;
        }


        if (!location.m_prefab)
        {
            DebugError($"Location {location.m_prefabName} prefab is null");
            return false;
        }

        var locationInstances =
            ZoneSystem.instance.GetGeneratedLocationsByName(location.m_prefabName).Select(x => x.Item2).ToList();
        var locationPosition = locationZdo.GetPosition();
        DebugWarning($"{location.m_prefabName} locationPosition:{locationPosition}, locationInstances: {
            locationInstances.Select(x => x.m_position).GetString()}");

        var locationRange = Max(location.m_interiorRadius, location.m_exteriorRadius) + 0.1f;
        if (checkWards && IsInWard(locationPosition,
                locationRange))
        {
            DebugWarning($"Location '{location.m_prefabName}' is in a ward");
            return false;
        }

        var locationPrefabZView = location.m_prefab.GetComponentsInChildren<ZNetView>().ToList();
        var locationPrefabZViewNames = locationPrefabZView.Select(x => x.GetPrefabName().GetStableHashCode()).ToList();
        var zdosInWorld = await ZoneSystem.instance.GetWorldObjectsAsync(searchZdo =>
        {
            if (!locationPrefabZViewNames.Contains(searchZdo.GetPrefab())) return false;
            var searchZdoPosition = searchZdo.GetPosition();
            var searchZdoPosition_byLoc = (searchZdoPosition.abs() - locationPosition.abs()).abs();
            if (searchZdoPosition_byLoc.magnitude > locationRange) return false;
            Vector3 cloneChildPosition = searchZdo.GetPosition();
            Quaternion cloneChildRotation = searchZdo.GetRotation();

            var zViewInLocation = locationPrefabZView.Find(prefabChild =>
            {
                Transform prefabChildTransform = prefabChild.transform;
                var prefabName = prefabChild.GetPrefabName();
                if (prefabName.GetStableHashCode() != searchZdo.GetPrefab()) return false;
                DebugWarning($"zViewInLocation finding, prefabName is okay: {prefabName}");

                var distanceIsOkay = CompareTransform(prefabChildTransform, cloneChildPosition, cloneChildRotation);

                if (!distanceIsOkay) return false;
                return true;
            });
            // if (zViewInLocation)
            DebugWarning(
                $"zViewInLocation for location {location.m_prefabName} {(zViewInLocation is null ? "NOT found" : "found")}");

            return true;
        });

        if (zdosInWorld.Count > 0)
            DebugWarning($"Found {zdosInWorld.Count} ZDOs in world for location '{location.m_prefabName}'");


        return true;
    }

    private static bool CompareTransform(Transform prefabChildTransform, Vector3 cloneChildPosition,
        Quaternion cloneChildRotation)
    {
        float tolerance = 0.2f;
        Transform tempTransform = new GameObject("TempTransform").transform;
        tempTransform.position = cloneChildPosition;
        tempTransform.rotation = cloneChildRotation;

        // Преобразование локальных координат prefabChild в мировые, относительно временного трансформа
        Vector3 worldPositionPrefabChild = tempTransform.TransformPoint(prefabChildTransform.localPosition);

        // Удаление временного объекта
        Destroy(tempTransform.gameObject);

        DebugWarning($""
                     + $"tolerance: {tolerance}\n"
                     + $"worldPositionPrefabChild: {worldPositionPrefabChild}\n"
                     + $"cloneChildPosition: {cloneChildPosition}\n");
        // Сравнение мировых позиций
        if (worldPositionPrefabChild.DistanceXZ(cloneChildPosition) < tolerance)
        {
            DebugWarning("Позиции совпадают");
            return true;
        } else
        {
            DebugError("Позиции не совпадают");
            return false;
        }
    }
}