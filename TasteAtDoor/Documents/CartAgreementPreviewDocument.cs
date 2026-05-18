using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TasteAtDoor.Models.ViewModels;

namespace TasteAtDoor.Documents;

public class CartAgreementPreviewDocument : IDocument
{
    private readonly AgreementViewModel _model;

    public CartAgreementPreviewDocument(AgreementViewModel model)
    {
        _model = model;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(28);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

            page.Header().Element(ComposeHeader);

            page.Content()
                .PaddingTop(10)
                .Column(column =>
                {
                    column.Spacing(9);

                    column.Item().Element(ComposeParties);
                    column.Item().Element(ComposePackages);
                    column.Item().Element(ComposeLegalTerms);
                    column.Item().Element(ComposeAcceptance);
                });

            page.Footer()
                .AlignCenter()
                .Text(text =>
                {
                    text.Span("TasteAtDoor Catering Agreement Preview - Page ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.Span(" / ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.TotalPages().FontSize(8).FontColor(Colors.Grey.Darken1);
                });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        var logoPackage = _model.Packages.FirstOrDefault(p => CanRenderLogo(p.CatererLogoData));

        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("TASTEATDOOR CATERING SERVICE AGREEMENT")
                    .FontSize(15)
                    .Bold()
                    .FontColor(Colors.Red.Darken4);

                column.Item().PaddingTop(4).Text("Pre-payment customer review copy")
                    .FontSize(9)
                    .FontColor(Colors.Grey.Darken2);

                column.Item().PaddingTop(7).Text($"Agreement No: {_model.AgreementNumber}")
                    .FontSize(8)
                    .Bold();

                column.Item().Text($"Agreement Date: {_model.AgreementDate:dd MMMM yyyy HH:mm}")
                    .FontSize(8);
            });

            row.ConstantItem(120).Column(column =>
            {
                if (logoPackage?.CatererLogoData is not null)
                {
                    column.Item()
                        .Width(105)
                        .Height(70)
                        .AlignRight()
                        .Border(1)
                        .BorderColor(Colors.Grey.Lighten2)
                        .Padding(4)
                        .Image(logoPackage.CatererLogoData)
                        .FitArea();

                    column.Item().PaddingTop(3).AlignCenter().Text("Caterer Logo")
                        .FontSize(7)
                        .FontColor(Colors.Grey.Darken2);
                }
                else
                {
                    column.Item()
                        .Width(105)
                        .Height(70)
                        .AlignRight()
                        .Border(1)
                        .BorderColor(Colors.Grey.Lighten2)
                        .Background(Colors.Grey.Lighten4)
                        .AlignCenter()
                        .AlignMiddle()
                        .Text("CATERER\nLOGO")
                        .FontSize(12)
                        .Bold()
                        .FontColor(Colors.Grey.Darken2);
                }
            });
        });
    }

    private void ComposeParties(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Element(c => SectionTitle(c, "1. Parties and Agreement Scope"));

            column.Item().Text("This agreement is prepared electronically through the TasteAtDoor platform before payment. The customer may read and download this document before accepting it. The catering order will only be created after this agreement is accepted and the payment simulation is completed successfully.")
                .LineHeight(1.25f);

            column.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                AddInfoCell(table, "Customer", _model.CustomerName);
                AddInfoCell(table, "Customer Email", _model.CustomerEmail);
                AddInfoCell(table, "Customer Address / Location", string.IsNullOrWhiteSpace(_model.CustomerAddress) ? "Saved customer location will be used." : _model.CustomerAddress);
                AddInfoCell(table, "Total Estimated Price", $"{_model.TotalEstimatedPrice:N2} TL");
            });
        });
    }

    private void ComposePackages(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Element(c => SectionTitle(c, "2. Selected Catering Packages"));

            foreach (var item in _model.Packages)
            {
                column.Item().PaddingBottom(7).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(card =>
                {
                    card.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text(item.PackageName).FontSize(11).Bold().FontColor(Colors.Red.Darken4);
                            left.Item().Text(item.CatererName).FontSize(8).FontColor(Colors.Grey.Darken1);
                            if (!string.IsNullOrWhiteSpace(item.CatererEmail))
                                left.Item().Text(item.CatererEmail).FontSize(8).FontColor(Colors.Grey.Darken1);
                        });

                        row.ConstantItem(120).AlignRight().Text($"{item.TotalPrice:N2} TL")
                            .FontSize(11)
                            .Bold();
                    });

                    card.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        AddSmallCell(table, "Event Type", Empty(item.EventType));
                        AddSmallCell(table, "Category", Empty(item.PackageCategory));
                        AddSmallCell(table, "Guest Count", item.GuestCount.ToString());
                        AddSmallCell(table, "Price / Person", $"{item.PricePerPerson:N2} TL");
                        AddSmallCell(table, "Min Guests", item.MinimumGuestCount?.ToString() ?? "Not specified");
                        AddSmallCell(table, "Max Guests", item.MaximumGuestCount?.ToString() ?? "Not specified");
                    });

                    if (!string.IsNullOrWhiteSpace(item.Description))
                        card.Item().PaddingTop(5).Text($"Package Description: {item.Description}").LineHeight(1.2f);

                    if (!string.IsNullOrWhiteSpace(item.IncludedItems))
                        card.Item().PaddingTop(3).Text($"Included Items: {item.IncludedItems}").LineHeight(1.2f);

                    if (!string.IsNullOrWhiteSpace(item.ServiceDetails))
                        card.Item().PaddingTop(3).Text($"Service Details: {item.ServiceDetails}").LineHeight(1.2f);

                    card.Item().PaddingTop(4).Text($"Includes: {BuildIncludesText(item)}").FontSize(8).Italic();
                });
            }
        });
    }

    private void ComposeLegalTerms(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Element(c => SectionTitle(c, "3. Payment, Non-Payment, Claims and Compensation"));

            AddTerm(column, "3.1 Payment and Order Creation", "The customer understands and accepts that this agreement review step does not create a confirmed catering order by itself. The catering request will only be created after the customer completes the payment simulation successfully.");
            AddTerm(column, "3.2 Non-Payment Consequences", "If the customer does not complete the payment step, no confirmed order will be created. In that case, the selected caterer will not be under an obligation to prepare, reserve, deliver, purchase ingredients for, or provide the selected catering service. The customer cannot claim that an unpaid request creates a binding service obligation.");
            AddTerm(column, "3.3 Package Availability and Price", "Until payment is completed, package availability and estimated price may be treated as provisional. The caterer may continue to offer the same package to other customers unless a paid and confirmed order exists.");
            AddTerm(column, "3.4 Customer Information Responsibility", "The customer is responsible for providing accurate event date, address, guest count, contact information, access conditions, and package selections. Claims arising from incorrect or incomplete information provided by the customer may be rejected.");
            AddTerm(column, "3.5 Claims and Complaints", "After payment is completed and the order is created, the customer may submit claims or complaints only for the paid and confirmed catering request. Claims must be related to the selected package details, guest count, included items, service details, confirmed customer location, and confirmed order information.");
            AddTerm(column, "3.6 Compensation and Legal Claims", "Compensation claims may only be evaluated for direct, proven, and order-related damages connected to a paid and confirmed catering request. Indirect damages, loss of expectation, emotional damages, loss of reputation, or speculative claims are outside the scope of this platform agreement unless required by mandatory law.");
            AddTerm(column, "3.7 Evidence and Platform Records", "If a dispute occurs, the parties should first use the platform records, order details, payment status, messages, agreement PDF, receipt information, and review records as supporting evidence.");
            AddTerm(column, "3.8 Demonstration Notice", "This agreement text is prepared for an academic project demonstration. It should be reviewed by a qualified legal professional before real commercial use.");
        });
    }

    private void ComposeAcceptance(IContainer container)
    {
        container.Border(1)
            .BorderColor(Colors.Red.Darken4)
            .Background(Colors.Grey.Lighten5)
            .Padding(9)
            .Column(column =>
            {
                column.Item().Text("4. Customer Acceptance Statement")
                    .Bold()
                    .FontColor(Colors.Red.Darken4);

                column.Item().PaddingTop(4).Text("By clicking the acceptance checkbox and continuing to payment, the customer confirms that the agreement has been opened, read, and accepted before payment. The customer also confirms that the selected package details, guest counts, estimated prices, payment condition, non-payment consequences, claims rules, and compensation limits have been reviewed and accepted.")
                    .LineHeight(1.25f);
            });
    }

    private static void SectionTitle(IContainer container, string title)
    {
        container.PaddingBottom(4).Text(title).FontSize(11).Bold().FontColor(Colors.Red.Darken4);
    }

    private static void AddTerm(ColumnDescriptor column, string title, string body)
    {
        column.Item().PaddingBottom(5).Text(text =>
        {
            text.Span(title + ": ").Bold();
            text.Span(body);
        });
    }

    private static void AddInfoCell(TableDescriptor table, string title, string value)
    {
        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Column(column =>
        {
            column.Item().Text(title).FontSize(7).Bold().FontColor(Colors.Grey.Darken1);
            column.Item().Text(string.IsNullOrWhiteSpace(value) ? "Not specified" : value).FontSize(8);
        });
    }

    private static void AddSmallCell(TableDescriptor table, string title, string value)
    {
        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Column(column =>
        {
            column.Item().Text(title).FontSize(7).Bold().FontColor(Colors.Grey.Darken1);
            column.Item().Text(value).FontSize(8);
        });
    }

    private static string BuildIncludesText(AgreementPackageLineViewModel item)
    {
        var parts = new List<string>();

        if (item.IncludesMainCourse) parts.Add("Main Course");
        if (item.IncludesDessert) parts.Add("Dessert");
        if (item.IncludesSnacks) parts.Add("Snacks");
        if (item.IncludesDrinks) parts.Add("Drinks");

        return parts.Any() ? string.Join(", ", parts) : "Not specified";
    }

    private static string Empty(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Not specified" : value;
    }

    private static bool CanRenderLogo(byte[]? data)
    {
        return data is not null && (IsPng(data) || IsJpeg(data));
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
}


