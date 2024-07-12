using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using GeneticSharp;

namespace TrackRunner.Runners.Genetic;

public sealed class GeneticRunner
{
    private readonly Action<Point, Point, Color> drawLine;
    private readonly Action generationDone;
    private GeneticAlgorithm geneticAlgorithm;
    private int lastGeneration;
    private bool isForceStop;

    public GeneticRunner(
        Point startPoint,
        IReadOnlyCollection<Line> track,
        IReadOnlyCollection<Line> checkpoints,
        int populationCount,
        int movementPool,
        Action<Point, Point, Color> drawLine,
        Action generationDone)
    {
        this.drawLine = drawLine;
        this.generationDone = generationDone;

        var population = new Population(populationCount / 2, populationCount, new RunnerChromosome(movementPool))
        {
            GenerationStrategy = new PerformanceGenerationStrategy()
        };

        this.geneticAlgorithm = new GeneticAlgorithm(
            population,
            new RunnerFitness(
                startPoint,
                track.Select(t => (new Point(t.X1, t.Y1), new Point(t.X2, t.Y2))).ToArray(),
                checkpoints.Select(t => (new Point(t.X1, t.Y1), new Point(t.X2, t.Y2))).ToArray()),
            new EliteSelection(),
            new OrderedCrossover(),
            new ReverseSequenceMutation())
        {
            Termination = new TimeEvolvingTermination(TimeSpan.FromSeconds(1000)),
            TaskExecutor = new ParallelTaskExecutor()
        };

        this.geneticAlgorithm.GenerationRan += OnGenerationRan;
    }

    public void Start()
    {
        this.isForceStop = false;
        this.geneticAlgorithm.Start();
    }

    public void Stop()
    {
        this.isForceStop = true;
        this.geneticAlgorithm.Stop();
    }

    private async void OnGenerationRan(object? sender, EventArgs e)
    {
        if (this.isForceStop)
        {
            return;
        }

        if (this.lastGeneration != this.geneticAlgorithm.GenerationsNumber)
        {
            this.geneticAlgorithm.Stop();
            this.lastGeneration = this.geneticAlgorithm.GenerationsNumber;

            this.generationDone();

            RunnerChromosome[] chromosomes = this.geneticAlgorithm.Population.Generations.Last().Chromosomes.Cast<RunnerChromosome>().ToArray();
            int maxSteps = chromosomes.Max(c => c.Path.Count);

            for (int i = 0; i < maxSteps; i++)
            {
                foreach (RunnerChromosome chromosome in chromosomes)
                {
                    if (chromosome.Path.Count > i)
                    {
                        (Point start, Point end) = chromosome.Path[i];
                        this.drawLine(start, end, chromosome.Color);
                    }
                }

                await Task.Delay(10);
            }

            if (!this.isForceStop)
            {
                this.geneticAlgorithm.Resume();
            }
        }
    }
}
