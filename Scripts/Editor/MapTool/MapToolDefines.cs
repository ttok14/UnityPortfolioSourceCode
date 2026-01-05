using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Tool
{
    [Serializable]
    public class ToolMapMetadata
    {
        [Serializable]
        public class Meta
        {
            public MapDataMetadata SourceMetadata;
            public TextAsset TextAsset;
            public string MetadataAbsolutePath;
            public string MapDataAbsoluteDir;
        }

        [Serializable]
        public class Map
        {
            public MapData SourceMapData;
            public TextAsset TextAsset;
            public string AbsolutePath;

            public void ChangeName(string name, string newName)
            {
                AbsolutePath = Path.Combine(Path.GetDirectoryName(AbsolutePath), $"{newName}.json");
                SourceMapData.name = newName;
            }
        }

        public Meta Metadata = new Meta();
        public List<Map> MapDataList = new List<Map>();

        public void RefreshInfo()
        {
            Metadata.SourceMetadata.Files.Clear();

            StringBuilder combinedHashSb = new StringBuilder();
            foreach (var mapData in MapDataList)
            {
                var json = JsonUtility.ToJson(mapData.SourceMapData, prettyPrint: true);
                var hash = Helper.GetHash(json);
                combinedHashSb.Append(Helper.GetHash(json));

                Metadata.SourceMetadata.Files.Add(new MapFileInfo()
                {
                    Name = mapData.SourceMapData.name,
                    Hash = hash,
                    ByteSize = Encoding.UTF8.GetBytes(json).Length
                });
            }

            Metadata.SourceMetadata.Version = DateTime.Now.ToString("yyMMdd_HHmmss");
            Metadata.SourceMetadata.TotalHash = Helper.GetHash(combinedHashSb.ToString());
        }
    }

    public class GridDrawer : MapNodeGrid
    {
        //public int Width { get; private set; }
        //public int Height { get; private set; }

        Vector3[] _gridPoints;

        public void Change(int width, int height)
        {
            base.SetGrid(width, height);
            RecalculateGrid();
        }

        public void Draw(SceneView sv)
        {
            var oriColor = Handles.color;
            Handles.color = new Color(0, 1, 0.3f, 0.8f);
            Handles.DrawLines(_gridPoints); //, _gridIndicies);
            Handles.color = oriColor;
        }

        void RecalculateGrid()
        {
            int endLinePointsCount = 4;
            _gridPoints = new Vector3[Width * 2 + Height * 2 + 2 + endLinePointsCount];

            for (int x = 0; x < Width; x++)
            {
                _gridPoints[x * 2] = new Vector3(x, 0, 0);
                _gridPoints[x * 2 + 1] = new Vector3(x, 0, Height);

                if (x == Width - 1)
                {
                    _gridPoints[x * 2 + 2] = new Vector3(x + 1, 0, 0);
                    _gridPoints[x * 2 + 3] = new Vector3(x + 1, 0, Height);
                }
            }

            int jumpZaxisLinesIdx = Width * 2 + 2;

            for (int y = 0; y < Height; y++)
            {
                _gridPoints[jumpZaxisLinesIdx + y * 2] = new Vector3(0, 0, y);
                _gridPoints[jumpZaxisLinesIdx + y * 2 + 1] = new Vector3(Width, 0, y);

                if (y == Height - 1)
                {
                    _gridPoints[jumpZaxisLinesIdx + y * 2 + 2] = new Vector3(0, 0, y + 1);
                    _gridPoints[jumpZaxisLinesIdx + y * 2 + 3] = new Vector3(Width, 0, y + 1);
                }
            }
        }
    }

}
