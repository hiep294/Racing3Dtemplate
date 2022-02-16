using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Waypoints : MonoBehaviour
{

    const float waypointGizmoRadius = 0.3f;
    private void OnDrawGizmos()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            int j = GetNextIndex(i);
            Gizmos.DrawSphere(GetWaypoint(i), waypointGizmoRadius);
            Gizmos.DrawLine(GetWaypoint(i), GetWaypoint(j));
        }
    }

    public int GetNextIndex(int i)
    {
        if (i + 1 >= transform.childCount)
        {
            return 0;
        }
        return i + 1;
    }

    // get position of the waypoint
    public Vector3 GetWaypoint(int i)
    {
        return transform.GetChild(i).position;
    }
}