using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Right now, Gene is just a vector3. This represents a movement direction
/// </summary>
public class Gene
{
    public Vector3 Direction;

    public Gene(Vector3 direction)
    {
        Direction = direction;
    }

    public Gene() : this(Vector3.zero) { }
}