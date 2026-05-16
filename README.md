# TracKeee

> Time tracking, invoicing, and client management for South African freelancers and small agencies.

![Status](https://img.shields.io/badge/status-in%20progress-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Azure](https://img.shields.io/badge/Azure-App%20Service-blue)

> 🌐 [Live Demo](https://trackeee-app-f4auhacrcdhqbxbn.southafricanorth-01.azurewebsites.net)

## What it does

TracKeee is a multi-tenant SaaS platform built for South African freelancers and small agencies who need a clean, simple way to manage their client work and get paid.

- **Client management** — add and manage clients with contact details and VAT numbers
- **Project tracking** — create projects linked to clients with hourly rates and status tracking (Active, On Hold, Completed, Cancelled)
- **Time tracking** — log hours against projects with automatic amount calculation
- **Invoice generation** — auto-generate invoices from uninvoiced time entries with SA VAT (15%) calculated
- **PDF export** — download professional branded invoices with your business details, banking info, and payment link
- **Business profile** — customise your invoices with your company name, logo, address, and banking details
- **Online payments** — integrated Yoco payment gateway — each freelancer connects their own Yoco account, clients pay via a shareable payment link
- **Dashboard** — real-time overview of clients, projects, hours logged, uninvoiced amounts, and recent activity
- **Email confirmation** — secure registration with email verification via Brevo SMTP

## Why SA-specific?

Most invoicing tools are built for US or EU markets. TracKeee handles South African VAT (15%), ZAR currency, and integrates with Yoco — a payment gateway built for the SA market.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core MVC (.NET 8) |
| Database | Azure SQL Database + Entity Framework Core |
| Auth | ASP.NET Core Identity (multi-tenant) |
| Email | Brevo SMTP (transactional emails) |
| PDF Generation | QuestPDF |
| Payments | Yoco Checkout API |
| Hosting | Azure App Service (South Africa North) |
| Frontend | Razor Views + Bootstrap + JavaScript |

## Architecture

Multi-tenant — each freelancer or agency gets their own isolated workspace with their own clients, projects, time entries, invoices, and business profile. One deployment serves all tenants.

## Features

| Feature | Description |
|---------|------------|
| Clients | CRUD with multi-tenant filtering |
| Projects | Linked to clients, hourly rates, status workflow |
| Time Entries | Log hours, automatic amount calculation, invoiced/pending tracking |
| Invoices | Auto-generated from time entries, VAT calculation, status workflow (Draft → Sent → Paid) |
| PDF Invoices | Branded with business profile, banking details, and online payment link |
| Business Profile | Company name, logo, address, banking details, Yoco API key |
| Yoco Payments | Per-user payment integration, public payment page for clients |
| Dashboard | Stats cards, recent activity, quick actions |
| Auth | Registration with email confirmation, login/logout |

## Status

🚧 **In active development** — building in public.

🌐 **Live demo:** https://trackeee-app-f4auhacrcdhqbxbn.southafricanorth-01.azurewebsites.net

Follow progress via commits.

## Running Locally

### Prerequisites
- .NET 8 SDK
- SQL Server LocalDB (installed with Visual Studio)
- Entity Framework Core tools (`dotnet tool install --global dotnet-ef`)
- A Brevo account for email sending (free tier — 300 emails/day)

### Setup

1. Clone the repository:
```bash
git clone https://github.com/RolinaVorster0101/TracKeee
cd TracKeee
```

2. Create an `appsettings.Development.json` file in the project root:
```json
{
  "ConnectionStrings": {
    "ApplicationDbContextConnection": "Server=(localdb)\\mssqllocaldb;Database=TracKeee;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "BrevoSmtp": {
    "Server": "smtp-relay.brevo.com",
    "Port": 587,
    "Username": "YOUR_BREVO_USERNAME",
    "Password": "YOUR_BREVO_SMTP_KEY",
    "FromEmail": "YOUR_VERIFIED_SENDER_EMAIL",
    "FromName": "TracKeee"
  }
}
```

3. Restore packages and run migrations:
```bash
dotnet restore
dotnet ef database update
```

4. Run the app:
```bash
dotnet run
```

The app will be available at `https://localhost:7151`.

### Yoco Payment Setup (Optional)
To enable online payments, create a Yoco account at [yoco.co.za](https://www.yoco.co.za), get your API keys from the Yoco Portal (Sales → Payment Gateway), and enter your secret key in the Business Settings page within the app.

## Author

**Rolina Vorster** — Full-Stack Developer & Visual Experience Designer

[LinkedIn](https://www.linkedin.com/in/rolina-vorster) | [GitHub](https://github.com/RolinaVorster0101)
