Param(
    [string]$Root = (Split-Path -Parent $MyInvocation.MyCommand.Path)
)

$files = Get-ChildItem -Path $Root -Recurse -Filter *.cs -File |
    Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' -and $_.FullName -notmatch '\\.git\\' }

foreach ($f in $files) {
    try {
        $text = Get-Content -Raw -Encoding UTF8 -ErrorAction Stop -LiteralPath $f.FullName
    } catch {
        Write-Host ("Skipping {0} - cannot read: {1}" -f $f.FullName, $_) -ForegroundColor Yellow
        continue
    }

    $text = [regex]::Replace($text,'(?s)/\*.*?\*/','')
    $text = [regex]::Replace($text,'(?m)^\s*///.*$','')
    $text = [regex]::Replace($text,'(?m)//.*$','')

    try {
        Set-Content -LiteralPath $f.FullName -Value $text -Encoding UTF8 -ErrorAction Stop
        Write-Host ("Processed: {0}" -f $f.FullName)
    } catch {
        Write-Host ("Failed to write {0}: {1}" -f $f.FullName, $_) -ForegroundColor Red
    }
}

Write-Host ("Done. Processed {0} files." -f $files.Count)
Param(
    [string]$Root = (Split-Path -Parent $MyInvocation.MyCommand.Path)
)

$files = Get-ChildItem -Path $Root -Recurse -Filter *.cs -File |
    Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' -and $_.FullName -notmatch '\\.git\\' }

foreach ($f in $files) {
    try {
        $text = Get-Content -Raw -Encoding UTF8 -ErrorAction Stop $f.FullName
    } catch {
        Write-Host "Skipping $($f.FullName) — cannot read: $_" -ForegroundColor Yellow
        continue
    }

    $text = [regex]::Replace($text,'(?s)/\*.*?\*/','')
    $text = [regex]::Replace($text,'(?m)^\s*///.*$','')
    $text = [regex]::Replace($text,'(?m)//.*$','')

    try {
        Set-Content -Path $f.FullName -Value $text -Encoding UTF8 -ErrorAction Stop
        Write-Host "Processed: $($f.FullName)"
    } catch {
        Write-Host "Failed to write $($f.FullName): $_" -ForegroundColor Red
    }
}

Write-Host "Done. Processed $($files.Count) files."
