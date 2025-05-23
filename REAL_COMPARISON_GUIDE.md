# Real NLopt vs C# Performance Comparison Guide

## Quick Start (With Sudo Access)

If you have sudo access, you can run the complete real comparison:

```bash
# 1. Install NLopt
sudo pacman -S nlopt

# 2. Build and run the comparison
cd Benchmarks
make run_comparison

# 3. View results
cat REAL_PERFORMANCE_COMPARISON.md
```

## Manual Setup (Without Sudo)

If you don't have sudo access, follow these steps:

### Step 1: Install NLopt (Ask System Administrator)
```bash
# System administrator needs to run:
sudo pacman -S nlopt
# or equivalent for your distribution
```

### Step 2: Build NLopt Benchmark
```bash
cd /home/ksollner/projects/trisi2/Benchmarks

# Check if NLopt is available
make check_nlopt

# If available, build the benchmark
make nlopt_benchmark
```

### Step 3: Run Benchmarks
```bash
# Run NLopt benchmarks
./nlopt_benchmark

# Run C# benchmarks
cd ..
dotnet run perf > Benchmarks/csharp_results.txt
```

### Step 4: Analyze Results
```bash
cd Benchmarks
python3 analyze_comparison.py
```

## What the Comparison Tests

### Mathematical Functions
- **Rosenbrock**: Classic optimization test (banana function)
- **Sphere**: Simple quadratic function in multiple dimensions
- **Booth**: Two-dimensional test with known minimum
- **Beale**: Non-convex function with global minimum
- **Himmelblau**: Multi-modal function
- **Powell**: Four-dimensional separable function

### Real-World Application
- **Double Gaussian Fitting**: Curve fitting with 500 data points
- **Scalability Tests**: 2D, 5D, 10D, and 20D sphere functions

### Metrics Compared
1. **Execution Time**: Wall-clock time in milliseconds
2. **Function Evaluations**: Number of objective function calls
3. **Convergence Quality**: Parameter error vs analytical solutions
4. **Scalability**: Performance vs problem dimension

## Expected Results

Based on typical C++ vs C# performance characteristics, we expect:

### Performance Ratios (NLopt time / C# time)
- **Best Case**: 0.8-1.2x (C# competitive or faster)
- **Typical Case**: 0.4-0.8x (NLopt 25-150% faster)
- **Worst Case**: 0.1-0.4x (NLopt 2.5-10x faster)

### Function Evaluation Efficiency
- **C# Advantage**: Typically 10-20% fewer function evaluations
- **Reason**: Modern algorithm implementation with optimizations

### Accuracy
- **Equivalent**: Both should achieve similar numerical precision
- **Tolerance**: Same convergence criteria (1e-8 for both)

## Interpreting Results

### Performance Interpretation
```
Ratio > 0.8: C# is competitive with NLopt
Ratio 0.5-0.8: Reasonable performance gap
Ratio < 0.5: Significant performance difference
```

### When C# Implementation is Preferred
- **Development Speed**: Faster to implement and debug
- **Type Safety**: Compile-time error checking
- **Ecosystem**: Easy integration with .NET applications
- **Maintenance**: Easier to modify and extend

### When NLopt is Preferred
- **Raw Performance**: Critical path optimization
- **Legacy Systems**: Existing C/Fortran codebases
- **Embedded**: Resource-constrained environments

## File Structure After Running

```
Benchmarks/
├── nlopt_benchmark                 # Compiled NLopt benchmark
├── nlopt_benchmark_results.csv    # NLopt results data
├── csharp_results.txt             # C# benchmark output
├── REAL_PERFORMANCE_COMPARISON.md # Analysis report
├── performance_comparison.png     # Visual comparison chart
└── analyze_comparison.py          # Analysis script
```

## Troubleshooting

### NLopt Not Found
```bash
# Check if installed
pkg-config --libs nlopt

# Install (requires admin)
sudo pacman -S nlopt       # Arch/Manjaro
sudo apt install nlopt-dev # Ubuntu/Debian
sudo yum install nlopt-devel # RHEL/CentOS
```

### Compilation Errors
```bash
# Check compiler
g++ --version

# Check NLopt headers
find /usr -name "nlopt.hpp" 2>/dev/null

# Manual compilation
g++ -std=c++17 -O3 -o nlopt_benchmark RealNLoptComparison.cpp -lnlopt
```

### Python Dependencies
```bash
# Install matplotlib for charts
pip install matplotlib numpy

# Or use conda
conda install matplotlib numpy
```

## Alternative: Docker-Based Comparison

If you cannot install NLopt directly, you can use Docker:

```dockerfile
FROM archlinux:latest
RUN pacman -Syu --noconfirm
RUN pacman -S --noconfirm nlopt gcc python python-matplotlib
COPY . /app
WORKDIR /app/Benchmarks
RUN make nlopt_benchmark
CMD ["make", "run_comparison"]
```

## Validation of Results

### Sanity Checks
1. **Convergence**: Both implementations should converge to same solutions
2. **Monotonicity**: Larger problems should take more time
3. **Consistency**: Multiple runs should give similar results

### Expected Performance Patterns
1. **Simple Functions**: C# may be competitive
2. **Complex Functions**: NLopt likely faster
3. **High Dimensions**: Performance gap may increase

---

This guide provides everything needed to run a legitimate, scientific comparison between NLopt and our C# implementation on identical hardware with identical test conditions.