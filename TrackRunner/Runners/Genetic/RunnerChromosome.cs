using GeneticSharp;

namespace TrackRunner.Runners.Genetic;

public sealed class RunnerChromosome : ChromosomeBase
{
    private int length;

    public RunnerChromosome(int length)
        : base(length)
    {
        this.length = length;

        for (int i = 0; i < length; i++)
        {
            var position = new PositionInfo
            {
                Direction = RandomizationProvider.Current.GetFloat() + RandomizationProvider.Current.GetFloat(),
                DistanceFront = RandomizationProvider.Current.GetFloat(0.0f, 30.0f),
                AngleInDegrees = RandomizationProvider.Current.GetFloat() * 270.0f + 225.0f
            };

            ReplaceGene(i, new Gene(position));
        }
    }

    public override Gene GenerateGene(int geneIndex)
    {
        return new Gene(
            new PositionInfo
            {
                Direction = RandomizationProvider.Current.GetFloat() + RandomizationProvider.Current.GetFloat(),
                DistanceFront = RandomizationProvider.Current.GetFloat(0.0f, 30.0f),
                AngleInDegrees = RandomizationProvider.Current.GetFloat() * 270.0f + 225.0f
            });
    }

    public override IChromosome CreateNew()
    {
        return new RunnerChromosome(this.length);
    }
}
