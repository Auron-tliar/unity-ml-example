using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class FinderAcademy : Academy
{
    public override void InitializeAcademy()
    {
        // make agents fall faster
        Physics.gravity *= 3f;
    }
}
