# حل مشكلة Mixed Content (CORS)

## المشكلة

عندما يكون التطبيق على Netlify (HTTPS) ويحاول الاتصال بـ API على HTTP، المتصفحات تمنع هذا لأسباب أمنية (Mixed Content Policy).

**الخطأ**: `blocked:mixed-content`

**الوضع الحالي**: Somee.com يدعم HTTP فقط، وليس HTTPS.

## الحل: استخدام Netlify Proxy

بما أن Somee يدعم HTTP فقط، يجب استخدام Netlify Proxy لتحويل الطلبات من HTTPS إلى HTTP.

### الحل الموصى به: Netlify Redirects (Proxy)

راجع ملف `Netlify_Proxy_Solution.md` للحل الكامل.

**ملخص سريع**:
1. أنشئ ملف `netlify.toml` في مجلد الجذر للفرونت إند
2. استخدم relative paths في الفرونت إند: `fetch("/auth/login", ...)`
3. Netlify سيتولى تحويل الطلبات إلى `http://hazerapi.somee.com`

### حل بديل: تغيير URL الـAPI (إذا كان HTTPS متاح)

إذا أصبح HTTPS متاحاً في المستقبل:

```javascript
// ❌ خطأ - HTTP (لا يعمل من HTTPS)
fetch("http://hazerapi.somee.com/auth/login", {
  // ...
});

// ✅ صحيح - HTTPS (إذا كان متاحاً)
fetch("https://hazerapi.somee.com/auth/login", {
  // ...
});
```

### 2. تحديث جميع طلبات API

تأكد من تغيير جميع URLs من `http://` إلى `https://`:

```javascript
// Base URL للـAPI
const API_BASE_URL = "https://hazerapi.somee.com";

// أمثلة
fetch(`${API_BASE_URL}/auth/login`, { ... });
fetch(`${API_BASE_URL}/api/employees`, { ... });
fetch(`${API_BASE_URL}/api/attendance/checkin`, { ... });
```

### 3. إعدادات CORS

الـAPI يدعم بالفعل CORS للأصول التالية:
- `http://localhost:7065`
- `https://localhost:7065`
- `https://celebrated-truffle-02ffc1.netlify.app`

### 4. التحقق من HTTPS على Somee

Somee.com يدعم HTTPS تلقائياً. تأكد من:
- استخدام `https://hazerapi.somee.com` بدلاً من `http://`
- أن شهادة SSL نشطة (عادة تكون تلقائية على Somee)

## مثال كود صحيح

```javascript
// ✅ استخدام HTTPS
fetch("https://hazerapi.somee.com/auth/login", {
  headers: {
    "content-type": "application/json"
  },
  body: JSON.stringify({
    username: "ahmed",
    password: "1122"
  }),
  method: "POST",
  mode: "cors",
  credentials: "include" // أو "omit" حسب الحاجة
});
```

## ملاحظات

- **Mixed Content Policy**: المتصفحات الحديثة تمنع تحميل موارد HTTP من صفحات HTTPS
- **CORS**: يجب أن يكون Origin (Netlify) مسموح به في CORS settings
- **Credentials**: إذا كنت تستخدم JWT tokens، قد تحتاج `credentials: "include"`

