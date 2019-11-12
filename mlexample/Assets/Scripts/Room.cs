using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    public float XMin = -9f, XMax = 9f, ZMin = -9f, ZMax = 9f; // dimensions of the platform where the objects can spawn

    [HideInInspector]
    public float MaxDistance; // maximal distance between the agent and the goal (to normalize the distance to the goal time penalty discount)

    private void Start()
    {
        MaxDistance = Vector3.Distance(new Vector3(XMin, 0f, ZMin), new Vector3(XMax, 0f, ZMax)) - 1f;
    }

    public Vector3 RandomPosition(float y) // return a random position within the room
    {
        return new Vector3(Random.Range(XMin, XMax), y, Random.Range(ZMin, ZMax));
    }
}
