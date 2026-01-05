using GameDB;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

public class WaveSequenceGroupData
{
    public int TotalGrade;
    public int TotalEntityCount;
}

[Attribute_GameDBAccessor()]
public class DBWave
{
    static Dictionary<uint, List<WaveSequenceTable>> _sequenceCache;
    static List<uint> _waveIDListbyOrder;

    static Dictionary<uint, WaveSequenceGroupData> _sequenceGroupData;

    public static WaveTable Get(uint id)
    {
        if (GameDBManager.Instance.Container.WaveTable_data.TryGetValue(id, out var data))
            return data;
        return null;
    }

    public static List<WaveSequenceTable> GetSequence(uint waveId)
    {
        if (_sequenceCache.TryGetValue(waveId, out var sequence))
            return sequence;
        return null;
    }


    public static void OnTableReady()
    {
        _sequenceCache = new Dictionary<uint, List<WaveSequenceTable>>(GameDBManager.Instance.Container.WaveTable_data.Count);
        _sequenceGroupData = new Dictionary<uint, WaveSequenceGroupData>();
        _waveIDListbyOrder = new List<uint>();

        foreach (var seqData in GameDBManager.Instance.Container.WaveSequenceTable_data)
        {
            if (_sequenceCache.TryGetValue(seqData.Value.WaveID, out var seqList) == false)
            {
                seqList = new List<WaveSequenceTable>();
                _sequenceCache.Add(seqData.Value.WaveID, seqList);
            }

            if (_waveIDListbyOrder.Contains(seqData.Value.WaveID) == false)
            {
                _waveIDListbyOrder.Add(seqData.Value.WaveID);
            }

            if (seqData.Value.CmdType == E_WaveCommandType.Spawn)
            {
                uint entityId = seqData.Value.IntValue01;

                if (GameDBManager.Instance.Container.EntityTable_data.TryGetValue(entityId, out var entityData) == false)
                {
                    TEMP_Logger.Err($"Entity Does not exist referenced by WaveSequenceTable | WaveSeq ID : {seqData.Key} , EntityID : {entityId}");
                    continue;
                }

                if (GameDBManager.Instance.Container.StatTable_data.TryGetValue(entityData.StatTableID, out var statData) == false)
                {
                    TEMP_Logger.Err($"Stat Does not exist referenced by WaveSequenceTable | WaveSeq ID : {seqData.Key} , EntityID : {entityId} , StatID : {entityData.StatTableID}");
                    continue;
                }

                if (_sequenceGroupData.TryGetValue(seqData.Value.WaveID, out var groupData) == false)
                {
                    groupData = new WaveSequenceGroupData();
                    _sequenceGroupData.Add(seqData.Value.WaveID, groupData);
                }

                // 몬스터 개수 * 한개당 Grade
                _sequenceGroupData[seqData.Value.WaveID].TotalGrade += (int)seqData.Value.IntValue02 * (int)statData.Grade;
                _sequenceGroupData[seqData.Value.WaveID].TotalEntityCount += (int)seqData.Value.IntValue02;
            }

            seqList.Add(seqData.Value);
        }

        foreach (var seqData in _sequenceCache)
        {
            seqData.Value.Sort((lhs, rhs) =>
            {
                if (lhs.Order == rhs.Order)
                {
                    TEMP_Logger.Err($"Duplicate Wave Order Detected | lhs ID : {lhs.ID} , rhs ID : {rhs.ID} | DuplicatedOrder : {lhs.Order} | WaveID : {lhs.WaveID}");
                    return -1;
                }

                return lhs.Order.CompareTo(rhs.Order);
            });
        }

        _waveIDListbyOrder.Sort();
    }

    public static uint GetFirstWaveID()
    {
        return _waveIDListbyOrder[0];
    }

    public static uint GetNextWave(uint idNextFrom)
    {
        int fromIdx = _waveIDListbyOrder.FindIndex((t) => t == idNextFrom);

        if (fromIdx == -1 || idNextFrom == _waveIDListbyOrder.Count - 1)
            return idNextFrom;

        return _waveIDListbyOrder[fromIdx + 1];
    }

    public static void Release()
    {

    }
}
