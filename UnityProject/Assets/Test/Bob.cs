using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using ModestTree;

public class Bob : MonoBehaviour 
{
    public string Id;

    public void Awake()
    {
        Log.Trace("Bob({0}).Awake", Id);
    }

    public void Start()
    {
        Log.Trace("Bob({0}).Start", Id);
    }

    [Inject]
    public void Construct(Jim jim)
    {
        Log.Trace("Bob({0}).received {1}", Id, jim);
    }
}
