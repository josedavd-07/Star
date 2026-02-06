# PowerShell script to install Star Language on Windows

Write-Host "🚀 Installing Star Language..." -ForegroundColor Blue

# 1. Define paths
$InstallDir = "$HOME\.star-language"
$BinDir = "$InstallDir\bin"
$FontDir = "$InstallDir\fonts"

# 2. Create directories
New-Item -ItemType Directory -Force -Path $BinDir
New-Item -ItemType Directory -Force -Path $FontDir

# 3. Copy files (assuming running from repo root)
Copy-Item "star" -Destination "$BinDir\star.ps1"
Copy-Item "Fonts\*.otf" -Destination $FontDir

# 4. Add to PATH permanently
$UserPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($UserPath -notlike "*$BinDir*") {
    $NewPath = "$UserPath;$BinDir"
    [Environment]::SetEnvironmentVariable("Path", $NewPath, "User")
    Write-Host "[+] Added $BinDir to PATH." -ForegroundColor Green
}

# 5. Instructions for fonts
Write-Host "[!] Please install the fonts located in $FontDir manually for the best experience." -ForegroundColor Yellow

Write-Host "`n✨ Star Language is now installed! Please restart your terminal." -ForegroundColor Blue
Write-Host "Try running: star --version" -ForegroundColor Green
