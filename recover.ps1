$in = Get-Content "scratch_normalstage.txt"
$out = [System.Collections.Generic.List[string]]::new()
$capturing = $false
foreach ($line in $in) {
    if ($line -match "The following code has been modified") {
        $capturing = $true
        continue
    }
    if ($capturing -and ($line -match "The above content shows the entire" -or $line -match "The above content does NOT show")) {
        $capturing = $false
        break
    }
    if ($capturing) {
        $cleanLine = $line
        if ($cleanLine.StartsWith(">")) {
            $cleanLine = $cleanLine.Substring(1).Trim()
        }
        if ($cleanLine -match "^\s*(\d+):\s(.*)$") {
            $out.Add($Matches[2])
        }
    }
}
[IO.File]::WriteAllLines("Assets/Script/Stage/NormalStage.cs", $out)
Write-Host "Recovered $($out.Count) lines."
