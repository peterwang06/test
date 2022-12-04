using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MergeSharp;

public class GVectorData<T>
{
    [JsonInclude]
    public List<T> List;
    [JsonInclude]
    public List<long> ListTime;
    [JsonInclude]
    public long startTime;

    public GVectorData()
    {
        List = new List<T>();
        ListTime = new List<long>();
        startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}

[TypeAntiEntropyProtocol(typeof(GVector<>))]
public class GVectorMsg<T> : PropagationMessage
{
    [JsonInclude]
    public Dictionary<Guid, GVectorData<T>> replicaInfo;

    public GVectorMsg()
    {
    }

    public GVectorMsg(Dictionary<Guid, GVectorData<T>> replicaInfo)
    {
        this.replicaInfo = replicaInfo;
    }

    public override void Decode(byte[] input)
    {
        var json = JsonSerializer.Deserialize<GVectorMsg<T>>(input);
        this.replicaInfo = json.replicaInfo;
    }

    public override byte[] Encode()
    {
        return JsonSerializer.SerializeToUtf8Bytes(this);
    }
}


[ReplicatedType("GVector")]
public class GVector<T> : CRDT, ICollection<T>
{
    private GVectorData<T> local; 
    private Guid replicaIdx;
    private Dictionary<Guid, GVectorData<T>> replicaInfo;

    public int Count
    {
        get
        {
            return this.local.List.Count;
        }
    }

    public bool IsReadOnly
    {
        get
        {
            return false;
        }
    }

    public GVector()
    {
        this.replicaIdx = Guid.NewGuid();
        this.replicaInfo = new Dictionary<Guid, GVectorData<T>>();
        this.local = new GVectorData<T>();
        this.replicaInfo[this.replicaIdx] = new GVectorData<T>();
    }

    public GVector(Guid id)
    {
        this.replicaIdx = id;
        this.replicaInfo = new Dictionary<Guid, GVectorData<T>>();
        this.local = new GVectorData<T>();
        this.replicaInfo[this.replicaIdx] = new GVectorData<T>();
    }

    [OperationType(OpType.Update)]
    public virtual void Add(T item)
    {
        long timeDiff = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - this.replicaInfo[this.replicaIdx].startTime;
        local.List.Add(item);
        local.ListTime.Add(timeDiff);
        this.replicaInfo[this.replicaIdx].List.Add(item);
        this.replicaInfo[this.replicaIdx].ListTime.Add(timeDiff);
    }

    [OperationType(OpType.Update)]
    public virtual bool Remove(T item)
    {
        throw new InvalidOperationException("Cannot remove a GVector");
        return false;
    }

    public List<T> LookupAll()
    {
        return local.List;
    }


    public override bool Equals(object obj)
    {
        if (obj is GVector<T>)
        {
            var other = (GVector<T>)obj;
            foreach (var item in this.LookupAll())
            {
                if (!other.Contains(item))
                {
                    return false;
                }
            }
        }
        return false;

    }

    public void Clear()
    {
        throw new InvalidOperationException("Cannot clear a GVector");
    }

    public bool Contains(T item)
    {
        return this.local.List.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public void Merge(GVectorMsg<T> received)
    {
        local.List.Clear();
        local.ListTime.Clear();
        List<Guid> keys = received.replicaInfo.Keys.ToList();
        for(int i = 0; i < keys.Count; i++)
        {
            if (keys[i].CompareTo(this.replicaIdx) != 0)
            {
                this.replicaInfo[keys[i]] = received.replicaInfo[keys[i]];
            }
        }
        keys = this.replicaInfo.Keys.ToList();
        if (keys.Count > 1)
        {
            Dictionary<Guid, long> offset = new Dictionary<Guid, long>();
            Dictionary<Guid, KeyValuePair<int, int>> elementIdx = new Dictionary<Guid, KeyValuePair<int, int>>();

            int total = 0;
            for (int i = 0; i < keys.Count; i++)
            {
                Guid replicaID = keys[i];
                elementIdx[replicaID] = new KeyValuePair<int, int>(0, replicaInfo[replicaID].List.Count);
                offset[replicaID] = this.replicaInfo[replicaID].startTime - this.replicaInfo[this.replicaIdx].startTime;
                total += replicaInfo[replicaID].List.Count;
            }
            for (int i = 0; i < total; i++)
            {
                Guid bestID = keys[0];
                for (int j = 1; j < keys.Count; j++)
                {
                    Guid replicaID = keys[j];
                    if (elementIdx[bestID].Key == elementIdx[bestID].Value)
                    {
                        bestID = replicaID;
                    }
                    else if (elementIdx[replicaID].Key != elementIdx[replicaID].Value)
                    {
                        long lowestTime = this.replicaInfo[bestID].ListTime[elementIdx[bestID].Key] + offset[bestID];
                        long replicaTime = this.replicaInfo[replicaID].ListTime[elementIdx[replicaID].Key] + offset[replicaID];
                        if (replicaTime == lowestTime)
                        {
                            bestID = bestID.CompareTo(replicaID) > 0 ? bestID : replicaID;
                        }
                        else if (replicaTime < lowestTime)
                        {
                            bestID = replicaID;
                        }
                    }
                }
                int bestIdx = elementIdx[bestID].Key;
                elementIdx[bestID] = new KeyValuePair<int, int>(elementIdx[bestID].Key+1, elementIdx[bestID].Value);
                local.List.Add(this.replicaInfo[bestID].List[bestIdx]);
                local.ListTime.Add(this.replicaInfo[bestID].ListTime[bestIdx]);
            }
        }
        else
        {
            local.List = this.replicaInfo[keys[0]].List;
            local.ListTime = this.replicaInfo[keys[0]].ListTime;
        }
    }

    public override void ApplySynchronizedUpdate(PropagationMessage ReceivedUpdate)
    {
        GVectorMsg<T> recieved = (GVectorMsg<T>)ReceivedUpdate;
        this.Merge(recieved);
    }

    public override PropagationMessage DecodePropagationMessage(byte[] input)
    {
        GVectorMsg<T> msg = new();
        msg.Decode(input);
        return msg;
    }

    public override PropagationMessage GetLastSynchronizedUpdate()
    {
        return new GVectorMsg<T>(this.replicaInfo);
    }


    public IEnumerator<T> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}