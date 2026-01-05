using System;
using System.Linq;
using System.IO;
using UnityEngine;
using System.Reflection;
using GameDB;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Tool
{
    public class MapToolGameDBContainer
    {
        public GameDBContainer Container { get; private set; }
        public TableMetadata TableMetaData { get; private set; }
        public bool IsReady { get; private set; }

        public Dictionary<string, EntityTable> ResourceKeyToEntityDic;

        public bool InitializeTable(string metadatPath, string tableBinDirectory)
        {
            Container = new GameDBContainer();

            if (File.Exists(metadatPath) == false)
            {
                TEMP_Logger.Err($"Table Metedata does not exist at : {metadatPath}");
                return false;
            }

            TableMetaData = JsonConvert.DeserializeObject<TableMetadata>(File.ReadAllText(metadatPath, System.Text.Encoding.UTF8));
            TEMP_Logger.Deb(@$"MapTool Table Metadata Read At : {metadatPath} | Version : {TableMetaData.Version} , TotalHash : {TableMetaData.TotalHash} , TableCount: {TableMetaData.Files}");

            int tableCount = TableMetaData.Files.Count;
            if (tableCount > 0)
            {
                // Deserialze - 데이터 조립
                foreach (var field in GameDBHelper.ContainerFieldsCache)
                {
                    var tableType = field.FieldType.GetGenericArguments()[1];
                    string binPath = Path.Combine(tableBinDirectory, $"{tableType.Name}.{GameDBHelper.BinaryExtension}");
                    var deserialized = GameDBHelper.LoadTableBinaryReadingFile(tableType, binPath);
                    field.SetValue(Container, deserialized);
                }

                // GameDBHelper.InitializeAccessors(null);
                IsReady = true;

                ResourceKeyToEntityDic = Container.EntityTable_data.ToDictionary((kv) => kv.Value.ResourceKey, (kv) => kv.Value);

                return true;
            }
            else
                TEMP_Logger.Err("No Table Exist by metadata");

            IsReady = false;
            return false;
        }
    }
}
