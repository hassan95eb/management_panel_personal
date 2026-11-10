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

Neon، EF Core و اولین Migration در MVP-03 اضافه می‌شوند. رشتهٔ اتصال در محیط API تنظیم می‌شود.
