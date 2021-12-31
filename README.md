# ProcessManager

## Folder structure

```
management
|   state.txt
|   defaultSchedule.txt
|___31-12-2021
|   |   state.txt
|   |   schedule.txt
|   |   keylogger.txt
|   |___capture
|   |   |   14h57m30.jpg
|   |   |   15h02m30.jpg
|   |   |   ...
|___01-01-2022
|   |   state.txt
|   |   schedule.txt
|   |   keylogger.txt
|   |___capture
|   |   |   14h57m30.jpg
|   |   |   15h02m30.jpg
|   |   |   ...
|___...
```

### state.txt

Ví dụ:

isUsing:0  
lastUse:12h21m32  
totalUse:8h32m23  
scheduleModify:0

Giải thích:  
isUsing - có đang hoạt động hay không 0-không,1-có  
lastUse - lần hoạt động gần nhất  
totalUse - tổng thời gian sử dụng  
scheduleModify - có đang chỉnh sửa file schedule.txt hay không 0-không, 1-có

### schedule.txt / defaultSchedule.txt

ví dụ:

F07:30 T11:30 D0060 I0020 S0150  
F12:30 T17:30 D---- I0020 S0150  
F19:30 T23:30 D0060 I---- S0150

Giải thích:  
F - thời điểm bắt đầu (giờ:phút)  
T - thời điểm kết thúc (giờ:phút)  
D - thời gian(phút) được sử dụng trong 1 lần hoạt động, để ---- nếu không có  
I - thời gian(phút) nghỉ giữa 2 lần hoạt động, để ---- nếu không có
S - tổng thời gian(phút) hoạt động cho khoảng thời gian này, để ---- nếu không có
