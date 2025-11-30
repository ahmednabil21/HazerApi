# حل مشكلة Mixed Content باستخدام Netlify Proxy

## المشكلة

- الفرونت إند على Netlify (HTTPS): `https://celebrated-truffle-02ffc1.netlify.app`
- الـAPI على Somee (HTTP فقط): `http://hazerapi.somee.com`
- المتصفحات تمنع طلبات HTTP من صفحات HTTPS (Mixed Content Policy)

## الحل: استخدام Netlify Proxy

Netlify يمكنه عمل Proxy للطلبات من HTTPS إلى HTTP. هذا يعني أن الفرونت إند سيتصل بـ Netlify (HTTPS) وNetlify سيتصل بالـAPI (HTTP).

### الخطوة 1: إنشاء ملف `netlify.toml`

أنشئ ملف `netlify.toml` في مجلد الجذر لمشروع الفرونت إند:

```toml
[[redirects]]
  from = "/api/*"
  to = "http://hazerapi.somee.com/api/:splat"
  status = 200
  force = true
  headers = {X-From = "Netlify"}

[[redirects]]
  from = "/auth/*"
  to = "http://hazerapi.somee.com/auth/:splat"
  status = 200
  force = true
  headers = {X-From = "Netlify"}

[[redirects]]
  from = "/swagger/*"
  to = "http://hazerapi.somee.com/swagger/:splat"
  status = 200
  force = true
```

### الخطوة 2: تحديث Base URL في الفرونت إند

بدلاً من الاتصال مباشرة بـ `http://hazerapi.somee.com`، استخدم نفس النطاق (Netlify):

```javascript
// ❌ قبل - لا يعمل بسبب Mixed Content
const API_BASE_URL = "http://hazerapi.somee.com";

// ✅ بعد - يعمل عبر Netlify Proxy
const API_BASE_URL = "https://celebrated-truffle-02ffc1.netlify.app";
// أو ببساطة:
const API_BASE_URL = ""; // نفس النطاق
```

### الخطوة 3: مثال كود محدث

```javascript
// ✅ يعمل عبر Netlify Proxy
fetch("https://celebrated-truffle-02ffc1.netlify.app/auth/login", {
  headers: {
    "content-type": "application/json"
  },
  body: JSON.stringify({
    username: "ahmed",
    password: "1122"
  }),
  method: "POST",
  mode: "cors",
  credentials: "include"
});
```

أو استخدام relative path:

```javascript
// ✅ أفضل - يعمل مع أي نطاق
fetch("/auth/login", {
  headers: {
    "content-type": "application/json"
  },
  body: JSON.stringify({
    username: "ahmed",
    password: "1122"
  }),
  method: "POST",
  mode: "cors",
  credentials: "include"
});
```

## كيف يعمل Proxy؟

1. الفرونت إند يرسل طلب إلى: `https://celebrated-truffle-02ffc1.netlify.app/auth/login`
2. Netlify يستقبل الطلب (HTTPS)
3. Netlify يرسل الطلب إلى: `http://hazerapi.somee.com/auth/login` (HTTP)
4. الـAPI يرد
5. Netlify يمرر الرد إلى الفرونت إند (HTTPS)

## ملاحظات مهمة

### 1. CORS Headers

تأكد من أن الـAPI يرسل CORS headers صحيحة. الـAPI الحالي يدعم:
- `https://celebrated-truffle-02ffc1.netlify.app` ✅

### 2. Headers الممررة

Netlify يمرر جميع headers تلقائياً، لكن قد تحتاج لإضافة:

```toml
[[redirects]]
  from = "/api/*"
  to = "http://hazerapi.somee.com/api/:splat"
  status = 200
  force = true
  headers = {
    X-Forwarded-Host = "hazerapi.somee.com"
  }
```

### 3. Query Parameters

`:splat` في Netlify يمرر كل شيء بما في ذلك query parameters.

### 4. POST/PUT/DELETE Requests

Proxy يعمل مع جميع أنواع الطلبات (GET, POST, PUT, DELETE, etc.).

## بديل: استخدام Netlify Functions

إذا كان Proxy لا يعمل بشكل صحيح، يمكن استخدام Netlify Functions:

### إنشاء `netlify/functions/api-proxy.js`

```javascript
exports.handler = async (event, context) => {
  const { path, httpMethod, body, headers } = event;
  
  const apiUrl = `http://hazerapi.somee.com${path}`;
  
  try {
    const response = await fetch(apiUrl, {
      method: httpMethod,
      headers: {
        ...headers,
        host: 'hazerapi.somee.com'
      },
      body: body
    });
    
    const data = await response.text();
    
    return {
      statusCode: response.status,
      headers: {
        'Content-Type': 'application/json',
        'Access-Control-Allow-Origin': '*'
      },
      body: data
    };
  } catch (error) {
    return {
      statusCode: 500,
      body: JSON.stringify({ error: error.message })
    };
  }
};
```

## التحقق من الحل

1. ارفع `netlify.toml` إلى مجلد الجذر في Netlify
2. أعد نشر التطبيق على Netlify
3. اختبر الطلبات من المتصفح
4. تحقق من Network tab - يجب أن ترى طلبات إلى `https://celebrated-truffle-02ffc1.netlify.app` بدلاً من `http://hazerapi.somee.com`

## مثال كامل لـ netlify.toml

```toml
[build]
  publish = "dist"
  command = "npm run build"

[[redirects]]
  from = "/api/*"
  to = "http://hazerapi.somee.com/api/:splat"
  status = 200
  force = true

[[redirects]]
  from = "/auth/*"
  to = "http://hazerapi.somee.com/auth/:splat"
  status = 200
  force = true
```

