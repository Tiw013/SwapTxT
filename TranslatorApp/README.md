# 🌸 TranslatorApp
### แปลภาษาอัตโนมัติ ด้วยคีย์ลัด — สำหรับ Windows 10/11

---

## ✨ คุณสมบัติหลัก

| ฟีเจอร์ | รายละเอียด |
|---------|-----------|
| ⌨️ **Global Hotkey** | Ctrl + Space (ปรับได้) ทำงานทุกแอป |
| 🔍 **Google Translate** | ฟรี ไม่ต้อง API Key เร็วทันใจ |
| 🤖 **AI Mode (BYOK)** | รองรับ OpenAI GPT-4o-mini และ Google Gemini |
| 🔄 **Auto Replace** | แทนที่ข้อความที่เลือกด้วยคำแปลทันที |
| 🌸 **System Tray** | ซ่อนตัวในถาดระบบ ไม่รบกวนหน้าจอ |
| 🌐 **Auto Language** | ตรวจจับภาษาอัตโนมัติ (ไทย↔อังกฤษ) |
| 🔔 **Toast Notification** | แจ้งผลการแปลแบบ popup สวยงาม |

---

## 🏗️ โครงสร้างโปรเจกต์

```
TranslatorApp/
├── TranslatorApp.csproj      # Project file (.NET 8 WPF)
├── app.manifest               # DPI Awareness & Compatibility
├── App.xaml / App.xaml.cs    # Application entry + Global styles
├── MainWindow.xaml/.cs        # Hidden window (Hotkey host + Tray)
├── SettingsWindow.xaml/.cs    # Settings UI (3 tabs)
├── ToastNotification.cs       # Custom animated toast popup
├── HelpWindow.cs              # Usage guide window
└── Core/
    ├── GlobalKeyboardHook.cs  # Win32 LowLevel keyboard hook
    ├── HotkeyManager.cs       # RegisterHotKey Win32 API
    ├── ClipboardTranslator.cs # Copy → Translate → Paste core
    ├── TranslationEngines.cs  # Google, OpenAI, Gemini engines
    └── AppSettings.cs         # JSON settings + Registry startup
```

---

## 🚀 วิธีติดตั้งและ Build

### ความต้องการ
- **Visual Studio 2022** (Community ฟรีได้) หรือ **VS Code + .NET SDK**
- **.NET 8 SDK** — ดาวน์โหลดที่ [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
- **Windows 10 หรือ 11** (64-bit)

### ขั้นตอน Build

```bash
# 1. Clone หรือวางไฟล์ในโฟลเดอร์
cd TranslatorApp

# 2. Restore NuGet packages
dotnet restore

# 3. Build แบบ Debug (ทดสอบ)
dotnet build

# 4. Run
dotnet run

# 5. Build แบบ Release (แจกจ่าย)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

> 📁 ไฟล์ .exe จะอยู่ใน `bin/Release/net8.0-windows/win-x64/publish/`

### วิธี Build ใน Visual Studio
1. เปิด `TranslatorApp.csproj` ด้วย Visual Studio
2. กด `F5` เพื่อ Run หรือ `Ctrl+Shift+B` เพื่อ Build

---

## ⚙️ วิธีใช้งาน

1. **เปิดโปรแกรม** → ไอคอน 🌸 จะปรากฏที่ System Tray มุมขวาล่าง
2. **เลือกข้อความ** ในแอปใดก็ได้ (คลุมดำ)
3. **กด `Ctrl + Space`** → โปรแกรมแปลและแทนที่อัตโนมัติ
4. **คลิกซ้าย** ที่ไอคอน Tray เพื่อเปิดหน้าตั้งค่า
5. **คลิกขวา** ที่ไอคอน Tray เพื่อเมนู

---

## 🤖 การตั้งค่า AI Mode

### OpenAI GPT (แนะนำ)
1. ไปที่ [platform.openai.com/api-keys](https://platform.openai.com/api-keys)
2. สร้าง API Key ใหม่
3. วางใน Settings → แปลภาษา → API Key
4. กดทดสอบก่อนบันทึก ✅

### Google Gemini (ฟรี!)
1. ไปที่ [aistudio.google.com/app/apikey](https://aistudio.google.com/app/apikey)
2. สร้าง API Key (ฟรี มี quota รายวัน)
3. เลือก Provider: Gemini แล้ววาง Key

---

## 🎨 การปรับแต่งเพิ่มเติม

### เพิ่ม Auto-Update
1. ติดตั้ง NuGet: `Install-Package AutoUpdater.NET`
2. Uncomment ใน `TranslatorApp.csproj`
3. เพิ่มในไฟล์ `MainWindow_Loaded`:
```csharp
AutoUpdater.Start("https://yourserver.com/update.xml");
```

### เปลี่ยนไอคอน Mascot
1. วางไฟล์ `icon.ico` ใน `Resources/` folder
2. Build ใหม่

### เปลี่ยน URL Donate
เปิด `Core/AppSettings.cs` แก้บรรทัด:
```csharp
public string DonateUrl { get; set; } = "https://promptpay.io/YOUR_NUMBER";
```

---

## 🐛 แก้ปัญหาที่พบบ่อย

| ปัญหา | วิธีแก้ |
|-------|--------|
| คีย์ลัดไม่ทำงาน | ตรวจสอบว่าไม่มีแอปอื่นใช้ Ctrl+Space อยู่ |
| แปลไม่ได้ (Google) | ตรวจสอบอินเทอร์เน็ต |
| AI Error 401 | API Key ผิดหรือหมดอายุ สร้างใหม่ |
| Clipboard ไม่ทำงาน | บางแอปป้องกัน Clipboard access |
| โปรแกรมไม่ขึ้น Tray | รอ 2-3 วินาที หรือดูใน Task Manager |

---

## 📝 License

สำหรับการใช้งานส่วนตัวและการศึกษา

---

*Made with 💕 — TranslatorApp v1.0.0*
