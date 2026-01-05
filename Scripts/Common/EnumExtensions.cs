using System;
using UnityEngine;

public static class EnumFlagUtil
{
    public static int Remove(int flag, int flagToRemove)
    {
        return flag &= ~flagToRemove;
    }

    public static int Add(int flag, int flagToAdd)
    {
        return flag |= flagToAdd;
    }
}
