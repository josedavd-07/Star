#!/bin/bash
set -e

VERSION="1.0.0"
DIST_DIR="dist"

echo "🌟 Initiating Star Language Release Protocol v${VERSION}..."
mkdir -p ${DIST_DIR}

# 0. Compile Base Binary (Linux)
echo "🔨 Compiling Core Binary..."
dotnet publish StarCompiler/StarCompiler.csproj -c Release -r linux-x64 --self-contained -o ./publish
cp ./publish/StarCompiler ./star_binary
chmod +x ./star_binary

# 1. Build Linux (Debian & Tarball)
echo "🐧 Building for Linux..."
./scripts/build-deb.sh
mv *.deb ${DIST_DIR}/

./scripts/build-tarball.sh
mv *.tar.gz ${DIST_DIR}/

# 2. Build Windows
echo "🪟 Building for Windows (x64)..."
dotnet publish StarCompiler/StarCompiler.csproj -c Release -r win-x64 --self-contained -o ./bin/Release/win-x64
cd bin/Release/win-x64
zip -r ../../../${DIST_DIR}/star-language-v${VERSION}-win-x64.zip .
cd ../../..

# 3. Build macOS (Apple Silicon & Intel)
echo "🍎 Building for macOS (ARM64)..."
dotnet publish StarCompiler/StarCompiler.csproj -c Release -r osx-arm64 --self-contained -o ./bin/Release/osx-arm64
cd bin/Release/osx-arm64
zip -r ../../../${DIST_DIR}/star-language-v${VERSION}-osx-arm64.zip .
cd ../../..

echo "✅ All Compiler artifacts are in '${DIST_DIR}/'"
ls -lh ${DIST_DIR}
