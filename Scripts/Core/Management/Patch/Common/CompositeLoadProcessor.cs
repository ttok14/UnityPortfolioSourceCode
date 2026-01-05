using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;

public class CompositeLoadProcessor : ILoadProcessor
{
    public class SubProcessor
    {
        public ILoadProcessor processor;
        public int weightPercentage;

        public SubProcessor(int weightPercentage, ILoadProcessor processor)
        {
            this.weightPercentage = weightPercentage;
            this.processor = processor;
        }

        public float WeightNormalized => weightPercentage / 100f;
    }

    private List<SubProcessor> _subProcessors;

    public CompositeLoadProcessor(params SubProcessor[] processors)
    {
        if (processors == null)
            return;

        int summed = 0;
        foreach (var p in processors)
        {
            summed += p.weightPercentage;
        }

        if (summed != 100)
            throw new Exception($"LoadProcessors Weight must be 100 in sum");

        _subProcessors = processors.ToList();
    }

    public float Progress
    {
        get
        {
            float sum = 0;
            foreach (var p in _subProcessors)
            {
                sum += p.WeightNormalized * p.processor.Progress;
            }
            return sum;
        }
    }

    public string CurrentStatus
    {
        get
        {
            var cur = _subProcessors.Find(t => t.processor.Result != LoadingProcessResult.Success);
            if (cur != null)
                return $"{cur.processor.CurrentStatus} ({(int)(Progress * 100)}%)";

            return _subProcessors[_subProcessors.Count - 1].processor.CurrentStatus;
        }
    }
    public LoadingProcessResult Result
    {
        get
        {
            foreach (var p in _subProcessors)
            {
                if (p.processor.Result != LoadingProcessResult.Success)
                {
                    return p.processor.Result;
                }

                //Result 에서 progress 를 체크하는 건 직관적이지 않고 불편하게만 만들것 같음 ?
                //if (p.processor.Progress < 1f)
                //{
                //    return LoadingProcessResult.None;
                //}
            }
            return LoadingProcessResult.Success;
        }
    }

    public IEnumerator Process()
    {
        foreach (var p in _subProcessors)
        {
            yield return p.processor.Process();
        }
    }
}
