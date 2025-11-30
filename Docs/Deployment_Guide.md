# دليل النشر على Somee

## متطلبات النشر

1. **قاعدة البيانات**: تم إعداد قاعدة بيانات SQL Server على `hazerdb.mssql.somee.com`
2. **.NET Runtime**: يجب تثبيت .NET 9.0 Runtime على السيرفر
3. **IIS**: Somee يستخدم IIS لاستضافة تطبيقات ASP.NET Core

## خطوات النشر

### 1. رفع الملفات

1. قم بضغط مجلد `publish` إلى ملف ZIP
2. ارفع الملفات إلى Somee عبر FTP أو File Manager:
   - جميع الملفات من مجلد `publish` يجب أن تكون في المجلد الرئيسي للتطبيق
   - تأكد من رفع مجلد `Logs` (سيتم إنشاؤه تلقائياً إذا لم يكن موجوداً)

### 2. إعدادات قاعدة البيانات

- Connection String موجود في `appsettings.json`:
  ```
  Server=hazerdb.mssql.somee.com;Database=hazerdb;User Id=hazer_SQLLogin_1;Password=xwpmnujany;TrustServerCertificate=True;
  ```

### 3. إعدادات IIS

- تأكد من أن `web.config` موجود في المجلد الرئيسي
- تأكد من أن Application Pool يستخدم .NET CLR Version: "No Managed Code"
- تأكد من أن Application Pool Mode هو "Integrated"

### 4. الأذونات

- تأكد من أن IIS_IUSRS لديه أذونات القراءة والكتابة على:
  - مجلد التطبيق
  - مجلد `Logs` (للكتابة)

### 5. التحقق من النشر

1. افتح المتصفح وانتقل إلى: `https://yourdomain.somee.com/swagger`
2. تحقق من أن Swagger UI يظهر بشكل صحيح
3. اختبر endpoint بسيط مثل `/api/auth/login`

## سجلات الأخطاء (Logs)

### موقع الملفات

- **File Logs**: `Logs/hazarapi-YYYY-MM-DD.txt`
- **Stdout Logs**: `logs/stdout` (إذا تم تفعيل stdout logging في web.config)

### أنواع السجلات

- **Information**: معلومات عامة عن تشغيل التطبيق
- **Warning**: تحذيرات
- **Error**: أخطاء في التطبيق
- **Fatal**: أخطاء حرجة تؤدي إلى إيقاف التطبيق

### تنظيف السجلات

- يتم الاحتفاظ بآخر 30 ملف سجل يومي تلقائياً
- يمكن حذف الملفات القديمة يدوياً من مجلد `Logs`

## استكشاف الأخطاء

### المشاكل الشائعة

1. **500 Internal Server Error**
   - تحقق من سجلات الأخطاء في `Logs/hazarapi-*.txt`
   - تحقق من اتصال قاعدة البيانات
   - تحقق من JWT SecretKey في `appsettings.json`

2. **Database Connection Failed**
   - تحقق من Connection String
   - تأكد من أن قاعدة البيانات متاحة
   - تحقق من أذونات المستخدم

3. **Swagger not loading**
   - تأكد من أن `app.UseSwagger()` و `app.UseSwaggerUI()` مفعلة
   - تحقق من أن الملفات موجودة في المكان الصحيح

## الأمان

- **JWT SecretKey**: يجب تغيير `SecretKey` في `appsettings.json` إلى قيمة آمنة عشوائية
- **HTTPS**: Somee يوفر HTTPS تلقائياً
- **CORS**: إذا كنت تحتاج CORS، أضف الإعدادات في `Program.cs`

## الدعم

في حالة وجود مشاكل:
1. تحقق من سجلات الأخطاء
2. تحقق من إعدادات Somee
3. تأكد من أن جميع الملفات مرفوعة بشكل صحيح

