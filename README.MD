# Discord Audio Guard

Fixes Discord Go Live echo caused when Discord injects audio into all devices. This only occurs when streaming your entire screen, not when streaming a specific application.

## ⚡ Quick Fix
1. [Download from releases]()
2. **Run** the app → appears in system tray
3. **Settings** → Right-click tray icon
4. **Uncheck** physical devices (speakers/headphones)
5. **Check** your virtual audio cable
6. **Save**

✅ **Checked** = Discord **plays** here  
❌ **Unchecked** = Discord **muted** here

## 🎯 Example
```
[✓] CABLE Input (VB-Audio)  ← Discord allowed here
[ ] Headphones              ← Discord muted here
[ ] Speakers                ← Discord muted here
[ ] Microphone              ← Discord muted here
```

## ⚠️ AI-Generated Code
This entire application was created with AI assistance. Please review the code thoroughly before deployment as it may contain unexpected behaviors or require adjustments for your specific setup.

---

That's it! Discord audio now routes only through your virtual cable—no more echo.

**Requirements:** Windows 10/11, .NET 8+
