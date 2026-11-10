# اتاق فرمان

اسکلت MVP-02 شامل React، TypeScript و Tailwind در `web/` و ASP.NET Core 10 در `api/` است. صفحهٔ اولیه نتیجهٔ درخواست `GET /api/health` را نمایش می‌دهد.

## اجرای محلی

پیش‌نیازها: Node.js 24 و .NET 10 SDK.

از ریشهٔ ریپو، در ترمینال اول API را اجرا کنید:

```sh
dotnet run --project api/ManagementPanel.Api.csproj -- --urls http://127.0.0.1:5080
```

در ترمینال دوم:

```sh
cd web
npm ci
npm run dev
```

آدرس پیش‌فرض فرانت `http://localhost:5173` است. Vite درخواست‌های `/api` را به پورت 5080 می‌فرستد. مسیر `/api/health` پاسخ `{"status":"ok"}` می‌دهد و باید وضعیت اتصال در صفحه نمایش داده شود.

## PostgreSQL و Migration

یک دیتابیس خالی به نام `management_panel` در PostgreSQL محلی یا Neon بسازید. رشتهٔ اتصال Npgsql را در متغیر محیطی `ConnectionStrings__Panel` تنظیم کنید. در Bash می‌توانید مقدار کامل را در یک ورودی مخفی وارد کنید:

```sh
read -rsp 'PostgreSQL connection string: ' ConnectionStrings__Panel
printf '\n'
export ConnectionStrings__Panel
dotnet tool restore
dotnet ef database update --project api
```

قالب اتصال محلی `Host=localhost;Port=5432;Database=management_panel;Username=<user>;Password=<password>` است. برای Neon از مشخصات endpoint و TLS ارائه‌شده در داشبورد Neon استفاده کنید. در Render مقدار واقعی را به‌عنوان secret محیط API تنظیم کنید. Migration را از یک محیط مجاز با دسترسی به دیتابیس اجرا کنید.

Migration اولیه جدول‌های `areas` و `projects` را ایجاد می‌کند و حوزهٔ `projects` را می‌سازد. برای بررسی نتیجه، این پرس‌وجو باید یک سطر برگرداند:

```sql
SELECT "Key", "Name" FROM areas WHERE "Key" = 'projects';
```

مسیر `/api/health` وضعیت اجرای HTTP را گزارش می‌دهد و برای بالا آمدن به دیتابیس نیاز ندارد.

## Build و CI

```sh
dotnet build api/ManagementPanel.Api.csproj --configuration Release
cd web
npm ci
npm run build
```

فایل `.github/workflows/ci.yml` این دو build را هنگام push و pull request اجرا می‌کند.

## استقرار آزمایشی

1. در Render، سرویس Docker را با `render.yaml` یا تنظیمات معادل آن بسازید. Root Directory برابر `api`، پورت 10000 و مسیر بررسی سلامت `/api/health` است.
2. نشانی واقعی Render را در مقصد rewrite فایل `web/vercel.json` قرار دهید. نام پیش‌فرض داخل فایل نمونه است.
3. در Vercel ریپو را با Root Directory برابر `web`، preset برابر Vite و Node.js 24 وارد کنید. فرمان build همان `npm run build` و خروجی `dist` است.
4. در آدرس Vercel، صفحهٔ اصلی و `/api/health` را بررسی کنید. پاسخ API باید JSON با `Cache-Control: no-store` باشد.

پیش از استفاده از قابلیت‌های وابسته به داده، Migration را روی دیتابیس همان محیط اجرا کنید.
