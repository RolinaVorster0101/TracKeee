using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TracKeee.Models;

namespace TracKeee.Services
{
    public class InvoicePdfService
    {
        public byte[] GenerateInvoicePdf(Invoice invoice, string? paymentUrl = null)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(c => ComposeHeader(c, invoice));
                    page.Content().Element(c => ComposeContent(c, invoice, paymentUrl));
                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeHeader(IContainer container, Invoice invoice)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text("INVOICE").Bold().FontSize(24).FontColor("#333333");
                        left.Item().Text(invoice.InvoiceNumber).FontSize(12).FontColor("#666666");
                    });

                    row.RelativeItem().AlignRight().Column(right =>
                    {
                        right.Item().Text("TracKeee").Bold().FontSize(16).FontColor("#0d6efd");
                        right.Item().Text("Time Tracking & Invoicing");
                    });
                });

                column.Item().PaddingVertical(10).LineHorizontal(1).LineColor("#dddddd");

                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text("Bill To:").Bold().FontColor("#666666");
                        left.Item().Text(invoice.Client?.Name ?? "").Bold().FontSize(12);
                        if (!string.IsNullOrEmpty(invoice.Client?.ContactPerson))
                            left.Item().Text(invoice.Client.ContactPerson);
                        if (!string.IsNullOrEmpty(invoice.Client?.Email))
                            left.Item().Text(invoice.Client.Email);
                        if (!string.IsNullOrEmpty(invoice.Client?.Address))
                            left.Item().Text(invoice.Client.Address);
                        if (!string.IsNullOrEmpty(invoice.Client?.VatNumber))
                            left.Item().Text($"VAT: {invoice.Client.VatNumber}");
                    });

                    row.RelativeItem().AlignRight().Column(right =>
                    {
                        right.Item().Text($"Issue Date: {invoice.IssueDate:dd MMM yyyy}");
                        right.Item().Text($"Due Date: {invoice.DueDate:dd MMM yyyy}");
                        right.Item().Padding(5).Background("#f8f9fa").Text($"Status: {invoice.Status}").Bold();
                    });
                });

                column.Item().PaddingTop(15);
            });
        }

        private void ComposeContent(IContainer container, Invoice invoice, string? paymentUrl)
        {
            container.Column(column =>
            {
                // Time entries table
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(80);  // Date
                        columns.RelativeColumn(2);    // Project
                        columns.RelativeColumn(3);    // Description
                        columns.ConstantColumn(50);   // Hours
                        columns.ConstantColumn(70);   // Rate
                        columns.ConstantColumn(80);   // Amount
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Background("#333333").Padding(5).Text("Date").FontColor("#ffffff").Bold();
                        header.Cell().Background("#333333").Padding(5).Text("Project").FontColor("#ffffff").Bold();
                        header.Cell().Background("#333333").Padding(5).Text("Description").FontColor("#ffffff").Bold();
                        header.Cell().Background("#333333").Padding(5).AlignRight().Text("Hours").FontColor("#ffffff").Bold();
                        header.Cell().Background("#333333").Padding(5).AlignRight().Text("Rate").FontColor("#ffffff").Bold();
                        header.Cell().Background("#333333").Padding(5).AlignRight().Text("Amount").FontColor("#ffffff").Bold();
                    });

                    // Rows
                    var entries = invoice.TimeEntries?.OrderBy(t => t.Date).ToList() ?? new List<TimeEntry>();
                    foreach (var entry in entries)
                    {
                        var amount = entry.Hours * (entry.Project?.HourlyRate ?? 0);
                        var bgColor = entries.IndexOf(entry) % 2 == 0 ? "#ffffff" : "#f8f9fa";

                        table.Cell().Background(bgColor).Padding(5).Text(entry.Date.ToString("dd MMM yyyy"));
                        table.Cell().Background(bgColor).Padding(5).Text(entry.Project?.Name ?? "");
                        table.Cell().Background(bgColor).Padding(5).Text(entry.Description ?? "-");
                        table.Cell().Background(bgColor).Padding(5).AlignRight().Text(entry.Hours.ToString("N2"));
                        table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"R {(entry.Project?.HourlyRate ?? 0):N2}");
                        table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"R {amount:N2}");
                    }
                });

                column.Item().PaddingTop(15);

                // Totals
                column.Item().AlignRight().Width(250).Table(totals =>
                {
                    totals.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(100);
                    });

                    totals.Cell().Padding(5).Text("Subtotal").Bold();
                    totals.Cell().Padding(5).AlignRight().Text($"R {invoice.Subtotal:N2}");

                    totals.Cell().Padding(5).Text($"VAT ({invoice.VatRate}%)").Bold();
                    totals.Cell().Padding(5).AlignRight().Text($"R {invoice.VatAmount:N2}");

                    totals.Cell().Background("#333333").Padding(5).Text("TOTAL").Bold().FontColor("#ffffff");
                    totals.Cell().Background("#333333").Padding(5).AlignRight().Text($"R {invoice.Total:N2}").Bold().FontColor("#ffffff");
                });

                // Notes
                if (!string.IsNullOrEmpty(invoice.Notes))
                {
                    column.Item().PaddingTop(20).Column(notes =>
                    {
                        notes.Item().Text("Notes:").Bold().FontColor("#666666");
                        notes.Item().Text(invoice.Notes);
                    });
                }

                // Banking details placeholder
                column.Item().PaddingTop(30).Column(banking =>
                {
                    banking.Item().Text("Banking Details").Bold().FontColor("#666666");
                    banking.Item().Text("Bank: [Your Bank]");
                    banking.Item().Text("Bank: [Your Bank]");
                    banking.Item().Text("Account: [Your Account Number]");
                    banking.Item().Text("Branch: [Your Branch Code]");
                    banking.Item().Text($"Reference: {invoice.InvoiceNumber}");
                });

                // Payment link
                if (!string.IsNullOrEmpty(paymentUrl) && invoice.Status != InvoiceStatus.Paid)
                {
                    column.Item().PaddingTop(20).Background("#e8f5e9").Padding(15).Column(pay =>
                    {
                        pay.Item().Text("Pay Online").Bold().FontSize(12).FontColor("#2e7d32");
                        pay.Item().Text("Click the link below or copy it into your browser to pay this invoice securely via Yoco:");
                        pay.Item().PaddingTop(5).Hyperlink(paymentUrl).Text(paymentUrl).FontColor("#0d6efd").Underline();
                    });
                }
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().LineHorizontal(1).LineColor("#dddddd");
                column.Item().PaddingTop(5).AlignCenter()
                    .Text("Generated by TracKeee — Time Tracking & Invoicing for SA Freelancers")
                    .FontSize(8).FontColor("#999999");
            });
        }
    }
}