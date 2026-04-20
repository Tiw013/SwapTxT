1. ข้อมูลพื้นฐานของโปรเจกต์
Platform: Windows OS (Windows 10/11)
Framework: C# .NET (แนะนำใช้ WPF เพื่อให้ทำ UI ได้สวยงามและทันสมัยกว่า WinForms)
รูปแบบโปรแกรม: Background Application (ทำงานซ่อนใน System Tray)

2. การทำงานของระบบหลัก (Core Logic)
Global Keyboard Hook: ใช้ไลบรารีเบื้องหลัง (เช่น Win32 API RegisterHotKey) เพื่อตรวจจับปุ่ม Ctrl + Space (หรือปุ่มที่ผู้ใช้ตั้งค่า) ให้ทำงานได้ตลอดเวลาแม้จะเปิดโปรแกรมอื่นทับอยู่

Clipboard Manipulation (หัวใจสำคัญ):

Step 1: เมื่อกดคีย์ลัด โปรแกรมสั่ง SendKeys.SendWait("^c"); เพื่อ Copy ข้อความที่คลุมดำไว้

Step 2: อ่านค่าจาก Clipboard (Clipboard.GetText())

Step 3: ส่ง Text ไปให้ Translation API ประมวลผล

Step 4: นำ Text ที่แปลเสร็จแล้วใส่กลับลงไป (Clipboard.SetText())

Step 5: สั่ง SendKeys.SendWait("^v"); เพื่อ Paste ทับที่เดิม

ข้อควรระวัง: ต้องจัดการเรื่อง STAThread สำหรับ Clipboard ใน C# ให้ดี และอาจต้องใส่ Task.Delay เล็กน้อย (ประมาณ 50-100ms) ระหว่าง Copy/Paste เพื่อให้ OS ทำงานทัน

3. ระบบแปลภาษา (Dual-Engine API)
Engine 1: Google Translate (Default): ใช้ HTTP Request ยิงไปที่ Endpoint ฟรี (เช่น translate.googleapis.com/translate_a/single) ไม่ต้องใช้ API Key

Engine 2: Custom AI (BYOK): เขียนฟังก์ชันรองรับ HTTP POST Request สำหรับยิงไปที่ OpenAI API หรือ Gemini API โดยดึง API Key มาจากที่ผู้ใช้บันทึกไว้

4. ระบบอัปเดต (Auto-Update)
ฝังไลบรารี AutoUpdater.NET ไว้ในฟังก์ชันตอนเริ่มโปรแกรม (MainWindow_Loaded) เพื่อเช็คไฟล์ XML บนเซิร์ฟเวอร์ทุกครั้งที่เปิดคอมพิวเตอร์