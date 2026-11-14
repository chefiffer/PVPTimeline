# PvP Timeline

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [한국어](README.ko-KR.md) | [Русский](README.ru-RU.md) | [Português (Brasil)](README.pt-BR.md) | [Español](README.es-ES.md) | [ไทย](README.th-TH.md) | **Tiếng Việt** | [日本語](README.ja-JP.md) | [Deutsch](README.de-DE.md) | [Français](README.fr-FR.md)

Dòng thời gian hành động cá nhân theo thời gian thực dành cho PvP trong FINAL FANTASY XIV.

Theo dõi trên cùng một dòng thời gian việc sử dụng hành động, niệm phép, thời gian hồi chiêu chung (GCD), khóa hoạt ảnh, hoạt ảnh của chính bạn và phản hồi hiệu ứng. Bạn có thể xem lại mình đã dùng hành động lúc nào, hiệu ứng được nhận lúc nào và đã chèn những ability nào ở giữa.

![Xem trước PvP Timeline trong game](assets/pvp-timeline-preview.gif)

> Đây là bản thử nghiệm. Bản cập nhật game có thể ảnh hưởng đến việc quan sát và hiển thị; phản hồi của một số giai đoạn hành động có thể vẫn còn thiếu. Dòng thời gian thể hiện quan sát cục bộ, không phải bản dựng lại chính xác quá trình xác định đòn đánh bên trong máy chủ. Plugin không tự động sử dụng hành động.

## Cách hoạt động

![Quan sát cục bộ và luồng thông điệp](assets/how-pvp-timeline-works.png)

Sơ đồ tách các thông điệp trao đổi giữa client và máy chủ khỏi thông tin thời gian mà plugin quan sát cục bộ. PvP Timeline ghi lại yêu cầu hành động đã được chấp nhận, trạng thái niệm và hồi chiêu nhìn thấy cục bộ, khóa hoạt ảnh, quá trình phát hoạt ảnh của bạn, thông điệp hiệu ứng nhận được và ảnh chụp trạng thái hiện tại. Các rãnh màu có cùng ý nghĩa với giao diện mặc định.

Plugin không gửi thêm gói tin chiến đấu và không thể xem quá trình quyết định nội bộ của máy chủ. Một lần bấm phím không trở thành yêu cầu hành động trong game thì không thể quan sát. Mốc phản hồi hiệu ứng là lúc client cục bộ nhận thông điệp, vì vậy bao gồm độ trễ mạng, máy chủ và client.

## Tính năng

- **Tách rãnh hành động:** biểu tượng lớn ở dưới hiển thị spell, weaponskill và một số hành động đặc biệt; biểu tượng nhỏ ở trên hiển thị ability, tối đa hai hàng để giữ giao diện gọn.
- **Tách các khoảng thời gian:** GCD, thời gian niệm, khóa hoạt ảnh và phát hoạt ảnh được ghi độc lập.
- **Phản hồi hiệu ứng:** hình thoi hồng đánh dấu sát thương, hồi máu, áp dụng trạng thái hoặc phản hồi được hỗ trợ; critical/direct hit tạo ánh sáng nhẹ quanh biểu tượng gốc.
- **Hiệu ứng trễ:** vụ nổ tự động và giai đoạn tiếp theo được hỗ trợ sẽ liên kết với lần dùng ban đầu, hoặc hiện bằng biểu tượng trong suốt mà không bịa thêm một lần bấm.
- **Trạng thái của bạn:** lịch sử nhỏ gọn cho các buff đã chọn và debuff quan sát được.
- **Tùy chỉnh hiển thị:** khóa cho phép nhấp xuyên, bố cục ngang/dọc, cuộn ngược, màu, độ mờ nền, kích thước biểu tượng và tỷ lệ tổng thể.
- **Tạm dừng và xem lại:** đóng băng màn hình để kiểm tra hành động gần đây hoặc xóa bản ghi để thử lại.

Việc quan sát chỉ hoạt động trong các khu vực được hỗ trợ của **Wolves' Den Pier, Crystalline Conflict, Frontline và Rival Wings**. Nó không bật trong khu PvE thông thường; bản đồ mới cần được thêm vào danh sách hỗ trợ.

## Bắt đầu

### Cài đặt

Cần môi trường có thể chạy plugin **Dalamud API 15**. Hãy dùng bản phát hành tương thích với game và Dalamud hiện tại.

1. Cài đặt và khởi động XIVLauncher với Dalamud đã bật.
2. Mở **Dalamud Settings → Experimental**.
3. Thêm URL kho plugin tùy chỉnh:

   ```text
   https://raw.githubusercontent.com/chefiffer/dalamud-plugins/main/repo.json
   ```

4. Trong Plugin Installer, tìm **PVPTimeline** và cài đặt.
5. Nhập lệnh sau trong chat game để mở cài đặt:

   ```text
   /pvptimeline
   ```

6. Vào Wolves' Den Pier hoặc bản đồ PvP được hỗ trợ rồi dùng hành động để quan sát dòng thời gian.

### Thiết lập lần đầu

Trước tiên hãy tắt **Combat only** và thử với bia tập tại Wolves' Den Pier:

1. Mở khóa overlay trong tab **Display**, sau đó di chuyển và đổi kích thước.
2. Trong **Layout**, điều chỉnh kích thước biểu tượng GCD/ability, vị trí và độ dày thanh.
3. Khi bố cục phù hợp, bật **Locked** để cho phép nhấp xuyên và tránh thao tác nhầm.

Dòng thời gian đã khóa không thể nhấp trực tiếp. Dùng `/pvptimeline` để mở lại cài đặt; khi mở khóa, bạn cũng có thể nhấp chuột phải lên dòng thời gian.

## Đọc dòng thời gian

### Biểu tượng và thời gian

`0s` là thời điểm quan sát hiện tại; giá trị âm là quá khứ. Khi hiển thị trực tiếp, lưới và nhãn thời gian đứng yên trong khi hành động và thanh di chuyển về phía lịch sử.

Rãnh dưới có biểu tượng lớn không có nghĩa mọi hành động ở đó đều dùng GCD: một số Limit Break và hành động đặc biệt cũng xuất hiện. Thanh GCD xanh chỉ được vẽ khi thực sự quan sát thấy hồi chiêu chung.

| Hiển thị mặc định | Ý nghĩa |
| --- | --- |
| Thanh xanh lam | GCD đã quan sát |
| Thanh xanh lá | Khoảng niệm quan sát cục bộ |
| Thanh vàng sáng ngắn | Khóa hoạt ảnh, ghi riêng với GCD |
| Thanh tím | Khoảng phát hoạt ảnh của bạn đã quan sát |
| Hình thoi hồng đặc | Phản hồi trực tiếp như sát thương, hồi máu, trạng thái; với một số hành động di chuyển đã xác minh, có thể là xác nhận máy chủ |
| Hình thoi hồng rỗng | Phản hồi như hiệu ứng hình ảnh hoặc thay đổi thanh đo; riêng nó không chứng minh sát thương hay hồi máu |
| Biểu tượng trong suốt | Lần niệm chưa xác nhận hoặc hiệu ứng thực được kích hoạt mà không có bản ghi dùng thủ công tương ứng; không có nghĩa bạn bấm lại |
| Ánh sáng biểu tượng | Critical hit, direct hit hoặc critical direct hit từ thông điệp hiệu ứng thực |
| Thanh xám mảnh (mặc định tắt) | Hồi chiêu độc lập của hành động, không phải GCD |

Mọi màu đều có thể thay đổi. Hành động nhiều mục tiêu có thể cho kết quả khác nhau; biểu tượng gộp các cờ đòn đánh đã quan sát trong lần dùng đó. Ánh sáng critical direct hit không có nghĩa mọi mục tiêu đều nhận kết quả này.

Recuperate và Shadowbringer còn có **thanh hồi chiêu độc lập màu xanh ngắn** trong rãnh ability. Chúng vẫn là hồi chiêu riêng và không trở thành hành động GCD.

### Vì sao đôi khi thiếu thanh tím hoặc hình thoi?

Thanh tím dựa trên quan sát thực tế hoạt ảnh của bạn. Thiếu quan sát, liên kết không chắc chắn, tạm dừng hoặc bị hành động khác thay thế có thể làm thanh mất, đứt hoặc ngắn lại. Dấu thay thế chỉ hiện khi xác nhận một hành động khác đã ghi đè hoạt ảnh; kết thúc bình thường không thêm hình thoi cam.

Hình thoi hồng cũng cần phản hồi hiệu ứng thực. Nếu phản hồi không có, không thể liên kết đáng tin cậy hoặc thuộc giai đoạn chưa hỗ trợ, plugin sẽ không đoán dấu dựa trên mô tả.

**Thiếu thanh hoặc dấu không chứng minh hành động thất bại.** Plugin không dùng độ dài GCD hoặc khóa hoạt ảnh thay cho thời gian phát hoạt ảnh.

## Đọc các hàng trạng thái

Các hàng chủ yếu hiển thị **trạng thái thực sự đang có trên nhân vật của bạn**. Nguồn có thể là bạn, đồng minh hoặc kẻ địch. Việc đồng minh dùng một hành động không tự nó chứng minh rằng bạn đã nhận hiệu ứng.

Buff dùng biểu tượng bắt đầu/kết thúc nối bằng thanh:

- Biểu tượng bắt đầu di chuyển theo thời gian và không bị nhân đôi ở mép trái trước khi đến đó.
- Khi tới mép, nó vào hàng chờ; nếu biết thời gian còn lại, hiển thị tối đa hai chữ số bên dưới, không có đơn vị và không hiện số 0.
- Trạng thái kết thúc sớm hơn được đặt xa hơn về bên phải.
- **Biểu tượng có gạch đỏ nhỏ bên dưới là điểm kết thúc.** Nếu biết thời điểm dự kiến, nó có thể xuất hiện trong vùng tương lai trước khi trạng thái hết; gạch đỏ không có nghĩa trạng thái đã kết thúc.
- Sau khi hết, điểm kết thúc di chuyển về quá khứ và đoạn biến mất khi gặp biểu tượng bắt đầu.

Trạng thái từ các nguồn khác nhau có thể được ghi riêng. Làm mới, cộng dồn và xóa tuân theo thay đổi thực trong danh sách trạng thái; plugin không tự tạo số cộng dồn từ số lần dùng hành động.

Không phải mọi trạng thái đều được hiển thị. Plugin lọc các buff job đã được xem xét và loại một số chỉ báo lựa chọn role action, trạng thái sẵn sàng hoặc tiếp nối. Đây chưa phải cơ sở dữ liệu hoàn chỉnh; hãy mở issue nếu muốn thêm buff.

**Plugin chỉ hiển thị trạng thái và đếm ngược có thể xác nhận.** Nếu không đọc được thời gian còn lại thì không hiện đếm ngược. Các điều kiện như “chỉ có hiệu lực khi đang chiến đấu” hiện chưa được đánh giá riêng.

## Cài đặt thường dùng

| Vị trí | Tùy chọn |
| --- | --- |
| Đầu trang cài đặt | Ngôn ngữ, tỷ lệ tổng thể, tạm dừng/tiếp tục, xóa bản ghi |
| Display | Bật, khóa nhấp xuyên, chỉ trong chiến đấu, ẩn khi cutscene, hàng trạng thái, quan sát hoạt ảnh native, hướng và nền |
| Layout | Kích thước biểu tượng, chiều cao/khoảng hàng, vị trí và độ dày thanh GCD/ability, vị trí niệm/hoạt ảnh |
| Tracks | Hiện và màu các rãnh thời gian, hồi chiêu độc lập ngắn, ability và auto-attack |
| Time | Pixel mỗi giây, phạm vi lịch sử, vùng tương lai, lưới và số phần chia mỗi giây |
| Feedback | Màu ánh sáng critical/direct hit, dấu thay thế hoạt ảnh, độ mờ biểu tượng hiệu ứng |

Nhập giá trị trực tiếp hoặc đặt chuột trên trường số rồi dùng bánh xe. Tỷ lệ tổng thể hỗ trợ **0,2–2,0×**, bao gồm chữ trên dòng thời gian.

Giao diện hỗ trợ tiếng Trung giản thể/phồn thể, tiếng Anh, Đức, Pháp và Nhật. Lần đầu nó theo ngôn ngữ UI của Dalamud và cũng có thể chọn thủ công. Tên hành động và trạng thái dùng tài nguyên đa ngôn ngữ hiện có, và có thể quay về tiếng Anh, ngôn ngữ client hoặc ID khi thiếu. Tên người chơi không được dịch. Sửa `ui-localization.txt` cạnh DLL rồi tải lại plugin để chỉnh văn bản UI; tệp này không điều khiển tên hành động hoặc trạng thái.

## Câu hỏi thường gặp

### Không thấy dòng thời gian trong game

Kiểm tra bạn đang ở khu PvP được hỗ trợ, plugin đã bật, cùng **Combat only** và **Hide in cutscene**. Nếu vẫn không thấy, kiểm tra trạng thái trong **Diagnostics**. Bản cập nhật game có thể cần bản sửa tương thích.

### Vì sao bấm liên tục nhưng không hiện mọi lần?

Dòng thời gian hiển thị việc dùng hành động có thể quan sát, không phải mọi cú nhấp vật lý. Lần bấm không tạo yêu cầu game không thể quan sát và các lần thử thất bại không tạo chuỗi biểu tượng.

### Vì sao sát thương tự kích hoạt dùng biểu tượng trong suốt?

Plugin có thể nhận hiệu ứng do bạn gây ra nhưng không có bản ghi dùng thủ công tương ứng. Vụ nổ tự động gây sát thương thật mà không cần bấm thêm. Giai đoạn liên kết đáng tin cậy cập nhật hành động gốc; hiệu ứng thực khác có thể giữ biểu tượng trong suốt.

### Mốc phản hồi có phải thời điểm máy chủ xác định đòn đánh không?

Không. Đó là lúc client của bạn nhận thông điệp và bao gồm mọi độ trễ. Hoạt ảnh cũng không phải ảnh chụp đòn đánh của máy chủ, và kết thúc niệm không xác định thời điểm sớm nhất có thể di chuyển.

### Plugin có tải dữ liệu lên hoặc lưu toàn bộ trận đấu không?

Không tự động. Plugin chỉ giữ bản ghi gần đây có giới hạn trong bộ nhớ và đặt lại khi đổi khu vực hoặc xóa. Chẩn đoán chỉ được sao chép vào clipboard cục bộ khi nhấn nút, có thể chứa entity ID, action/status ID và thời gian; hãy kiểm tra trước khi chia sẻ.

## Giới hạn và lưu ý

- Cửa sổ liên kết phản hồi cho mỗi lần dùng tối đa **14 giây**; phản hồi trễ hoặc mơ hồ không bị ép gán.
- Lịch sử giới hạn 256 mục và 120 giây; các trạng thái cũng có giới hạn.
- Quan sát hoạt ảnh tập trung vào nhân vật của bạn và không dựng lại đầy đủ hoạt ảnh phía mục tiêu, đạn bay hoặc hiệu ứng mặt đất.
- Quan sát native dùng giao diện nội bộ của game; cập nhật có thể làm hỏng hoặc giảm ổn định. Tắt nó không nhất thiết gỡ mọi observation hook.
- Đây là plugin bên thứ ba không chính thức, không đảm bảo an toàn tài khoản hoặc không bao giờ crash. Hãy hiểu quy định của game và nền tảng.

## Báo cáo vấn đề

Hãy cung cấp phiên bản plugin, khu vực/phiên bản game, ngôn ngữ, job và hành động, kích hoạt thủ công/tự động, kết quả mong đợi/thực tế và bước tái hiện. Nếu có thể, đính kèm ảnh hoặc GIF ngắn, chẩn đoán hiệu ứng cho lỗi liên kết và log Dalamud đã loại thông tin riêng tư cho lỗi tải. Không chia sẻ token, mật khẩu hoặc log cá nhân không liên quan.

## Lời cảm ơn

Cách trình bày, tương tác khóa và tùy chỉnh được tham khảo từ:

- [ActionTimelineEx](https://github.com/Ted-Jin-Lab/ActionTimelineEx)
- [ActionTimelineReplacement](https://github.com/Loskh/ActionTimelineReplacement)

PvP Timeline không phải bản chính thức của các dự án này. Biểu tượng game và tài liệu liên quan thuộc về chủ sở hữu quyền tương ứng.

## Giấy phép

PvP Timeline được cấp phép theo GNU Affero General Public License v3.0 (AGPL-3.0). Xem [LICENSE](https://github.com/chefiffer/PVPTimeline/blob/main/LICENSE).

Thông báo bản quyền và giấy phép của dự án gốc dùng MIT được giữ trong [THIRD-PARTY-NOTICES.txt](https://github.com/chefiffer/PVPTimeline/blob/main/THIRD-PARTY-NOTICES.txt).
