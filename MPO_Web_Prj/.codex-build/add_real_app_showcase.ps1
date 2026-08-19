param(
    [string]$InputPath = 'C:\Intern\mpo_webproject\MPO-main\MPO\MPO_Web_Prj\MPO_Panasonic_Template_Preserved.pptx',
    [string]$TemplatePath = 'C:\Users\70R7056\Downloads\Blue Modern Data Analysis Presentation.pptx',
    [string]$OutputPath = 'C:\Intern\mpo_webproject\MPO-main\MPO\MPO_Web_Prj\MPO_Panasonic_Real_App_Showcase.pptx'
)

$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing

function RGB([int]$r,[int]$g,[int]$b){$r+256*$g+65536*$b}
$WHITE=RGB 255 255 255
$LIGHT=RGB 230 240 250
$CYAN=RGB 0 230 240
$NAVY=RGB 2 12 90
$BLUE=RGB 0 80 136
$GRAY=RGB 148 163 184
$FONT='Arial'

function Add-Text($slide,[string]$text,[double]$x,[double]$y,[double]$w,[double]$h,[double]$size,[int]$color,[bool]$bold,[int]$align=1){
    $sh=$slide.Shapes.AddTextbox(1,$x,$y,$w,$h)
    $sh.TextFrame.TextRange.Text=$text
    $sh.TextFrame.MarginLeft=0;$sh.TextFrame.MarginRight=0;$sh.TextFrame.MarginTop=0;$sh.TextFrame.MarginBottom=0
    $sh.TextFrame.TextRange.Font.Name=$FONT;$sh.TextFrame.TextRange.Font.Size=$size
    $sh.TextFrame.TextRange.Font.Bold=$(if($bold){-1}else{0})
    $sh.TextFrame.TextRange.Font.Color.RGB=$color
    $sh.TextFrame.TextRange.ParagraphFormat.Alignment=$align
    return $sh
}

function Add-Note($slide,[string]$note){
    try{$slide.NotesPage.Shapes.Placeholders.Item(2).TextFrame.TextRange.Text=$note}catch{}
}

$shots=@(
    @{Path='C:\Users\70R7056\OneDrive - Panasonic\Pictures\Screenshots\Screenshot 2026-08-18 153525.png';Title='QUICK ACCESS';Value='One-click access to the complete MPO reporting suite.'},
    @{Path='C:\Users\70R7056\OneDrive - Panasonic\Pictures\Screenshots\Screenshot 2026-08-18 153755.png';Title='OVERALL DASHBOARD';Value='Management view combining output, equipment risk and downtime.'},
    @{Path='C:\Users\70R7056\OneDrive - Panasonic\Pictures\Screenshots\Screenshot 2026-08-18 154005.png';Title='BOARD COUNT';Value='Hourly output comparison by line and lane.'},
    @{Path='C:\Users\70R7056\OneDrive - Panasonic\Pictures\Screenshots\Screenshot 2026-08-18 154237.png';Title='PRODUCTION REPORT';Value='Filtered production history with Excel export.'},
    @{Path='C:\Users\70R7056\OneDrive - Panasonic\Pictures\Screenshots\Screenshot 2026-08-18 154325.png';Title='PICK-PLACEMENT BY PART';Value='Trace misses and placement quality to part level.'},
    @{Path='C:\Users\70R7056\OneDrive - Panasonic\Pictures\Screenshots\Screenshot 2026-08-18 154414.png';Title='PICK-PLACEMENT BY FEEDER';Value='Identify feeder, slot and material-related losses.'},
    @{Path='C:\Users\70R7056\OneDrive - Panasonic\Pictures\Screenshots\Screenshot 2026-08-18 154450.png';Title='PICK-PLACEMENT BY NOZZLE';Value='Prioritize nozzle and head maintenance from evidence.'},
    @{Path='C:\Users\70R7056\OneDrive - Panasonic\Pictures\Screenshots\Screenshot 2026-08-18 154559.png';Title='CYCLE TIME REPORT';Value='Compare cycle time by line, model and group.'},
    @{Path='C:\Users\70R7056\OneDrive - Panasonic\Pictures\Screenshots\Screenshot 2026-08-18 154630.png';Title='DOWNTIME REPORT';Value='Measure error count and stop duration by line.'},
    @{Path='C:\Users\70R7056\OneDrive - Panasonic\Pictures\Screenshots\Screenshot 2026-08-18 154650.png';Title='TOTAL PICKUP / PLACEMENT';Value='Compare pickup, placement and PPM across lines.'}
)

foreach($shot in $shots){if(-not(Test-Path -LiteralPath $shot.Path)){throw "Missing screenshot: $($shot.Path)"}}

# Build 39 flattened headers from the exact network background used by the deck.
$headerBg='C:\Intern\mpo_webproject\MPO-main\MPO\MPO_Web_Prj\.codex-build\header_background.png'
$headerDir='C:\Intern\mpo_webproject\MPO-main\MPO\MPO_Web_Prj\.codex-build\headers_39'
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

for($i=1;$i -le 39;$i++){
    $active=if($i -le 4){0}elseif($i -le 10){1}elseif($i -le 16){2}elseif($i -le 34){3}else{4}
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
$template=$ppt.Presentations.Open($TemplatePath,$true,$false,$false)
$source=$ppt.Presentations.Open($InputPath,$true,$false,$false)
if(Test-Path -LiteralPath $OutputPath){Remove-Item -LiteralPath $OutputPath -Force}
$source.SaveAs($OutputPath,24);$source.Close()
$deck=$ppt.Presentations.Open($OutputPath,$false,$false,$false)

# Insert the real application sequence immediately after the report portfolio (slide 19).
for($k=0;$k -lt $shots.Count;$k++){
    $index=20+$k
    $slide=$deck.Slides.Add($index,12)
    $slide.FollowMasterBackground=0
    $slide.Background.Fill.Solid();$slide.Background.Fill.ForeColor.RGB=$NAVY
    $template.Slides.Item(6).Shapes.Item('Freeform 2').Copy();$a=$slide.Shapes.Paste();$a.Left=0;$a.Top=0;$a.Width=1440;$a.Height=810
    $template.Slides.Item(6).Shapes.Item('Group 3').Copy();$b=$slide.Shapes.Paste();$b.Left=0;$b.Top=0;$b.Width=1440;$b.Height=810
    $b.ZOrder(1);$a.ZOrder(1)

    Add-Text $slide ('REAL APPLICATION • '+$shots[$k].Title) 70 118 920 32 23 $WHITE $true | Out-Null
    Add-Text $slide $shots[$k].Value 70 146 1020 24 14 $LIGHT $false | Out-Null
    $tag=$slide.Shapes.AddShape(5,1175,122,195,34)
    $tag.Fill.Solid();$tag.Fill.ForeColor.RGB=$CYAN;$tag.Line.Visible=0
    Add-Text $slide ('SCREEN '+('{0:D2}' -f ($k+1))+' / 10') 1183 129 179 20 13 $NAVY $true 2 | Out-Null

    $img=[Drawing.Image]::FromFile($shots[$k].Path)
    $imgW=$img.Width;$imgH=$img.Height;$img.Dispose()
    $targetW=1200.0;$targetH=$targetW*$imgH/$imgW
    if($targetH -gt 600){$targetH=600;$targetW=$targetH*$imgW/$imgH}
    $x=(1440-$targetW)/2;$y=176
    $pic=$slide.Shapes.AddPicture($shots[$k].Path,0,-1,$x,$y,$targetW,$targetH)
    $pic.Line.Visible=-1;$pic.Line.ForeColor.RGB=$CYAN;$pic.Line.Weight=1.5
    Add-Note $slide ($shots[$k].Title+': '+$shots[$k].Value+' Use this screen as evidence of the implemented workflow and explain the visible filters, results and export actions.')
}

# Replace every header with the new 39-slide flattened version and update footer numbers.
for($i=1;$i -le $deck.Slides.Count;$i++){
    $slide=$deck.Slides.Item($i)
    for($j=$slide.Shapes.Count;$j -ge 1;$j--){
        $sh=$slide.Shapes.Item($j)
        if($sh.Top -ge 0 -and $sh.Top -lt 110 -and $sh.Width -ge 1400 -and $sh.Height -le 120){$sh.Delete()}
        elseif($sh.HasTextFrame -eq -1 -and $sh.TextFrame.HasText -eq -1){
            $text=$sh.TextFrame.TextRange.Text
            if($text -match '^PANASONIC • \d{2}$'){$sh.TextFrame.TextRange.Text=('PANASONIC • {0:D2}' -f $i)}
        }
    }
    $header=Join-Path $headerDir ('header_{0:D2}.png' -f $i)
    $hp=$slide.Shapes.AddPicture($header,0,-1,0,0,1440,110);$hp.ZOrder(0)
}

$deck.Save();$deck.Close();$template.Close();$ppt.Quit()
[System.Runtime.InteropServices.Marshal]::ReleaseComObject($ppt)|Out-Null
Write-Output "Created: $OutputPath"
