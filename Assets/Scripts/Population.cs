using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public enum PopulationState
{
    Initializing,
    Evaluating,
    Done
}

/// <summary>
/// Population handles the whole GA
/// 
/// Steps:
/// 1. Initialize population
/// 2. Calculate fitness for all chromosomes
/// 3. Crossover
/// 4. Mutate
/// 5. Select survivors
/// 6. Go back to step 2
/// </summary>
public class Population : MonoBehaviour
{
    [SerializeField]
    private Vector3 targetPos;
    [SerializeField]
    private GameObject agentPrefab;

    private GameObject[] agents;
    private Chromosome[] population;

    [Header("Do not change at runtime.")]
    [SerializeField]
    private int populationNumber = 100;
    [SerializeField]
    private int chromosomeGeneNum = 10;

    [Tooltip("Time it takes to move on to the next gene. In other words, how long each gene's direction is used for movement.")]
    [SerializeField]
    private float timeBeforeGeneAdvance = 1.0f;
    private int currentGene = 0;

    [SerializeField]
    private Vector3 startingPos = new Vector3(-8, 0, 0);

    private PopulationState currentState;
    private PopulationState previousState;

    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float killRatio = 0.5f;
    private int killNumber;

    [Header("Feel free to change at runtime!")]
    [Range(0.0f, 1.0f)]
    public float mutationRate = 0.2f;

    [Range(0.0f, 100.0f)]
    public float timeScale = 1.0f;

    private int currentGeneration;
    public int CurrentGeneration 
    {
        get => currentGeneration;
        private set
        {
            currentGeneration = value;
            generationText.text = "Generation: " + value;
        }
    }

    private float bestFitness;
    public float BestFitness
    {
        get => bestFitness;
        set
        {
            bestFitness = value;
            bestFitnessText.text = "Best Fitness: " + value;
        }
    }

    [SerializeField]
    private Text timescaleText;
    [SerializeField]
    private Text mutationrateText;
    [SerializeField]
    private Text generationText;
    [SerializeField]
    private Text bestFitnessText;

    [SerializeField]
    private Text nextPopSizeText;
    [SerializeField]
    private Text nextGenesPerChromText;

    private void Start()
    {
        GameObject.Find("TimeSlider").GetComponent<Slider>().value = timeScale;
        GameObject.Find("MutationSlider").GetComponent<Slider>().value = mutationRate;

        ChangeMutationRate(mutationRate);
        ChangeTimeScale(timeScale);

        nextPopSize = populationNumber;
        nextGenesPerChrom = chromosomeGeneNum;

        GameObject.Find("NextPopSizeSlider").GetComponent<Slider>().value = nextPopSize;
        GameObject.Find("NextGenesPerChromosomeSlider").GetComponent<Slider>().value = nextGenesPerChrom;

        ChangeNextPopSize(nextPopSize);
        ChangeNextGenesPerChrom(nextGenesPerChrom);

        currentState = PopulationState.Initializing;
        Init();
    }





    #region Phenotype/gene expression things
    private void Update()
    {
        Time.timeScale = timeScale;

        PopulationState toState = currentState;
        switch (currentState)
        {
            case PopulationState.Evaluating:
                if (previousState != PopulationState.Evaluating) InitiatePopulationMove();
                Evaluate();
                if (cycleThroughGenesCoroutine == null) toState = PopulationState.Done;
                break;
            case PopulationState.Done:
                Done();
                toState = PopulationState.Evaluating;
                break;
            default:
                break;
        }

        previousState = currentState;
        currentState = toState;
    }

    private float timeMovedThisGene = 0.0f;
    private void Evaluate()
    {
        MovePopulationForTime(Time.deltaTime);
    }

    private void MovePopulationForTime(float time)
    {
        for (int i = 0; i < populationNumber; i++)
        {
            // If they hit the wall, they stop
            if (agents[i].GetComponent<Agent>().HitWall) continue;

            agents[i].transform.up = population[i].Genes[currentGene].Direction;
            // Cant move in one direction for any more than timeBeforeGeneAdvance seconds so clamp the time to that
            agents[i].transform.position += population[i].Genes[currentGene].Direction * Mathf.Clamp(time, 0, timeBeforeGeneAdvance - timeMovedThisGene);
        }
        timeMovedThisGene += Mathf.Clamp(time, 0, timeBeforeGeneAdvance - timeMovedThisGene);
    }

    private void Done()
    {
        // Steps for GA:
        // 1. Initialize population
        // 2. Calculate fitness for all chromosomes
        // 3. Crossover
        // 4. Mutate
        // 5. Select survivors
        // 6. Go back to step 2
        CalculateFitness();
        var children = Crossover();
        Mutate(children);
        HashSet<int> killedChromosomeIndices = CullPopulation();

        int childi = 0;
        foreach (var index in killedChromosomeIndices)
        {
            population[index] = children[childi];
            childi++;
        }

        // Reset all fitnesses and collisions and go back to evaluating!
        for (int i = 0; i < populationNumber; i++)
        {
            population[i].Fitness = 0.0f;
            agents[i].GetComponent<Agent>().Reset();
        }

        // Moving to the next generation B)
        CurrentGeneration++;
    }

    private void InitiatePopulationMove()
    {
        // Stick all agents at the start
        for (int i = 0; i < populationNumber; i++) agents[i].transform.position = startingPos;
        // Restart gene cycle
        currentGene = 0;
        cycleThroughGenesCoroutine = StartCoroutine(CycleThroughGenes());
    }

    private Coroutine cycleThroughGenesCoroutine = null;
    private IEnumerator CycleThroughGenes()
    {
        yield return new WaitForSeconds(timeBeforeGeneAdvance);
        // If we haven't moved the full amount, move the full amount
        if (timeMovedThisGene < timeBeforeGeneAdvance)
        {
            float timeLeft = timeBeforeGeneAdvance - timeMovedThisGene;
            MovePopulationForTime(timeLeft);
        }

        // If we're done traversing genes, get out of the coroutine
        if (currentGene >= chromosomeGeneNum - 1)
        {
            cycleThroughGenesCoroutine = null;
            yield break;
        }
        
        // Advance the current gene
        currentGene++;
        timeMovedThisGene = 0.0f;
        StartCoroutine(CycleThroughGenes());
    }
    #endregion





    #region Genetic Algorithm
    // Steps for GA:
    // 1. Initialize population
    // 2. Calculate fitness for all chromosomes
    // 3. Crossover
    // 4. Mutate
    // 5. Select survivors
    // 6. Go back to step 2

    private void Init()
    {
        CurrentGeneration = 0;
        BestFitness = 0;

        populationNumber = nextPopSize;
        chromosomeGeneNum = nextGenesPerChrom;

        killNumber = (int)(killRatio * populationNumber);
        // Gotta kill something, and something has to stay alive
        killNumber = Mathf.Clamp(killNumber, 1, populationNumber - 1);

        population = new Chromosome[populationNumber];
        agents = new GameObject[populationNumber];

        for (int i = 0; i < populationNumber; i++)
        {
            // Create new chromosome and randomize its genes
            population[i] = new Chromosome(chromosomeGeneNum);
            RandomizeGenes(population[i]);

            // Create an agent/phenotype for the chromosome
            if (agentPrefab != null) agents[i] = Instantiate(agentPrefab, startingPos, Quaternion.identity);
        }

        // Start the population going!
        currentState = PopulationState.Evaluating;
    }

    private void CalculateFitness()
    {
        float startingDistance = Vector3.Distance(startingPos, targetPos);
        for (int i = 0; i < populationNumber; i++)
        {
            Vector3 agentPos = agents[i].transform.position;
            float agentDistance = Vector3.Distance(agentPos, targetPos);

            // Fitness at start or further should be zero... so ignore anything further than the starting pos
            //float worstDistance = Mathf.Min(startingDistance, agentDistance);

            // Fitness will be startingDistance - agentDistance / 1
            population[i].Fitness = (startingDistance - agentDistance) / startingDistance;
            // Square it to make differences more dramatic
            if (population[i].Fitness > 0) // Only square positive fitnesses
            {
                population[i].Fitness *= population[i].Fitness;
            }
        }
    }

    /// <summary>
    /// Do crossover
    /// </summary>
    /// <returns>Array of children of size <see cref="populationNumber"/> - <see cref="killNumber"/></returns>
    private Chromosome[] Crossover()
    {
        var newPopulation = new Chromosome[populationNumber];
        // We want a child for every pop we kill
        var children = new Chromosome[populationNumber - killNumber];

        // Enforcing monogamy in order to not get weird edge cases
        HashSet<int> takenParents = new HashSet<int>();

        // Get the best fitness of the population
        float bestFit = population[0].Fitness;
        for (int i = 0; i < population.Length; i++)
        {
            if (population[i].Fitness > bestFit) bestFit = population[i].Fitness;
        }

        // Save the best fitness
        BestFitness = bestFit;

        // Normalize fitness
        for (int i = 0; i < population.Length; i++)
        {
            population[i].Fitness /= bestFit;
        }

        // Sort array by fitness, descending order
        System.Array.Sort(population, (c1, c2) => (int)(100.0f * (c2.Fitness - c1.Fitness)));

        // Select an appropriate number of parents -- two for each child
        for (int i = 0; i < children.Length; i++)
        {
            int parent1 = GetWeightedParentIndex(takenParents);
            int parent2 = GetWeightedParentIndex(takenParents);

            takenParents.Add(parent1);
            takenParents.Add(parent2);

            // Create a child! 
            children[i] = CrossParents(population[parent1], population[parent2]);
        }

        return children;
    }

    private void Mutate(Chromosome[] children)
    {
        for (int i = 0; i < children.Length; i++)
        {
            Mutate(children[i]);
        }
    }

    /// <summary>
    /// Culls the population <see cref="killNumber"/> number of times
    /// </summary>
    /// <returns>indices of all the killed chromosomes</returns>
    private HashSet<int> CullPopulation()
    {
        HashSet<int> alreadyKilled = new HashSet<int>();
        for (int i = 0; i < killNumber; i++)
        {
            int killIndex = GetKillIndex(alreadyKilled);
            alreadyKilled.Add(killIndex);
            population[killIndex] = null;
        }
        return alreadyKilled;
    }
    #endregion





    #region Misc Helpers
    private void RandomizeGenes(Chromosome chromosome)
    {
        for (int i = 0; i < chromosomeGeneNum; i++)
        {
            if (chromosome.Genes[i] == null) chromosome.Genes[i] = new Gene();
            RandomizeGene(chromosome.Genes[i]);
        }
    }

    private Chromosome CrossParents(Chromosome parent1, Chromosome parent2)
    {
        Chromosome child = new Chromosome(chromosomeGeneNum);

        var betterParent = parent1.Fitness > parent2.Fitness ? parent1 : parent2;
        var worseParent = betterParent == parent1 ? parent2 : parent1;

        var parent1List = CopyGeneList(betterParent.Genes);
        var parent2List = CopyGeneList(worseParent.Genes);

        float fitnessWeight = Mathf.Min(parent1.Fitness, parent2.Fitness) / (parent1.Fitness + parent2.Fitness);
        for (int i = 0; i < chromosomeGeneNum; i++)
        {
            float weight = Random.value;
            if (weight < fitnessWeight)
            {
                child.Genes[i] = parent2List[i];
            }
            else
            {
                child.Genes[i] = parent1List[i];
            }
        }

        return child;
    }

    private void Mutate(Chromosome chromosome)
    {
        for (int i = 0; i < chromosomeGeneNum; i++)
        {
            if (Random.value < mutationRate)
            {
                RandomizeGene(chromosome.Genes[i]);
            }
        }
    }

    private void RandomizeGene(Gene gene)
    {
        gene.Direction = Random.insideUnitCircle;
    }

    private Gene[] CopyGeneList(Gene[] geneList)
    {
        return geneList.Select((g) => new Gene(g.Direction)).ToArray();
    }

    /// <summary>
    /// Assumes <see cref="population"/> is sorted in descending order
    /// </summary>
    /// <returns></returns>
    private int GetWeightedParentIndex(HashSet<int> takenParents)
    {
        int index = -1;
        while (index < 0 || index >= populationNumber || takenParents.Contains(index))
        {
            float randomNum = Random.value;
            float randomIndexRatio = Random.value;

            // 75% chance to take from the top 25%
            if (randomNum < .75f)
            {
                index = (int)(populationNumber * 0.25f * randomIndexRatio);
            }

            // 20% chance to take from the next 50%
            else if (randomNum < .95f)
            {
                // Bottom index is 0.25 * popnumber, then generate a number to add to that 0-50% of population number
                index = (int)(populationNumber * 0.25f + (populationNumber * 0.5f * randomIndexRatio));
            }

            // 5% chance to take from the bottom 25%
            else
            {
                // Same thing as the else if above, but with .75 as the base number and generating a number to add to that within 0-35% of population number
                index = (int)(populationNumber * 0.75f + (populationNumber * 0.25f * randomIndexRatio));
            }
        }

        return index;
    }

    /// <summary>
    /// Assumes <see cref="population"/> is sorted in descending order
    /// </summary>
    /// <returns></returns>
    private int GetKillIndex(HashSet<int> alreadyKilled)
    {
        int index = -1;
        while (index < 0 || index >= populationNumber || alreadyKilled.Contains(index))
        {
            float randomNum = Random.value;
            float randomIndexRatio = Random.value;

            // 80% chance to take from the bottom 25%
            if (randomNum < .80f)
            {
                index = (int)(populationNumber * 0.75f + (populationNumber * 0.25f * randomIndexRatio));
            }

            // 19.5% chance to take from the next 50%
            else if (randomNum < .995f)
            {
                // Bottom index is 0.25 * popnumber, then generate a number to add to that 0-50% of population number
                index = (int)(populationNumber * 0.25f + (populationNumber * 0.5f * randomIndexRatio));
            }

            // .5% chance to take from the top 25%
            else
            {
                // Same thing as the else if above, but with .75 as the base number and generating a number to add to that within 0-35% of population number
                index = (int)(populationNumber * 0.25f * randomIndexRatio);
            }
        }

        return index;
    }
    #endregion

    #region UI Methods
    public void ChangeTimeScale(float value)
    {
#if UNITY_EDITOR
        value = Mathf.Clamp(value, 0.0f, 100.0f);
#endif

        timeScale = value;
        Time.timeScale = timeScale;
        timescaleText.text = "Timescale: " + timeScale;
    }

    public void ChangeMutationRate(float value)
    {
        mutationRate = value;
        mutationrateText.text = "Mutation Rate: " + mutationRate;
    }

    private int nextPopSize;
    private int nextGenesPerChrom;

    public void ChangeNextPopSize(float value)
    {
        nextPopSize = (int)value;
        nextPopSizeText.text = "Next Population Size: " + nextPopSize;
    }

    public void ChangeNextGenesPerChrom(float value)
    {
        nextGenesPerChrom = (int)value;
        nextGenesPerChromText.text = "# of Genes Per Chromosome: " + nextGenesPerChrom;
    }

    public void ReinitializePopulationWithNewValues()
    {
        StopAllCoroutines();
        for (int i = 0; i < agents.Length; i++)
        {
            Destroy(agents[i]);
        }
        currentState = PopulationState.Initializing;
        previousState = currentState;
        Init();
    }
    #endregion
}
