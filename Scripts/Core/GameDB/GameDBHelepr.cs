using GameDB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class GameDBHelper
{
    // 타입 캐시.
    // TODO : 근데 Release 는 언제하는게 좋을까? 
    public readonly static FieldInfo[] ContainerFieldsCache = typeof(GameDBContainer).GetFields();
    public readonly static IEnumerable<Type> GameDBAccessorsCache = Assembly.GetExecutingAssembly().GetTypes().Where(
        t => t.GetCustomAttribute<Attribute_GameDBAccessor>() != null && t.IsClass && t.IsAbstract == false);
    // 테이블 바이너리 확장자 
    public const string BinaryExtension = "bytes";

    public static void RegisterMessagePackResolvers()
    {
        MessagePack.Resolvers.DBCompositeResolver.Instance.Register(
            GameDB.Resolvers.GameDBContainerResolver.Instance,
            MessagePack.Unity.UnityResolver.Instance,
            MessagePack.Resolvers.StandardResolver.Instance
        );

        var options = MessagePack.MessagePackSerializerOptions.Standard.WithResolver(MessagePack.Resolvers.DBCompositeResolver.Instance);
        MessagePack.MessagePackSerializer.DefaultOptions = options;
    }

    public static object LoadTableBinary(string fieldName, byte[] bytes)
    {
        return LoadTableBinary(Array.Find(ContainerFieldsCache, t => t.Name == fieldName), bytes);
    }

    public static object LoadTableBinary(FieldInfo fieldInfo, byte[] bytes)
    {
        var tableType = fieldInfo.FieldType.GetGenericArguments()[1];
        return InvokeDeserialize(tableType, bytes);
    }

    public static object LoadTableBinaryReadingFile(FieldInfo fieldInfo)
    {
        var tableType = fieldInfo.FieldType.GetGenericArguments()[1];
        return LoadTableBinaryReadingFile(tableType, Path.Combine(Constants.Paths.TableBinFileDirectory, $"{tableType.Name}.{BinaryExtension}"));
    }

    public static object LoadTableBinaryReadingFile(Type tableType, string binPath)
    {
        byte[] bytesRead = null;

        try
        {
#if USE_REMOTE
            bytesRead = File.ReadAllBytes(binPath);
            if (bytesRead == null || bytesRead.Length == 0)
            {
                TEMP_Logger.Wrn($"WrongTableBytes Detected | Table : {tableType.Name}");
                return null;
            }
#else
            var txtAsset = Resources.Load<TextAsset>($"Table/{Path.GetFileNameWithoutExtension(binPath)}");
            if (txtAsset == null)
            {
                TEMP_Logger.Err($"[BuiltIn] Failed to load Table binary | : {Path.GetFileNameWithoutExtension(binPath)}");
                return null;

            }

            bytesRead = txtAsset.bytes;
            if (bytesRead == null || bytesRead.Length == 0)
            {
                TEMP_Logger.Wrn($"WrongTableBytes Detected | Table : {tableType.Name}");
                return null;
            }
#endif
        }
        catch (Exception exp)
        {
            TEMP_Logger.Err($"Error occured during reading table binary files : {binPath} | {exp}");
        }

        return InvokeDeserialize(tableType, bytesRead);
    }

    private static object InvokeDeserialize(Type tableType, byte[] bytes)
    {
        return tableType.InvokeMember("Deserialize", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, new object[] { bytes });
    }

    public static void InitializeAccessors(Action<int> onProgressed)
    {
        var allAccessors = GameDBHelper.GameDBAccessorsCache;
        if (allAccessors == null || allAccessors.Count() == 0)
            return;

        int currentCount = 0;
        foreach (var accessor in allAccessors)
        {
            var loadMethod = accessor.GetMethod(Attribute_GameDBAccessor.InitializeBootstrap, BindingFlags.Static | BindingFlags.Public);
            if (loadMethod != null)
            {
                loadMethod.Invoke(null, null);
                currentCount++;
                onProgressed?.Invoke(currentCount);
            }
        }
    }

    public static IEnumerator LoadAccessorsTableReadyCo(Action<int> onProgressed)
    {
        // Real 프로젝트에서는 이 프로세스에서는 빌드 전에 포함시켜서 Validation 과정에 포함시켜서 런타임 퍼포먼스를 조금이라도 올릴 수 있을 듯.
        if (GameDBAccessorsCache != null && GameDBAccessorsCache.Count() >= 1)
        {
            int totalCount = GameDBAccessorsCache.Count();
            int currentCount = 0;

            foreach (var accessor in GameDBAccessorsCache)
            {
                /// <see cref="Attribute_GameDBAccessor.OnTableReady"/> 이름과 동일한 메서드를 구현할 것을 강제
                var loadMethod = accessor.GetMethod(Attribute_GameDBAccessor.OnTableReady, BindingFlags.Static | BindingFlags.Public);
                if (loadMethod == null)
                    throw new Exception($"All Classes with {nameof(Attribute_GameDBAccessor)}({accessor}) must implement a method [public static void {Attribute_GameDBAccessor.OnTableReady}()]");

                loadMethod.Invoke(null, null);

                currentCount++;
                onProgressed?.Invoke(currentCount);

                yield return null;
            }
        }
    }
}
