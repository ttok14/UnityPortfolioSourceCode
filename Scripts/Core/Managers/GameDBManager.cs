using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GameDB;
using MessagePack;
using MessagePack.Unity;

public class Attribute_GameDBAccessor : Attribute
{
    // 아래 함수들이 자동으로 호출된다.

    // 빌트인 테이블 로드는 여기서 하면 된다.
    public const string InitializeBootstrap = "InitializeBootstrap";
    public const string OnTableReady = "OnTableReady";
    public const string Release = "Release";
}

public class GameDBManager : SingletonBase<GameDBManager>
{
    public GameDBContainer Container { get; private set; }

    IEnumerable<Type> _accessorTypeCache;

    public override void Initialize()
    {
        base.Initialize();
        Container = new GameDBContainer();
        GameDBHelper.RegisterMessagePackResolvers();

        // 미리 빌트인 테이블들을 가져올수 있게 로드
        InitializeAccessors(null);
    }

    protected override void OnDestroyed()
    {
        base.OnDestroyed();
        Release();
    }

    public void InitializeAccessors(Action<int> onProgressed)
    {
        GameDBHelper.InitializeAccessors(onProgressed);
    }

    public IEnumerator LoadAccessorsTableReady(Action<int> onProgressed)
    {
        yield return GameDBHelper.LoadAccessorsTableReadyCo(onProgressed);
    }

    public override void Release()
    {
        foreach (var accessor in _accessorTypeCache)
        {
            var releaseMethod = accessor.GetMethod(Attribute_GameDBAccessor.Release, BindingFlags.Static | BindingFlags.Public);
            if (releaseMethod == null)
                throw new Exception($"All Classes with {nameof(Attribute_GameDBAccessor)}({accessor}) must implement a method [public static void {Attribute_GameDBAccessor.Release}()]");

            releaseMethod.Invoke(null, null);
        }

        _accessorTypeCache = null;
    }
}
