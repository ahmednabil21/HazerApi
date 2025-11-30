# دليل تفعيل HTTPS على Somee.com

## نظرة عامة

Somee.com يوفر شهادات SSL مجانية تلقائياً لجميع المواقع. تحتاج فقط إلى تفعيلها من لوحة التحكم.

## الخطوات

### 1. تفعيل SSL Certificate من لوحة التحكم

1. **سجل الدخول** إلى لوحة تحكم Somee.com
2. **اختر موقعك**: `hazerapi.somee.com`
3. **انتقل إلى تبويب "Domains / Bindings"**
4. **ابحث عن قسم SSL Certificate** أو **HTTPS Settings**
5. **فعّل SSL Certificate**:
   - إذا كان هناك زر "Enable SSL" أو "Activate SSL"، اضغط عليه
   - أو ابحث عن خيار "Free SSL Certificate" وفعّله
   - Somee عادة يوفر شهادة SSL مجانية تلقائياً

### 2. إضافة HTTPS Binding

في نفس صفحة "Domains / Bindings":

1. **ابحث عن قائمة Bindings** أو **Site Bindings**
2. **أضف Binding جديد**:
   - **Type**: HTTPS
   - **Port**: 443
   - **Host name**: `hazerapi.somee.com` (أو اتركه فارغاً)
   - **SSL Certificate**: اختر الشهادة المتاحة (عادة "Somee Free SSL" أو مشابه)
3. **احفظ التغييرات**

### 3. تفعيل HTTPS Redirection (اختياري لكن موصى به)

في `web.config`، يمكن إضافة redirect من HTTP إلى HTTPS:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\HazarApi.dll" stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
      
      <!-- HTTPS Redirection -->
      <rewrite>
        <rules>
          <rule name="HTTP to HTTPS redirect" stopProcessing="true">
            <match url="(.*)" />
            <conditions>
              <add input="{HTTPS}" pattern="off" ignoreCase="true" />
            </conditions>
            <action type="Redirect" url="https://{HTTP_HOST}/{R:1}" redirectType="Permanent" />
          </rule>
        </rules>
      </rewrite>
    </system.webServer>
  </location>
</configuration>
```

### 4. تحديث Program.cs لدعم HTTPS

في `Program.cs`، يمكن تفعيل HTTPS Redirection:

```csharp
// HTTPS Redirection - فعّله بعد تفعيل SSL
app.UseHttpsRedirection();
```

**ملاحظة**: إذا كان Somee يتولى HTTPS Redirection تلقائياً، قد لا تحتاج هذا.

### 5. التحقق من HTTPS

بعد تفعيل SSL:

1. **انتظر 5-10 دقائق** حتى يتم تفعيل الشهادة
2. **اختبر HTTPS**:
   ```
   https://hazerapi.somee.com/swagger
   ```
3. **تحقق من الشهادة**:
   - افتح الموقع في المتصفح
   - اضغط على أيقونة القفل بجانب العنوان
   - يجب أن ترى "Connection is secure" أو "Secure"

## المشاكل الشائعة

### 1. SSL Certificate غير متاح

**الحل**:
- تأكد من أن النطاق `hazerapi.somee.com` نشط
- انتظر بضع دقائق بعد تفعيل SSL
- تحقق من أن النطاق ليس محظوراً

### 2. Mixed Content Warnings

**الحل**:
- تأكد من أن جميع الطلبات تستخدم HTTPS
- تحقق من CORS settings في `appsettings.json`

### 3. Certificate Not Trusted

**الحل**:
- Somee يستخدم شهادات SSL موثوقة
- إذا ظهرت رسالة "Not Secure"، انتظر حتى يتم تفعيل الشهادة بالكامل (قد يستغرق حتى 24 ساعة)

## بعد تفعيل HTTPS

### 1. تحديث CORS Settings

تأكد من أن `appsettings.json` يحتوي على HTTPS في CORS:

```json
{
  "CorsSettings": {
    "AllowedOrigins": [
      "http://localhost:7065",
      "https://localhost:7065",
      "https://celebrated-truffle-02ffc1.netlify.app"
    ]
  }
}
```

### 2. تحديث Base URL في الفرونت إند

في كود الفرونت إند، استخدم HTTPS:

```javascript
// ✅ بعد تفعيل HTTPS
const API_BASE_URL = "https://hazerapi.somee.com";
```

### 3. إزالة Netlify Proxy (إذا كان مستخدماً)

إذا كنت تستخدم Netlify Proxy، يمكنك إزالته الآن واستخدام HTTPS مباشرة.

## ملاحظات مهمة

1. **Somee Free SSL**: Somee يوفر شهادات SSL مجانية تلقائياً لجميع المواقع
2. **التفعيل التلقائي**: في بعض الحالات، يتم تفعيل SSL تلقائياً بعد نشر الموقع
3. **الانتظار**: قد يستغرق تفعيل SSL من بضع دقائق إلى 24 ساعة
4. **HTTP و HTTPS**: يمكن أن يعمل كلاهما في نفس الوقت، لكن HTTPS هو الموصى به

## الدعم

إذا واجهت مشاكل:
1. تحقق من لوحة تحكم Somee - قسم SSL/Domains
2. راجع وثائق Somee حول SSL
3. تواصل مع دعم Somee إذا استمرت المشكلة

