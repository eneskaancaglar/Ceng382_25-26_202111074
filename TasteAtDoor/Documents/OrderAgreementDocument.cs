using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TasteAtDoor.Models;

namespace TasteAtDoor.Documents
{
    public class OrderAgreementDocument : IDocument
    {
        private readonly Order _order;

        public OrderAgreementDocument(Order order)
        {
            _order = order;
        }

        public DocumentMetadata GetMetadata() => new()
        {
            Title = $"Catering Service Agreement - Request #{_order.Id}",
            Author = "TasteAtDoor",
            Subject = "Catering Service and Event Food Supply Agreement",
            Keywords = "catering, agreement, event, service, TasteAtDoor, Turkish law"
        };

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);

                page.Content()
                    .PaddingVertical(12)
                    .Column(column =>
                    {
                        column.Spacing(10);

                        ComposeImportantNotice(column);
                        ComposeParties(column);
                        ComposeEventInformation(column);
                        ComposePackageTable(column);
                        ComposeLegalTerms(column);
                        ComposeKvkkTerms(column);
                        ComposeSignatureArea(column);
                    });

                page.Footer().Element(ComposeFooter);
            });
        }

        private void ComposeHeader(IContainer container)
        {
            var primaryCaterer = GetPrimaryCaterer();
            var logoBytes = GetLogoBytesForPdf(primaryCaterer) ?? GetFirstPackageImageBytesForPdf();

            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem(2).Column(left =>
                    {
                        left.Item().Text("CATERING SERVICE AND EVENT")
                            .FontSize(15)
                            .Bold()
                            .FontColor(Colors.Red.Darken4);

                        left.Item().Text("FOOD SUPPLY AGREEMENT")
                            .FontSize(15)
                            .Bold()
                            .FontColor(Colors.Red.Darken4);

                        left.Item().PaddingTop(3).Text($"Agreement / Request No: #{_order.Id}")
                            .FontSize(10)
                            .Bold();

                        left.Item().Text($"Issue Date: {DateTime.Now:dd.MM.yyyy HH:mm}")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken2);

                        left.Item().Text("Electronically generated project agreement document.")
                            .FontSize(8)
                            .Italic()
                            .FontColor(Colors.Grey.Darken2);
                    });

                    row.ConstantItem(125).AlignRight().Column(right =>
                    {
                        if (logoBytes is not null)
                        {
                            right.Item()
                                .AlignRight()
                                .Width(105)
                                .Height(70)
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(4)
                                .Image(logoBytes)
                                .FitArea();

                            right.Item().PaddingTop(3).AlignCenter().Text("Service Provider Logo")
                                .FontSize(7)
                                .FontColor(Colors.Grey.Darken2);
                        }
                        else
                        {
                            right.Item()
                                .Width(105)
                                .Height(70)
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Background(Colors.Grey.Lighten4)
                                .AlignCenter()
                                .AlignMiddle()
                                .Text("CATERER\nLOGO")
                                .FontSize(12)
                                .Bold()
                                .FontColor(Colors.Grey.Darken2);

                            right.Item().PaddingTop(3).AlignCenter().Text("No PNG/JPG caterer logo found")
                                .FontSize(7)
                                .FontColor(Colors.Grey.Darken2);
                        }
                    });
                });

                column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Red.Darken4);
            });
        }

        private void ComposeImportantNotice(ColumnDescriptor column)
        {
            column.Item()
                .Border(1)
                .BorderColor(Colors.Orange.Lighten2)
                .Background(Colors.Orange.Lighten5)
                .Padding(8)
                .Column(box =>
                {
                    box.Item().Text("PRELIMINARY INFORMATION AND LEGAL NATURE")
                        .Bold()
                        .FontColor(Colors.Orange.Darken4);

                    box.Item().PaddingTop(3).Text(
                        "This agreement is an electronically generated catering service and event food supply agreement draft, created in relation to the catering service request submitted by the customer through the TasteAtDoor platform. " +
                        "The agreement is deemed to be formed between the customer and the service provider caterer based on the event date, guest count, selected catering packages, additional options, service address, and party information recorded in the system.");

                    box.Item().PaddingTop(3).Text(
                        "This document is a project output. For real commercial use, the agreement should be reviewed by a qualified lawyer and customized according to the service provider's business scope, tax/commercial registry information, food safety obligations, consumer legislation, and applicable Turkish law.")
                        .Italic()
                        .FontSize(8.5f);
                });
        }

        private void ComposeParties(ColumnDescriptor column)
        {
            var caterers = GetCaterers();

            column.Item().Element(SectionTitle).Text("1. PARTIES");

            column.Item().Row(row =>
            {
                row.RelativeItem().Element(InfoBox).Column(customer =>
                {
                    customer.Item().Text("1.1. Customer / Service Recipient").Bold().FontColor(Colors.Red.Darken4);
                    customer.Item().Text($"Full Name / Title: {Safe(_order.ApplicationUser?.FullName, "Unknown")}");
                    customer.Item().Text($"Email: {Safe(_order.ApplicationUser?.Email, "Not specified")}");
                    customer.Item().Text($"Phone: {Safe(_order.ApplicationUser?.PhoneNumber, "Not specified")}");
                    customer.Item().Text($"Registered Address: {Safe(_order.ApplicationUser?.Address, "Not specified")}");
                });

                row.ConstantItem(10);

                row.RelativeItem().Element(InfoBox).Column(platform =>
                {
                    platform.Item().Text("1.2. Platform").Bold().FontColor(Colors.Red.Darken4);
                    platform.Item().Text("Platform Name: TasteAtDoor");
                    platform.Item().Text("Role: A project platform that enables electronic request creation, document generation, and communication between the parties.");
                    platform.Item().Text("Note: Within the scope of this project, the platform provides payment simulation and document generation services.");
                });
            });

            column.Item().Element(InfoBox).Column(catererBox =>
            {
                catererBox.Item().Text("1.3. Service Provider Caterer / Caterers")
                    .Bold()
                    .FontColor(Colors.Red.Darken4);

                if (!caterers.Any())
                {
                    catererBox.Item().Text("Service provider information could not be found.");
                    return;
                }

                foreach (var caterer in caterers)
                {
                    catererBox.Item().PaddingTop(4).Text($"Caterer: {Safe(caterer.FullName, "Caterer")}")
                        .Bold();

                    catererBox.Item().Text($"Email: {Safe(caterer.Email, "Not specified")}");
                    catererBox.Item().Text($"Phone: {Safe(caterer.PhoneNumber, "Not specified")}");
                    catererBox.Item().Text($"Service / Business Address: {Safe(caterer.Address, "Not specified")}");
                }
            });
        }

        private void ComposeEventInformation(ColumnDescriptor column)
        {
            column.Item().Element(SectionTitle).Text("2. EVENT AND SERVICE INFORMATION");

            column.Item().Element(InfoBox).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(2.8f);
                });

                AddInfoRow(table, "Request No", $"#{_order.Id}");
                AddInfoRow(table, "Request Date", _order.OrderDate.ToString("dd.MM.yyyy HH:mm"));
                AddInfoRow(table, "Request Status", Safe(_order.Status, "Pending"));
                AddInfoRow(table, "Event Type", Safe(_order.EventType, "Not specified"));
                AddInfoRow(table, "Event Date", _order.EventDate.HasValue ? _order.EventDate.Value.ToString("dd.MM.yyyy HH:mm") : "Not specified");
                AddInfoRow(table, "Total Guest Count", _order.GuestCount > 0 ? _order.GuestCount.ToString() : CalculateGuestCount().ToString());
                AddInfoRow(table, "Event Address", Safe(_order.EventAddress, "Not specified"));
                AddInfoRow(table, "Customer Note", Safe(_order.EventNote, "None"));
                AddInfoRow(table, "Total Estimated Price", $"{_order.TotalPrice:0.00} TL");
            });
        }

        private void ComposePackageTable(ColumnDescriptor column)
        {
            column.Item().Element(SectionTitle).Text("3. SELECTED CATERING PACKAGES AND PRICE");

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.4f);
                    columns.RelativeColumn(1.8f);
                    columns.RelativeColumn(0.9f);
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(1.1f);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Package");
                    header.Cell().Element(HeaderCell).Text("Caterer");
                    header.Cell().Element(HeaderCell).Text("Guests");
                    header.Cell().Element(HeaderCell).Text("Base Unit");
                    header.Cell().Element(HeaderCell).Text("Final Unit");
                    header.Cell().Element(HeaderCell).Text("Total");
                });

                foreach (var item in _order.OrderItems)
                {
                    table.Cell().Element(BodyCell).Column(package =>
                    {
                        package.Item().Text(Safe(item.MenuItem?.Name, "Catering Package")).Bold();

                        package.Item().Text(Safe(item.MenuItem?.Description, "No description provided"))
                            .FontSize(8)
                            .FontColor(Colors.Grey.Darken2);

                        if (!string.IsNullOrWhiteSpace(item.MenuItem?.IncludedItems))
                        {
                            package.Item().PaddingTop(2).Text($"Included Items: {item.MenuItem.IncludedItems}")
                                .FontSize(8);
                        }

                        if (!string.IsNullOrWhiteSpace(item.MenuItem?.ServiceDetails))
                        {
                            package.Item().Text($"Service Details: {item.MenuItem.ServiceDetails}")
                                .FontSize(8);
                        }

                        if (item.SelectedCustomizations.Any())
                        {
                            package.Item().PaddingTop(3).Text("Additional Options:").FontSize(8).Bold();

                            foreach (var customization in item.SelectedCustomizations)
                            {
                                var price = customization.PriceChange != 0
                                    ? $" ({customization.PriceChange:0.00} TL / guest)"
                                    : "";

                                package.Item().Text($"- {customization.GroupTitle}: {customization.OptionName}{price}")
                                    .FontSize(8);
                            }
                        }
                        else
                        {
                            package.Item().PaddingTop(2).Text("No additional option selected.")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Darken2);
                        }
                    });

                    table.Cell().Element(BodyCell).Text(Safe(item.MenuItem?.Caretaker?.FullName, "Caterer"));
                    table.Cell().Element(BodyCell).Text(item.Quantity.ToString());
                    table.Cell().Element(BodyCell).Text($"{item.BaseUnitPrice:0.00} TL");
                    table.Cell().Element(BodyCell).Text($"{item.FinalUnitPrice:0.00} TL");
                    table.Cell().Element(BodyCell).Text($"{item.LineTotal:0.00} TL");
                }
            });

            column.Item()
                .AlignRight()
                .PaddingTop(5)
                .Text($"GRAND TOTAL: {_order.TotalPrice:0.00} TL")
                .Bold()
                .FontSize(12)
                .FontColor(Colors.Red.Darken4);
        }

        private void ComposeLegalTerms(ColumnDescriptor column)
        {
            column.Item().Element(SectionTitle).Text("4. AGREEMENT TERMS");

            AddArticle(column, "4.1. Subject of the Agreement",
                "The subject of this agreement is to define the rights and obligations of the parties regarding the provision of catering services, including food, desserts, snacks, beverages, service support, and/or event-related food supply services, based on the catering packages, additional options, event details, guest count, and event address selected by the customer through the TasteAtDoor platform.");

            AddArticle(column, "4.2. Nature of the Service",
                "The catering service is prepared specifically for a particular event by taking into account the event type, event date, guest count, package content, procurement preparation, and service conditions. Therefore, the service has the nature of a date-specific and event-specific service/supply arrangement rather than a standard product sale.");

            AddArticle(column, "4.3. Obligations of the Service Provider",
                "The service provider caterer agrees to perform the service with due care, taking into account the selected package content, registered guest count, event date, event address, and any additional options. The caterer shall exercise professional diligence regarding food safety, hygiene, packaging, delivery, and, where applicable, service staff organization.");

            AddArticle(column, "4.4. Obligations of the Customer",
                "The customer agrees to provide accurate and complete information required for the performance of the service, including the event date, event address, guest count, access/setup conditions, and special notes. Any delay, additional cost, or service disruption arising from incorrect or incomplete information shall be borne by the party responsible for providing such incorrect or incomplete information.");

            AddArticle(column, "4.5. Price and Payment",
                "The agreement price is calculated based on the per-guest price of the selected packages, the guest count, and any additional option prices. Within the scope of this project, the payment transaction is a simulation. In real commercial use, payment, advance payment, remaining balance, invoice, and collection terms should be regulated separately by the parties.");

            AddArticle(column, "4.6. Change Requests",
                "The customer must notify the caterer of any requested changes regarding guest count, address, date, menu content, or service conditions within a reasonable time. Acceptance of changes requested shortly before the event date depends on the caterer's procurement, production, and operational capacity. Such changes may result in additional fees or modifications to the service scope.");

            AddArticle(column, "4.7. Cancellation Policy",
                "Since catering services are prepared for a specific date and event, require procurement planning, and may involve perishable food products, cancellation requests shall be evaluated according to the time remaining until the event date. In the event of late cancellation, preparation, personnel, material, procurement, and operational costs already incurred by the caterer may be charged to the customer. In real commercial use, cancellation periods and refund rates should be clearly defined by the caterer.");

            AddArticle(column, "4.8. Right of Withdrawal and Exceptions",
                "Where the customer qualifies as a consumer, the applicable provisions of consumer protection legislation shall remain reserved. However, exceptions to the right of withdrawal may apply to services and goods prepared specifically for personal/event needs, perishable food products, and food/beverage supply services to be performed on a specific date. This clause is a project agreement draft; legal review under current Turkish legislation is recommended for real commercial use.");

            AddArticle(column, "4.9. Defective or Incomplete Performance Notice",
                "The customer shall notify any alleged defect, shortage, delay, or quality issue regarding delivery, service, package content, or performance as soon as possible, preferably during the event or immediately after the service is performed, through platform chat records, email, or written notice. The parties agree to seek a solution in good faith.");

            AddArticle(column, "4.10. Force Majeure",
                "Earthquakes, floods, fires, epidemics, decisions of public authorities, transportation restrictions, severe weather conditions, general strikes, war, terrorist incidents, utility outages, and similar circumstances beyond the reasonable control of the parties shall be considered force majeure. In the event of force majeure, the affected party shall notify the other party as soon as reasonably possible, and the parties shall seek a reasonable solution such as postponement, modification, or cancellation of the service.");

            AddArticle(column, "4.11. Role of the Platform",
                "TasteAtDoor is an electronic project platform that enables the customer and caterer to meet, create catering requests, communicate through chat/live call, perform payment simulation, and generate PDF documents. In real commercial use, the platform's legal role as intermediary service provider, seller, service provider, data controller, or data processor should be expressly defined.");

            AddArticle(column, "4.12. Electronic Approval",
                "By completing the checkout/payment simulation step, the customer is deemed to have electronically accepted the selected packages, guest count, event information, and the terms of this agreement. The caterer may review the catering request through the system, and the parties may communicate through platform chat/live call regarding the request.");

            AddArticle(column, "4.13. Dispute Resolution",
                "The parties shall first attempt to resolve disputes in good faith through platform communication records and written notices. Where the customer qualifies as a consumer, statutory application rights before consumer arbitration committees and consumer courts under Turkish consumer legislation remain reserved. For commercial disputes, the rules regarding competent courts and enforcement offices shall apply in accordance with general Turkish law.");
        }

        private void ComposeKvkkTerms(ColumnDescriptor column)
        {
            column.Item().Element(SectionTitle).Text("5. PROTECTION OF PERSONAL DATA AND INFORMATION NOTICE");

            AddArticle(column, "5.1. Categories of Processed Data",
                "Within the scope of the establishment and performance of this agreement, personal data such as customer name, email address, phone number, registered address, event address, event note, request/package information, payment simulation information, chat records, and system transaction records may be processed.");

            AddArticle(column, "5.2. Purposes of Processing",
                "Personal data is processed for the purposes of creating the catering request, transmitting the request to the relevant caterer, enabling communication between the parties, generating receipt/agreement documents, conducting payment simulation, displaying request history, managing disputes, and ensuring system security.");

            AddArticle(column, "5.3. Legal Basis and Transfer",
                "Personal data may be processed on the legal grounds that such processing is directly related to the establishment or performance of a contract, necessary for compliance with legal obligations, and necessary for the establishment, exercise, or protection of a right. Request information is shared with the relevant caterer for the performance of the service. In real commercial use, data controller/data processor roles, retention periods, and data subject application channels should be expressly stated.");

            AddArticle(column, "5.4. Rights of the Data Subject",
                "Data subjects may, within the scope and conditions provided by applicable legislation, request information on whether their personal data is processed, request details of such processing, request correction, deletion or destruction, learn the third parties to whom data is transferred, and claim compensation if they suffer damage due to unlawful processing.");
        }

        private void ComposeSignatureArea(ColumnDescriptor column)
        {
            column.Item().PaddingTop(8).Element(SectionTitle).Text("6. ELECTRONIC ACCEPTANCE AND SIGNATURE AREA");

            column.Item().Text(
                "This document has been electronically generated by the TasteAtDoor system. Within the scope of this project, customer approval is associated with the checkout/payment simulation step, and caterer notification is associated with the viewing of the request created in the system. In real commercial use requiring physical signatures, the parties may sign the fields below.");

            column.Item().PaddingTop(18).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("CUSTOMER / SERVICE RECIPIENT").Bold().FontColor(Colors.Red.Darken4);
                    c.Item().Text(Safe(_order.ApplicationUser?.FullName, "Customer"));
                    c.Item().PaddingTop(28).LineHorizontal(1);
                    c.Item().Text("Signature").FontSize(8).FontColor(Colors.Grey.Darken2);
                });

                row.ConstantItem(35);

                row.RelativeItem().Column(c =>
                {
                    var primaryCaterer = GetPrimaryCaterer();

                    c.Item().Text("CATERER / SERVICE PROVIDER").Bold().FontColor(Colors.Red.Darken4);
                    c.Item().Text(Safe(primaryCaterer?.FullName, "Caterer"));
                    c.Item().PaddingTop(28).LineHorizontal(1);
                    c.Item().Text("Signature / Stamp").FontSize(8).FontColor(Colors.Grey.Darken2);
                });
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Text($"TasteAtDoor Catering Agreement - Request #{_order.Id}")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken2);

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken2));
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }

        private static void AddArticle(ColumnDescriptor column, string title, string text)
        {
            column.Item()
                .BorderLeft(3)
                .BorderColor(Colors.Red.Darken4)
                .PaddingLeft(7)
                .Column(article =>
                {
                    article.Item().Text(title)
                        .Bold()
                        .FontColor(Colors.Red.Darken4);

                    article.Item().PaddingTop(2).Text(text);
                });
        }

        private static void AddInfoRow(TableDescriptor table, string label, string value)
        {
            table.Cell().Element(LabelCell).Text(label).Bold();
            table.Cell().Element(ValueCell).Text(value);
        }

        private static IContainer SectionTitle(IContainer container)
        {
            return container
                .PaddingTop(3)
                .PaddingBottom(3)
                .BorderBottom(1)
                .BorderColor(Colors.Red.Darken4)
                .DefaultTextStyle(x => x.FontSize(12).Bold().FontColor(Colors.Red.Darken4));
        }

        private static IContainer InfoBox(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Background(Colors.Grey.Lighten5)
                .Padding(8);
        }

        private static IContainer HeaderCell(IContainer container)
        {
            return container
                .Background(Colors.Red.Darken4)
                .DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(8))
                .Padding(5)
                .Border(1)
                .BorderColor(Colors.Red.Darken4);
        }

        private static IContainer BodyCell(IContainer container)
        {
            return container
                .Padding(5)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .DefaultTextStyle(x => x.FontSize(8));
        }

        private static IContainer LabelCell(IContainer container)
        {
            return container
                .PaddingVertical(3)
                .PaddingRight(5)
                .DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Red.Darken4));
        }

        private static IContainer ValueCell(IContainer container)
        {
            return container
                .PaddingVertical(3)
                .DefaultTextStyle(x => x.FontSize(9));
        }

        private ApplicationUser? GetPrimaryCaterer()
        {
            return _order.OrderItems
                .Select(i => i.MenuItem?.Caretaker)
                .FirstOrDefault(c => c is not null);
        }

        private List<ApplicationUser> GetCaterers()
        {
            return _order.OrderItems
                .Where(i => i.MenuItem?.Caretaker is not null)
                .Select(i => i.MenuItem!.Caretaker!)
                .GroupBy(c => c.Id)
                .Select(g => g.First())
                .ToList();
        }

        private int CalculateGuestCount()
        {
            return _order.OrderItems.Sum(i => i.Quantity);
        }

        private static byte[]? GetLogoBytesForPdf(ApplicationUser? caterer)
        {
            if (caterer?.ProfileImageData is null || caterer.ProfileImageData.Length == 0)
            {
                return null;
            }

            var data = caterer.ProfileImageData;

            if (IsPng(data) || IsJpeg(data))
            {
                return data;
            }

            return null;
        }

        private byte[]? GetFirstPackageImageBytesForPdf()
        {
            var packageImage = _order.OrderItems
                .Select(i => i.MenuItem?.ImageData)
                .FirstOrDefault(data => data is not null && data.Length > 0);

            if (packageImage is null || packageImage.Length == 0)
            {
                return null;
            }

            if (IsPng(packageImage) || IsJpeg(packageImage))
            {
                return packageImage;
            }

            return null;
        }

        private static bool IsPng(byte[] data)
        {
            return data.Length >= 8 &&
                   data[0] == 0x89 &&
                   data[1] == 0x50 &&
                   data[2] == 0x4E &&
                   data[3] == 0x47 &&
                   data[4] == 0x0D &&
                   data[5] == 0x0A &&
                   data[6] == 0x1A &&
                   data[7] == 0x0A;
        }

        private static bool IsJpeg(byte[] data)
        {
            return data.Length >= 3 &&
                   data[0] == 0xFF &&
                   data[1] == 0xD8 &&
                   data[2] == 0xFF;
        }

        private static string Safe(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value)
                ? fallback
                : value.Trim();
        }
    }
}

