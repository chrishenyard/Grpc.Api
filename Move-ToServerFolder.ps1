#!/usr/bin/env pwsh
# Move all projects to server folder and update .sln and scripts

param(
    [string]$SolutionFile = (Get-ChildItem -Filter "*.slnx" | Select-Object -First 1).Name
)

if (-not $SolutionFile -or -not (Test-Path $SolutionFile)) {
    Write-Error "Solution file not found. Please specify with -SolutionFile parameter."
    exit 1
}

Write-Host "Using solution file: $SolutionFile" -ForegroundColor Cyan

# Create server folder
$serverFolder = "server"
if (-not (Test-Path $serverFolder)) {
    New-Item -ItemType Directory -Path $serverFolder | Out-Null
    Write-Host "Created folder: $serverFolder" -ForegroundColor Green
}

# Find all project folders (directories containing .csproj or .sqlproj files)
$projectFolders = Get-ChildItem -Directory -Exclude $serverFolder, ".git", ".vs", "bin", "obj" | 
    Where-Object { 
        (Get-ChildItem -Path $_.FullName -Filter "*.csproj" -ErrorAction SilentlyContinue) -or 
        (Get-ChildItem -Path $_.FullName -Filter "*.sqlproj" -ErrorAction SilentlyContinue)
    }

if ($projectFolders.Count -eq 0) {
    Write-Warning "No project folders found to move."
    exit 0
}

Write-Host "Found $($projectFolders.Count) project folder(s) to move:" -ForegroundColor Cyan
$projectFolders | ForEach-Object { Write-Host "  - $($_.Name)" }

# Move each project folder using git mv to preserve history
foreach ($folder in $projectFolders) {
    $sourcePath = $folder.Name
    $targetPath = Join-Path $serverFolder $folder.Name
    
    Write-Host "Moving $sourcePath -> $targetPath" -ForegroundColor Yellow
    
    # Use git mv if in a git repo, otherwise regular move
    if (Test-Path ".git") {
        git mv $sourcePath $targetPath
    } else {
        Move-Item -Path $sourcePath -Destination $targetPath
    }
}

# Update .sln file
Write-Host "`nUpdating solution file..." -ForegroundColor Cyan
$slnContent = Get-Content $SolutionFile -Raw

# Update Project(...) paths
$slnContent = $slnContent -replace '(Project\([^\)]+\)\s*=\s*"[^"]+",\s*")([^\\]+\\[^"]+\.(?:csproj|sqlproj))', "`$1$serverFolder\`$2"

# Save updated .sln
Set-Content -Path $SolutionFile -Value $slnContent -NoNewline
Write-Host "Solution file updated." -ForegroundColor Green

# Update scripts
Write-Host "`nUpdating PowerShell scripts..." -ForegroundColor Cyan

# Update Publish-Database.ps1
if (Test-Path "Publish-Database.ps1") {
    $content = Get-Content "Publish-Database.ps1" -Raw
    $content = $content -replace '(\\|\/)Grpc\.Service(\\|\/)Grpc\.Service\.csproj', "\$serverFolder\Grpc.Service\Grpc.Service.csproj"
    $content = $content -replace '(\\|\/)Grpc\.Database(\\|\/)Grpc\.Database\.csproj', "\$serverFolder\Grpc.Database\Grpc.Database.csproj"
    $content = $content -replace '(\\|\/)Grpc\.Database(\\|\/)bin', "\$serverFolder\Grpc.Database\bin"
    Set-Content -Path "Publish-Database.ps1" -Value $content -NoNewline
    Write-Host "  Updated Publish-Database.ps1" -ForegroundColor Green
}

# Update Build-Database.ps1
if (Test-Path "Build-Database.ps1") {
    $content = Get-Content "Build-Database.ps1" -Raw
    $content = $content -replace '(\\|\/)Grpc\.Database(\\|\/)Grpc\.Database\.csproj', "\$serverFolder\Grpc.Database\Grpc.Database.csproj"
    $content = $content -replace '(\\|\/)Grpc\.Database(\\|\/)bin', "\$serverFolder\Grpc.Database\bin"
    Set-Content -Path "Build-Database.ps1" -Value $content -NoNewline
    Write-Host "  Updated Build-Database.ps1" -ForegroundColor Green
}

# Update Docker-Compose-Up.ps1 (no path changes needed, but list it)
if (Test-Path "Docker-Compose-Up.ps1") {
    Write-Host "  Docker-Compose-Up.ps1 (no changes needed)" -ForegroundColor Gray
}

# Update Dockerfile
if (Test-Path "$serverFolder/Grpc.Service/Dockerfile") {
    $content = Get-Content "$serverFolder/Grpc.Service/Dockerfile" -Raw
    $content = $content -replace 'COPY \["Directory\.Packages\.props", "\."', 'COPY ["server/Directory.Packages.props", "."'
    $content = $content -replace 'COPY \["(Grpc\.[^/]+)/([^"]+)", "([^"]+)/"', 'COPY ["server/$1/$2", "$3/"'
    $content = $content -replace 'WORKDIR "/src/Grpc\.Service"', 'WORKDIR "/src/server/Grpc.Service"'
    Set-Content -Path "$serverFolder/Grpc.Service/Dockerfile" -Value $content -NoNewline
    Write-Host "  Updated Dockerfile" -ForegroundColor Green
}

# Move Directory.Packages.props if it exists
if (Test-Path "Directory.Packages.props") {
    if (Test-Path ".git") {
        git mv Directory.Packages.props $serverFolder/
    } else {
        Move-Item Directory.Packages.props $serverFolder/
    }
    Write-Host "  Moved Directory.Packages.props to $serverFolder" -ForegroundColor Green
}

Write-Host "`nAll done! Next steps:" -ForegroundColor Cyan
Write-Host "1. Run: dotnet restore" -ForegroundColor Yellow
Write-Host "2. Run: dotnet build" -ForegroundColor Yellow
Write-Host "3. Verify tests run correctly" -ForegroundColor Yellow
Write-Host "4. Review and commit changes" -ForegroundColor Yellow
