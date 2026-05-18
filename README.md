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
- **POPIA compliant** — privacy policy, terms of service, cookie consent, and data protection in line with South African law

## Why SA-specific?

Most invoicing tools are built for US or EU markets. TracKeee handles South African VAT (15%), ZAR currency, and integrates with Yoco — a payment gateway built for the SA market. Legal pages comply with the Protection of Personal Information Act (POPIA) rather than GDPR, and invoices are formatted for SA business conventions.

## Tech Stack

| Layer | Technology | Why |
|-------|-----------|-----|
| Backend | ASP.NET Core MVC (.NET 8) | Industry standard, strongly typed, good tooling |
| Database | Azure SQL Database + Entity Framework Core | Managed SQL with code-first migrations |
| Auth | ASP.NET Core Identity | Built-in user management with email confirmation |
| Email | Brevo SMTP | 300 free transactional emails/day, reliable delivery |
| PDF Generation | QuestPDF | Clean fluent API, no external dependencies |
| Payments | Yoco Checkout API | SA-native payment gateway, per-tenant API keys |
| Hosting | Azure App Service (South Africa North) | Low latency for SA users, free tier available |
| Frontend | Razor Views + Bootstrap + JavaScript | Server-rendered, minimal client complexity |

## Architecture

### Multi-Tenancy

Multi-tenancy is implemented via application-level tenant scoping rather than separate databases or schemas. Every query is filtered by the authenticated user's identifier, enforced at the controller level using ASP.NET Core Identity's `UserManager`, so there is no path for one tenant's data to surface in another's workspace. This approach trades some isolation strictness for operational simplicity — one database, one deployment, straightforward Azure SQL costs at small scale.

### Payment Integration

Payment processing follows a per-tenant integration model. Each freelancer connects their own Yoco account by entering their API key in their Business Profile. When a client pays an invoice, the payment flows directly to the freelancer's Yoco account — TracKeee facilitates the checkout flow but never holds funds. This avoids marketplace payment complexity and regulatory requirements.

### Invoice Generation

Invoices are auto-generated from uninvoiced time entries. The system aggregates all unbilled hours for a selected client, calculates the subtotal based on each project's hourly rate, applies SA VAT at 15%, and produces a PDF with the freelancer's branding, banking details, and a shareable payment link. Time entries are then marked as invoiced and linked to the invoice record, preventing double-billing.

### Email Architecture

Transactional emails (account confirmation, password reset) are sent via Brevo SMTP. The email sender is registered as a dependency-injected service implementing `IEmailSender`, making it swappable for any other provider without changing application code.

## Features

| Feature | Description |
|---------|------------|
| Clients | CRUD with tenant-scoped filtering |
| Projects | Linked to clients, hourly rates, status workflow |
| Time Entries | Log hours, automatic amount calculation, invoiced/pending tracking |
| Invoices | Auto-generated from time entries, VAT calculation, status workflow (Draft → Sent → Paid) |
| PDF Invoices | Branded with business profile, banking details, and online payment link |
| Business Profile | Company name, logo, address, banking details, Yoco API key |
| Yoco Payments | Per-tenant payment integration, public payment page for clients |
| Dashboard | Stats cards, recent activity, quick actions |
| Auth | Registration with email confirmation, login/logout |
| Legal & Compliance | POPIA-compliant privacy policy, terms of service, cookie consent |

## Planned Features

| Phase | Feature | Status |
|-------|---------|--------|
| 1 | Organization-level tenancy with team members | Planned |
| 2 | Role-based authorization (Owner, Admin, Accountant, Employee) | Planned |
| 3 | Security hardening (2FA, rate limiting, audit logging) | Planned |
| 4 | Live start/stop timer and dashboard charts | Planned |
| 5 | Search and filtering across all pages | Planned |
| 6 | Email invoices directly to clients from the app | Planned |
| 7 | Reporting (monthly revenue, hours by client, profit/loss) | Planned |
| 8 | Client portal with unique login link | Planned |
| 9 | CSV/Excel exports and structured logging (Serilog) | Planned |
| 10 | Activity log and audit trail | Planned |
| 11 | Custom UI — replacing Bootstrap with hand-crafted CSS | Planned |

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
