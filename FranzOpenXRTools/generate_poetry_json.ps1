$poetryFolder = "d:\UnityProjects\FranzOpenXRTools\FranzOpenXRTools\Assets\StreamingAssets\poetry"
$outputFile = "$poetryFolder\poetry_tangsong.json"

$csvFiles = Get-ChildItem -Path $poetryFolder -Filter "*.csv" | Where-Object { $_.Name -match '唐|宋' }

Write-Host "找到以下CSV文件:"
$csvFiles | ForEach-Object { Write-Host $_.Name }

$jsonArray = New-Object System.Collections.Generic.List[PSObject]
$totalCount = 0

foreach ($file in $csvFiles) {
    Write-Host "正在处理: $($file.Name)"
    $lines = Get-Content $file.FullName -Encoding UTF8
    
    for ($i = 1; $i -lt $lines.Length; $i++) {
        $line = $lines[$i]
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }
        
        $parts = $line -split '","'
        if ($parts.Length -ge 4) {
            $title = $parts[0].Trim('"')
            $dynasty = $parts[1].Trim('"')
            $author = $parts[2].Trim('"')
            $content = $parts[3].Trim('"')
            
            if ($dynasty -eq '唐' -or $dynasty -eq '宋') {
                $jsonArray.Add([PSCustomObject]@{
                    title = $title
                    dynasty = $dynasty
                    author = $author
                    content = $content
                })
                $totalCount++
            }
        }
    }
    
    Write-Host "  已提取 $totalCount 条"
}

Write-Host "正在生成JSON文件..."
$jsonArray | ConvertTo-Json -Compress | Out-File $outputFile -Encoding UTF8

Write-Host "完成！共生成 $totalCount 条诗词数据"
Write-Host "输出文件: $outputFile"