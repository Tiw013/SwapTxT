# SwapTxT Changelog - v1.0.5 (2026-05-13)

## 🚀 New Features & Improvements (ฟีเจอร์ใหม่และการปรับปรุง)

### 1. Local AI CLI Enhancements (ปรับปรุงระบบ Local CLI)
- **Tool Presets**: Added a new dropdown to select Gemini, Codex, or Claude. The program now handles complex arguments automatically.
  *(ระบบเลือกเครื่องมือสำเร็จรูป: เพิ่มเมนูให้เลือก Gemini, Codex หรือ Claude ได้ทันที โดยโปรแกรมจะจัดการคำสั่ง Arguments ที่ซับซ้อนให้เองเบื้องหลัง)*
- **Smart Auto-fill**: Typing tool names in the CLI Command field still triggers automatic suggestions.
  *(ระบบกรอกคำสั่งอัตโนมัติ: การพิมพ์ชื่อเครื่องมือในช่อง Command ยังคงรองรับการแนะนำคำสั่งอัตโนมัติเช่นเดิม)*
- **Clean Configuration UI**: Hidden complex CLI arguments by default to keep the settings interface clean and professional.
  *(หน้าตาตั้งค่าที่สะอาดตา: ซ่อนช่องกรอก Arguments ที่ซับซ้อนออกเพื่อความสวยงาม โดยจะแสดงเฉพาะเมื่อเลือกโหมด Custom เท่านั้น)*
- **Robust Process Execution**: Rewrote background handler to prevent deadlocks and timeouts.
  *(ปรับปรุงการรันเบื้องหลัง: แก้ไขระบบรับส่งข้อมูลใหม่ทั้งหมด เพื่อป้องกันอาการโปรแกรมค้างหรือ Timeout)*
- **ANSI Strip & Clean Output**: Filters color codes and TUI decorations for clean results.
  *(ล้างรหัสขยะ: ระบบจะลบโค้ดสีและสัญลักษณ์ตกแต่งของ Terminal ออกให้อัตโนมัติ เพื่อให้ได้ข้อความแปลที่สะอาด)*

### 2. Specialized Technical Tones (เพิ่มโหมดการแปลสายเทคนิค)
- **Tech Expert**: Professional technical terminology and industry jargon.
  *(ระดับผู้เชี่ยวชาญ: ใช้คำศัพท์เทคนิคระดับสูงและภาษาวงการมาตรฐาน)*
- **Tech Audience**: Tailored for engineers/IT, maintaining critical terms.
  *(ระดับคนทำงานสายไอที: ปรับสำนวนให้เหมาะกับวิศวกร/IT และคงศัพท์เทคนิคที่จำเป็นไว้)*
- **Tech Systemic**: Technical precision and system-specific nomenclature.
  *(ระดับความแม่นยำสูง: เน้นความถูกต้องเชิงระบบและการรักษาชื่อเฉพาะทางเทคนิค)*

---

## 🐞 Bug Fixes (รายการแก้ไขบัค)

### 1. Tone Editor Stability (ความเสถียรของเครื่องมือปรับแต่ง Mood & Tone)
- **Fixed Re-entrant Rename Bug**: Resolved an issue where renaming a tone and pressing Enter caused UI ghosting or duplicate entries.
  *(แก้บัคเปลี่ยนชื่อ Tone: แก้ไขปัญหาที่การกด Enter ตอนเปลี่ยนชื่อแล้วทำให้เกิดรายการซ้ำซ้อนหรือแสดงผลผิดพลาด)*

### 2. Model Selection Logic (ระบบการเลือก Model)
- **Fixed Model Mixing Bug**: Gemini models no longer disappear when typing.
  *(แก้บัครายชื่อ Model หาย: รายชื่อ Model ของ Gemini จะไม่ถูกแทนที่ด้วย OpenAI/OpenRouter เวลาพิมพ์ค้นหาอีกต่อไป)*
- **Provider-Aware Presets**: Lists now correctly filter based on active provider.
  *(ระบบแยกประเภทตาม Provider: รายชื่อ Model จะถูกกรองให้ตรงกับผู้ให้บริการที่เลือกอยู่เท่านั้น)*
- **Recent Models Isolation**: Custom models are tracked separately per provider.
  *(แยกประวัติการใช้งาน: ระบบจะจำ Model ล่าสุดแยกตามรายชื่อผู้ให้บริการ ไม่นำมาปนกัน)*

### 3. Process Stability (ความเสถียรของระบบ)
- **Deadlock Fix**: Resolved timeout issues for simple commands like `whoami`.
  *(แก้ปัญหาโปรแกรมค้าง: แก้ไขจุดบกพร่องที่ทำให้คำสั่งง่ายๆ เกิดอาการค้างจนหมดเวลา)*
- **Improved Timeout Handling**: Increased timeout to 120s for local LLMs.
  *(ปรับปรุงการรอคำตอบ: เพิ่มเวลารอเป็น 120 วินาที เพื่อรองรับ Model ในเครื่องที่อาจประมวลผลช้า)*

---

## 🛠️ Technical Details (รายละเอียดทางเทคนิค)
- **Files Modified (ไฟล์ที่แก้ไข)**: `MainWindow.xaml.cs`, `MainWindow.xaml`, `TranslationService.cs`, `ToneEditorWindow.cs`, `AppSettings.cs`
