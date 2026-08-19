param(
    [string]$InputPath = 'C:\Users\70R7056\Downloads\MPO_Project.pptx',
    [string]$OutputPath = 'C:\Intern\mpo_webproject\MPO-main\MPO\MPO_Web_Prj\MPO_Project_English.pptx'
)

$ErrorActionPreference='Stop'

$map=@{
    'Dữ liệu máy → Hiệu suất → Hành động'='Machine Data → Performance → Action'
    'Hệ thống báo cáo sản xuất tập trung cho quản lý, kỹ thuật và bảo trì.'='Centralized production reporting for management, engineering, and maintenance.'
    'TÀI LIỆU TỔNG HỢP QUÁ TRÌNH & THIẾT KẾ DỰ ÁN'='PROJECT PROCESS & DESIGN OVERVIEW'
    'MPO là hệ thống web báo cáo hiệu suất sản xuất, chuyển dữ liệu máy LNB thô thành dashboard và báo cáo phục vụ vận hành hằng ngày.'='MPO is a web-based manufacturing performance reporting system that turns raw LNB machine data into dashboards and reports for daily operations.'

    'MPO biến dữ liệu máy thành hành động'='MPO Turns Machine Data into Action'
    'Một luồng tự động hợp nhất sản lượng, chất lượng và downtime — từ file .u01 đến dashboard quản trị.'='One automated flow unifies output, quality, and downtime—from .u01 files to the management dashboard.'
    'Tập trung'='Centralize'
    'Một nguồn báo cáo thống nhất cho dữ liệu máy LNB.'='One unified reporting source for LNB machine data.'
    'Phân tích'='Analyze'
    'Drill-down theo line, lane, máy, model, part, feeder và nozzle.'='Drill down by line, lane, machine, model, part, feeder, and nozzle.'
    'Hành động'='Act'
    'Xác định thiết bị rủi ro, điều tra nguyên nhân và export dữ liệu.'='Identify at-risk equipment, investigate root causes, and export data.'
    'Kết quả: dữ liệu thô trở thành bằng chứng có thể sử dụng trong vận hành hằng ngày.'='Outcome: raw data becomes evidence for daily operations.'
    'Dashboard chỉ ra khu vực cần chú ý; các báo cáo chi tiết cung cấp bằng chứng để điều tra.'='The dashboard highlights areas that need attention; detailed reports provide evidence for investigation.'

    'Bốn giá trị trực tiếp cho vận hành'='Four Direct Operational Benefits'
    'Một nguồn dữ liệu'='One Data Source'
    'Tập trung production, quality và downtime.'='Centralize production, quality, and downtime.'
    'Giảm thủ công'='Reduce Manual Work'
    'Loại bỏ nhiều bước tổng hợp và đối chiếu file.'='Eliminate multiple consolidation and file-reconciliation steps.'
    'Root-cause nhanh'='Faster Root-Cause Analysis'
    'Truy vết từ line đến feeder, nozzle hoặc part.'='Trace issues from line to feeder, nozzle, or part.'
    'Chia sẻ linh hoạt'='Flexible Sharing'
    'Export kết quả đã lọc sang Excel-compatible.'='Export filtered results in an Excel-compatible format.'

    'Một kiến trúc, một dòng dữ liệu'='One Architecture, One Data Flow'
    'Không cần xử lý file thủ công sau khi dữ liệu vào PostgreSQL.'='No manual file handling is needed after data enters PostgreSQL.'
    'Luồng tổng thể: LNB raw data, SFTP, incoming folder, Node-RED Watch, parse và delta, PostgreSQL, ASP.NET Core và web frontend trong Docker.'='Overall flow: LNB raw data, SFTP, incoming folder, Node-RED Watch, parsing and delta calculation, PostgreSQL, ASP.NET Core, and the web frontend in Docker.'

    'Cách MPO tiếp nhận, chuẩn hóa và bảo toàn ý nghĩa của counter tích lũy.'='How MPO ingests, standardizes, and preserves cumulative counter meaning.'
    'Từ dữ liệu thô đến quyết định vận hành'='From Raw Data to Operational Decisions'

    'Luồng dữ liệu tự động qua 8 bước'='Automated Data Flow in 8 Steps'
    'LNB tạo file'='LNB Generates File'
    'SFTP chuyển file'='SFTP Transfers File'
    'Watch phát hiện'='Watch Detects File'
    'lọc file hợp lệ'='filter valid files'
    'Parse nội dung'='Parse Content'
    'Tính delta'='Calculate Delta'
    'Ghi database'='Write to Database'
    'Khôi phục state'='Restore State'
    'sau restart'='after restart'
    'Hiển thị web'='Display on Web'
    'Điểm kiểm soát trọng yếu: delta counter + state persistence.'='Critical control: delta counters + state persistence.'
    'Toàn bộ quy trình là tự động, từ file raw đến báo cáo. Sau khi dữ liệu được ghi vào PostgreSQL, người dùng thao tác trực tiếp trên web dashboard.'='The entire process is automated, from raw files to reports. After data is stored in PostgreSQL, users work directly through the web dashboard.'

    'Node-RED chuẩn hóa file .u01'='Node-RED Standardizes .u01 Files'
    '→ danh sách feeder theo part, address, reel và miss counters'='→ feeder list by part, address, reel, and miss counters'
    '→ danh sách nozzle theo head, address, name và miss counters'='→ nozzle list by head, address, name, and miss counters'

    'Delta counter bảo toàn sản lượng thực'='Delta Counters Preserve Actual Output'
    'Áp dụng cho'='Applied to'
    'Lần đầu'='First Reading'
    'Không có state trước đó'='No previous state'
    'Bình thường'='Normal'
    'Chỉ ghi phần phát sinh'='Record only the increment'
    'Tránh cộng lặp • Xử lý reset máy • Duy trì tính đúng theo kỳ'='Prevent double counting • Handle machine resets • Maintain period accuracy'
    'Tránh cộng lặp • Xử lý reset máy • Duy trì tính chính xác theo chu kỳ'='Prevent double counting • Handle machine resets • Maintain cycle accuracy'
    'Đây là điểm quan trọng nhất của flow. Máy gửi số tích lũy, còn hệ thống cần số phát sinh thực tế trong kỳ. Delta ngăn cộng lặp và xử lý trường hợp reset counter.'='This is the most important point in the flow. Machines send cumulative values, while the system needs the actual increment for each period. Delta calculation prevents double counting and handles counter resets.'

    'Chi tiết thiết bị cũng được tính delta'='Equipment Details Also Use Delta'
    'Pickup, mount và các nhóm miss theo từng feeder / slot / part.'='Pickup, mount, and miss groups by feeder / slot / part.'
    'Pickup, mount và các nhóm miss theo từng nozzle / head.'='Pickup, mount, and miss groups by nozzle / head.'
    'JSON payload thống nhất → report_data + feeder_data + nozzle_data + raw_state'='Unified JSON payload → report_data + feeder_data + nozzle_data + raw_state'

    'Restart không làm mất ngữ cảnh delta'='Restart Does Not Lose Delta Context'
    'machine_counter_state lưu raw counter gần nhất, feeder_state và nozzle_state JSONB.'='machine_counter_state stores the latest raw counters plus feeder_state and nozzle_state JSONB.'
    'Node-RED khởi động → nạp state vào flow context → tiếp tục tính delta chính xác.'='Node-RED starts → loads state into flow context → continues accurate delta calculation.'
    'Khi Node-RED khởi động, flow truy vấn machine_counter_state và nạp lại counter của từng máy, feeder và nozzle vào flow context.'='When Node-RED starts, the flow queries machine_counter_state and reloads the counters for each machine, feeder, and nozzle into the flow context.'

    'PostgreSQL kết hợp relational schema và JSONB để vừa nhanh vừa linh hoạt.'='PostgreSQL combines relational schemas and JSONB for speed and flexibility.'
    'Cấu trúc database'='Database Structure'

    'Bốn bảng tạo xương sống dữ liệu'='Four Tables Form the Data Backbone'
    'Danh mục máy chuẩn hóa'='Standardized machine catalog'
    'Lịch sử báo cáo trung tâm'='Central report history'
    'Chi tiết feeder theo report'='Feeder details by report'
    'Chi tiết nozzle theo report'='Nozzle details by report'
    'Raw state gần nhất'='Latest raw state'
    'Database tách master, lịch sử báo cáo, chi tiết feeder/nozzle và technical state. Quan hệ rõ ràng giúp báo cáo nhanh nhưng vẫn khôi phục delta sau restart.'='The database separates master data, report history, feeder/nozzle details, and technical state. Clear relationships support fast reporting while preserving delta recovery after restart.'

    'production_reports: dữ liệu lịch sử'='production_reports: Historical Data'
    'Cấp phân tích: machine • report date • MJS • product • lot • file'='Analysis dimensions: machine • report date • MJS • product • lot • file'

    'Feeder và nozzle mở khóa root-cause'='Feeder and Nozzle Enable Root-Cause Analysis'
    'Pickup, mount và miss counters'='Pickup, mount, and miss counters'
    'Nhiều feeder trên mỗi report'='Multiple feeders per report'
    'Nhiều nozzle trên mỗi report'='Multiple nozzles per report'
    'Kết quả: ưu tiên đúng feeder, slot, reel, part, nozzle hoặc head cần kiểm tra.'='Outcome: prioritize the exact feeder, slot, reel, part, nozzle, or head to inspect.'

    'Relational + JSONB: nhanh và linh hoạt'='Relational + JSONB: Fast and Flexible'
    'Các trường cốt lõi phục vụ filter, join, index và báo cáo ổn định.'='Core fields support stable filtering, joins, indexes, and reporting.'
    'Rare time stats, count stats, feeder state và nozzle state linh hoạt.'='Rare time stats, count stats, feeder state, and nozzle state remain flexible.'
    'Foreign keys bảo toàn quan hệ'='Foreign keys preserve relationships'
    'ON DELETE CASCADE cho detail'='ON DELETE CASCADE for details'
    'Index theo date, machine, MJS, lot, report và address'='Indexes by date, machine, MJS, lot, report, and address'

    'Một điểm ghi dữ liệu duy nhất'='One Centralized Write Point'
    'Upsert máy'='Upsert Machine'
    'Node-RED chuẩn hóa dữ liệu; PostgreSQL đảm nhiệm liên kết, ghi và cập nhật state.'='Node-RED standardizes data; PostgreSQL handles relationships, writes, and state updates.'
    'Stored procedure là điểm ghi tập trung: đọc machine_id, upsert master, insert report, duyệt feeders/nozzles và cập nhật machine_counter_state.'='The stored procedure is the centralized write point: read machine_id, upsert master data, insert the report, process feeders/nozzles, and update machine_counter_state.'

    'Ứng dụng biến dữ liệu PostgreSQL thành dashboard, drill-down và export.'='The application turns PostgreSQL data into dashboards, drill-downs, and exports.'
    'Tech stack tối ưu cho báo cáo web'='Tech Stack Optimized for Web Reporting'

    'Chín góc nhìn trên cùng một dữ liệu'='Nine Views of the Same Data'
    'Dashboard để định hướng → report chi tiết để chứng minh và hành động.'='Dashboard for direction → detailed reports for evidence and action.'

    'Dashboard ưu tiên điều cần chú ý'='Dashboard Prioritizes What Matters'
    'Output theo line'='Output by line'
    'Chất lượng tương đối'='Relative quality'
    'Thời gian dừng lỗi'='Error-stop duration'
    'So sánh theo line và khoảng thời gian'='Compare by line and time range'
    'Tập trung vào top worst feeder / nozzle'='Focus on top worst feeder / nozzle'
    'Theo dõi pickup, placement, PPM và stop time'='Track pickup, placement, PPM, and stop time'
    'KPI placeholder — giá trị thực phụ thuộc bộ lọc.'='KPI placeholder — actual values depend on filters.'
    'Dashboard kết hợp board produced, top worst feeder/nozzle, total pickup/placement, PPM và error stop time theo line.'='The dashboard combines board produced, top worst feeder/nozzle, total pickup/placement, PPM, and error-stop time by line.'

    'Ba báo cáo theo dõi năng suất'='Three Reports Track Productivity'
    'Xu hướng output theo line, lane, thời gian và model.'='Output trends by line, lane, time, and model.'
    'Sản lượng panel / pattern theo line, lane, model và group.'='Panel / pattern output by line, lane, model, and group.'
    'Đánh giá hiệu suất chu trình theo line, model và group.'='Evaluate cycle performance by line, model, and group.'
    'Xu hướng'='Trend'
    'So sánh output'='Output comparison'

    'Từ triệu chứng đến đối tượng'='From Symptoms to the Source'
    'Part rủi ro / scrap ratio'='At-risk part / scrap ratio'
    'Cùng logic KPI, thay đổi cấp phân tích.'='Same KPI logic, different analysis level.'

    'Downtime và PPM định hướng cải tiến'='Downtime and PPM Guide Improvement'
    'So sánh giữa các line'='Compare across lines'
    'Ưu tiên nhóm tổn thất lớn'='Prioritize the largest losses'
    'Đo count và time'='Measure count and time'
    'Thanh biểu đồ chỉ minh họa layout; không phải số liệu thực.'='Bars illustrate layout only; they are not actual data.'

    'Một trải nghiệm phân tích lặp lại'='A Repeatable Analysis Experience'
    'Chart + table chi tiết theo cùng điều kiện'='Detailed chart + table under the same criteria'
    'Pagination + điều chỉnh độ rộng cột'='Pagination + adjustable column widths'
    'Toàn bộ kết quả đã lọc → Excel-compatible'='All filtered results → Excel-compatible'
    'Cùng bộ lọc → cùng bằng chứng → chia sẻ nhanh hơn.'='Same filters → same evidence → faster sharing.'

    'Kịch bản trình diễn dẫn người xem từ tổng quan đến bằng chứng chi tiết.'='A demonstration journey from overview to detailed evidence.'
    'Demo theo hành trình ra quyết định'='Demo Along the Decision Journey'
    'Mở Dashboard'='Open Dashboard'
    'Tổng quan sản lượng, chất lượng, downtime'='Overview of output, quality, and downtime'
    'Chọn thời gian'='Select Time'
    'So sánh output theo line / lane'='Compare output by line / lane'
    'Ưu tiên feeder và nozzle rủi ro'='Prioritize at-risk feeders and nozzles'
    'Xác định nhóm dừng lớn'='Identify the largest stop category'
    'Xem count + time từng lỗi'='View count + time for each error'
    'Lọc feeder / nozzle theo line, part'='Filter feeder / nozzle by line and part'
    'Chia sẻ đúng tập dữ liệu đã lọc'='Share the exact filtered dataset'
    'Thông điệp xuyên suốt: từ “điều gì xảy ra?” đến “cần kiểm tra ở đâu?”.'='Core message: from “what happened?” to “where should we inspect?”'
    'Kịch bản demo: dashboard, chọn thời gian, Board Produced, top worst feeder/nozzle, Error Stop Time, Downtime Report, drill-down thiết bị và Export Excel.'='Demo journey: dashboard, time selection, Board Produced, top worst feeder/nozzle, Error Stop Time, Downtime Report, equipment drill-down, and Excel export.'

    'Phạm vi hiện tại đã được xác nhận'='Current Scope Confirmed'
    'Overall Dashboard + 8 nhóm báo cáo chi tiết'='Overall Dashboard + 8 detailed report groups'
    'Report filter, pagination và điều chỉnh độ rộng cột'='Report filters, pagination, and adjustable column widths'
    'Excel-compatible export theo kết quả đã lọc'='Excel-compatible export of filtered results'
    'PostgreSQL retry, timeout và cancellation handling'='PostgreSQL retry, timeout, and cancellation handling'
    'Antiforgery validation cho unsafe MVC forms'='Antiforgery validation for unsafe MVC forms'
    'Không nằm trong phần implementation đã kiểm tra.'='Not included in the verified implementation.'
    'User flow hiện mở trực tiếp tới dashboard.'='The current user flow opens directly to the dashboard.'

    'Ba thông điệp dành cho quản lý'='Three Management Takeaways'
    'Một nguồn sự thật'='One Source of Truth'
    'MPO tập trung dữ liệu máy, chất lượng và downtime.'='MPO centralizes machine, quality, and downtime data.'
    'Từ KPI đến nguyên nhân'='From KPIs to Root Causes'
    'Dashboard định hướng; report chi tiết xác minh.'='The dashboard guides; detailed reports verify.'
    'Sẵn sàng vận hành'='Operationally Ready'
    'Pipeline tự động, state khôi phục và export linh hoạt.'='Automated pipeline, restored state, and flexible export.'

    'Dữ liệu đúng • Phân tích nhanh • Hành động tập trung'='Accurate Data • Faster Analysis • Focused Action'
}

$ppt=New-Object -ComObject PowerPoint.Application
$ppt.Visible=-1
$source=$ppt.Presentations.Open($InputPath,$true,$false,$false)
if(Test-Path -LiteralPath $OutputPath){Remove-Item -LiteralPath $OutputPath -Force}
$source.SaveAs($OutputPath,24);$source.Close()
$deck=$ppt.Presentations.Open($OutputPath,$false,$false,$false)

$translated=0
foreach($slide in $deck.Slides){
    foreach($shape in $slide.Shapes){
        if($shape.HasTextFrame -eq -1 -and $shape.TextFrame.HasText -eq -1){
            $key=$shape.TextFrame.TextRange.Text.Trim()
            if($map.ContainsKey($key)){
                $shape.TextFrame.TextRange.Text=$map[$key]
                $translated++
            }
        }
        if($shape.HasTable -eq -1){
            for($r=1;$r -le $shape.Table.Rows.Count;$r++){
                for($c=1;$c -le $shape.Table.Columns.Count;$c++){
                    $cell=$shape.Table.Cell($r,$c).Shape.TextFrame.TextRange
                    $key=$cell.Text.Trim()
                    if($map.ContainsKey($key)){$cell.Text=$map[$key];$translated++}
                }
            }
        }
    }
    try{
        $notes=$slide.NotesPage.Shapes.Placeholders.Item(2).TextFrame.TextRange
        $key=$notes.Text.Trim()
        if($map.ContainsKey($key)){$notes.Text=$map[$key];$translated++}
    }catch{}
}

$deck.Save();$deck.Close();$ppt.Quit()
[System.Runtime.InteropServices.Marshal]::ReleaseComObject($ppt)|Out-Null
Write-Output "Translated text blocks: $translated"
Write-Output "Created: $OutputPath"
