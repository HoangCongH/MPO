param(
    [string]$TemplatePath = 'C:\Users\70R7056\Downloads\Blue Modern Data Analysis Presentation.pptx',
    [string]$InputPath = 'C:\Intern\mpo_webproject\MPO-main\MPO\MPO_Web_Prj\MPO_Panasonic_Executive_Deck.pptx',
    [string]$OutputPath = 'C:\Intern\mpo_webproject\MPO-main\MPO\MPO_Web_Prj\MPO_Panasonic_Template_Preserved.pptx'
)

$ErrorActionPreference='Stop'
function RGB([int]$r,[int]$g,[int]$b){$r+256*$g+65536*$b}
$WHITE=RGB 255 255 255
$CYAN=RGB 0 230 240
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

function Add-Line($slide,[double]$x1,[double]$y1,[double]$x2,[double]$y2,[int]$color,[double]$weight){
    $sh=$slide.Shapes.AddLine($x1,$y1,$x2,$y2)
    $sh.Line.ForeColor.RGB=$color;$sh.Line.Weight=$weight
    return $sh
}

$ppt=New-Object -ComObject PowerPoint.Application
$ppt.Visible=-1
$template=$ppt.Presentations.Open($TemplatePath,$true,$false,$false)
$deck=$ppt.Presentations.Open($InputPath,$true,$false,$false)

if(Test-Path -LiteralPath $OutputPath){Remove-Item -LiteralPath $OutputPath -Force}
$deck.SaveAs($OutputPath,24)
$deck.Close()
$deck=$ppt.Presentations.Open($OutputPath,$false,$false,$false)

# Remove the cosmic cover artwork. The rest of the deck already uses a dark base.
$cover=$deck.Slides.Item(1)
foreach($name in @('Freeform 2','Group 3')){
    try{$cover.Shapes.Item($name).Delete()}catch{}
}

# Use the exact full-page network background from original template slide 6.
for($i=1;$i -le $deck.Slides.Count;$i++){
    $target=$deck.Slides.Item($i)
    $template.Slides.Item(6).Shapes.Item('Freeform 2').Copy()
    $a=$target.Shapes.Paste();$a.Left=0;$a.Top=0;$a.Width=1440;$a.Height=810
    $template.Slides.Item(6).Shapes.Item('Group 3').Copy()
    $b=$target.Shapes.Paste();$b.Left=0;$b.Top=0;$b.Width=1440;$b.Height=810
    # Preserve original source stacking: Freeform behind Group, both behind content.
    $b.ZOrder(1);$a.ZOrder(1)
}

# Re-create the wordmark after the background insertion so it always stays on top.
$BLUE=RGB 0 80 136
for($i=1;$i -le $deck.Slides.Count;$i++){
    $s=$deck.Slides.Item($i)
    $r=$s.Shapes.AddShape(1,52,30,194,70)
    $r.Fill.Solid();$r.Fill.ForeColor.RGB=$BLUE;$r.Line.Visible=0
    $t=Add-Text $s 'Panasonic' 67 52 165 34 23 $WHITE $true 2
    $r.ZOrder(0);$t.ZOrder(0)
}

# Rebuild cover so it shares the same header, typography and rhythm as content slides.
$removeTexts=@('Manufacturing','Performance','Overview','2026','Tài liệu tổng hợp quá trình & thiết kế dự án','LNB • SFTP • Node-RED • PostgreSQL • ASP.NET Core')
for($j=$cover.Shapes.Count;$j -ge 1;$j--){
    $sh=$cover.Shapes.Item($j)
    if($sh.HasTextFrame -eq -1 -and $sh.TextFrame.HasText -eq -1){
        $t=$sh.TextFrame.TextRange.Text
        if($removeTexts -contains $t -or $t -like '*MANUFACTURING PERFORMANCE OVERVIEW*'){$sh.Delete()}
    }
}

# Existing line from the template is moved to the common footer position.
foreach($sh in $cover.Shapes){
    if($sh.Type -eq 9 -and $sh.Height -lt 5 -and $sh.Width -gt 1000){$sh.Left=70;$sh.Top=752;$sh.Width=1300}
}

$nav=@('MPO','PIPELINE','DATABASE','REPORTS','DEMO')
$xs=@(330,455,610,775,920)
for($i=0;$i -lt $nav.Count;$i++){
    $c=$(if($i -eq 0){$CYAN}else{$WHITE})
    Add-Text $cover $nav[$i] $xs[$i] 52 125 24 15 $c ($i -eq 0) 2 | Out-Null
}
Add-Line $cover 352 83 433 83 $CYAN 2 | Out-Null
Add-Text $cover '01' 1300 48 70 26 15 $GRAY $true 3 | Out-Null
Add-Text $cover 'MANUFACTURING PERFORMANCE OVERVIEW' 70 170 760 28 15 $CYAN $true | Out-Null
Add-Text $cover 'MPO' 70 220 720 105 76 $WHITE $true | Out-Null
Add-Text $cover 'Dữ liệu máy → Hiệu suất → Hành động' 70 350 840 48 30 $WHITE $true | Out-Null
Add-Text $cover 'Hệ thống báo cáo sản xuất tập trung cho quản lý, kỹ thuật và bảo trì.' 70 425 850 54 21 $WHITE $false | Out-Null
Add-Text $cover 'LNB • SFTP • Node-RED • PostgreSQL • ASP.NET Core • Docker' 70 565 900 30 17 $CYAN $true | Out-Null
Add-Text $cover 'TÀI LIỆU TỔNG HỢP QUÁ TRÌNH & THIẾT KẾ DỰ ÁN' 70 625 900 25 13 $GRAY $true | Out-Null
Add-Text $cover 'MPO | Manufacturing Performance Overview' 70 764 520 18 11 $GRAY $false | Out-Null
Add-Text $cover 'PANASONIC • 01' 1210 764 160 18 11 $GRAY $false 3 | Out-Null

$deck.Save()
$deck.Close();$template.Close();$ppt.Quit()
[System.Runtime.InteropServices.Marshal]::ReleaseComObject($ppt)|Out-Null
Write-Output "Created: $OutputPath"
