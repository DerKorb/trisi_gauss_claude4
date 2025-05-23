using System.Numerics;
using System.Runtime.InteropServices;

namespace Optimization.Core.Algorithms;

public static class NelderMead<T> where T : IFloatingPoint<T>
{
    private static readonly T Alpha = T.CreateChecked(1.0);   // Reflection
    private static readonly T Gamma = T.CreateChecked(2.0);   // Expansion
    private static readonly T Rho = T.CreateChecked(0.5);     // Contraction
    private static readonly T Sigma = T.CreateChecked(0.5);   // Shrink
    private static readonly T PenaltyFactor = T.CreateChecked(1e6);

    public static OptimizationResult<T> Minimize(
        Func<Span<T>, T> objective,
        Span<T> initialGuess,
        INelderMeadOptions<T>? options = null)
    {
        options ??= new NelderMeadOptions<T>();
        
        int n = initialGuess.Length;
        if (n == 0) throw new ArgumentException("Initial guess cannot be empty");

        // Create bounded objective function if bounds are specified
        var boundedObjective = CreateBoundedObjective(objective, options.LowerBounds, options.UpperBounds);
        
        // Initialize simplex with n+1 vertices
        var simplex = InitializeSimplex(initialGuess, options.InitialSimplexSize, options.LowerBounds, options.UpperBounds);
        var values = new T[n + 1];
        var indices = new int[n + 1];
        
        int functionEvaluations = 0;
        
        // Evaluate initial simplex
        for (int i = 0; i <= n; i++)
        {
            var vertex = simplex.AsSpan(i * n, n);
            values[i] = boundedObjective(vertex);
            indices[i] = i;
            functionEvaluations++;
        }

        // Working arrays
        var centroid = new T[n];
        var reflected = new T[n];
        var expanded = new T[n];
        var contracted = new T[n];

        for (int iteration = 0; iteration < options.MaxIterations; iteration++)
        {
            // Sort vertices by function value
            SortVertices(values, indices);
            
            int best = indices[0];
            int worst = indices[n];
            int secondWorst = indices[n - 1];

            // Check convergence
            T functionSpread = values[worst] - values[best];
            if (functionSpread <= options.FunctionTolerance)
            {
                var result = new T[n];
                simplex.AsSpan(best * n, n).CopyTo(result);
                return new OptimizationResult<T>(
                    result, values[best], iteration, functionEvaluations, true, "Function tolerance reached");
            }

            // Calculate centroid of all vertices except worst
            CalculateCentroid(simplex, indices, centroid, worst, n);

            // Reflection
            Reflect(simplex.AsSpan(worst * n, n), centroid, reflected);
            T reflectedValue = boundedObjective(reflected);
            functionEvaluations++;

            if (values[best] <= reflectedValue && reflectedValue < values[secondWorst])
            {
                // Accept reflection
                reflected.CopyTo(simplex.AsSpan(worst * n, n));
                values[worst] = reflectedValue;
                continue;
            }

            if (reflectedValue < values[best])
            {
                // Try expansion
                Expand(centroid, reflected, expanded);
                T expandedValue = boundedObjective(expanded);
                functionEvaluations++;

                if (expandedValue < reflectedValue)
                {
                    expanded.CopyTo(simplex.AsSpan(worst * n, n));
                    values[worst] = expandedValue;
                }
                else
                {
                    reflected.CopyTo(simplex.AsSpan(worst * n, n));
                    values[worst] = reflectedValue;
                }
                continue;
            }

            // Contraction
            bool useReflected = reflectedValue < values[worst];
            var contractionPoint = useReflected ? reflected : simplex.AsSpan(worst * n, n);
            
            Contract(centroid, contractionPoint, contracted);
            T contractedValue = boundedObjective(contracted);
            functionEvaluations++;

            T comparisonValue = useReflected ? reflectedValue : values[worst];
            if (contractedValue < comparisonValue)
            {
                contracted.CopyTo(simplex.AsSpan(worst * n, n));
                values[worst] = contractedValue;
                continue;
            }

            // Shrink simplex toward best vertex
            ShrinkSimplex(simplex, indices, best, n);
            for (int i = 1; i <= n; i++)
            {
                var vertex = simplex.AsSpan(indices[i] * n, n);
                values[indices[i]] = boundedObjective(vertex);
                functionEvaluations++;
            }
        }

        // Return best result found
        SortVertices(values, indices);
        var finalResult = new T[n];
        simplex.AsSpan(indices[0] * n, n).CopyTo(finalResult);
        
        return new OptimizationResult<T>(
            finalResult, values[indices[0]], options.MaxIterations, functionEvaluations, false, "Maximum iterations reached");
    }

    private static Func<Span<T>, T> CreateBoundedObjective(
        Func<Span<T>, T> objective,
        ReadOnlyMemory<T> lowerBounds,
        ReadOnlyMemory<T> upperBounds)
    {
        if (lowerBounds.IsEmpty && upperBounds.IsEmpty)
            return objective;

        return parameters =>
        {
            T penalty = T.Zero;
            
            if (!lowerBounds.IsEmpty)
            {
                var lowerSpan = lowerBounds.Span;
                for (int i = 0; i < parameters.Length && i < lowerSpan.Length; i++)
                {
                    if (parameters[i] < lowerSpan[i])
                    {
                        T violation = lowerSpan[i] - parameters[i];
                        penalty += PenaltyFactor * violation * violation;
                    }
                }
            }
            
            if (!upperBounds.IsEmpty)
            {
                var upperSpan = upperBounds.Span;
                for (int i = 0; i < parameters.Length && i < upperSpan.Length; i++)
                {
                    if (parameters[i] > upperSpan[i])
                    {
                        T violation = parameters[i] - upperSpan[i];
                        penalty += PenaltyFactor * violation * violation;
                    }
                }
            }

            return objective(parameters) + penalty;
        };
    }

    private static T[] InitializeSimplex(
        Span<T> initialGuess,
        T simplexSize,
        ReadOnlyMemory<T> lowerBounds,
        ReadOnlyMemory<T> upperBounds)
    {
        int n = initialGuess.Length;
        var simplex = new T[(n + 1) * n];
        
        // First vertex is the initial guess
        initialGuess.CopyTo(simplex.AsSpan(0, n));
        
        // Create additional vertices
        for (int i = 1; i <= n; i++)
        {
            var vertex = simplex.AsSpan(i * n, n);
            initialGuess.CopyTo(vertex);
            
            // Modify the (i-1)th parameter
            int paramIndex = i - 1;
            T step = T.Abs(initialGuess[paramIndex]) * simplexSize;
            if (step == T.Zero) step = simplexSize;
            
            vertex[paramIndex] += step;
            
            // Ensure bounds are respected
            if (!upperBounds.IsEmpty)
            {
                var upperSpan = upperBounds.Span;
                if (paramIndex < upperSpan.Length && vertex[paramIndex] > upperSpan[paramIndex])
                {
                    vertex[paramIndex] = initialGuess[paramIndex] - step;
                }
            }
            if (!lowerBounds.IsEmpty)
            {
                var lowerSpan = lowerBounds.Span;
                if (paramIndex < lowerSpan.Length && vertex[paramIndex] < lowerSpan[paramIndex])
                {
                    vertex[paramIndex] = initialGuess[paramIndex] + T.Abs(step);
                }
            }
        }
        
        return simplex;
    }

    private static void SortVertices(Span<T> values, Span<int> indices)
    {
        // Simple selection sort for small arrays
        for (int i = 0; i < values.Length - 1; i++)
        {
            int minIndex = i;
            for (int j = i + 1; j < values.Length; j++)
            {
                if (values[indices[j]] < values[indices[minIndex]])
                    minIndex = j;
            }
            if (minIndex != i)
                (indices[i], indices[minIndex]) = (indices[minIndex], indices[i]);
        }
    }

    private static void CalculateCentroid(Span<T> simplex, Span<int> indices, Span<T> centroid, int excludeIndex, int n)
    {
        centroid.Clear();
        
        for (int i = 0; i <= n; i++)
        {
            if (indices[i] == excludeIndex) continue;
            
            var vertex = simplex.Slice(indices[i] * n, n);
            for (int j = 0; j < n; j++)
                centroid[j] += vertex[j];
        }
        
        T divisor = T.CreateChecked(n);
        for (int i = 0; i < n; i++)
            centroid[i] /= divisor;
    }

    private static void Reflect(ReadOnlySpan<T> worst, ReadOnlySpan<T> centroid, Span<T> reflected)
    {
        for (int i = 0; i < reflected.Length; i++)
            reflected[i] = centroid[i] + Alpha * (centroid[i] - worst[i]);
    }

    private static void Expand(ReadOnlySpan<T> centroid, ReadOnlySpan<T> reflected, Span<T> expanded)
    {
        for (int i = 0; i < expanded.Length; i++)
            expanded[i] = centroid[i] + Gamma * (reflected[i] - centroid[i]);
    }

    private static void Contract(ReadOnlySpan<T> centroid, ReadOnlySpan<T> point, Span<T> contracted)
    {
        for (int i = 0; i < contracted.Length; i++)
            contracted[i] = centroid[i] + Rho * (point[i] - centroid[i]);
    }

    private static void ShrinkSimplex(Span<T> simplex, Span<int> indices, int bestIndex, int n)
    {
        var bestVertex = simplex.Slice(bestIndex * n, n);
        
        for (int i = 1; i <= n; i++)
        {
            var vertex = simplex.Slice(indices[i] * n, n);
            for (int j = 0; j < n; j++)
                vertex[j] = bestVertex[j] + Sigma * (vertex[j] - bestVertex[j]);
        }
    }
}