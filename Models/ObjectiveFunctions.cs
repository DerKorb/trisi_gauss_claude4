using System.Numerics;

namespace Optimization.Core.Models;

public static class ObjectiveFunctions
{
    public static T SumSquaredResiduals<T>(
        Span<T> parameters,
        ReadOnlySpan<T> xData,
        ReadOnlySpan<T> yData) where T : IFloatingPoint<T>
    {
        if (xData.Length != yData.Length)
            throw new ArgumentException("X and Y data must have the same length");

        T sumSquaredError = T.Zero;
        
        for (int i = 0; i < xData.Length; i++)
        {
            T predicted = DoubleGaussian.Evaluate(parameters, xData[i]);
            T residual = yData[i] - predicted;
            sumSquaredError += residual * residual;
        }
        
        return sumSquaredError;
    }

    public static Func<Span<T>, T> CreateSumSquaredResidualsFunction<T>(
        ReadOnlySpan<T> xData,
        ReadOnlySpan<T> yData) where T : IFloatingPoint<T>
    {
        // Copy data to avoid capturing spans
        var xDataCopy = xData.ToArray();
        var yDataCopy = yData.ToArray();
        
        return parameters => SumSquaredResiduals(parameters, xDataCopy, yDataCopy);
    }

    public static T WeightedSumSquaredResiduals<T>(
        Span<T> parameters,
        ReadOnlySpan<T> xData,
        ReadOnlySpan<T> yData,
        ReadOnlySpan<T> weights) where T : IFloatingPoint<T>
    {
        if (xData.Length != yData.Length || xData.Length != weights.Length)
            throw new ArgumentException("X, Y, and weights data must have the same length");

        T sumSquaredError = T.Zero;
        
        for (int i = 0; i < xData.Length; i++)
        {
            T predicted = DoubleGaussian.Evaluate(parameters, xData[i]);
            T residual = yData[i] - predicted;
            sumSquaredError += weights[i] * residual * residual;
        }
        
        return sumSquaredError;
    }
}