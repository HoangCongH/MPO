param(
    [string]$InputPath = 'C:\Intern\mpo_webproject\MPO-main\MPO\MPO_Web_Prj\MPO_Panasonic_Real_App_Showcase.pptx',
    [string]$OutputPath = 'C:\Intern\mpo_webproject\MPO-main\MPO\MPO_Web_Prj\MPO_Panasonic_Executive_Summary_10_Slides.pptx'
)

$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing

# Existing slide numbers to preserve, in presentation order:
# Cover, executive summary, business value, system overview, delta logic,
# database model, tech stack, real quick access, real dashboard, takeaways.
$keep=@(1,2,3,4,8,12,18,20,21,38)

# Build ten headers with the exact same background, colors, typography and spacing.
$headerBg='C:\Intern\mpo_webproject\MPO-main\MPO\MPO_Web_Prj\.codex-build\header_background.png'
$headerDir='C:\Intern\mpo_webproject\MPO-main\MPO\MPO_Web_Prj\.codex-build\headers_10'
New-Item -ItemType Directory -Force -Path $headerDir|Out-Null
$bg=[Drawing.Image]::FromFile($headerBg)
$font=New-Object Drawing.Font('Arial',15,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
$bold=New-Object Drawing.Font('Arial',15,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
$logoFont=New-Object Drawing.Font('Arial',24,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
$whiteBrush=New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(230,240,250))
$pureWhite=New-Object Drawing.SolidBrush([Drawing.Color]::White)
$cyanBrush=New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(0,230,240))
$grayBrush=New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(148,163,184))
$blueBrush=New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(0,80,136))
$cyanPen=New-Object Drawing.Pen([Drawing.Color]::FromArgb(0,230,240),2)
$labels=@('MPO','PIPELINE','DATABASE','REPORTS','DEMO')
$xs=@(330,455,610,775,920)
$activeSections=@(0,0,0,0,1,2,3,3,3,4)

for($i=1;$i -le 10;$i++){
    $active=$activeSections[$i-1]
    $bmp=New-Object Drawing.Bitmap 1440,110
    $g=[Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.TextRenderingHint=[Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.DrawImage($bg,0,0,(New-Object Drawing.Rectangle 0,0,1440,110),[Drawing.GraphicsUnit]::Pixel)
    $g.FillRectangle($blueBrush,52,30,194,70)
    $ls=$g.MeasureString('Panasonic',$logoFont)
    $g.DrawString('Panasonic',$logoFont,$pureWhite,52+(194-$ls.Width)/2,30+(70-$ls.Height)/2)
    for($j=0;$j -lt 5;$j++){
        $f=if($j -eq $active){$bold}else{$font}
        $br=if($j -eq $active){$cyanBrush}else{$whiteBrush}
        $size=$g.MeasureString($labels[$j],$f)
        $x=$xs[$j]+(125-$size.Width)/2
        $g.DrawString($labels[$j],$f,$br,$x,51)
        if($j -eq $active){$g.DrawLine($cyanPen,$xs[$j]+22,83,$xs[$j]+103,83)}
    }
    $n=('{0:D2}' -f $i);$ns=$g.MeasureString($n,$bold)
    $g.DrawString($n,$bold,$grayBrush,1370-$ns.Width,48)
    $g.Dispose()
    $bmp.Save((Join-Path $headerDir ('header_{0:D2}.png' -f $i)),[Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}
$font.Dispose();$bold.Dispose();$logoFont.Dispose();$whiteBrush.Dispose();$pureWhite.Dispose();$cyanBrush.Dispose();$grayBrush.Dispose();$blueBrush.Dispose();$cyanPen.Dispose();$bg.Dispose()

$ppt=New-Object -ComObject PowerPoint.Application
$ppt.Visible=-1
$source=$ppt.Presentations.Open($InputPath,$true,$false,$false)
if(Test-Path -LiteralPath $OutputPath){Remove-Item -LiteralPath $OutputPath -Force}
$source.SaveAs($OutputPath,24);$source.Close()
$deck=$ppt.Presentations.Open($OutputPath,$false,$false,$false)

# Delete unselected slides from the end so original indices remain valid.
for($i=$deck.Slides.Count;$i -ge 1;$i--){
    if($keep -notcontains $i){$deck.Slides.Item($i).Delete()}
}

for($i=1;$i -le $deck.Slides.Count;$i++){
    $slide=$deck.Slides.Item($i)

    # Remove the old flattened header and add the same header with the new number.
    for($j=$slide.Shapes.Count;$j -ge 1;$j--){
        $shape=$slide.Shapes.Item($j)
        if($shape.Top -ge 0 -and $shape.Top -lt 110 -and $shape.Width -ge 1400 -and $shape.Height -le 120){
            $shape.Delete()
        } elseif($shape.HasTextFrame -eq -1 -and $shape.TextFrame.HasText -eq -1) {
            $text=$shape.TextFrame.TextRange.Text
            if($text -match '^PANASONIC • \d{2}$'){$shape.TextFrame.TextRange.Text=('PANASONIC • {0:D2}' -f $i)}
        }
    }
    $header=Join-Path $headerDir ('header_{0:D2}.png' -f $i)
    $pic=$slide.Shapes.AddPicture($header,0,-1,0,0,1440,110);$pic.ZOrder(0)

    # Remove all presenter notes.
    try{$slide.NotesPage.Shapes.Placeholders.Item(2).TextFrame.TextRange.Text=''}catch{}
}

$deck.Save();$deck.Close();$ppt.Quit()
[System.Runtime.InteropServices.Marshal]::ReleaseComObject($ppt)|Out-Null
Write-Output "Created: $OutputPath"
