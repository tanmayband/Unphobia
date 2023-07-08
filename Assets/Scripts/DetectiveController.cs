using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DetectiveController : MonoBehaviour
{
    [SerializeField]
    private NavMeshAgent navMeshAgent;
    [SerializeField]
    private Transform targetTransform;

    // Start is called before the first frame update
    void Start()
    {
        navMeshAgent.updateRotation = false;
		navMeshAgent.updateUpAxis = false;
        navMeshAgent.destination = targetTransform.position;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
