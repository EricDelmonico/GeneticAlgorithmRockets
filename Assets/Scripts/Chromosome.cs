using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Just contains the data for each Chromosome in the data structure <see cref="genes"/> and its own fitness.
/// </summary>
public class Chromosome
{
    public Gene[] Genes { get; set; }
    public float Fitness { get; set; }

    public Chromosome(int size)
    {
        Genes = new Gene[size];
        Fitness = 0;
    }
}
