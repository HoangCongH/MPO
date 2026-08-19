param(
    [string]$TemplatePath = 'C:\Users\70R7056\Downloads\Blue Modern Data Analysis Presentation.pptx',
    [string]$OutputPath = 'C:\Intern\mpo_webproject\MPO-main\MPO\MPO_Web_Prj\MPO_Panasonic_Executive_Deck.pptx'
)

$ErrorActionPreference = 'Stop'

function RGB([int]$r,[int]$g,[int]$b) { return $r + 256*$g + 65536*$b }

$NAVY   = RGB 2 12 90
$NAVY2  = RGB 5 24 115
$BLUE   = RGB 0 80 136
$CYAN   = RGB 0 230 240
$LIGHT  = RGB 230 240 250
$WHITE  = RGB 255 255 255
$GRAY   = RGB 148 163 184
$MID    = RGB 71 85 105
$GREEN  = RGB 34 197 94
$AMBER  = RGB 245 158 11
$RED    = RGB 239 68 68
$FONT   = 'Arial'
$slideW = 1440
$slideH = 810

function Add-Text($slide,[string]$text,[double]$x,[double]$y,[double]$w,[double]$h,[double]$size=24,[int]$color=$WHITE,[bool]$bold=$false,[int]$align=1) {
    $sh = $slide.Shapes.AddTextbox(1,$x,$y,$w,$h)
    $sh.TextFrame.TextRange.Text = $text
    $sh.TextFrame.MarginLeft = 0
    $sh.TextFrame.MarginRight = 0
    $sh.TextFrame.MarginTop = 0
    $sh.TextFrame.MarginBottom = 0
    $sh.TextFrame.WordWrap = -1
    $sh.TextFrame.TextRange.Font.Name = $FONT
    $sh.TextFrame.TextRange.Font.Size = $size
    $sh.TextFrame.TextRange.Font.Bold = $(if($bold){-1}else{0})
    $sh.TextFrame.TextRange.Font.Color.RGB = $color
    $sh.TextFrame.TextRange.ParagraphFormat.Alignment = $align
    return $sh
}

function Add-Rect($slide,[double]$x,[double]$y,[double]$w,[double]$h,[int]$fill=$NAVY2,[double]$radius=0,[int]$lineColor=$fill,[double]$lineWeight=0) {
    $shapeType = $(if($radius -gt 0){5}else{1})
    $sh = $slide.Shapes.AddShape($shapeType,$x,$y,$w,$h)
    $sh.Fill.Solid(); $sh.Fill.ForeColor.RGB = $fill
    if($lineWeight -gt 0){$sh.Line.Visible=-1;$sh.Line.ForeColor.RGB=$lineColor;$sh.Line.Weight=$lineWeight}else{$sh.Line.Visible=0}
    return $sh
}

function Add-Line($slide,[double]$x1,[double]$y1,[double]$x2,[double]$y2,[int]$color=$CYAN,[double]$weight=2,[bool]$arrow=$false) {
    $ln=$slide.Shapes.AddLine($x1,$y1,$x2,$y2)
    $ln.Line.ForeColor.RGB=$color; $ln.Line.Weight=$weight
    if($arrow){$ln.Line.EndArrowheadStyle=3}
    return $ln
}

function Add-Circle($slide,[double]$x,[double]$y,[double]$d,[int]$fill=$CYAN,[int]$lineColor=$fill) {
    $sh=$slide.Shapes.AddShape(9,$x,$y,$d,$d)
    $sh.Fill.Solid();$sh.Fill.ForeColor.RGB=$fill
    $sh.Line.ForeColor.RGB=$lineColor
    return $sh
}

function Add-Label($slide,[string]$text,[double]$x,[double]$y,[double]$w,[int]$fill=$CYAN,[int]$textColor=$NAVY) {
    $sh=Add-Rect $slide $x $y $w 34 $fill 1
    $sh.Adjustments.Item(1)=0.25
    $tx=Add-Text $slide $text ($x+8) ($y+6) ($w-16) 22 14 $textColor $true 2
    return @($sh,$tx)
}

function Add-Card($slide,[string]$title,[string]$body,[double]$x,[double]$y,[double]$w,[double]$h,[string]$tag='',[int]$accent=$CYAN) {
    Add-Rect $slide $x $y $w $h $NAVY2 1 $accent 1 | Out-Null
    Add-Rect $slide $x $y 8 $h $accent 0 | Out-Null
    if($tag){
        $tagW=[Math]::Max(62,24+$tag.Length*9)
        Add-Label $slide $tag ($x+24) ($y+18) $tagW $accent $NAVY | Out-Null
    }
    $titleY = $(if($tag){$y+66}else{$y+24})
    Add-Text $slide $title ($x+24) $titleY ($w-48) 34 22 $WHITE $true | Out-Null
    Add-Text $slide $body ($x+24) ($titleY+48) ($w-48) ($h-($titleY-$y)-62) 16 $LIGHT $false | Out-Null
}

function Add-BulletList($slide,[string[]]$items,[double]$x,[double]$y,[double]$w,[double]$lineH=48,[int]$color=$WHITE,[double]$size=20) {
    for($i=0;$i -lt $items.Count;$i++){
        Add-Circle $slide $x ($y+$i*$lineH+8) 10 $CYAN $CYAN | Out-Null
        Add-Text $slide $items[$i] ($x+24) ($y+$i*$lineH) ($w-24) ($lineH-2) $size $color $false | Out-Null
    }
}

function Add-Base($slide,[int]$index,[string]$section,[string]$title,[string]$kicker='') {
    $slide.FollowMasterBackground = 0
    $slide.Background.Fill.Solid();$slide.Background.Fill.ForeColor.RGB=$NAVY
    # Rebuild the source wordmark as native shapes. The template's original logo is
    # an image-filled freeform that PowerPoint renders inconsistently after copying.
    Add-Rect $slide 52 30 194 70 $BLUE 0 | Out-Null
    Add-Text $slide 'Panasonic' 67 52 165 34 23 $WHITE $true 2 | Out-Null
    $labels=@('MPO','PIPELINE','DATABASE','REPORTS','DEMO')
    $xs=@(330,455,610,775,920)
    for($i=0;$i -lt $labels.Count;$i++){
        $c=$(if($labels[$i] -eq $section){$CYAN}else{$LIGHT})
        $b=($labels[$i] -eq $section)
        Add-Text $slide $labels[$i] $xs[$i] 52 125 24 15 $c $b 2 | Out-Null
        if($b){Add-Line $slide ($xs[$i]+22) 83 ($xs[$i]+103) 83 $CYAN 2 | Out-Null}
    }
    Add-Text $slide ('{0:D2}' -f $index) 1300 48 70 26 15 $GRAY $true 3 | Out-Null
    if($kicker){Add-Text $slide $kicker 70 124 600 24 13 $CYAN $true | Out-Null}
    Add-Text $slide $title 70 151 1300 64 34 $WHITE $true | Out-Null
    Add-Line $slide 70 752 1370 752 $GRAY 0.8 | Out-Null
    Add-Text $slide 'MPO | Manufacturing Performance Overview' 70 764 520 18 11 $GRAY $false | Out-Null
    Add-Text $slide ('PANASONIC • {0:D2}' -f $index) 1210 764 160 18 11 $GRAY $false 3 | Out-Null
}

function Add-SectionSlide($slide,[int]$index,[string]$number,[string]$title,[string]$subtitle,[string]$section) {
    Add-Base $slide $index $section '' ''
    Add-Text $slide $number 70 180 280 210 150 $CYAN $true | Out-Null
    Add-Line $slide 356 236 356 548 $CYAN 4 | Out-Null
    Add-Text $slide $title 410 232 850 110 48 $WHITE $true | Out-Null
    Add-Text $slide $subtitle 414 368 780 110 23 $LIGHT $false | Out-Null
    Add-Text $slide 'Từ dữ liệu thô đến quyết định vận hành' 414 520 650 30 16 $CYAN $true | Out-Null
    for($i=0;$i -lt 5;$i++){Add-Circle $slide (1130+$i*34) (580+$i*18) (10+$i*3) $CYAN $CYAN | Out-Null}
}

function Add-Note($slide,[string]$note) {
    try {
        $ph=$slide.NotesPage.Shapes.Placeholders.Item(2)
        $ph.TextFrame.TextRange.Text=$note
    } catch {}
}

function Add-Step($slide,[string]$num,[string]$title,[string]$body,[double]$x,[double]$y,[double]$w,[int]$accent=$CYAN) {
    Add-Circle $slide $x $y 42 $accent $accent | Out-Null
    Add-Text $slide $num ($x+2) ($y+8) 38 24 16 $NAVY $true 2 | Out-Null
    Add-Text $slide $title ($x+58) ($y-1) ($w-58) 28 18 $WHITE $true | Out-Null
    Add-Text $slide $body ($x+58) ($y+29) ($w-58) 42 14 $LIGHT $false | Out-Null
}

$ppt=New-Object -ComObject PowerPoint.Application
$ppt.Visible=-1
$source=$ppt.Presentations.Open($TemplatePath,$true,$false,$false)
$script:logo=$source.Slides.Item(1).Shapes.Item('Freeform 8')
$target=$ppt.Presentations.Add()
$target.PageSetup.SlideWidth=$slideW
$target.PageSetup.SlideHeight=$slideH

# 01 — Cover: preserve the original template artwork.
$source.Slides.Item(1).Copy(); $target.Slides.Paste() | Out-Null
$s=$target.Slides.Item(1)
foreach($sh in $s.Shapes){
    if($sh.HasTextFrame -eq -1 -and $sh.TextFrame.HasText -eq -1){
        $t=$sh.TextFrame.TextRange.Text.Trim()
        switch($t){
            'DATA COLLECTION' {$sh.TextFrame.TextRange.Text="MPO`rMANUFACTURING PERFORMANCE OVERVIEW";$sh.TextFrame.TextRange.Font.Name=$FONT;$sh.TextFrame.TextRange.Font.Size=54;$sh.TextFrame.TextRange.Font.Bold=-1}
            'Home' {$sh.TextFrame.TextRange.Text='Manufacturing'}
            'About' {$sh.TextFrame.TextRange.Text='Performance'}
            'Content' {$sh.TextFrame.TextRange.Text='Overview'}
            'Others' {$sh.TextFrame.TextRange.Text='2026'}
            '-' {$sh.TextFrame.TextRange.Text='LNB • SFTP • Node-RED • PostgreSQL • ASP.NET Core';$sh.TextFrame.TextRange.Font.Size=14}
        }
    }
}
Add-Text $s 'Tài liệu tổng hợp quá trình & thiết kế dự án' 300 545 840 34 20 $WHITE $false 2 | Out-Null
Add-Note $s 'MPO là hệ thống web báo cáo hiệu suất sản xuất, chuyển dữ liệu máy LNB thô thành dashboard và báo cáo phục vụ vận hành hằng ngày.'

# 02 — Executive summary
$s=$target.Slides.Add(2,12); Add-Base $s 2 'MPO' 'MPO biến dữ liệu máy thành hành động' 'EXECUTIVE SUMMARY'
Add-Text $s 'Một luồng tự động hợp nhất sản lượng, chất lượng và downtime — từ file .u01 đến dashboard quản trị.' 70 222 1240 62 24 $LIGHT $false | Out-Null
$cards=@(
    @('01','Tập trung','Một nguồn báo cáo thống nhất cho dữ liệu máy LNB.'),
    @('02','Phân tích','Drill-down theo line, lane, máy, model, part, feeder và nozzle.'),
    @('03','Hành động','Xác định thiết bị rủi ro, điều tra nguyên nhân và export dữ liệu.')
)
for($i=0;$i -lt 3;$i++){Add-Card $s $cards[$i][1] $cards[$i][2] (70+$i*430) 330 390 250 $cards[$i][0] | Out-Null}
Add-Text $s 'Kết quả: dữ liệu thô trở thành bằng chứng có thể sử dụng trong vận hành hằng ngày.' 70 625 1240 34 18 $CYAN $true 2 | Out-Null
Add-Note $s 'Dashboard chỉ ra khu vực cần chú ý; các báo cáo chi tiết cung cấp bằng chứng để điều tra.'

# 03 — Business value
$s=$target.Slides.Add(3,12); Add-Base $s 3 'MPO' 'Bốn giá trị trực tiếp cho vận hành' 'BUSINESS VALUE'
$vals=@(
    @('01','Một nguồn dữ liệu','Tập trung production, quality và downtime.'),
    @('02','Giảm thủ công','Loại bỏ nhiều bước tổng hợp và đối chiếu file.'),
    @('03','Root-cause nhanh','Truy vết từ line đến feeder, nozzle hoặc part.'),
    @('04','Chia sẻ linh hoạt','Export kết quả đã lọc sang Excel-compatible.')
)
for($i=0;$i -lt 4;$i++){
    $x=70+($i%2)*650;$y=235+[math]::Floor($i/2)*205
    Add-Card $s $vals[$i][1] $vals[$i][2] $x $y 610 165 $vals[$i][0] | Out-Null
}

# 04 — Architecture at a glance
$s=$target.Slides.Add(4,12); Add-Base $s 4 'MPO' 'Một kiến trúc, một dòng dữ liệu' 'SYSTEM AT A GLANCE'
$nodes=@(
    @('LNB','.u01 raw data'),@('SFTP','Secure transfer'),@('Node-RED','Parse + delta'),@('PostgreSQL','Store + state'),@('Web MPO','Dashboard + export')
)
for($i=0;$i -lt 5;$i++){
    $x=55+$i*272
    Add-Rect $s $x 300 220 160 $NAVY2 1 $CYAN 1.2 | Out-Null
    Add-Circle $s ($x+79) 250 62 $CYAN $CYAN | Out-Null
    Add-Text $s ('0'+($i+1)) ($x+90) 267 40 25 15 $NAVY $true 2 | Out-Null
    Add-Text $s $nodes[$i][0] ($x+18) 322 184 30 21 $WHITE $true 2 | Out-Null
    Add-Text $s $nodes[$i][1] ($x+18) 372 184 42 15 $LIGHT $false 2 | Out-Null
    if($i -lt 4){Add-Line $s ($x+220) 380 ($x+264) 380 $CYAN 3 $true | Out-Null}
}
Add-Label $s 'AUTOMATED' 552 520 150 $CYAN $NAVY | Out-Null
Add-Text $s 'Không cần xử lý file thủ công sau khi dữ liệu vào PostgreSQL.' 305 580 830 34 20 $LIGHT $false 2 | Out-Null
Add-Note $s 'Luồng tổng thể: LNB raw data, SFTP, incoming folder, Node-RED Watch, parse và delta, PostgreSQL, ASP.NET Core và web frontend trong Docker.'

# 05 — Section
$s=$target.Slides.Add(5,12); Add-SectionSlide $s 5 '01' 'DATA PIPELINE' 'Cách MPO tiếp nhận, chuẩn hóa và bảo toàn ý nghĩa của counter tích lũy.' 'PIPELINE'

# 06 — Eight-step flow
$s=$target.Slides.Add(6,12); Add-Base $s 6 'PIPELINE' 'Luồng dữ liệu tự động qua 8 bước' 'END-TO-END FLOW'
$flow=@(
    @('01','LNB tạo file','.u01 counters'),@('02','SFTP chuyển file','incoming folder'),@('03','Watch phát hiện','lọc file hợp lệ'),@('04','Parse nội dung','machine + report'),
    @('05','Tính delta','count + time'),@('06','Ghi database','stored procedure'),@('07','Khôi phục state','sau restart'),@('08','Hiển thị web','dashboard + export')
)
for($i=0;$i -lt 8;$i++){
    $row=[math]::Floor($i/4);$col=$i%4;$x=70+$col*330;$y=235+$row*220
    Add-Step $s $flow[$i][0] $flow[$i][1] $flow[$i][2] $x $y 290 | Out-Null
    if($col -lt 3){Add-Line $s ($x+288) ($y+21) ($x+318) ($y+21) $GRAY 1.5 $true | Out-Null}
}
Add-Text $s 'Điểm kiểm soát trọng yếu: delta counter + state persistence.' 70 665 1240 30 18 $CYAN $true 2 | Out-Null
Add-Note $s 'Toàn bộ quy trình là tự động, từ file raw đến báo cáo. Sau khi dữ liệu được ghi vào PostgreSQL, người dùng thao tác trực tiếp trên web dashboard.'

# 07 — Ingestion and parsing
$s=$target.Slides.Add(7,12); Add-Base $s 7 'PIPELINE' 'Node-RED chuẩn hóa file .u01' 'INGESTION & PARSING'
Add-Card $s 'Machine identity' 'Line • machine • lane • stage • type • version' 70 235 390 170 '01' | Out-Null
Add-Card $s 'Production context' 'MJS ID • lot • product • report date • output' 525 235 390 170 '02' | Out-Null
Add-Card $s 'Operational counters' 'Cycle time • production • downtime • error counters' 980 235 390 170 '03' | Out-Null
Add-Rect $s 70 470 1300 155 $NAVY2 1 $BLUE 1 | Out-Null
Add-Text $s 'MountPickupFeeder' 105 505 290 30 20 $CYAN $true | Out-Null
Add-Text $s '→ danh sách feeder theo part, address, reel và miss counters' 405 505 890 30 18 $LIGHT $false | Out-Null
Add-Text $s 'MountPickupNozzle' 105 560 290 30 20 $CYAN $true | Out-Null
Add-Text $s '→ danh sách nozzle theo head, address, name và miss counters' 405 560 890 30 18 $LIGHT $false | Out-Null

# 08 — Delta engine
$s=$target.Slides.Add(8,12); Add-Base $s 8 'PIPELINE' 'Delta counter bảo toàn sản lượng thực' 'CORE PROCESSING LOGIC'
Add-Rect $s 70 235 540 310 $NAVY2 1 $CYAN 1.5 | Out-Null
Add-Text $s 'DELTA' 105 275 170 44 34 $CYAN $true | Out-Null
Add-Text $s '= Current counter − Previous counter' 105 335 440 36 24 $WHITE $true | Out-Null
Add-Line $s 105 392 535 392 $GRAY 1 | Out-Null
Add-Text $s 'Áp dụng cho' 105 420 150 24 15 $GRAY $true | Out-Null
Add-Text $s 'Production • Time • Feeder • Nozzle' 105 455 430 30 20 $LIGHT $false | Out-Null
$cases=@(
    @('Lần đầu','Delta = Current','Không có state trước đó',$GREEN),
    @('Bình thường','Delta = Current − Previous','Chỉ ghi phần phát sinh',$CYAN),
    @('Counter reset','Delta = Current','Current < Previous',$AMBER)
)
for($i=0;$i -lt 3;$i++){
    $y=235+$i*135
    Add-Rect $s 680 $y 690 110 $NAVY2 1 $cases[$i][3] 1 | Out-Null
    Add-Text $s $cases[$i][0] 710 ($y+18) 180 26 18 $cases[$i][3] $true | Out-Null
    Add-Text $s $cases[$i][1] 915 ($y+18) 410 26 18 $WHITE $true | Out-Null
    Add-Text $s $cases[$i][2] 710 ($y+58) 615 24 15 $LIGHT $false | Out-Null
}
Add-Text $s 'Tránh cộng lặp • Xử lý reset máy • Duy trì tính đúng theo kỳ' 70 650 1300 28 18 $CYAN $true 2 | Out-Null
Add-Note $s 'Đây là điểm quan trọng nhất của flow. Máy gửi số tích lũy, còn hệ thống cần số phát sinh thực tế trong kỳ. Delta ngăn cộng lặp và xử lý trường hợp reset counter.'

# 09 — Feeder/nozzle detail
$s=$target.Slides.Add(9,12); Add-Base $s 9 'PIPELINE' 'Chi tiết thiết bị cũng được tính delta' 'EQUIPMENT-LEVEL DATA'
Add-Card $s 'Feeder delta' 'Pickup, mount và các nhóm miss theo từng feeder / slot / part.' 70 245 600 230 'F' | Out-Null
Add-Card $s 'Nozzle delta' 'Pickup, mount và các nhóm miss theo từng nozzle / head.' 770 245 600 230 'N' | Out-Null
Add-Line $s 360 510 360 590 $CYAN 3 $true | Out-Null
Add-Line $s 1070 510 1070 590 $CYAN 3 $true | Out-Null
Add-Rect $s 240 595 880 70 $BLUE 1 | Out-Null
Add-Text $s 'JSON payload thống nhất → report_data + feeder_data + nozzle_data + raw_state' 275 616 810 28 18 $WHITE $true 2 | Out-Null

# 10 — State recovery
$s=$target.Slides.Add(10,12); Add-Base $s 10 'PIPELINE' 'Restart không làm mất ngữ cảnh delta' 'STATE RECOVERY'
Add-Circle $s 195 290 145 $BLUE $CYAN | Out-Null
Add-Text $s 'RUN' 230 340 75 35 24 $WHITE $true 2 | Out-Null
Add-Circle $s 648 290 145 $NAVY2 $CYAN | Out-Null
Add-Text $s 'STATE' 670 340 100 35 22 $CYAN $true 2 | Out-Null
Add-Circle $s 1100 290 145 $BLUE $CYAN | Out-Null
Add-Text $s 'RESTORE' 1116 340 112 35 20 $WHITE $true 2 | Out-Null
Add-Line $s 340 362 640 362 $CYAN 4 $true | Out-Null
Add-Line $s 793 362 1092 362 $CYAN 4 $true | Out-Null
Add-Line $s 1172 440 1172 540 $GRAY 2 | Out-Null
Add-Line $s 1172 540 268 540 $GRAY 2 $true | Out-Null
Add-Text $s 'machine_counter_state lưu raw counter gần nhất, feeder_state và nozzle_state JSONB.' 160 585 1120 34 20 $LIGHT $false 2 | Out-Null
Add-Text $s 'Node-RED khởi động → nạp state vào flow context → tiếp tục tính delta chính xác.' 160 635 1120 34 18 $CYAN $true 2 | Out-Null
Add-Note $s 'Khi Node-RED khởi động, flow truy vấn machine_counter_state và nạp lại counter của từng máy, feeder và nozzle vào flow context.'

# 11 — Section
$s=$target.Slides.Add(11,12); Add-SectionSlide $s 11 '02' 'DATA FOUNDATION' 'PostgreSQL kết hợp relational schema và JSONB để vừa nhanh vừa linh hoạt.' 'DATABASE'

# 12 — Data model
$s=$target.Slides.Add(12,12); Add-Base $s 12 'DATABASE' 'Bốn bảng tạo xương sống dữ liệu' 'POSTGRESQL DATA MODEL'
$tables=@(
    @('master_machines','1','Danh mục máy chuẩn hóa'),
    @('production_reports','N','Lịch sử báo cáo trung tâm'),
    @('feeder_logs','N','Chi tiết feeder theo report'),
    @('nozzle_logs','N','Chi tiết nozzle theo report'),
    @('machine_counter_state','1','Raw state gần nhất')
)
Add-Card $s $tables[0][0] $tables[0][2] 70 310 300 155 'MASTER' | Out-Null
Add-Card $s $tables[1][0] $tables[1][2] 545 250 350 170 'FACT' | Out-Null
Add-Card $s $tables[2][0] $tables[2][2] 1070 210 300 140 'DETAIL' | Out-Null
Add-Card $s $tables[3][0] $tables[3][2] 1070 405 300 140 'DETAIL' | Out-Null
Add-Card $s $tables[4][0] $tables[4][2] 545 520 350 145 'STATE' | Out-Null
Add-Line $s 370 385 535 335 $CYAN 2 $true | Out-Null
Add-Text $s '1 : N' 420 340 70 24 14 $CYAN $true 2 | Out-Null
Add-Line $s 895 325 1060 275 $CYAN 2 $true | Out-Null
Add-Line $s 895 345 1060 475 $CYAN 2 $true | Out-Null
Add-Text $s '1 : N' 965 260 70 24 14 $CYAN $true 2 | Out-Null
Add-Text $s '1 : N' 965 440 70 24 14 $CYAN $true 2 | Out-Null
Add-Line $s 370 410 535 590 $GRAY 2 $true | Out-Null
Add-Text $s '1 : 1' 430 520 70 24 14 $GRAY $true 2 | Out-Null
Add-Note $s 'Database tách master, lịch sử báo cáo, chi tiết feeder/nozzle và technical state. Quan hệ rõ ràng giúp báo cáo nhanh nhưng vẫn khôi phục delta sau restart.'

# 13 — Central fact table
$s=$target.Slides.Add(13,12); Add-Base $s 13 'DATABASE' 'production_reports: dữ liệu lịch sử' 'CENTRAL FACT TABLE'
$groups=@(
    @('OUTPUT','output_qty • board • module • pickup • mount',$GREEN),
    @('TIME','power on • production • stop • wait • error',$CYAN),
    @('QUALITY','pickup miss • mount miss • recognition • trouble',$AMBER),
    @('CYCLE','cycle_time_1 • cycle_time_2 • cycle_time_3',$BLUE)
)
for($i=0;$i -lt 4;$i++){
    $x=70+($i%2)*650;$y=240+[math]::Floor($i/2)*205
    Add-Rect $s $x $y 610 165 $NAVY2 1 $groups[$i][2] 1 | Out-Null
    Add-Label $s $groups[$i][0] ($x+24) ($y+22) 120 $groups[$i][2] $NAVY | Out-Null
    Add-Text $s $groups[$i][1] ($x+24) ($y+82) 555 45 18 $LIGHT $false | Out-Null
}
Add-Text $s 'Cấp phân tích: machine • report date • MJS • product • lot • file' 70 660 1300 30 18 $CYAN $true 2 | Out-Null

# 14 — Feeder vs nozzle
$s=$target.Slides.Add(14,12); Add-Base $s 14 'DATABASE' 'Feeder và nozzle mở khóa root-cause' 'DETAIL TABLES'
Add-Rect $s 70 235 610 385 $NAVY2 1 $CYAN 1.5 | Out-Null
Add-Text $s 'feeder_logs' 105 270 300 38 28 $CYAN $true | Out-Null
Add-BulletList $s @('Block code / serial / part / reel','Feeder & sub-feeder address','Pickup, mount và miss counters','Nhiều feeder trên mỗi report') 105 340 520 58 $LIGHT 18
Add-Rect $s 760 235 610 385 $NAVY2 1 $GREEN 1.5 | Out-Null
Add-Text $s 'nozzle_logs' 795 270 300 38 28 $GREEN $true | Out-Null
Add-BulletList $s @('Head number / nozzle name','Nozzle & component address','Pickup, mount và miss counters','Nhiều nozzle trên mỗi report') 795 340 520 58 $LIGHT 18
Add-Text $s 'Kết quả: ưu tiên đúng feeder, slot, reel, part, nozzle hoặc head cần kiểm tra.' 70 665 1300 30 18 $WHITE $true 2 | Out-Null

# 15 — Hybrid design
$s=$target.Slides.Add(15,12); Add-Base $s 15 'DATABASE' 'Relational + JSONB: nhanh và linh hoạt' 'HYBRID STORAGE'
Add-Card $s 'Relational columns' 'Các trường cốt lõi phục vụ filter, join, index và báo cáo ổn định.' 70 250 590 220 'SQL' $CYAN | Out-Null
Add-Card $s 'JSONB fields' 'Rare time stats, count stats, feeder state và nozzle state linh hoạt.' 780 250 590 220 'JSON' $GREEN | Out-Null
Add-Line $s 660 360 780 360 $WHITE 3 $false | Out-Null
Add-Circle $s 704 325 72 $BLUE $CYAN | Out-Null
Add-Text $s '+' 724 340 32 32 28 $WHITE $true 2 | Out-Null
$ints=@('Foreign keys bảo toàn quan hệ','ON DELETE CASCADE cho detail','Index theo date, machine, MJS, lot, report và address')
Add-BulletList $s $ints 245 545 965 52 $LIGHT 18

# 16 — Stored procedure
$s=$target.Slides.Add(16,12); Add-Base $s 16 'DATABASE' 'Một điểm ghi dữ liệu duy nhất' 'STORED PROCEDURE'
Add-Label $s 'CALL' 70 238 90 $CYAN $NAVY | Out-Null
Add-Text $s 'insert_full_production_report(payload JSONB)' 180 238 700 34 24 $WHITE $true | Out-Null
$steps=@('Upsert máy','Insert report','Insert feeders','Insert nozzles','Update counter state')
for($i=0;$i -lt 5;$i++){
    $x=70+$i*260
    Add-Circle $s ($x+65) 345 72 $CYAN $CYAN | Out-Null
    Add-Text $s ('0'+($i+1)) ($x+82) 366 38 24 16 $NAVY $true 2 | Out-Null
    Add-Text $s $steps[$i] $x 445 205 48 18 $WHITE $true 2 | Out-Null
    if($i -lt 4){Add-Line $s ($x+140) 381 ($x+250) 381 $GRAY 2 $true | Out-Null}
}
Add-Rect $s 70 565 1300 74 $BLUE 1 | Out-Null
Add-Text $s 'Node-RED chuẩn hóa dữ liệu; PostgreSQL đảm nhiệm liên kết, ghi và cập nhật state.' 110 586 1220 30 18 $WHITE $true 2 | Out-Null
Add-Note $s 'Stored procedure là điểm ghi tập trung: đọc machine_id, upsert master, insert report, duyệt feeders/nozzles và cập nhật machine_counter_state.'

# 17 — Section
$s=$target.Slides.Add(17,12); Add-SectionSlide $s 17 '03' 'REPORTING APP' 'Ứng dụng biến dữ liệu PostgreSQL thành dashboard, drill-down và export.' 'REPORTS'

# 18 — Tech stack
$s=$target.Slides.Add(18,12); Add-Base $s 18 'REPORTS' 'Tech stack tối ưu cho báo cáo web' 'APPLICATION ARCHITECTURE'
$layers=@(
    @('COLLECT','SFTP + Node-RED',$CYAN),@('STORE','PostgreSQL',$GREEN),@('BACKEND','.NET 9 + ASP.NET Core MVC',$BLUE),@('DATA','EF Core + Npgsql',$AMBER),@('FRONTEND','Razor + Bootstrap + Chart.js',$CYAN),@('DEPLOY','Docker containers',$GREEN)
)
for($i=0;$i -lt 6;$i++){
    $row=[math]::Floor($i/3);$col=$i%3;$x=70+$col*435;$y=240+$row*190
    Add-Rect $s $x $y 390 150 $NAVY2 1 $layers[$i][2] 1 | Out-Null
    Add-Label $s $layers[$i][0] ($x+22) ($y+20) 105 $layers[$i][2] $NAVY | Out-Null
    Add-Text $s $layers[$i][1] ($x+22) ($y+78) 345 40 19 $WHITE $true | Out-Null
}
Add-Text $s 'PostgreSQL → report services → MVC controllers → Razor pages → dashboard / table / export' 70 660 1300 30 17 $LIGHT $false 2 | Out-Null

# 19 — Report portfolio
$s=$target.Slides.Add(19,12); Add-Base $s 19 'REPORTS' 'Chín góc nhìn trên cùng một dữ liệu' 'REPORT PORTFOLIO'
$reports=@('Overall Dashboard','Board Count','Production Report','By Part','By Feeder','By Nozzle','Cycle Time','Downtime','Total Pickup / Placement')
for($i=0;$i -lt 9;$i++){
    $row=[math]::Floor($i/3);$col=$i%3;$x=70+$col*435;$y=225+$row*145
    Add-Rect $s $x $y 390 115 $NAVY2 1 $CYAN 0.8 | Out-Null
    Add-Circle $s ($x+24) ($y+31) 48 $BLUE $CYAN | Out-Null
    Add-Text $s ('0'+($i+1)) ($x+31) ($y+44) 34 22 14 $WHITE $true 2 | Out-Null
    Add-Text $s $reports[$i] ($x+90) ($y+34) 275 42 18 $WHITE $true | Out-Null
}
Add-Text $s 'Dashboard để định hướng → report chi tiết để chứng minh và hành động.' 70 675 1300 28 18 $CYAN $true 2 | Out-Null

# 20 — Overall dashboard
$s=$target.Slides.Add(20,12); Add-Base $s 20 'REPORTS' 'Dashboard ưu tiên điều cần chú ý' 'OVERALL DASHBOARD'
$kpis=@(@('BOARD PRODUCED','Output theo line',$GREEN),@('PPM','Chất lượng tương đối',$CYAN),@('ERROR STOP','Thời gian dừng lỗi',$AMBER))
for($i=0;$i -lt 3;$i++){
    $x=70+$i*290
    Add-Rect $s $x 240 250 150 $NAVY2 1 $kpis[$i][2] 1 | Out-Null
    Add-Text $s '—' ($x+24) 266 202 55 40 $kpis[$i][2] $true 2 | Out-Null
    Add-Text $s $kpis[$i][0] ($x+24) 326 202 22 15 $WHITE $true 2 | Out-Null
    Add-Text $s $kpis[$i][1] ($x+24) 352 202 20 13 $GRAY $false 2 | Out-Null
}
Add-Rect $s 960 240 410 330 $NAVY2 1 $CYAN 1 | Out-Null
Add-Text $s 'ACTION LISTS' 995 270 330 25 16 $CYAN $true | Out-Null
$bars=@(@('Worst feeder',0.82,$RED),@('Worst nozzle',0.66,$AMBER),@('Pickup / Placement',0.91,$GREEN),@('Stop category',0.55,$CYAN))
for($i=0;$i -lt 4;$i++){
    $y=325+$i*55
    Add-Text $s $bars[$i][0] 995 $y 150 20 14 $LIGHT $false | Out-Null
    Add-Rect $s 1150 ($y+3) 170 13 $MID 0 | Out-Null
    Add-Rect $s 1150 ($y+3) (170*$bars[$i][1]) 13 $bars[$i][2] 0 | Out-Null
}
Add-BulletList $s @('So sánh theo line và khoảng thời gian','Tập trung vào top worst feeder / nozzle','Theo dõi pickup, placement, PPM và stop time') 70 455 780 58 $LIGHT 18
Add-Text $s 'KPI placeholder — giá trị thực phụ thuộc bộ lọc.' 960 595 410 24 12 $GRAY $false 2 | Out-Null
Add-Note $s 'Dashboard kết hợp board produced, top worst feeder/nozzle, total pickup/placement, PPM và error stop time theo line.'

# 21 — Production analytics
$s=$target.Slides.Add(21,12); Add-Base $s 21 'REPORTS' 'Ba báo cáo theo dõi năng suất' 'PRODUCTION ANALYTICS'
Add-Card $s 'Board Count' 'Xu hướng output theo line, lane, thời gian và model.' 70 235 390 165 '01' $CYAN | Out-Null
Add-Card $s 'Production Report' 'Sản lượng panel / pattern theo line, lane, model và group.' 525 235 390 165 '02' $GREEN | Out-Null
Add-Card $s 'Cycle Time' 'Đánh giá hiệu suất chu trình theo line, model và group.' 980 235 390 165 '03' $AMBER | Out-Null
# mini visualizations
Add-Line $s 100 590 415 590 $GRAY 1 | Out-Null
Add-Line $s 100 590 100 455 $GRAY 1 | Out-Null
$pts=@(@(105,570),@(170,545),@(235,555),@(300,500),@(370,475));for($i=0;$i -lt $pts.Count-1;$i++){Add-Line $s $pts[$i][0] $pts[$i][1] $pts[$i+1][0] $pts[$i+1][1] $CYAN 3 | Out-Null}
for($i=0;$i -lt 4;$i++){Add-Rect $s (565+$i*75) (590-(40+$i*22)) 42 (40+$i*22) $GREEN 0 | Out-Null}
for($i=0;$i -lt 3;$i++){Add-Circle $s (1045+$i*92) (500-$i*25) (74+$i*10) $(if($i -eq 2){$AMBER}else{$BLUE}) $CYAN | Out-Null}
Add-Text $s 'Xu hướng' 100 615 315 20 13 $GRAY $false 2 | Out-Null
Add-Text $s 'So sánh output' 550 615 340 20 13 $GRAY $false 2 | Out-Null
Add-Text $s 'Cycle distribution' 1010 615 320 20 13 $GRAY $false 2 | Out-Null

# 22 — Quality drilldown
$s=$target.Slides.Add(22,12); Add-Base $s 22 'REPORTS' 'Từ triệu chứng đến đối tượng' 'QUALITY ROOT-CAUSE'
$chain=@(@('PART','Part rủi ro / scrap ratio'),@('FEEDER','Table • slot • side • reel'),@('NOZZLE','Head • nozzle • miss'))
for($i=0;$i -lt 3;$i++){
    $x=90+$i*435
    Add-Circle $s ($x+120) 255 120 $(if($i -eq 1){$BLUE}else{$NAVY2}) $CYAN | Out-Null
    Add-Text $s $chain[$i][0] ($x+130) 298 100 28 18 $WHITE $true 2 | Out-Null
    Add-Text $s $chain[$i][1] $x 415 360 45 18 $LIGHT $false 2 | Out-Null
    if($i -lt 2){Add-Line $s ($x+250) 315 ($x+405) 315 $CYAN 3 $true | Out-Null}
}
Add-Rect $s 190 530 1060 90 $BLUE 1 | Out-Null
Add-Text $s 'Pickup + Placement + Miss groups + Scrap ratio' 225 552 990 30 22 $WHITE $true 2 | Out-Null
Add-Text $s 'Cùng logic KPI, thay đổi cấp phân tích.' 225 586 990 22 15 $LIGHT $false 2 | Out-Null

# 23 — Downtime and PPM
$s=$target.Slides.Add(23,12); Add-Base $s 23 'REPORTS' 'Downtime và PPM định hướng cải tiến' 'LOSS ANALYSIS'
Add-Rect $s 70 235 785 390 $NAVY2 1 $CYAN 1 | Out-Null
Add-Text $s 'DOWNTIME GROUPS' 105 270 350 28 18 $CYAN $true | Out-Null
$dt=@(@('Pickup error',0.86,$RED),@('Recognition error',0.68,$AMBER),@('Single error stop',0.52,$CYAN),@('Trouble stop',0.39,$BLUE),@('Part exhaust',0.61,$GREEN))
for($i=0;$i -lt 5;$i++){
    $y=330+$i*53
    Add-Text $s $dt[$i][0] 105 $y 210 20 14 $LIGHT $false | Out-Null
    Add-Rect $s 320 ($y+2) 460 18 $MID 0 | Out-Null
    Add-Rect $s 320 ($y+2) (460*$dt[$i][1]) 18 $dt[$i][2] 0 | Out-Null
}
Add-Rect $s 925 235 445 390 $NAVY2 1 $GREEN 1 | Out-Null
Add-Text $s 'PPM' 960 275 150 42 34 $GREEN $true | Out-Null
Add-Text $s '= Quality misses × 1,000,000' 960 350 350 26 18 $WHITE $true | Out-Null
Add-Text $s 'Total pickup / placement' 960 390 350 26 18 $LIGHT $false | Out-Null
Add-Line $s 960 430 1315 430 $GRAY 1 | Out-Null
Add-BulletList $s @('So sánh giữa các line','Ưu tiên nhóm tổn thất lớn','Đo count và time') 960 470 350 45 $LIGHT 16
Add-Text $s 'Thanh biểu đồ chỉ minh họa layout; không phải số liệu thực.' 70 655 1300 22 12 $GRAY $false 2 | Out-Null

# 24 — Reporting UX
$s=$target.Slides.Add(24,12); Add-Base $s 24 'REPORTS' 'Một trải nghiệm phân tích lặp lại' 'FILTER • TABLE • EXPORT'
$ux=@(
    @('01','FILTER','Date • shift • line • model • part'),
    @('02','REVIEW','Chart + table chi tiết theo cùng điều kiện'),
    @('03','REFINE','Pagination + điều chỉnh độ rộng cột'),
    @('04','EXPORT','Toàn bộ kết quả đã lọc → Excel-compatible')
)
for($i=0;$i -lt 4;$i++){
    $x=70+$i*325
    Add-Rect $s $x 250 285 300 $NAVY2 1 $CYAN 1 | Out-Null
    Add-Circle $s ($x+104) 205 76 $CYAN $CYAN | Out-Null
    Add-Text $s $ux[$i][0] ($x+123) 231 38 22 14 $NAVY $true 2 | Out-Null
    Add-Text $s $ux[$i][1] ($x+24) 295 237 30 20 $WHITE $true 2 | Out-Null
    Add-Text $s $ux[$i][2] ($x+24) 355 237 90 17 $LIGHT $false | Out-Null
    if($i -lt 3){Add-Line $s ($x+285) 390 ($x+316) 390 $GRAY 2 $true | Out-Null}
}
Add-Text $s 'Cùng bộ lọc → cùng bằng chứng → chia sẻ nhanh hơn.' 70 625 1300 34 20 $CYAN $true 2 | Out-Null

# 25 — Section
$s=$target.Slides.Add(25,12); Add-SectionSlide $s 25 '04' 'DEMO & SCOPE' 'Kịch bản trình diễn dẫn người xem từ tổng quan đến bằng chứng chi tiết.' 'DEMO'

# 26 — Demo journey
$s=$target.Slides.Add(26,12); Add-Base $s 26 'DEMO' 'Demo theo hành trình ra quyết định' '8-STEP DEMO'
$demo=@(
    @('01','Mở Dashboard','Tổng quan sản lượng, chất lượng, downtime'),@('02','Chọn thời gian','Date range + shift → Apply'),
    @('03','Board Produced','So sánh output theo line / lane'),@('04','Worst equipment','Ưu tiên feeder và nozzle rủi ro'),
    @('05','Error Stop','Xác định nhóm dừng lớn'),@('06','Downtime Report','Xem count + time từng lỗi'),
    @('07','Drill-down','Lọc feeder / nozzle theo line, part'),@('08','Export Excel','Chia sẻ đúng tập dữ liệu đã lọc')
)
for($i=0;$i -lt 8;$i++){
    $row=[math]::Floor($i/4);$col=$i%4;$x=70+$col*325;$y=225+$row*225
    Add-Step $s $demo[$i][0] $demo[$i][1] $demo[$i][2] $x $y 285 | Out-Null
}
Add-Text $s 'Thông điệp xuyên suốt: từ “điều gì xảy ra?” đến “cần kiểm tra ở đâu?”.' 70 680 1300 28 18 $CYAN $true 2 | Out-Null
Add-Note $s 'Kịch bản demo: dashboard, chọn thời gian, Board Produced, top worst feeder/nozzle, Error Stop Time, Downtime Report, drill-down thiết bị và Export Excel.'

# 27 — Scope
$s=$target.Slides.Add(27,12); Add-Base $s 27 'DEMO' 'Phạm vi hiện tại đã được xác nhận' 'CURRENT SCOPE'
Add-Rect $s 70 235 820 400 $NAVY2 1 $GREEN 1.5 | Out-Null
Add-Label $s 'IN SCOPE' 105 270 120 $GREEN $NAVY | Out-Null
Add-BulletList $s @('Overall Dashboard + 8 nhóm báo cáo chi tiết','Report filter, pagination và điều chỉnh độ rộng cột','Excel-compatible export theo kết quả đã lọc','PostgreSQL retry, timeout và cancellation handling','Antiforgery validation cho unsafe MVC forms') 105 340 720 55 $LIGHT 18
Add-Rect $s 960 235 410 400 $NAVY2 1 $AMBER 1.5 | Out-Null
Add-Label $s 'NOT VERIFIED' 995 270 145 $AMBER $NAVY | Out-Null
Add-Text $s 'Login / role-based access' 995 350 330 35 22 $WHITE $true | Out-Null
Add-Text $s 'Không nằm trong phần implementation đã kiểm tra.' 995 415 330 70 17 $LIGHT $false | Out-Null
Add-Text $s 'User flow hiện mở trực tiếp tới dashboard.' 995 525 330 54 16 $CYAN $true | Out-Null

# 28 — Management takeaways
$s=$target.Slides.Add(28,12); Add-Base $s 28 'DEMO' 'Ba thông điệp dành cho quản lý' 'MANAGEMENT TAKEAWAYS'
$take=@(
    @('01','Một nguồn sự thật','MPO tập trung dữ liệu máy, chất lượng và downtime.'),
    @('02','Từ KPI đến nguyên nhân','Dashboard định hướng; report chi tiết xác minh.'),
    @('03','Sẵn sàng vận hành','Pipeline tự động, state khôi phục và export linh hoạt.')
)
for($i=0;$i -lt 3;$i++){
    $x=70+$i*430
    Add-Rect $s $x 260 390 300 $NAVY2 1 $CYAN 1.2 | Out-Null
    Add-Text $s $take[$i][0] ($x+25) 292 80 60 42 $CYAN $true | Out-Null
    Add-Text $s $take[$i][1] ($x+25) 378 340 35 22 $WHITE $true | Out-Null
    Add-Text $s $take[$i][2] ($x+25) 435 340 78 17 $LIGHT $false | Out-Null
}
Add-Text $s 'Raw machine data → operational evidence → focused action' 70 635 1300 34 21 $CYAN $true 2 | Out-Null

# 29 — Closing
$s=$target.Slides.Add(29,12); Add-Base $s 29 'DEMO' '' ''
Add-Text $s 'THANK YOU' 70 215 1300 90 60 $CYAN $true 2 | Out-Null
Add-Text $s 'Q&A' 70 335 1300 100 70 $WHITE $true 2 | Out-Null
Add-Text $s 'Manufacturing Performance Overview' 70 500 1300 36 22 $LIGHT $false 2 | Out-Null
Add-Text $s 'LNB → SFTP → Node-RED → PostgreSQL → Docker Web Portal' 70 555 1300 32 17 $CYAN $true 2 | Out-Null
Add-Text $s 'Dữ liệu đúng • Phân tích nhanh • Hành động tập trung' 70 635 1300 32 18 $WHITE $false 2 | Out-Null

# Save and close.
if(Test-Path -LiteralPath $OutputPath){Remove-Item -LiteralPath $OutputPath -Force}
$target.SaveAs($OutputPath,24)
$target.Close();$source.Close();$ppt.Quit()
[System.Runtime.InteropServices.Marshal]::ReleaseComObject($ppt) | Out-Null
Write-Output "Created: $OutputPath"
