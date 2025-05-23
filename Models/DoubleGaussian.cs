using System.Numerics;
using Optimization.Core.Algorithms;

namespace Optimization.Core.Models;

public static class DoubleGaussian
{
    public static T Evaluate<T>(ReadOnlySpan<T> parameters, T x) where T : IFloatingPoint<T>
    {
        if (parameters.Length != 6)
            throw new ArgumentException("Double Gaussian requires exactly 6 parameters: [A1, μ1, σ1, A2, μ2, σ2]");

        T a1 = parameters[0];
        T mu1 = parameters[1]; 
        T sigma1 = parameters[2];
        T a2 = parameters[3];
        T mu2 = parameters[4];
        T sigma2 = parameters[5];

        // Handle edge cases for sigma values
        T minSigma = T.CreateChecked(1e-10);
        if (sigma1 <= T.Zero) sigma1 = minSigma;
        if (sigma2 <= T.Zero) sigma2 = minSigma;

        // Calculate first Gaussian: A1 * exp(-0.5 * ((x - μ1) / σ1)²)
        T diff1 = x - mu1;
        T normalized1 = diff1 / sigma1;
        T exponent1 = -T.CreateChecked(0.5) * normalized1 * normalized1;
        T gaussian1 = a1 * T.CreateChecked(Math.Exp(double.CreateChecked(exponent1)));

        // Calculate second Gaussian: A2 * exp(-0.5 * ((x - μ2) / σ2)²)
        T diff2 = x - mu2;
        T normalized2 = diff2 / sigma2;
        T exponent2 = -T.CreateChecked(0.5) * normalized2 * normalized2;
        T gaussian2 = a2 * T.CreateChecked(Math.Exp(double.CreateChecked(exponent2)));

        return gaussian1 + gaussian2;
    }

    public static void EvaluateRange<T>(
        ReadOnlySpan<T> parameters,
        ReadOnlySpan<T> xValues,
        Span<T> result) where T : IFloatingPoint<T>
    {
        if (result.Length != xValues.Length)
            throw new ArgumentException("Result span must have same length as x values");

        for (int i = 0; i < xValues.Length; i++)
        {
            result[i] = Evaluate(parameters, xValues[i]);
        }
    }

    public static OptimizationResult<T> Fit<T>(
        ReadOnlySpan<T> xData,
        ReadOnlySpan<T> yData,
        ReadOnlySpan<T> initialGuess,
        INelderMeadOptions<T>? options = null) where T : IFloatingPoint<T>
    {
        if (initialGuess.Length != 6)
            throw new ArgumentException("Initial guess must have exactly 6 parameters for double Gaussian");

        var objective = Models.ObjectiveFunctions.CreateSumSquaredResidualsFunction(xData, yData);
        var guess = initialGuess.ToArray().AsSpan();
        
        return NelderMead<T>.Minimize(objective, guess, options);
    }

    public static ReadOnlySpan<T> GetDefaultBounds<T>() where T : IFloatingPoint<T>
    {
        // Default bounds: [A1_min, μ1_min, σ1_min, A2_min, μ2_min, σ2_min]
        //                 [A1_max, μ1_max, σ1_max, A2_max, μ2_max, σ2_max]
        return new T[]
        {
            T.CreateChecked(-1000), T.CreateChecked(-1000), T.CreateChecked(1e-10),
            T.CreateChecked(-1000), T.CreateChecked(-1000), T.CreateChecked(1e-10),
            T.CreateChecked(1000), T.CreateChecked(1000), T.CreateChecked(1000),
            T.CreateChecked(1000), T.CreateChecked(1000), T.CreateChecked(1000)
        };
    }

    public static ReadOnlySpan<T> GenerateInitialGuess<T>(
        ReadOnlySpan<T> xData,
        ReadOnlySpan<T> yData) where T : IFloatingPoint<T>
    {
        if (xData.Length != yData.Length || xData.Length < 6)
            throw new ArgumentException("Need at least 6 data points for double Gaussian fitting");

        // Find peaks and estimate parameters
        T maxY = T.Zero;
        int maxIndex = 0;
        T minX = xData[0];
        T maxX = xData[0];

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
        
        // Simple heuristic: assume two peaks at 1/4 and 3/4 of the range
        T mu1 = minX + quarter;
        T mu2 = minX + T.CreateChecked(3) * quarter;
        T sigma = range / T.CreateChecked(8); // Estimate width
        T amplitude = maxY / T.CreateChecked(2); // Split amplitude

        return new T[] { amplitude, mu1, sigma, amplitude, mu2, sigma };
    }
}