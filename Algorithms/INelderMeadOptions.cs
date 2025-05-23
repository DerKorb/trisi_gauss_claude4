using System.Numerics;

namespace Optimization.Core.Algorithms;

public interface INelderMeadOptions<T> where T : IFloatingPoint<T>
{
    T FunctionTolerance { get; }
    T ParameterTolerance { get; }
    int MaxIterations { get; }
    ReadOnlyMemory<T> LowerBounds { get; }
    ReadOnlyMemory<T> UpperBounds { get; }
    T InitialSimplexSize { get; }
}

public class NelderMeadOptions<T> : INelderMeadOptions<T> where T : IFloatingPoint<T>
{
    public T FunctionTolerance { get; set; } = T.CreateChecked(1e-8);
    public T ParameterTolerance { get; set; } = T.CreateChecked(1e-8);
    public int MaxIterations { get; set; } = 1000;
    public ReadOnlyMemory<T> LowerBounds { get; set; } = ReadOnlyMemory<T>.Empty;
    public ReadOnlyMemory<T> UpperBounds { get; set; } = ReadOnlyMemory<T>.Empty;
    public T InitialSimplexSize { get; set; } = T.CreateChecked(0.05);
}