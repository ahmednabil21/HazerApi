# حل مشكلة Microsoft.Data.SqlClient

## المشكلة
التطبيق يبحث عن `Microsoft.Data.SqlClient, Version=5.0.0.0` لكن لا يجده على السيرفر.

## الحل

### 1. تأكد من رفع جميع الملفات التالية:

#### ملفات DLL الرئيسية:
- `Microsoft.Data.SqlClient.dll` (في المجلد الرئيسي)
- جميع ملفات DLL الأخرى من مجلد `publish`

#### مجلد runtimes (مهم جداً):
يجب رفع مجلد `runtimes` بالكامل مع جميع محتوياته:
```
runtimes/
├── unix/
│   └── lib/
│       └── net6.0/
│           └── Microsoft.Data.SqlClient.dll
├── win/
│   └── lib/
│       └── net6.0/
│           └── Microsoft.Data.SqlClient.dll
├── win-arm/
│   └── native/
│       └── Microsoft.Data.SqlClient.SNI.dll
├── win-arm64/
│   └── native/
│       └── Microsoft.Data.SqlClient.SNI.dll
├── win-x64/
│   └── native/
│       └── Microsoft.Data.SqlClient.SNI.dll
└── win-x86/
    └── native/
        └── Microsoft.Data.SqlClient.SNI.dll
```

### 2. إعادة تشغيل Application Pool

بعد رفع الملفات:
1. افتح IIS Manager
2. اذهب إلى Application Pools
3. ابحث عن Application Pool الخاص بتطبيقك
4. انقر بزر الماوس الأيمن واختر "Recycle"

### 3. التحقق من الأذونات

تأكد من أن IIS_IUSRS لديه أذونات القراءة على:
- جميع ملفات DLL
- مجلد runtimes بالكامل

### 4. التحقق من الملفات

بعد الرفع، تحقق من وجود الملفات التالية في المجلد الرئيسي:
- `Microsoft.Data.SqlClient.dll` ✓
- `HazarApi.dll` ✓
- `HazarApi.deps.json` ✓
- `web.config` ✓
- مجلد `runtimes/` بالكامل ✓

### 5. إذا استمرت المشكلة

1. تحقق من سجلات الأخطاء في `Logs/hazarapi-*.txt`
2. تحقق من stdout logs في `logs/stdout`
3. تأكد من أن .NET 9.0 Runtime مثبت على السيرفر
4. تأكد من أن جميع الملفات مرفوعة بشكل صحيح

## ملاحظة مهمة

مجلد `runtimes` ضروري جداً لعمل `Microsoft.Data.SqlClient` على Windows. بدون هذا المجلد، لن يعمل التطبيق.

