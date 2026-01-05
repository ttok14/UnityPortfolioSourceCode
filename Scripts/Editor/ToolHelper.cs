using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.AddressableAssets;
using System;
using UnityEngine.SceneManagement;

namespace Tool
{
    public static class ToolHelper
    {
        public static bool RaycastPlane(Ray ray, Plane plane, out Vector3 intersection)
        {
            intersection = default;
            if (plane.Raycast(ray, out var enter) == false)
                return false;
            intersection = ray.GetPoint(enter);
            return true;
        }

        /// <param name="targetCheck"> 찾던게 맞으면 True => 순회 중지 </param>
        public static void IterateTargetGameObject(GameObject go, Func<GameObject, bool> targetCheck)
        {
            if (targetCheck.Invoke(go))
            {
                return;
            }

            foreach (Transform child in go.transform)
            {
                //이거없어야하지않나? 우랑 중복가튼데 
                //if (targetCheck.Invoke(child.gameObject))
                //{
                //    continue;
                //}

                IterateTargetGameObject(child.gameObject, targetCheck);
            }
        }

        public static void IterateAllSceneObjects(GameObjectSelectionFlags flags, Action<GameObject> cb)
        {
            var activeScene = SceneManager.GetActiveScene();
            GameObject[] rootObjects = activeScene.GetRootGameObjects();

            foreach (var go in rootObjects)
            {
                IterateTargetGameObject(go, (foundGo) =>
                {
                    if (flags.HasFlag(GameObjectSelectionFlags.OnlyPrefabRoot))
                    {
                        if (!string.IsNullOrEmpty(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(foundGo)))
                        {
                            cb.Invoke(foundGo);
                            // 그만 찾도록 , 이 경우 혹시 프리팹이 아닌 일반 오브젝트가 프리팹 루트로 들어가있으면 걔네는 생략되겠지 
                            return true;
                        }
                        return false;
                    }
                    else if (flags.HasFlag(GameObjectSelectionFlags.PartOfPrefab))
                    {
                        if (PrefabUtility.IsPartOfPrefabInstance(foundGo))
                        {
                            cb.Invoke(foundGo);
                        }
                        return false;
                    }
                    return false;
                });
            }
        }

        public static string ConvertAbsoluteToAssetPath(string absolutePath)
        {
            string normalizedDataPath = Application.dataPath.Replace('\\', '/');
            string normalizedAbsolutePath = absolutePath.Replace('\\', '/');
            if (normalizedAbsolutePath.StartsWith(normalizedDataPath))
            {
                string relativePath = "Assets" + normalizedAbsolutePath.Substring(normalizedDataPath.Length);
                return relativePath;
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
