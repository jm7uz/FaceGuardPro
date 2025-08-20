Write-Host "OpenCV Haar Cascade modellarini yuklab olamiz..." -ForegroundColor Green

# Models papkasini yaratish
$modelsPath = "src/FaceGuardPro.AI/Models"
if (-not (Test-Path $modelsPath)) {
    New-Item -ItemType Directory -Path $modelsPath -Force
    Write-Host "Models papkasi yaratildi: $modelsPath" -ForegroundColor Yellow
}

# Haar Cascade fayllarini yuklab olish
$cascadeFiles = @{
    "haarcascade_frontalface_alt2.xml" = "https://raw.githubusercontent.com/opencv/opencv/master/data/haarcascades/haarcascade_frontalface_alt2.xml"
    "haarcascade_frontalface_default.xml" = "https://raw.githubusercontent.com/opencv/opencv/master/data/haarcascades/haarcascade_frontalface_default.xml"
    "haarcascade_eye.xml" = "https://raw.githubusercontent.com/opencv/opencv/master/data/haarcascades/haarcascade_eye.xml"
    "haarcascade_profileface.xml" = "https://raw.githubusercontent.com/opencv/opencv/master/data/haarcascades/haarcascade_profileface.xml"
}

foreach ($fileName in $cascadeFiles.Keys) {
    $filePath = Join-Path $modelsPath $fileName
    $url = $cascadeFiles[$fileName]
    
    Write-Host "Yuklab olinmoqda: $fileName..." -ForegroundColor Cyan
    
    try {
        Invoke-WebRequest -Uri $url -OutFile $filePath
        Write-Host "✅ Muvaffaqiyatli yuklab olindi: $fileName" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Xatolik: $fileName yuklab olinmadi - $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n🎉 Barcha modellar yuklab olindi!" -ForegroundColor Green
Write-Host "📁 Models joylashuvi: $modelsPath" -ForegroundColor Yellow

# Models fayllarini tekshirish
Write-Host "`n📋 Yuklab olingan fayllar:" -ForegroundColor Cyan
Get-ChildItem -Path $modelsPath -Name | ForEach-Object {
    $size = (Get-Item (Join-Path $modelsPath $_)).Length
    Write-Host "  - $_ ($([math]::Round($size/1KB, 2)) KB)" -ForegroundColor White
}