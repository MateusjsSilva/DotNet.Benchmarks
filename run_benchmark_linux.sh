#!/bin/bash
set -e

echo "🚀 Iniciando Setup do Benchmark .NET 11 no Linux..."

# 1. Instalar dependências básicas
sudo apt-get update && sudo apt-get install -y wget curl libicu-dev build-essential

# 2. Instalar .NET 11 Preview SDK
echo "⬇️ Baixando script de instalação do .NET..."
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh

echo "📦 Instalando SDK 11.0 Preview..."
./dotnet-install.sh --channel 11.0 --quality preview

# 3. Configurar variáveis de ambiente
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$DOTNET_ROOT

echo "✅ .NET SDK Instalado:"
dotnet --version

# 4. Compilar o projeto
echo "🔨 Compilando Benchmark..."
cd src/DotNet.Benchmarks.Compression
dotnet build -c Release

# 5. Rodar o Benchmark
echo "🔥 Executando Benchmark (Isso pode levar alguns minutos)..."
dotnet run -c Release -- --job short # Smoke test primeiro, depois full run se passar
