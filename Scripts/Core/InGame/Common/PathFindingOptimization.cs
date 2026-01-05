//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;

//public class PathCaching
//{
//    public Vector3 from;
//    public List<List<Vector3>> pathVariations;
//    // public List<Vector3> path;

//    public PathCaching(Vector3 from)
//    {
//        this.from = from;
//        this.pathVariations = new List<List<Vector3>>();
//        //this.path = path;
//    }

//    public List<Vector3> Get(Vector3 pos, int allowRange = 1)
//    {
//        // var tilePos = MapUtils.WorldPosToTilePos(pos);
//        // int cost = Mathf.Max(Mathf.Abs(from.x - tilePos.x), Mathf.Abs(from.y - tilePos.y));
//        if (pathVariations.Count == 0)
//            return null;

//        float costSqr = Vector3.SqrMagnitude(from - pos);
//        if (costSqr <= allowRange * allowRange)
//        {
//            int idx = UnityEngine.Random.Range(0, pathVariations.Count);
//            var randomVariation = pathVariations[idx];
//            var destModifiedPos = MapUtils.SomewhereInTilePos(randomVariation[randomVariation.Count - 1]);

//            var result = randomVariation.ToList();
//            result[result.Count - 1] = destModifiedPos;

//            return result;
//        }
//        return null;
//    }
//}
