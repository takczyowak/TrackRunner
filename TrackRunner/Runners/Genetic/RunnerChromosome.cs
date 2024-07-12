using System.Windows;
using System.Windows.Media;
using GeneticSharp;
using static Microsoft.ML.Data.SchemaDefinition;

namespace TrackRunner.Runners.Genetic;

public sealed class RunnerChromosome : ChromosomeBase
{
    private int length;

    public RunnerChromosome(int length)
        : base(length)
    {
        this.length = length;
        Color = Color.FromRgb(
            (byte)RandomizationProvider.Current.GetInt(0, 256),
            (byte)RandomizationProvider.Current.GetInt(0, 256),
            (byte)RandomizationProvider.Current.GetInt(0, 256));

        for (int i = 0; i < length; i++)
        {
            ReplaceGene(i, new Gene(CreateImperfectPosition()));
        }
    }

    public Color Color { get; }

    public List<(Point start, Point end)> Path { get; set; }

    public override Gene GenerateGene(int geneIndex)
    {
        return new Gene(CreateImperfectPosition());
    }

    private PositionInfo CreatePosition()
    {
        float direction = RandomizationProvider.Current.GetFloat() + RandomizationProvider.Current.GetFloat();
        float angle = 180.0f * direction / 2.0f;

        return new PositionInfo
        {
            Direction = direction,
            DistanceFront = RandomizationProvider.Current.GetFloat(0.0f, 50.0f),
            AngleInDegrees = 270.0f + angle
        };
    }

    private PositionInfo CreateImperfectPosition()
    {
        PositionInfo position = CreatePosition();
        if (RandomizationProvider.Current.GetInt(0, 5) == 0)
        {
            position.AngleInDegrees = 270.0f + RandomizationProvider.Current.GetFloat() * 180.0f;
        }

        return position;
    }

    private PositionInfo CreateFullRandomPosition()
    {
        return new PositionInfo
        {
            Direction = RandomizationProvider.Current.GetFloat() + RandomizationProvider.Current.GetFloat(),
            DistanceFront = RandomizationProvider.Current.GetFloat(0.0f, 30.0f),
            AngleInDegrees = RandomizationProvider.Current.GetFloat() * 270.0f + 225.0f
        };
    }

    public override IChromosome CreateNew()
    {
        return new RunnerChromosome(this.length);
    }
}
