# Usage Guide - High-Performance Nelder-Mead Optimization Library

## Quick Start

### 1. Basic Function Minimization

```csharp
using Optimization.Core.Algorithms;

// Define your objective function
static double MyFunction(Span<double> x) => 
    Math.Pow(x[0] - 2.0, 2) + Math.Pow(x[1] - 3.0, 2);

// Set initial guess
var initialGuess = new double[] { 0.0, 0.0 };

// Minimize
var result = NelderMead<double>.Minimize(MyFunction, initialGuess);

// Check results
Console.WriteLine($"Converged: {result.Converged}");
Console.WriteLine($"Optimal point: ({result.OptimalParameters.Span[0]:F6}, {result.OptimalParameters.Span[1]:F6})");
Console.WriteLine($"Optimal value: {result.OptimalValue:E6}");
```

### 2. Double Gaussian Curve Fitting

```csharp
using Optimization.Core.Models;

// Your experimental data
double[] xData = { -3.0, -2.5, -2.0, -1.5, -1.0, -0.5, 0.0, 0.5, 1.0, 1.5, 2.0, 2.5, 3.0 };
double[] yData = { 0.05, 0.15, 0.45, 0.80, 1.20, 0.90, 0.60, 0.85, 1.15, 0.75, 0.35, 0.15, 0.05 };

// Automatic initial guess generation
var initialGuess = DoubleGaussian.GenerateInitialGuess<double>(xData, yData);

// Fit the double Gaussian model
var result = DoubleGaussian.Fit(xData, yData, initialGuess);

// Extract fitted parameters: [A1, μ1, σ1, A2, μ2, σ2]
var parameters = result.OptimalParameters.Span;
Console.WriteLine($"A1={parameters[0]:F3}, μ1={parameters[1]:F3}, σ1={parameters[2]:F3}");
Console.WriteLine($"A2={parameters[3]:F3}, μ2={parameters[4]:F3}, σ2={parameters[5]:F3}");
```

## Advanced Configuration

### 3. Custom Optimization Options

```csharp
var options = new NelderMeadOptions<double>
{
    FunctionTolerance = 1e-10,     // Stop when function values converge
    ParameterTolerance = 1e-10,    // Stop when parameters converge  
    MaxIterations = 5000,          // Maximum iterations before giving up
    InitialSimplexSize = 0.1,      // Size of initial simplex relative to parameters
    
    // Bounds constraints (optional)
    LowerBounds = new double[] { 0.0, -10.0, 0.01 },  // Minimum values
    UpperBounds = new double[] { 100.0, 10.0, 5.0 }   // Maximum values
};

var result = NelderMead<double>.Minimize(objective, initialGuess, options);
```

### 4. Bounded Optimization

```csharp
// Objective function
static double BoundedObjective(Span<double> x) => x[0] * x[0] + x[1] * x[1];

var initialGuess = new double[] { 2.0, 2.0 };
var options = new NelderMeadOptions<double>
{
    LowerBounds = new double[] { 1.0, 1.0 },  // x, y >= 1
    UpperBounds = new double[] { 5.0, 5.0 },  // x, y <= 5
    FunctionTolerance = 1e-8
};

var result = NelderMead<double>.Minimize(BoundedObjective, initialGuess, options);
// Result will be approximately (1.0, 1.0) - the constrained minimum
```

## Working with Float Precision

### 5. Float Type Support

```csharp
// Define function for float precision
static float FloatObjective(Span<float> x) => 
    (x[0] - 2.0f) * (x[0] - 2.0f) + (x[1] - 3.0f) * (x[1] - 3.0f);

var initialGuessFloat = new float[] { 0.0f, 0.0f };
var optionsFloat = new NelderMeadOptions<float>
{
    FunctionTolerance = 1e-6f,  // Adjust tolerance for float precision
    MaxIterations = 1000
};

var result = NelderMead<float>.Minimize(FloatObjective, initialGuessFloat, optionsFloat);
```

## Custom Objective Functions

### 6. Weighted Least Squares

```csharp
using Optimization.Core.Models;

double[] xData = { /* your x values */ };
double[] yData = { /* your y values */ };  
double[] weights = { /* your weights */ };

// Create weighted objective function
Func<Span<double>, double> weightedObjective = parameters =>
    ObjectiveFunctions.WeightedSumSquaredResiduals(parameters, xData, yData, weights);

var result = NelderMead<double>.Minimize(weightedObjective, initialGuess);
```

### 7. Custom Models

```csharp
// Example: Single Gaussian + Linear Background
static double GaussianPlusLinear(Span<double> parameters, double x)
{
    // Parameters: [A, μ, σ, slope, intercept]
    double gaussian = parameters[0] * Math.Exp(-0.5 * Math.Pow((x - parameters[1]) / parameters[2], 2));
    double linear = parameters[3] * x + parameters[4];
    return gaussian + linear;
}

static double CustomObjective(Span<double> parameters)
{
    double sumSquaredError = 0;
    for (int i = 0; i < xData.Length; i++)
    {
        double predicted = GaussianPlusLinear(parameters, xData[i]);
        double residual = yData[i] - predicted;
        sumSquaredError += residual * residual;
    }
    return sumSquaredError;
}
```

## Best Practices

### 8. Choosing Good Initial Guesses

```csharp
// For double Gaussian fitting, use the built-in generator
var autoGuess = DoubleGaussian.GenerateInitialGuess<double>(xData, yData);

// Or provide domain knowledge
var manualGuess = new double[] 
{
    maxYValue * 0.6,  // A1 - amplitude of first peak
    xAtFirstPeak,     // μ1 - location of first peak  
    estimatedWidth1,  // σ1 - width of first peak
    maxYValue * 0.4,  // A2 - amplitude of second peak
    xAtSecondPeak,    // μ2 - location of second peak
    estimatedWidth2   // σ2 - width of second peak
};
```

### 9. Parameter Scaling

```csharp
// Scale parameters to similar magnitudes for better convergence
var scaledGuess = new double[]
{
    amplitude / maxY,      // Scale amplitudes to ~1
    (position - minX) / (maxX - minX),  // Scale positions to 0-1
    width / (maxX - minX), // Scale widths to reasonable range
    // ... repeat for second Gaussian
};

// Remember to scale bounds accordingly
var scaledLowerBounds = new double[] { 0.0, 0.0, 0.001, 0.0, 0.0, 0.001 };
var scaledUpperBounds = new double[] { 2.0, 1.0, 0.5, 2.0, 1.0, 0.5 };
```

### 10. Convergence Troubleshooting

```csharp
var diagnosticOptions = new NelderMeadOptions<double>
{
    FunctionTolerance = 1e-8,
    MaxIterations = 5000,
    InitialSimplexSize = 0.1,  // Try larger values if stuck in local minimum
};

var result = NelderMead<double>.Minimize(objective, initialGuess, diagnosticOptions);

// Check convergence status
if (!result.Converged)
{
    Console.WriteLine($"Failed to converge after {result.Iterations} iterations");
    Console.WriteLine($"Final function value: {result.OptimalValue}");
    
    // Try different initial guess or adjust options
}
```

## Error Handling

### 11. Robust Error Handling

```csharp
try
{
    var result = NelderMead<double>.Minimize(objective, initialGuess, options);
    
    if (result.Converged)
    {
        // Use result.OptimalParameters
        Console.WriteLine($"Optimization successful: {result.Message}");
    }
    else
    {
        // Handle non-convergence
        Console.WriteLine($"Optimization did not converge: {result.Message}");
        // Possibly retry with different settings
    }
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid parameters: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Optimization error: {ex.Message}");
}
```

## Performance Tips

### 12. Optimization Performance

```csharp
// 1. Use appropriate data types
// Use float for less precision requirements, faster computation
var resultFloat = NelderMead<float>.Minimize(objectiveFloat, guessFloat);

// 2. Minimize function evaluation cost
static double EfficientObjective(Span<double> parameters)
{
    // Cache expensive computations
    // Avoid unnecessary allocations
    // Use efficient math operations
    return result;
}

// 3. Set reasonable tolerances
var efficientOptions = new NelderMeadOptions<double>
{
    FunctionTolerance = 1e-6,    // Don't over-optimize
    MaxIterations = 1000,        // Set reasonable limits
};
```

### 13. Memory Efficiency

```csharp
// Reuse arrays when possible
var workspace = new double[6];  // For parameter storage

// Use spans for efficient data passing  
ReadOnlySpan<double> xDataSpan = xData;
ReadOnlySpan<double> yDataSpan = yData;

// Create objective function that uses spans efficiently
Func<Span<double>, double> efficientObjective = parameters =>
{
    // Efficient implementation using spans
    return ObjectiveFunctions.SumSquaredResiduals(parameters, xDataSpan, yDataSpan);
};
```

## Integration Examples

### 14. Integration with Data Processing

```csharp
// Example: Process multiple datasets
var datasets = LoadMultipleDatasets();
var results = new List<OptimizationResult<double>>();

foreach (var (xData, yData) in datasets)
{
    var initialGuess = DoubleGaussian.GenerateInitialGuess<double>(xData, yData);
    var result = DoubleGaussian.Fit(xData, yData, initialGuess);
    
    if (result.Converged)
    {
        results.Add(result);
        
        // Extract and store fitted parameters
        var parameters = result.OptimalParameters.Span;
        SaveResults(parameters.ToArray());
    }
}
```

### 15. Parallel Processing

```csharp
// Process multiple optimization problems in parallel
var problems = CreateOptimizationProblems();

var results = problems.AsParallel().Select(problem => 
{
    var (objective, initialGuess, options) = problem;
    return NelderMead<double>.Minimize(objective, initialGuess, options);
}).ToArray();

// Collect successful results
var convergedResults = results.Where(r => r.Converged).ToArray();
```

This comprehensive guide covers most common use cases and best practices for the high-performance Nelder-Mead optimization library. The library provides robust, efficient optimization capabilities suitable for a wide range of applications, particularly double Gaussian curve fitting in scientific and engineering contexts.