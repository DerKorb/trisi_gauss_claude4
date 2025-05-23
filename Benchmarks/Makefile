# Makefile for NLopt benchmark comparison

CXX = g++
CXXFLAGS = -std=c++17 -O3 -march=native -DNDEBUG
LIBS = -lnlopt -lm

# Default target
all: nlopt_benchmark

# Build NLopt benchmark
nlopt_benchmark: RealNLoptComparison.cpp
	$(CXX) $(CXXFLAGS) -o nlopt_benchmark RealNLoptComparison.cpp $(LIBS)

# Run comparison (requires NLopt to be installed)
run_comparison: nlopt_benchmark
	@echo "Running NLopt benchmarks..."
	./nlopt_benchmark
	@echo ""
	@echo "Running C# benchmarks..."
	cd .. && dotnet run perf > Benchmarks/csharp_results.txt
	@echo ""
	@echo "Generating comparison analysis..."
	python3 analyze_comparison.py

# Check if NLopt is available
check_nlopt:
	@echo "Checking for NLopt installation..."
	@pkg-config --exists nlopt && echo "✓ NLopt found" || echo "✗ NLopt not found - install with: sudo pacman -S nlopt"
	@pkg-config --libs nlopt 2>/dev/null || echo "Install nlopt development package"

# Install NLopt (requires sudo)
install_nlopt:
	@echo "Installing NLopt..."
	sudo pacman -S nlopt --noconfirm

# Clean build artifacts
clean:
	rm -f nlopt_benchmark nlopt_benchmark_results.csv csharp_results.txt

# Show help
help:
	@echo "Available targets:"
	@echo "  check_nlopt     - Check if NLopt is installed"
	@echo "  install_nlopt   - Install NLopt (requires sudo)"
	@echo "  nlopt_benchmark - Build the benchmark executable"
	@echo "  run_comparison  - Run full comparison between NLopt and C#"
	@echo "  clean          - Remove build artifacts"
	@echo "  help           - Show this help"

.PHONY: all run_comparison check_nlopt install_nlopt clean help