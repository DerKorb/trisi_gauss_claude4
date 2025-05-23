using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Buffers;

namespace Optimization.Core.Algorithms;

/// <summary>
/// High-performance optimized Nelder-Mead implementation with advanced performance techniques
/// </summary>
public static class NelderMeadOptimized<T> where T : unmanaged, IFloatingPoint<T>
{
    // Pre-computed constants to avoid repeated CreateChecked calls
    private static readonly T Alpha = T.CreateChecked(1.0);   // Reflection
    private static readonly T Gamma = T.CreateChecked(2.0);   // Expansion  
    private static readonly T Rho = T.CreateChecked(0.5);     // Contraction
    private static readonly T Sigma = T.CreateChecked(0.5);   // Shrink
    private static readonly T PenaltyFactor = T.CreateChecked(1e6);
    private static readonly T Half = T.CreateChecked(0.5);
    private static readonly T Zero = T.Zero;
    private static readonly T One = T.One;

    /// <summary>
    /// Optimized workspace for reusable arrays and reduced allocations
    /// </summary>
    private readonly struct OptimizationWorkspace : IDisposable
    {
        public readonly T[] Simplex;
        public readonly T[] Values;
        public readonly int[] Indices;
        public readonly T[] Centroid;
        public readonly T[] Reflected;
        public readonly T[] Expanded;
        public readonly T[] Contracted;
        public readonly T[] TempArray;

        public OptimizationWorkspace(int dimensions)
        {
            int n = dimensions;
            int simplexSize = (n + 1) * n;
            
            // Use ArrayPool for larger arrays to reduce GC pressure
            if (simplexSize > 1024)
            {
                Simplex = ArrayPool<T>.Shared.Rent(simplexSize);
            }
            else
            {
                Simplex = new T[simplexSize];
            }
            
            Values = new T[n + 1];
            Indices = new int[n + 1];
            Centroid = new T[n];
            Reflected = new T[n];
            Expanded = new T[n];
            Contracted = new T[n];
            TempArray = new T[n];
        }

        public void Dispose()
        {
            if (Simplex.Length > 1024)
            {
                ArrayPool<T>.Shared.Return(Simplex);
            }
        }
    }

    public static OptimizationResult<T> Minimize(
        Func<ReadOnlySpan<T>, T> objective,
        ReadOnlySpan<T> initialGuess,
        INelderMeadOptions<T>? options = null)
    {
        options ??= new NelderMeadOptions<T>();
        
        int n = initialGuess.Length;
        if (n == 0) throw new ArgumentException("Initial guess cannot be empty");

        // Use workspace pattern to reduce allocations
        using var workspace = new OptimizationWorkspace(n);
        
        // Pre-compute bounds spans to avoid repeated Memory.Span calls
        var lowerBounds = options.LowerBounds.IsEmpty ? ReadOnlySpan<T>.Empty : options.LowerBounds.Span;
        var upperBounds = options.UpperBounds.IsEmpty ? ReadOnlySpan<T>.Empty : options.UpperBounds.Span;
        bool hasBounds = !lowerBounds.IsEmpty || !upperBounds.IsEmpty;

        // Initialize simplex
        InitializeSimplexOptimized(initialGuess, options.InitialSimplexSize, lowerBounds, upperBounds, workspace.Simplex, n);
        
        int functionEvaluations = 0;
        
        // Evaluate initial simplex
        for (int i = 0; i <= n; i++)
        {
            var vertex = workspace.Simplex.AsSpan(i * n, n);
            workspace.Values[i] = hasBounds ? 
                EvaluateWithBounds(objective, vertex, lowerBounds, upperBounds) : 
                objective(vertex);
            workspace.Indices[i] = i;
            functionEvaluations++;
        }

        // Main optimization loop
        for (int iteration = 0; iteration < options.MaxIterations; iteration++)
        {
            // Optimized sorting for small arrays
            SortVerticesOptimized(workspace.Values, workspace.Indices);
            
            int best = workspace.Indices[0];
            int worst = workspace.Indices[n];
            int secondWorst = workspace.Indices[n - 1];

            // Check convergence with fast comparison
            T functionSpread = workspace.Values[worst] - workspace.Values[best];
            if (functionSpread <= options.FunctionTolerance)
            {
                // Copy result efficiently
                var result = new T[n];
                workspace.Simplex.AsSpan(best * n, n).CopyTo(result);
                return new OptimizationResult<T>(
                    result, workspace.Values[best], iteration, functionEvaluations, true, "Function tolerance reached");
            }

            // Calculate centroid - optimized with SIMD potential
            CalculateCentroidOptimized(workspace.Simplex, workspace.Indices, workspace.Centroid, worst, n);

            // Reflection
            var worstVertex = workspace.Simplex.AsSpan(worst * n, n);
            ReflectOptimized(worstVertex, workspace.Centroid, workspace.Reflected);
            
            T reflectedValue = hasBounds ? 
                EvaluateWithBounds(objective, workspace.Reflected, lowerBounds, upperBounds) : 
                objective(workspace.Reflected);
            functionEvaluations++;

            if (workspace.Values[best] <= reflectedValue && reflectedValue < workspace.Values[secondWorst])
            {
                // Accept reflection - fast copy
                workspace.Reflected.CopyTo(worstVertex);
                workspace.Values[worst] = reflectedValue;
                continue;
            }

            if (reflectedValue < workspace.Values[best])
            {
                // Try expansion
                ExpandOptimized(workspace.Centroid, workspace.Reflected, workspace.Expanded);
                T expandedValue = hasBounds ? 
                    EvaluateWithBounds(objective, workspace.Expanded, lowerBounds, upperBounds) : 
                    objective(workspace.Expanded);
                functionEvaluations++;

                if (expandedValue < reflectedValue)
                {
                    workspace.Expanded.CopyTo(worstVertex);
                    workspace.Values[worst] = expandedValue;
                }
                else
                {
                    workspace.Reflected.CopyTo(worstVertex);
                    workspace.Values[worst] = reflectedValue;
                }
                continue;
            }

            // Contraction
            bool useReflected = reflectedValue < workspace.Values[worst];
            var contractionPoint = useReflected ? 
                workspace.Reflected.AsSpan() : 
                worstVertex;
            
            ContractOptimized(workspace.Centroid, contractionPoint, workspace.Contracted);
            T contractedValue = hasBounds ? 
                EvaluateWithBounds(objective, workspace.Contracted, lowerBounds, upperBounds) : 
                objective(workspace.Contracted);
            functionEvaluations++;

            T comparisonValue = useReflected ? reflectedValue : workspace.Values[worst];
            if (contractedValue < comparisonValue)
            {
                workspace.Contracted.CopyTo(worstVertex);
                workspace.Values[worst] = contractedValue;
                continue;
            }

            // Shrink simplex toward best vertex
            ShrinkSimplexOptimized(workspace.Simplex, workspace.Indices, best, n);
            for (int i = 1; i <= n; i++)
            {
                var vertex = workspace.Simplex.AsSpan(workspace.Indices[i] * n, n);
                workspace.Values[workspace.Indices[i]] = hasBounds ? 
                    EvaluateWithBounds(objective, vertex, lowerBounds, upperBounds) : 
                    objective(vertex);
                functionEvaluations++;
            }
        }

        // Return best result found
        SortVerticesOptimized(workspace.Values, workspace.Indices);
        var finalResult = new T[n];
        workspace.Simplex.AsSpan(workspace.Indices[0] * n, n).CopyTo(finalResult);
        
        return new OptimizationResult<T>(
            finalResult, workspace.Values[workspace.Indices[0]], options.MaxIterations, functionEvaluations, false, "Maximum iterations reached");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T EvaluateWithBounds(
        Func<ReadOnlySpan<T>, T> objective,
        ReadOnlySpan<T> parameters,
        ReadOnlySpan<T> lowerBounds,
        ReadOnlySpan<T> upperBounds)
    {
        T penalty = Zero;
        
        // Unrolled bounds checking for common small dimensions
        if (parameters.Length <= 6)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i < lowerBounds.Length && parameters[i] < lowerBounds[i])
                {
                    T violation = lowerBounds[i] - parameters[i];
                    penalty += PenaltyFactor * violation * violation;
                }
                if (i < upperBounds.Length && parameters[i] > upperBounds[i])
                {
                    T violation = parameters[i] - upperBounds[i];
                    penalty += PenaltyFactor * violation * violation;
                }
            }
        }
        else
        {
            // Vectorizable loop for larger dimensions
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i < lowerBounds.Length && parameters[i] < lowerBounds[i])
                {
                    T violation = lowerBounds[i] - parameters[i];
                    penalty += PenaltyFactor * violation * violation;
                }
                if (i < upperBounds.Length && parameters[i] > upperBounds[i])
                {
                    T violation = parameters[i] - upperBounds[i];
                    penalty += PenaltyFactor * violation * violation;
                }
            }
        }

        return objective(parameters) + penalty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InitializeSimplexOptimized(
        ReadOnlySpan<T> initialGuess,
        T simplexSize,
        ReadOnlySpan<T> lowerBounds,
        ReadOnlySpan<T> upperBounds,
        Span<T> simplex,
        int n)
    {
        // First vertex is the initial guess
        initialGuess.CopyTo(simplex.Slice(0, n));
        
        // Create additional vertices with loop unrolling for small dimensions
        if (n <= 6)
        {
            for (int i = 1; i <= n; i++)
            {
                var vertex = simplex.Slice(i * n, n);
                initialGuess.CopyTo(vertex);
                
                int paramIndex = i - 1;
                T step = T.Abs(initialGuess[paramIndex]) * simplexSize;
                if (step == Zero) step = simplexSize;
                
                vertex[paramIndex] += step;
                
                // Bounds checking
                if (paramIndex < upperBounds.Length && vertex[paramIndex] > upperBounds[paramIndex])
                {
                    vertex[paramIndex] = initialGuess[paramIndex] - step;
                }
                if (paramIndex < lowerBounds.Length && vertex[paramIndex] < lowerBounds[paramIndex])
                {
                    vertex[paramIndex] = initialGuess[paramIndex] + T.Abs(step);
                }
            }
        }
        else
        {
            // Standard loop for larger dimensions
            for (int i = 1; i <= n; i++)
            {
                var vertex = simplex.Slice(i * n, n);
                initialGuess.CopyTo(vertex);
                
                int paramIndex = i - 1;
                T step = T.Abs(initialGuess[paramIndex]) * simplexSize;
                if (step == Zero) step = simplexSize;
                
                vertex[paramIndex] += step;
                
                if (paramIndex < upperBounds.Length && vertex[paramIndex] > upperBounds[paramIndex])
                {
                    vertex[paramIndex] = initialGuess[paramIndex] - step;
                }
                if (paramIndex < lowerBounds.Length && vertex[paramIndex] < lowerBounds[paramIndex])
                {
                    vertex[paramIndex] = initialGuess[paramIndex] + T.Abs(step);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SortVerticesOptimized(Span<T> values, Span<int> indices)
    {
        // Optimized sorting for typical small arrays (n+1 vertices)
        int length = indices.Length;
        
        if (length <= 8)
        {
            // Insertion sort for very small arrays - cache friendly
            for (int i = 1; i < length; i++)
            {
                int current = indices[i];
                T currentValue = values[current];
                int j = i - 1;
                
                while (j >= 0 && values[indices[j]] > currentValue)
                {
                    indices[j + 1] = indices[j];
                    j--;
                }
                indices[j + 1] = current;
            }
        }
        else
        {
            // Selection sort for larger arrays
            for (int i = 0; i < length - 1; i++)
            {
                int minIndex = i;
                for (int j = i + 1; j < length; j++)
                {
                    if (values[indices[j]] < values[indices[minIndex]])
                        minIndex = j;
                }
                if (minIndex != i)
                    (indices[i], indices[minIndex]) = (indices[minIndex], indices[i]);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CalculateCentroidOptimized(Span<T> simplex, Span<int> indices, Span<T> centroid, int excludeIndex, int n)
    {
        centroid.Clear();
        
        // Optimized centroid calculation with potential for vectorization
        if (n <= 6)
        {
            // Unrolled loop for small dimensions
            for (int i = 0; i <= n; i++)
            {
                if (indices[i] == excludeIndex) continue;
                
                var vertex = simplex.Slice(indices[i] * n, n);
                for (int j = 0; j < n; j++)
                    centroid[j] += vertex[j];
            }
        }
        else
        {
            // Vectorizable loop for larger dimensions
            for (int i = 0; i <= n; i++)
            {
                if (indices[i] == excludeIndex) continue;
                
                var vertex = simplex.Slice(indices[i] * n, n);
                for (int j = 0; j < n; j++)
                    centroid[j] += vertex[j];
            }
        }
        
        T divisor = T.CreateChecked(n);
        
        // Potential for SIMD vectorization
        for (int i = 0; i < n; i++)
            centroid[i] /= divisor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReflectOptimized(ReadOnlySpan<T> worst, ReadOnlySpan<T> centroid, Span<T> reflected)
    {
        // Vectorizable operation: reflected = centroid + alpha * (centroid - worst)
        for (int i = 0; i < reflected.Length; i++)
            reflected[i] = centroid[i] + Alpha * (centroid[i] - worst[i]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ExpandOptimized(ReadOnlySpan<T> centroid, ReadOnlySpan<T> reflected, Span<T> expanded)
    {
        // Vectorizable operation: expanded = centroid + gamma * (reflected - centroid)
        for (int i = 0; i < expanded.Length; i++)
            expanded[i] = centroid[i] + Gamma * (reflected[i] - centroid[i]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ContractOptimized(ReadOnlySpan<T> centroid, ReadOnlySpan<T> point, Span<T> contracted)
    {
        // Vectorizable operation: contracted = centroid + rho * (point - centroid)
        for (int i = 0; i < contracted.Length; i++)
            contracted[i] = centroid[i] + Rho * (point[i] - centroid[i]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ShrinkSimplexOptimized(Span<T> simplex, Span<int> indices, int bestIndex, int n)
    {
        var bestVertex = simplex.Slice(bestIndex * n, n);
        
        // Vectorizable shrinkage operation
        for (int i = 1; i <= n; i++)
        {
            var vertex = simplex.Slice(indices[i] * n, n);
            for (int j = 0; j < n; j++)
                vertex[j] = bestVertex[j] + Sigma * (vertex[j] - bestVertex[j]);
        }
    }
}