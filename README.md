# TracKeee

> Time tracking, invoicing, and client management for South African freelancers and small agencies.

![Status](https://img.shields.io/badge/status-in%20progress-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Azure](https://img.shields.io/badge/Azure-App%20Service-blue)

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

Follow progress via commits and the project board.

## Running Locally

```bash
git clone https://github.com/RolinaVorster0101/TracKeee
cd TracKeee
dotnet restore
dotnet run
```

*Full setup instructions coming as the project develops.*

## Author

**Rolina Vorster** — Full-Stack Developer & Visual Experience Designer
[LinkedIn](https://www.linkedin.com/in/rolina-vorster) | [GitHub](https://github.com/RolinaVorster0101) 
