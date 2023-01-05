using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{
    public bool HitWall { get; private set; } = false;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HitWall = true;
    }

    public void Reset()
    {
        HitWall = false;
    }
}
