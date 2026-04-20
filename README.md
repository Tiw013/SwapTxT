# SwapTxT Installer v1.0.2 🚀

ยินดีต้อนรับสู่ตัวติดตั้ง **SwapTxT** — โปรแกรมช่วยแปลภาษาอัจฉริยะที่ช่วยให้คุณแปลข้อความได้ทันทีในทุกโปรแกรมบน Windows!

## 📥 วิธีการติดตั้ง (Installation)
1. ดาวน์โหลดไฟล์ `install-SwapTxT_V1-0-2.exe` จากหน้า [Releases](https://github.com/Tiw013/SwapTxT/releases)
2. ย้ายไฟล์ไปไว้ในโฟลเดอร์ที่คุณต้องการ (เช่น `C:\Users\YourName\Documents\SwapTxT`)
3. ดับเบิลคลิกเพื่อเริ่มใช้งานได้ทันที (Portable Application)

## 🔧 Features

### Translation Engines
| Engine | รายละเอียด |
|--------|------------|
| **Google Translate** (default) | ฟรี ไม่ต้อง API key |
| **OpenRouter** | ใช้ key เดียว เข้าถึง model หลายร้อยตัว |
| **OpenAI** | GPT-4o, GPT-4o-mini |
| **Google Gemini** | Gemini 1.5 Flash/Pro |

### Auto Detect Mode
เมื่อเลือก `TH→EN` และข้อความใน field คือ:
```
I'm หนาวมากเลย
```
โปรแกรมจะ auto-detect `หนาวมากเลย` (block ภาษาไทยล่าสุด) และแปลเฉพาะส่วนนั้น ผลลัพธ์:
```
I'm very cold
```

### Customizable Hotkey
เปิด Settings → กด "Change Key" → กดปุ่มที่ต้องการ → Save
---

## 🔒 ความปลอดภัยและความเป็นส่วนตัว (Security & Privacy)
เราให้ความสำคัญกับความปลอดภัยของข้อมูลคุณเป็นอันดับหนึ่ง:

*   **API Key Protection:** API Key ของคุณ (OpenAI, Gemini, OpenRouter) จะถูกเก็บไว้เฉพาะในเครื่องคอมพิวเตอร์ของคุณเท่านั้น (Local Machine) ผ่านทางโฟลเดอร์ `%AppData%`
*   **No Server Intermediary:** โปรแกรมนี้จะส่งข้อมูลการแปลตรงไปยังผู้ให้บริการ AI (Google, OpenAI, Anthropic) โดยตรง ไม่มีการส่งผ่านเซิร์ฟเวอร์คนกลางของเรา
*   **Open Source Metadata:** ข้อมูลที่อยู่บน GitHub นี้มีเพียงไฟล์ Metadata สำหรับระบบ Auto-Update เท่านั้น โค้ดส่วนตัวและการตั้งค่าของคุณจะไม่ถูกอัปโหลดขึ้นมา
*   **Direct Translation:** ข้อความที่คุณไฮไลท์จะถูกส่งไปประมวลผลการแปลเท่านั้น และไม่มีการบันทึกประวัติข้อความ (Logging) ไว้ในเซิร์ฟเวอร์ภายนอก

---

## ⚙️ การตั้งค่าเริ่มต้น (First Run)
1. เมื่อเปิดโปรแกรมครั้งแรก ให้ไปที่หน้า **Settings**
2. เลือก **Translation Engine** (แนะนำ AI สำหรับความแม่นยำสูง)
3. ใส่ **API Key** ของคุณ (สามารถดูวิธีขอ Key ได้ในเมนู Help)
4. กด **Save Settings** และเริ่มใช้งานได้ทันทีด้วยปุ่ม `Ctrl + Space`

---

## 🛠️ ระบบ Auto-Update
SwapTxT มาพร้อมกับระบบตรวจสอบเวอร์ชันอัตโนมัติ ทุกครั้งที่คุณเปิดโปรแกรม ระบบจะตรวจสอบเวอร์ชันล่าสุดจาก GitHub นี้ หากมีการอัปเดตใหม่ๆ (v1.1.0+) คุณจะได้รับการแจ้งเตือนทันที!

---
**Made with ❤️ by TIXS**
*Support Developer: [Buy Me a Coffee](https://www.buymeacoffee.com/tixs)*
