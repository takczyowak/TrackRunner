using GeneticSharp;

namespace TrackRunner.Runners.Genetic;

public sealed class GeneticRunner
{
    public GeneticRunner(int populationCount, int movementPool)
    {
        var population = new Population(populationCount / 2, populationCount, new RunnerChromosome(movementPool))
        {
            GenerationStrategy = new PerformanceGenerationStrategy()
        };

        var ga = new GeneticAlgorithm(population, new RunnerFitness(), new EliteSelection(), new OrderedCrossover(), new ReverseSequenceMutation())
        {
            Termination = new FitnessStagnationTermination(100),
            TaskExecutor = new ParallelTaskExecutor()
        };

        ga.GenerationRan += OnGenerationRan;

        ga.Start();
    }

    private void OnGenerationRan(object? sender, EventArgs e)
    {

    }
}
