#!/bin/bash
set -e

BENCHMARK_DIR="$(cd "$(dirname "$0")" && pwd)"
LOG_FILE="$BENCHMARK_DIR/benchmark_output.log"
RESULTS_DIR="$BENCHMARK_DIR/results"

echo "🚀 Starting .NET 11 Benchmark Setup on Linux..."

# 1. Install system dependencies + screen (for background session)
echo "📦 Installing system dependencies..."
apt-get update && apt-get install -y wget curl libicu-dev build-essential screen

# 2. Install .NET 11 Preview SDK (if not installed)
if command -v dotnet &> /dev/null && dotnet --list-sdks | grep -q "11.0"; then
    echo "✅ .NET 11 SDK is already installed"
else
    echo "⬇️ Downloading .NET install script..."
    wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
    chmod +x /tmp/dotnet-install.sh

    echo "📦 Installing SDK 11.0 Preview..."
    /tmp/dotnet-install.sh --channel 11.0 --quality preview
fi

# 3. Configure environment variables
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$DOTNET_ROOT

echo "✅ .NET SDK Installed:"
dotnet --version

# 4. Restore and build the Compression project
echo "🔨 Building Compression Benchmark..."
cd "$BENCHMARK_DIR/src/DotNet.Benchmarks.Compression"
dotnet restore
dotnet build -c Release --no-restore

# 5. Create results directory
mkdir -p "$RESULTS_DIR"

# 6. Run Benchmark in background
echo ""
echo "══════════════════════════════════════════════════════════════"
echo "  🔥 Starting Benchmark in BACKGROUND"
echo "  📋 Log: $LOG_FILE"
echo "  📁 Results: $RESULTS_DIR"
echo ""
echo "  To follow live progress:"
echo "    tail -f $LOG_FILE"
echo ""
echo "  Or reattach to screen session:"
echo "    screen -r benchmark"
echo ""
echo "  To detach from screen without killing:"
echo "    Ctrl+A, then D"
echo "══════════════════════════════════════════════════════════════"
echo ""

# Kill previous session if exists
screen -S benchmark -X quit 2>/dev/null || true

# Start new screen session in background running the benchmark
screen -dmS benchmark bash -c "
    export DOTNET_ROOT=\$HOME/.dotnet
    export PATH=\$PATH:\$DOTNET_ROOT
    cd $BENCHMARK_DIR/src/DotNet.Benchmarks.Compression

    echo '🔥 Benchmark started at: $(date)' | tee $LOG_FILE
    echo '' | tee -a $LOG_FILE

    dotnet run -c Release --no-build -- \
        --artifacts $RESULTS_DIR \
        2>&1 | tee -a $LOG_FILE

    echo '' | tee -a $LOG_FILE
    echo '✅ Benchmark finished at: $(date)' | tee -a $LOG_FILE
    echo '📁 Results saved to: $RESULTS_DIR' | tee -a $LOG_FILE
"

echo "✅ Benchmark running in background! (session: benchmark)"
echo "   Use 'screen -r benchmark' to view progress"
echo "   Use 'tail -f $LOG_FILE' to view log"
