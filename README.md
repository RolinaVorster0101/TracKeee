# TracKeee

> Time tracking, invoicing, and client management for South African freelancers and small agencies.

![Status](https://img.shields.io/badge/status-in%20progress-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Azure](https://img.shields.io/badge/Azure-App%20Service-blue)
> 🌐 [Live Demo](https://trackeee-app-f4auhacrcdhqbxbn.southafricanorth-01.azurewebsites.net)

## What it does

TracKeee is a multi-tenant SaaS platform built for South African freelancers and small agencies who need a clean, simple way to manage their client work and get paid.

- **Time tracking** — log hours against projects and clients
- **Invoice generation** — create professional invoices with VAT calculation (SA-compliant)
- **PDF export** — download and send invoices directly to clients
- **Client portal** — clients can view their projects and invoices
- **Project status** — track project progress from brief to delivery
- **Yoco payments** — integrated SA payment processing

## Why SA-specific?

Most invoicing tools are built for US or EU markets. TracKeee handles South African VAT (15%), ZAR currency, and integrates with Yoco — a payment gateway built for the SA market.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core MVC (.NET 8) |
| Database | Azure SQL Database + Entity Framework Core |
| Auth | Microsoft Identity (multi-tenant) |
| File Storage | Azure Blob Storage (invoice PDFs) |
| Payments | Yoco API |
| Hosting | Azure App Service |
| Frontend | Razor Views + CSS + JavaScript |

## Architecture

Multi-tenant — each freelancer or agency gets their own isolated workspace. One deployment serves all tenants.

## Status

🚧 **In active development** — building in public.

🌐 **Live demo:** https://trackeee-app-f4auhacrcdhqbxbn.southafricanorth-01.azurewebsites.net

Follow progress via commits and the project board.

## Running Locally

### Prerequisites
- .NET 8 SDK
- SQL Server LocalDB (installed with Visual Studio)
- A Brevo account for email sending (free tier)

### Setup

1. Clone the repository:
```bash
   git clone https://github.com/RolinaVorster0101/TracKeee
   cd TracKeee
```

2. Create an `appsettings.Development.json` file in the project root with the following structure:
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

## Author

**Rolina Vorster** — Full-Stack Developer & Visual Experience Designer
[LinkedIn](https://www.linkedin.com/in/rolina-vorster) | [GitHub](https://github.com/RolinaVorster0101) 
