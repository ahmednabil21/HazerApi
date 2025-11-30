# وثيقة API - نظام إدارة الدوام والموظفين

## معلومات عامة

**Base URL**: `http://localhost:5151` (أو عنوان الخادم في الإنتاج)

**Authentication**: جميع الـEndpoints (ما عدا `/auth/login`) تتطلب JWT Token في Header:
```
Authorization: Bearer {your_token}
```

---

## 1. Authentication APIs

### 1.1 تسجيل الدخول
**Endpoint**: `POST /auth/login`

**الوصف**: تسجيل دخول الموظف للحصول على JWT Token

**Authentication**: غير مطلوب

**Request Body**:
```json
{
  "username": "ahmed",
  "password": "password123"
}
```

**Response (200 OK)**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "guid-string",
  "expiresAt": "2024-11-28T15:30:00Z",
  "username": "ahmed",
  "role": "Employee"
}
```

**ملاحظات**:
- احفظ `accessToken` واستخدمه في جميع الطلبات التالية
- `expiresAt` يحدد وقت انتهاء صلاحية التوكن
- `role` يمكن أن يكون "Admin" أو "Employee"

---

### 1.2 تجديد التوكن
**Endpoint**: `POST /auth/refresh`

**الوصف**: تجديد JWT Token باستخدام Refresh Token

**Authentication**: غير مطلوب

**Request Body**:
```json
{
  "refreshToken": "guid-string"
}
```

**Response (200 OK)**:
```json
{
  "accessToken": "new-token...",
  "refreshToken": "new-refresh-token",
  "expiresAt": "2024-11-28T16:30:00Z",
  "username": "ahmed",
  "role": "Employee"
}
```

---

### 1.3 جلب جلسات المستخدم
**Endpoint**: `GET /auth/sessions?days=30`

**الوصف**: جلب تاريخ جلسات دخول/خروج الموظف

**Authentication**: مطلوب (JWT Token)

**Query Parameters**:
- `days` (optional): عدد الأيام الماضية (افتراضي: 30)

**Response (200 OK)**:
```json
[
  {
    "id": 1,
    "employeeId": 1,
    "employeeName": "أحمد محمد",
    "loginTime": "2024-11-28T07:00:00Z",
    "logoutTime": "2024-11-28T14:00:00Z",
    "ipAddress": "192.168.1.1",
    "userAgent": "Mozilla/5.0...",
    "isActive": false
  }
]
```

---

## 2. Attendance APIs (سجلات الحضور)

### 2.1 تسجيل الدخول (Check-In)
**Endpoint**: `POST /api/attendance/check-in`

**الوصف**: تسجيل دخول الموظف في يوم معين مع وقت الدخول

**Authentication**: مطلوب (JWT Token)

**Request Body**:
```json
{
  "workDate": "2024-11-28",
  "checkInTime": "07:30",
  "timeOffMinutesUsed": 0,
  "notes": "تسجيل دخول"
}
```

**Response (200 OK)**:
```json
{
  "id": 1,
  "workDate": "2024-11-28",
  "checkIn": "07:30",
  "checkOut": "14:00",
  "timeOffMinutesUsed": 0,
  "delayMinutes": 30,
  "overtimeMinutes": 0,
  "ninetyMinutesDeducted": 30,
  "notes": "تسجيل دخول",
  "isLocked": false
}
```

**ملاحظات**:
- `workDate`: تاريخ العمل (صيغة: YYYY-MM-DD)
- `checkInTime`: وقت الدخول (صيغة: HH:mm)
- `timeOffMinutesUsed`: الزمنية المستخدمة (اختياري، افتراضي: 0)
- النظام يحسب التأخير تلقائياً ويخصم من 90 دقيقة
- لا يمكن التسجيل في يوم الجمعة أو السبت
- لا يمكن التسجيل مرتين في نفس اليوم

**Errors**:
- `400 Bad Request`: إذا كان اليوم جمعة أو سبت، أو إذا كان السجل موجود مسبقاً
- `401 Unauthorized`: إذا كان التوكن غير صالح

---

### 2.2 تسجيل الخروج (Check-Out)
**Endpoint**: `POST /api/attendance/check-out`

**الوصف**: تسجيل خروج الموظف وتحديث وقت الخروج

**Authentication**: مطلوب (JWT Token)

**Request Body**:
```json
{
  "workDate": "2024-11-28",
  "checkOutTime": "14:30",
  "notes": "تسجيل خروج"
}
```

**Response (200 OK)**:
```json
{
  "id": 1,
  "workDate": "2024-11-28",
  "checkIn": "07:30",
  "checkOut": "14:30",
  "timeOffMinutesUsed": 0,
  "delayMinutes": 30,
  "overtimeMinutes": 30,
  "ninetyMinutesDeducted": 30,
  "notes": "تسجيل خروج",
  "isLocked": false
}
```

**ملاحظات**:
- يجب أن يكون هناك سجل دخول (check-in) مسبقاً في نفس اليوم
- النظام يحسب الإضافي تلقائياً
- يتم تحديث الملخص الشهري تلقائياً

**Errors**:
- `404 Not Found`: إذا لم يكن هناك سجل دخول في هذا اليوم
- `400 Bad Request`: إذا كان السجل مقفولاً

---

### 2.3 إضافة زمنية
**Endpoint**: `POST /api/attendance/time-off`

**الوصف**: إضافة زمنية لتغطية التأخير أو استخدامها لأغراض أخرى

**Authentication**: مطلوب (JWT Token)

**Request Body**:
```json
{
  "timeOffDate": "2024-11-28",
  "minutesUsed": 30,
  "reason": "تغطية تأخير"
}
```

**Response (200 OK)**:
```json
{
  "success": true,
  "message": "Time off added successfully. 30 minutes deducted from monthly balance."
}
```

**ملاحظات**:
- `timeOffDate`: تاريخ الزمنية
- `minutesUsed`: عدد الدقائق (يُخصم من 7 ساعات الزمنيات الشهرية)
- `reason`: سبب الزمنية (مطلوب)
- إذا كان هناك تأخير في نفس اليوم وتغطي الزمنية التأخير:
  - لا يتم خصم من 90 دقيقة
  - يتم استعادة ما تم خصمه من 90 دقيقة
  - يتم الخصم من 7 ساعات الزمنيات فقط
- الحد الأقصى للزمنية اليومية: 240 دقيقة (4 ساعات)
- لا يمكن إضافة زمنية في يوم الجمعة أو السبت

**Errors**:
- `400 Bad Request`: إذا كان الرصيد غير كافي، أو إذا كان اليوم جمعة/سبت، أو إذا تجاوزت 240 دقيقة

---

### 2.4 جلب سجلات الحضور الشهرية
**Endpoint**: `GET /api/attendance/{employeeId}/{year}/{month}?includeLocked=false`

**الوصف**: جلب جميع سجلات الحضور لموظف في شهر معين

**Authentication**: مطلوب (JWT Token)

**Path Parameters**:
- `employeeId`: معرف الموظف
- `year`: السنة (مثال: 2024)
- `month`: الشهر (1-12)

**Query Parameters**:
- `includeLocked` (optional): تضمين السجلات المقفولة (افتراضي: false)

**Response (200 OK)**:
```json
[
  {
    "id": 1,
    "workDate": "2024-11-01",
    "checkIn": "07:30",
    "checkOut": "14:00",
    "timeOffMinutesUsed": 0,
    "delayMinutes": 30,
    "overtimeMinutes": 0,
    "ninetyMinutesDeducted": 30,
    "notes": null,
    "isLocked": false
  }
]
```

**ملاحظات**:
- الموظف يمكنه رؤية سجلاته فقط
- المدير يمكنه رؤية سجلات جميع الموظفين

---

## 3. Employee APIs (إدارة الموظفين)

### 3.1 جلب رصيد الموظف الحالي
**Endpoint**: `GET /api/employees/me/balance`

**الوصف**: جلب رصيد الموظف الحالي من الزمنيات والـ90 دقيقة

**Authentication**: مطلوب (JWT Token)

**Response (200 OK)**:
```json
{
  "employeeId": 1,
  "employeeName": "أحمد محمد",
  "monthlyTimeOffBalance": 390,
  "ninetyMinutesBalance": 60,
  "year": 2024,
  "month": 11,
  "totalTimeOffUsed": 30,
  "remainingTimeOffMinutes": 390
}
```

**ملاحظات**:
- `monthlyTimeOffBalance`: الرصيد المتبقي من الزمنيات (بالدقائق)
- `ninetyMinutesBalance`: الرصيد المتبقي من الـ90 دقيقة
- `totalTimeOffUsed`: إجمالي الزمنيات المستخدمة هذا الشهر
- `remainingTimeOffMinutes`: المتبقي من الزمنيات (نفس `monthlyTimeOffBalance`)

---

### 3.2 جلب قائمة الموظفين (Admin Only)
**Endpoint**: `GET /api/employees?pageNumber=1&pageSize=20&searchTerm=&includeInactive=false`

**الوصف**: جلب قائمة الموظفين مع الترقيم

**Authentication**: مطلوب (JWT Token - Admin Only)

**Query Parameters**:
- `pageNumber` (optional): رقم الصفحة (افتراضي: 1)
- `pageSize` (optional): عدد العناصر في الصفحة (افتراضي: 20)
- `searchTerm` (optional): كلمة البحث (اسم أو username)
- `includeInactive` (optional): تضمين الموظفين المعطلين (افتراضي: false)

**Response (200 OK)**:
```json
{
  "items": [
    {
      "id": 1,
      "fullName": "أحمد محمد",
      "username": "ahmed",
      "jobTitle": "مطور",
      "isActive": true,
      "role": "Employee",
      "monthlyTimeOffBalance": 420,
      "ninetyMinutesBalance": 90
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 50,
  "totalPages": 3
}
```

---

### 3.3 جلب موظف محدد (Admin Only)
**Endpoint**: `GET /api/employees/{id}`

**الوصف**: جلب بيانات موظف محدد

**Authentication**: مطلوب (JWT Token - Admin Only)

**Path Parameters**:
- `id`: معرف الموظف

**Response (200 OK)**:
```json
{
  "id": 1,
  "fullName": "أحمد محمد",
  "username": "ahmed",
  "jobTitle": "مطور",
  "isActive": true,
  "role": "Employee",
  "monthlyTimeOffBalance": 420,
  "ninetyMinutesBalance": 90
}
```

---

### 3.4 إضافة موظف جديد (Admin Only)
**Endpoint**: `POST /api/employees`

**الوصف**: إضافة موظف جديد إلى النظام

**Authentication**: مطلوب (JWT Token - Admin Only)

**Request Body**:
```json
{
  "fullName": "محمد علي",
  "username": "mohammed",
  "password": "password123",
  "jobTitle": "مدير"
}
```

**Response (201 Created)**:
```json
{
  "id": 2,
  "fullName": "محمد علي",
  "username": "mohammed",
  "jobTitle": "مدير",
  "isActive": true,
  "role": "Employee",
  "monthlyTimeOffBalance": 420,
  "ninetyMinutesBalance": 90
}
```

**ملاحظات**:
- `jobTitle` اختياري
- كلمة المرور تُشفّر تلقائياً
- الرصيد الافتراضي: 420 دقيقة زمنيات، 90 دقيقة تأخير

---

### 3.5 تحديث بيانات موظف (Admin Only)
**Endpoint**: `PUT /api/employees/{id}`

**الوصف**: تحديث بيانات موظف موجود

**Authentication**: مطلوب (JWT Token - Admin Only)

**Path Parameters**:
- `id`: معرف الموظف

**Request Body**:
```json
{
  "fullName": "محمد علي أحمد",
  "jobTitle": "مدير مشاريع",
  "isActive": true,
  "role": "Employee"
}
```

**Response (200 OK)**:
```json
{
  "id": 2,
  "fullName": "محمد علي أحمد",
  "username": "mohammed",
  "jobTitle": "مدير مشاريع",
  "isActive": true,
  "role": "Employee",
  "monthlyTimeOffBalance": 420,
  "ninetyMinutesBalance": 90
}
```

---

### 3.6 تعطيل/تفعيل موظف (Admin Only)
**Endpoint**: `PUT /api/employees/{id}/status?isActive=true`

**الوصف**: تعطيل أو تفعيل موظف

**Authentication**: مطلوب (JWT Token - Admin Only)

**Path Parameters**:
- `id`: معرف الموظف

**Query Parameters**:
- `isActive`: true للتفعيل، false للتعطيل

**Response (200 OK)**:
```json
{
  "success": true,
  "message": "Employee activated successfully."
}
```

---

### 3.7 إعادة تعيين كلمة المرور (Admin Only)
**Endpoint**: `PUT /api/employees/{id}/reset-password`

**الوصف**: إعادة تعيين كلمة مرور موظف

**Authentication**: مطلوب (JWT Token - Admin Only)

**Path Parameters**:
- `id`: معرف الموظف

**Request Body**:
```json
{
  "newPassword": "newpassword123"
}
```

**Response (200 OK)**:
```json
{
  "success": true,
  "message": "Password reset successfully."
}
```

---

### 3.8 حذف موظف (Admin Only)
**Endpoint**: `DELETE /api/employees/{id}`

**الوصف**: حذف منطقي (Soft Delete) لموظف

**Authentication**: مطلوب (JWT Token - Admin Only)

**Path Parameters**:
- `id`: معرف الموظف

**Response (200 OK)**:
```json
{
  "success": true,
  "message": "Employee deleted successfully."
}
```

**ملاحظات**:
- الحذف منطقي (Soft Delete) - البيانات تبقى في قاعدة البيانات
- الموظف المحذوف لا يستطيع تسجيل الدخول

---

## 4. Monthly Summary APIs (الملخصات الشهرية)

### 4.1 جلب الملخص الشهري
**Endpoint**: `GET /api/summary/{employeeId}/{year}/{month}`

**الوصف**: جلب الملخص الشهري لموظف

**Authentication**: مطلوب (JWT Token)

**Path Parameters**:
- `employeeId`: معرف الموظف
- `year`: السنة
- `month`: الشهر (1-12)

**Response (200 OK)**:
```json
{
  "employeeId": 1,
  "employeeName": "أحمد محمد",
  "year": 2024,
  "month": 11,
  "totalTimeOffMinutesUsed": 30,
  "totalDelayMinutes": 120,
  "ninetyMinutesConsumed": 90,
  "remainingTimeOffMinutes": 390,
  "remainingNinetyMinutes": 0,
  "totalOvertimeMinutes": 60,
  "calculatedAt": "2024-11-28T10:00:00Z"
}
```

**ملاحظات**:
- الموظف يمكنه رؤية ملخصه فقط
- المدير يمكنه رؤية ملخص أي موظف
- إذا لم يكن هناك ملخص، يتم إنشاؤه تلقائياً

---

### 4.2 إعادة حساب شهر (Admin Only)
**Endpoint**: `POST /api/summary/recalculate`

**الوصف**: إعادة حساب ملخص شهر كامل لموظف

**Authentication**: مطلوب (JWT Token - Admin Only)

**Request Body**:
```json
{
  "employeeId": 1,
  "year": 2024,
  "month": 11
}
```

**Response (200 OK)**:
```json
{
  "success": true,
  "message": "Summary recalculated successfully."
}
```

**ملاحظات**:
- يعيد حساب جميع القيم من السجلات الموجودة
- مفيد عند حدوث خطأ في الحسابات

---

## 5. Dashboard APIs (لوحة التحكم - Admin Only)

### 5.1 إحصائيات عامة
**Endpoint**: `GET /api/dashboard/stats?year=2024&month=11`

**الوصف**: جلب إحصائيات عامة لجميع الموظفين

**Authentication**: مطلوب (JWT Token - Admin Only)

**Query Parameters**:
- `year` (optional): السنة (افتراضي: السنة الحالية)
- `month` (optional): الشهر (افتراضي: الشهر الحالي)

**Response (200 OK)**:
```json
{
  "totalEmployees": 50,
  "totalDelayMinutes": 5000,
  "totalTimeOffMinutes": 1200,
  "totalOvertimeMinutes": 3000,
  "year": 2024,
  "month": 11
}
```

---

### 5.2 ترتيب الموظفين حسب التأخير
**Endpoint**: `GET /api/dashboard/top-delays?year=2024&month=11&take=5`

**الوصف**: جلب قائمة الموظفين الأكثر تأخراً

**Authentication**: مطلوب (JWT Token - Admin Only)

**Query Parameters**:
- `year` (optional): السنة
- `month` (optional): الشهر
- `take` (optional): عدد الموظفين (افتراضي: 5)

**Response (200 OK)**:
```json
[
  {
    "employeeId": 5,
    "employeeName": "علي حسن",
    "minutes": 180,
    "value": 180,
    "metric": "DelayMinutes"
  }
]
```

---

### 5.3 ترتيب الموظفين حسب الالتزام
**Endpoint**: `GET /api/dashboard/top-commitment?year=2024&month=11&take=5`

**الوصف**: جلب قائمة الموظفين الأكثر التزاماً

**Authentication**: مطلوب (JWT Token - Admin Only)

**Query Parameters**:
- `year` (optional): السنة
- `month` (optional): الشهر
- `take` (optional): عدد الموظفين (افتراضي: 5)

**Response (200 OK)**:
```json
[
  {
    "employeeId": 1,
    "employeeName": "أحمد محمد",
    "minutes": 120,
    "value": 120,
    "metric": "CommitmentScore"
  }
]
```

**ملاحظات**:
- الالتزام = (الإضافي - التأخير)
- الموظفون الأكثر التزاماً هم الأقل تأخراً والأكثر إضافياً

---

## 6. Attendance Policy APIs (سياسة الدوام - Admin Only)

### 6.1 جلب السياسة النشطة
**Endpoint**: `GET /api/policies/active`

**الوصف**: جلب إعدادات الدوام النشطة

**Authentication**: مطلوب (JWT Token - Admin Only)

**Response (200 OK)**:
```json
{
  "id": 1,
  "workdayStart": "07:00",
  "workdayEnd": "14:00",
  "monthlyTimeOffAllowance": 420,
  "ninetyMinutesAllowance": 90,
  "isActive": true
}
```

---

### 6.2 تحديث السياسة
**Endpoint**: `PUT /api/policies`

**الوصف**: تحديث إعدادات الدوام

**Authentication**: مطلوب (JWT Token - Admin Only)

**Request Body**:
```json
{
  "id": 1,
  "workdayStart": "07:00",
  "workdayEnd": "14:00",
  "monthlyTimeOffAllowance": 420,
  "ninetyMinutesAllowance": 90,
  "isActive": true
}
```

**Response (200 OK)**:
```json
{
  "id": 1,
  "workdayStart": "07:00",
  "workdayEnd": "14:00",
  "monthlyTimeOffAllowance": 420,
  "ninetyMinutesAllowance": 90,
  "isActive": true
}
```

**ملاحظات**:
- عند تفعيل سياسة جديدة، يتم تعطيل السياسات الأخرى تلقائياً
- التغييرات تؤثر على الحسابات المستقبلية فقط

---

## قواعد العمل المهمة

1. **أوقات الدوام**: 07:00 - 14:00 (قابلة للتعديل من السياسة)
2. **الزمنيات الشهرية**: 7 ساعات (420 دقيقة)
3. **رصيد الـ90 دقيقة**: 90 دقيقة تأخير تُخصم من الرصيد أولاً
4. **الزمنية اليومية**: لا تتجاوز 4 ساعات (240 دقيقة)
5. **العطل**: الجمعة والسبت - لا يمكن تسجيل دوام أو إضافة زمنية
6. **التأخير**: يُحسب من (وقت الدخول - 07:00) إذا كان موجباً
7. **الإضافي**: يُحسب من (وقت الخروج - 14:00) إذا كان موجباً

---

## أمثلة على الاستخدام

### سيناريو كامل: تسجيل دخول وخروج وإضافة زمنية

1. **تسجيل الدخول**:
```bash
POST /auth/login
{
  "username": "ahmed",
  "password": "password123"
}
# احفظ accessToken
```

2. **تسجيل دخول الدوام**:
```bash
POST /api/attendance/check-in
Authorization: Bearer {token}
{
  "workDate": "2024-11-28",
  "checkInTime": "07:30",
  "timeOffMinutesUsed": 0,
  "notes": "تسجيل دخول"
}
```

3. **إضافة زمنية لتغطية التأخير**:
```bash
POST /api/attendance/time-off
Authorization: Bearer {token}
{
  "timeOffDate": "2024-11-28",
  "minutesUsed": 30,
  "reason": "تغطية تأخير"
}
```

4. **تسجيل خروج الدوام**:
```bash
POST /api/attendance/check-out
Authorization: Bearer {token}
{
  "workDate": "2024-11-28",
  "checkOutTime": "14:30",
  "notes": "تسجيل خروج"
}
```

5. **التحقق من الرصيد**:
```bash
GET /api/employees/me/balance
Authorization: Bearer {token}
```

---

## رموز الحالة (Status Codes)

- `200 OK`: العملية نجحت
- `201 Created`: تم إنشاء العنصر بنجاح
- `400 Bad Request`: خطأ في البيانات المرسلة
- `401 Unauthorized`: التوكن غير صالح أو غير موجود
- `403 Forbidden`: ليس لديك صلاحية للوصول
- `404 Not Found`: العنصر غير موجود
- `500 Internal Server Error`: خطأ في الخادم

---

## ملاحظات مهمة للمطور

1. **إدارة التوكن**: احفظ التوكن بشكل آمن (مثلاً: SecureStorage في Flutter)
2. **تجديد التوكن**: راقب `expiresAt` وقم بتجديد التوكن قبل انتهاء الصلاحية
3. **معالجة الأخطاء**: تحقق من رموز الحالة ومعالجتها بشكل مناسب
4. **التاريخ والوقت**: استخدم صيغة ISO 8601 للتواريخ والأوقات
5. **الصلاحيات**: تحقق من صلاحيات المستخدم قبل عرض الخيارات (Admin vs Employee)
6. **التحديث التلقائي**: الرصيد والملخصات تُحدّث تلقائياً عند تسجيل الدخول/الخروج أو إضافة زمنية

---

## اختبار الـAPI

يمكنك استخدام Swagger UI للاختبار:
- افتح: `http://localhost:5151/swagger`
- اضغط على "Authorize" وأدخل التوكن
- جرّب جميع الـEndpoints مباشرة

---

**آخر تحديث**: نوفمبر 2024

