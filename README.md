# BarClip API 🎥✂️

BarClip is a lightweight API that trims weightlifting videos by tracking when the bar moves.

---

## 📦 Features

✅ **Upload And Trim Video**
Upload video file directly and receive a link to the automatically trimmed video.

✅ **Re-trim Videos**  
Trim started or finished too early or too late? Re-trim existing videos with updated start/finish points. (Make sure not to change the name of the file first)

✅ **Azure Blob Storage Integration**  
Efficiently store large video files securely.

✅ **SAS URL Playback**  
Stream videos directly from Azure using secure, expiring URLs.

---

## 💻 Tech Stack

- **API**: ASP.NET Core
- **MLM**: YOLOv8
- **Database**: EF Core + SQL Server  
- **Video Processing**: FFmpeg (via FFMpegCore)  
- **Cloud Storage**: Azure Blob Storage  


