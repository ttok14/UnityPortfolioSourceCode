using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.AddressableAssets;
using System;

namespace Tool
{
    public static class MapToolUtil
    {
        public static List<CustomAddressableAssetEntry> IterateGroupAssets(string groupName, params Type[] typeFilters)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null)
            {
                Debug.LogError("Addressable Settings를 찾을 수 없습니다.");
                return new List<CustomAddressableAssetEntry>();
            }

            AddressableAssetGroup targetGroup = settings.FindGroup(groupName);

            if (targetGroup == null)
            {
                Debug.LogWarning($"그룹 '{groupName}'을(를) 찾을 수 없습니다.");
                return new List<CustomAddressableAssetEntry>();
            }

            Debug.Log($"그룹: {groupName} 에셋 순회 시작...");

            var result = new List<CustomAddressableAssetEntry>();

            foreach (var entry in targetGroup.entries)
            {
                if (typeFilters != null)
                {
                    if (Array.TrueForAll(typeFilters, t => t != entry.MainAssetType))
                    {
                        continue;
                    }
                }

                string assetPath = entry.AssetPath;
                string address = entry.address;
                string guid = entry.guid;

                result.Add(new CustomAddressableAssetEntry(groupName, assetPath, address, guid, entry.MainAsset/*, ToMapObjectTypeByKey(entry.address)*/));
            }

            return result;
        }

        //public static MapObjectType ToMapObjectTypeByKey(string addressablesKey)
        //{
        //    foreach (var kv in MapObjectEntryEditor.ObjectTypeAndInitialNameMap)
        //    {
        //        foreach (var n in kv.Value)
        //        {
        //            if (addressablesKey.StartsWith(n))
        //            {
        //                return kv.Key;
        //            }
        //        }
        //    }
        //    Debug.LogError($"Could not find object Type with given key: {addressablesKey}");
        //    return MapObjectType.None;
        //}
    }
}
