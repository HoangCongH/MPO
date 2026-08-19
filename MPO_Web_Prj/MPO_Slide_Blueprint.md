# MPO — Slide Blueprint & Content Copy

## Design system áp dụng

- Tỷ lệ 16:9 (1440 × 810 trong template PowerPoint).
- Logo Panasonic cố định góc trên trái, có vùng an toàn riêng.
- Màu: navy template, Panasonic Blue, cyan accent, trắng, xám nhạt.
- Font: Arial; tiêu đề 34–60 pt; nội dung 14–24 pt.
- Mỗi slide có một thông điệp chính; tối đa 3–5 cụm thông tin.
- Các chart không có dữ liệu thực đều được ghi rõ là minh họa layout.

## Slide Blueprint Table

| # | Tên slide | Layout | Nội dung copy hoàn chỉnh | Visual / chart |
|---:|---|---|---|---|
| 1 | MPO | Title slide | **MPO — Manufacturing Performance Overview**<br>Tài liệu tổng hợp quá trình & thiết kế dự án<br>LNB • SFTP • Node-RED • PostgreSQL • ASP.NET Core | Giữ background công nghệ và logo từ template |
| 2 | Dữ liệu thành hành động | 3 cards | Tập trung — Một nguồn báo cáo thống nhất cho dữ liệu máy LNB.<br>Phân tích — Drill-down theo line, lane, máy, model, part, feeder và nozzle.<br>Hành động — Xác định thiết bị rủi ro, điều tra nguyên nhân và export dữ liệu. | 3 thẻ đánh số 01–03 |
| 3 | Giá trị cho vận hành | 2 × 2 cards | Một nguồn dữ liệu — Tập trung production, quality và downtime.<br>Giảm thủ công — Loại bỏ nhiều bước tổng hợp và đối chiếu file.<br>Root-cause nhanh — Truy vết từ line đến feeder, nozzle hoặc part.<br>Chia sẻ linh hoạt — Export kết quả đã lọc sang Excel-compatible. | 4 value tiles |
| 4 | Kiến trúc tổng thể | Horizontal flow | LNB → SFTP → Node-RED → PostgreSQL → Web MPO.<br>Không cần xử lý file thủ công sau khi dữ liệu vào PostgreSQL. | Pipeline 5 node |
| 5 | Data Pipeline | Section divider | Cách MPO tiếp nhận, chuẩn hóa và bảo toàn ý nghĩa của counter tích lũy. | Section number 01 |
| 6 | Luồng dữ liệu 8 bước | Process grid | 1. LNB tạo file<br>2. SFTP chuyển file<br>3. Watch phát hiện<br>4. Parse nội dung<br>5. Tính delta<br>6. Ghi database<br>7. Khôi phục state<br>8. Hiển thị web | 8 bước, 2 hàng |
| 7 | Chuẩn hóa file .u01 | 3 cards + strip | Machine identity: line, machine, lane, stage, type, version.<br>Production context: MJS, lot, product, date, output.<br>Operational counters: cycle, production, downtime, error.<br>MountPickupFeeder/Nozzle → danh sách chi tiết. | 3 nhóm dữ liệu + 2 parser rows |
| 8 | Delta counter | Formula + cases | **Delta = Current − Previous**<br>Lần đầu: Delta = Current.<br>Bình thường: Delta = Current − Previous.<br>Counter reset: Delta = Current khi Current < Previous. | Formula lớn + 3 case cards |
| 9 | Delta theo thiết bị | Two-column | Feeder delta — Pickup, mount và miss theo feeder / slot / part.<br>Nozzle delta — Pickup, mount và miss theo nozzle / head.<br>JSON payload: report_data + feeder_data + nozzle_data + raw_state. | Feeder vs nozzle |
| 10 | State recovery | Circular flow | RUN → STATE → RESTORE.<br>machine_counter_state lưu raw counter, feeder_state và nozzle_state.<br>Node-RED restart → nạp state → tiếp tục delta chính xác. | Recovery loop |
| 11 | Data Foundation | Section divider | PostgreSQL kết hợp relational schema và JSONB để vừa nhanh vừa linh hoạt. | Section number 02 |
| 12 | Mô hình dữ liệu | ER diagram | master_machines 1:N production_reports.<br>production_reports 1:N feeder_logs / nozzle_logs.<br>master_machines 1:1 machine_counter_state. | ER relationship map |
| 13 | Dữ liệu lịch sử | 2 × 2 categories | Output — output_qty, board, module, pickup, mount.<br>Time — power on, production, stop, wait, error.<br>Quality — miss, recognition, trouble.<br>Cycle — cycle_time_1/2/3. | Four metric groups |
| 14 | Feeder & nozzle detail | Comparison | feeder_logs — block, part, reel, address, pickup/mount/miss.<br>nozzle_logs — head, nozzle, address, pickup/mount/miss.<br>Ưu tiên đúng feeder, slot, reel, part, nozzle hoặc head cần kiểm tra. | Two-column comparison |
| 15 | Relational + JSONB | Two-column hybrid | Relational columns phục vụ filter, join, index và báo cáo ổn định.<br>JSONB lưu rare stats và technical state linh hoạt.<br>Foreign keys, cascade và indexes bảo toàn hiệu năng/toàn vẹn. | SQL + JSONB cards |
| 16 | Stored procedure | 5-step flow | CALL insert_full_production_report(payload JSONB).<br>1. Upsert máy<br>2. Insert report<br>3. Insert feeders<br>4. Insert nozzles<br>5. Update counter state | Transaction flow |
| 17 | Reporting App | Section divider | Ứng dụng biến dữ liệu PostgreSQL thành dashboard, drill-down và export. | Section number 03 |
| 18 | Tech stack | 2 × 3 cards | Collect: SFTP + Node-RED.<br>Store: PostgreSQL.<br>Backend: .NET 9 + ASP.NET Core MVC.<br>Data: EF Core + Npgsql.<br>Frontend: Razor + Bootstrap + Chart.js.<br>Deploy: Docker. | Layered technology cards |
| 19 | Danh mục báo cáo | 3 × 3 grid | Overall Dashboard; Board Count; Production Report; By Part; By Feeder; By Nozzle; Cycle Time; Downtime; Total Pickup / Placement. | 9 report tiles |
| 20 | Overall Dashboard | KPI cards + action list | Board Produced; PPM; Error Stop.<br>Top worst feeder / nozzle.<br>Total pickup / placement.<br>So sánh theo line và thời gian. | KPI placeholders + ranked bars |
| 21 | Theo dõi năng suất | 3 cards + mini charts | Board Count — output theo line/lane/time/model.<br>Production Report — panel/pattern theo line/lane/model/group.<br>Cycle Time — chu trình theo line/model/group. | Line, bars, distribution |
| 22 | Root-cause chất lượng | Drill-down chain | PART → FEEDER → NOZZLE.<br>Pickup + Placement + Miss groups + Scrap ratio.<br>Cùng logic KPI, thay đổi cấp phân tích. | 3-node drill-down |
| 23 | Downtime & PPM | Bar chart + formula | Pickup error; Recognition error; Single error stop; Trouble stop; Part exhaust.<br>PPM = Quality misses × 1,000,000 / Total pickup or placement.<br>So sánh line và ưu tiên nhóm tổn thất lớn. | Illustrative bars + PPM card |
| 24 | Filter, table, export | 4-step process | FILTER → REVIEW → REFINE → EXPORT.<br>Date/shift/line/model/part.<br>Chart + table cùng điều kiện.<br>Pagination + column resize.<br>Export toàn bộ kết quả đã lọc. | Four-step UX flow |
| 25 | Demo & Scope | Section divider | Kịch bản trình diễn dẫn người xem từ tổng quan đến bằng chứng chi tiết. | Section number 04 |
| 26 | Demo 8 bước | Process grid | Mở Dashboard → Chọn thời gian → Board Produced → Worst equipment → Error Stop → Downtime Report → Drill-down → Export Excel. | 8-step demo map |
| 27 | Phạm vi xác nhận | In / Not verified | In scope: dashboard, 8 report groups, filters, pagination, column resize, export, retry/timeout/cancellation, antiforgery.<br>Not verified: login / role-based access.<br>User flow mở trực tiếp tới dashboard. | Green scope panel + amber caveat |
| 28 | Thông điệp quản lý | 3 cards | Một nguồn sự thật — tập trung dữ liệu máy, chất lượng, downtime.<br>Từ KPI đến nguyên nhân — dashboard định hướng, report xác minh.<br>Sẵn sàng vận hành — pipeline tự động, state khôi phục, export linh hoạt. | Three takeaway cards |
| 29 | Q&A | Closing | **THANK YOU**<br>Q&A<br>Manufacturing Performance Overview<br>Dữ liệu đúng • Phân tích nhanh • Hành động tập trung | Minimal closing slide |

## Speaker notes trọng tâm

- Slide 2: Dashboard chỉ ra khu vực cần chú ý; report chi tiết cung cấp bằng chứng để điều tra.
- Slide 4: Nhấn mạnh toàn bộ pipeline tự động từ file raw tới web report.
- Slide 8: Delta là logic quan trọng nhất để tránh cộng lặp và xử lý counter reset.
- Slide 10: State persistence giúp delta tiếp tục chính xác sau Node-RED restart.
- Slide 12: Tách master, history, detail và technical state để cân bằng truy vấn và reliability.
- Slide 16: Node-RED chuẩn hóa dữ liệu; stored procedure quản lý việc ghi và liên kết tại một điểm.
- Slide 20: Dashboard dành cho định hướng hành động, không thay thế drill-down chi tiết.
- Slide 26: Demo phải đi theo câu hỏi “điều gì xảy ra?” → “cần kiểm tra ở đâu?”.

## Ghi chú sử dụng

- Các dấu “—” trên slide KPI là placeholder, thay bằng dữ liệu thật khi có nguồn báo cáo.
- Các thanh chart ở slide 20 và 23 chỉ minh họa layout, không đại diện số liệu sản xuất.
- File PowerPoint đã chứa speaker notes tại các slide trọng tâm.

## Bổ sung: Real Application Showcase

Bản `MPO_Panasonic_Real_App_Showcase.pptx` có 39 slide. Mười màn hình ứng dụng thật được chèn sau slide Report Portfolio:

| Slide | Màn hình thật | Thông điệp trình bày |
|---:|---|---|
| 20 | Quick Access | Một điểm truy cập tới toàn bộ nhóm dashboard và báo cáo MPO. |
| 21 | Overall Dashboard | Kết hợp output, feeder/nozzle risk và downtime trong một màn hình quản trị. |
| 22 | Board Count | So sánh board output theo giờ, line và lane. |
| 23 | Production Report | Theo dõi lịch sử sản lượng với filter và Excel export. |
| 24 | Pick-Placement by Part | Truy vết pickup, placement và miss tới cấp part. |
| 25 | Pick-Placement by Feeder | Xác định feeder, slot và vật tư liên quan tới tổn thất. |
| 26 | Pick-Placement by Nozzle | Đánh giá nozzle/head để ưu tiên bảo trì. |
| 27 | Cycle Time Report | So sánh cycle time theo line, model và group. |
| 28 | Downtime Report | Đo count và thời gian của từng nhóm dừng theo line. |
| 29 | Total Pickup / Placement | So sánh pickup, placement và PPM giữa các line. |

Các slide cũ từ Overall Dashboard trở đi được dịch thêm 10 vị trí; phần Demo & Scope bắt đầu tại slide 35.
