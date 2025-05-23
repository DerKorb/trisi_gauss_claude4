using System.Numerics;
using System.Runtime.CompilerServices;
using Optimization.Core.Algorithms;

namespace Optimization.Core.Models;

/// <summary>
/// Performance-optimized double Gaussian model with efficient implementations
/// </summary>
public static class DoubleGaussianOptimizedFixed
{
    private static readonly double MinSigma = 1e-10;
    private static readonly double NegativeHalf = -0.5;

    /// <summary>
    /// Optimized double Gaussian evaluation with inlined calculations
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Evaluate<T>(ReadOnlySpan<T> parameters, T x) where T : unmanaged, IFloatingPoint<T>
    {
        if (parameters.Length != 6)
            throw new ArgumentException("Double Gaussian requires exactly 6 parameters: [A1, μ1, σ1, A2, μ2, σ2]");

        // Extract parameters
        T a1 = parameters[0];
        T mu1 = parameters[1]; 
        T sigma1 = parameters[2];
        T a2 = parameters[3];
        T mu2 = parameters[4];
        T sigma2 = parameters[5];

        // Handle edge cases for sigma values
        T minSigma = T.CreateChecked(MinSigma);
        if (sigma1 <= T.Zero) sigma1 = minSigma;
        if (sigma2 <= T.Zero) sigma2 = minSigma;

        // Calculate both Gaussians efficiently
        T diff1 = x - mu1;
        T normalized1 = diff1 / sigma1;
        T exponent1 = T.CreateChecked(NegativeHalf) * normalized1 * normalized1;
        
        T diff2 = x - mu2;
        T normalized2 = diff2 / sigma2;
        T exponent2 = T.CreateChecked(NegativeHalf) * normalized2 * normalized2;

        // Use fast math operations when possible
        T gaussian1 = a1 * T.CreateChecked(Math.Exp(double.CreateChecked(exponent1)));
        T gaussian2 = a2 * T.CreateChecked(Math.Exp(double.CreateChecked(exponent2)));

        return gaussian1 + gaussian2;
    }

    /// <summary>
    /// Optimized range evaluation with efficient memory usage
    /// </summary>
    public static void EvaluateRange<T>(
        ReadOnlySpan<T> parameters,
        ReadOnlySpan<T> xValues,
        Span<T> result) where T : unmanaged, IFloatingPoint<T>
    {
        if (result.Length != xValues.Length)
            throw new ArgumentException("Result span must have same length as x values");

        if (parameters.Length != 6)
            throw new ArgumentException("Double Gaussian requires exactly 6 parameters: [A1, μ1, σ1, A2, μ2, σ2]");

        // Extract and pre-process parameters once
        T a1 = parameters[0];
        T mu1 = parameters[1];
        T sigma1 = parameters[2];
        T a2 = parameters[3];
        T mu2 = parameters[4];
        T sigma2 = parameters[5];

        T minSigma = T.CreateChecked(MinSigma);
        if (sigma1 <= T.Zero) sigma1 = minSigma;
        if (sigma2 <= T.Zero) sigma2 = minSigma;

        T negHalf = T.CreateChecked(NegativeHalf);

        // Optimized loop with potential for auto-vectorization
        for (int i = 0; i < xValues.Length; i++)
        {
            T x = xValues[i];
            
            // First Gaussian
            T diff1 = x - mu1;
            T norm1 = diff1 / sigma1;
            T exp1 = negHalf * norm1 * norm1;
            T gauss1 = a1 * T.CreateChecked(Math.Exp(double.CreateChecked(exp1)));
            
            // Second Gaussian
            T diff2 = x - mu2;
            T norm2 = diff2 / sigma2;
            T exp2 = negHalf * norm2 * norm2;
            T gauss2 = a2 * T.CreateChecked(Math.Exp(double.CreateChecked(exp2)));
            
            result[i] = gauss1 + gauss2;
        }
    }

    /// <summary>
    /// Optimized sum of squared residuals with efficient memory usage
    /// </summary>
    public static T SumSquaredResidualsOptimized<T>(
        ReadOnlySpan<T> parameters,
        ReadOnlySpan<T> xData,
        ReadOnlySpan<T> yData) where T : unmanaged, IFloatingPoint<T>
    {
        if (xData.Length != yData.Length)
            throw new ArgumentException("X and Y data must have the same length");

        if (parameters.Length != 6)
            throw new ArgumentException("Double Gaussian requires exactly 6 parameters");

        // For small datasets, use direct calculation to avoid allocation
        if (xData.Length <= 64)
        {
            return SumSquaredResidualsInline(parameters, xData, yData);
        }

        // For larger datasets, use pre-computed predictions with stack allocation when possible
        if (xData.Length <= 1024)
        {
            Span<T> predicted = stackalloc T[xData.Length];
            EvaluateRange(parameters, xData, predicted);
            return CalculateResiduals(yData, predicted);
        }

        // For very large datasets, use heap allocation
        var predictedLarge = new T[xData.Length];
        EvaluateRange(parameters, xData, predictedLarge);
        return CalculateResiduals(yData, predictedLarge);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T SumSquaredResidualsInline<T>(
        ReadOnlySpan<T> parameters,
        ReadOnlySpan<T> xData,
        ReadOnlySpan<T> yData) where T : unmanaged, IFloatingPoint<T>
    {
        // Extract parameters once
        T a1 = parameters[0];
        T mu1 = parameters[1];
        T sigma1 = parameters[2];
        T a2 = parameters[3];
        T mu2 = parameters[4];
        T sigma2 = parameters[5];

        T minSigma = T.CreateChecked(MinSigma);
        if (sigma1 <= T.Zero) sigma1 = minSigma;
        if (sigma2 <= T.Zero) sigma2 = minSigma;

        T negHalf = T.CreateChecked(NegativeHalf);
        T sumSquaredError = T.Zero;

        // Inline calculation - no function call overhead
        for (int i = 0; i < xData.Length; i++)
        {
            T x = xData[i];
            
            // Calculate prediction inline
            T diff1 = x - mu1;
            T norm1 = diff1 / sigma1;
            T exp1 = negHalf * norm1 * norm1;
            T gauss1 = a1 * T.CreateChecked(Math.Exp(double.CreateChecked(exp1)));
            
            T diff2 = x - mu2;
            T norm2 = diff2 / sigma2;
            T exp2 = negHalf * norm2 * norm2;
            T gauss2 = a2 * T.CreateChecked(Math.Exp(double.CreateChecked(exp2)));
            
            T predicted = gauss1 + gauss2;
            T residual = yData[i] - predicted;
            sumSquaredError += residual * residual;
        }

        return sumSquaredError;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T CalculateResiduals<T>(ReadOnlySpan<T> yData, ReadOnlySpan<T> predicted) where T : unmanaged, IFloatingPoint<T>
    {
        T sumSquaredError = T.Zero;
        
        // Vectorizable residual calculation
        for (int i = 0; i < yData.Length; i++)
        {
            T residual = yData[i] - predicted[i];
            sumSquaredError += residual * residual;
        }
        
        return sumSquaredError;
    }

    /// <summary>
    /// Create optimized objective function for fitting
    /// </summary>
    public static Func<ReadOnlySpan<T>, T> CreateOptimizedObjective<T>(
        T[] xData, T[] yData) where T : unmanaged, IFloatingPoint<T>
    {
        // Capture arrays to avoid span issues in lambda
        return parameters => SumSquaredResidualsOptimized(parameters, xData, yData);
    }

    /// <summary>
    /// Optimized fitting function using high-performance algorithm
    /// </summary>
    public static OptimizationResult<T> FitOptimized<T>(
        ReadOnlySpan<T> xData,
        ReadOnlySpan<T> yData,
        ReadOnlySpan<T> initialGuess,
        INelderMeadOptions<T>? options = null) where T : unmanaged, IFloatingPoint<T>
    {
        if (initialGuess.Length != 6)
            throw new ArgumentException("Initial guess must have exactly 6 parameters for double Gaussian");

        // Convert to arrays for lambda capture
        var xArray = xData.ToArray();
        var yArray = yData.ToArray();

        // Create optimized objective function
        var objective = CreateOptimizedObjective<T>(xArray, yArray);
        
        return NelderMeadOptimized<T>.Minimize(objective, initialGuess, options);
    }

    /// <summary>
    /// Generate intelligent initial guess with optimized calculations
    /// </summary>
    public static ReadOnlySpan<T> GenerateOptimizedInitialGuess<T>(
        ReadOnlySpan<T> xData,
        ReadOnlySpan<T> yData) where T : unmanaged, IFloatingPoint<T>
    {
        if (xData.Length != yData.Length || xData.Length < 6)
            throw new ArgumentException("Need at least 6 data points for double Gaussian fitting");

        // Find peaks and estimate parameters efficiently
        T maxY = T.Zero;
        int maxIndex = 0;
        T minX = xData[0];
        T maxX = xData[0];

        // Single pass to find min, max, and peak
        for (int i = 0; i < yData.Length; i++)
        {
            if (yData[i] > maxY)
            {
                maxY = yData[i];
                maxIndex = i;
            }
            if (xData[i] < minX) minX = xData[i];
            if (xData[i] > maxX) maxX = xData[i];
        }

        T range = maxX - minX;
        T quarter = range / T.CreateChecked(4);
        
        // Heuristic: assume two peaks at 1/4 and 3/4 of the range
        T mu1 = minX + quarter;
        T mu2 = minX + T.CreateChecked(3) * quarter;
        T sigma = range / T.CreateChecked(8); // Estimate width
        T amplitude = maxY / T.CreateChecked(2); // Split amplitude

        return new T[] { amplitude, mu1, sigma, amplitude, mu2, sigma };
    }
}