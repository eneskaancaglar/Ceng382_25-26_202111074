using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TasteAtDoor.Models;

namespace TasteAtDoor.Documents
{
    public class OrderReceiptDocument : IDocument
    {
        private readonly Order _order;

        public OrderReceiptDocument(Order order)
        {
            _order = order;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Column(column =>
                    {
                        column.Item().Text("TasteAtDoor Receipt")
                            .FontSize(22)
                            .Bold()
                            .FontColor(Colors.Red.Darken4);

                        column.Item().Text($"Order Receipt #{_order.Id}")
                            .FontSize(12)
                            .FontColor(Colors.Grey.Darken2);

                        column.Item().PaddingTop(4).Text($"Generated on: {DateTime.Now:dd.MM.yyyy HH:mm}");
                    });

                page.Content()
                    .PaddingVertical(16)
                    .Column(column =>
                    {
                        column.Spacing(12);

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text("Customer Information")
                                    .Bold()
                                    .FontSize(14);

                                left.Item().Text($"Name: {_order.ApplicationUser?.FullName ?? "Unknown"}");
                                left.Item().Text($"Email: {_order.ApplicationUser?.Email ?? "No email"}");
                            });

                            row.RelativeItem().Column(right =>
                            {
                                right.Item().Text("Order Information")
                                    .Bold()
                                    .FontSize(14);

                                right.Item().Text($"Order ID: {_order.Id}");
                                right.Item().Text($"Order Date: {_order.OrderDate:dd.MM.yyyy HH:mm}");
                                right.Item().Text($"Status: {_order.Status}");
                            });
                        });

                        column.Item().PaddingTop(8).Text("Ordered Items")
                            .Bold()
                            .FontSize(14);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("Menu Item");
                                header.Cell().Element(HeaderCell).Text("Qty");
                                header.Cell().Element(HeaderCell).Text("Base");
                                header.Cell().Element(HeaderCell).Text("Final");
                                header.Cell().Element(HeaderCell).Text("Total");
                            });

                            foreach (var item in _order.OrderItems)
                            {
                                table.Cell().Element(BodyCell).Column(cellColumn =>
                                {
                                    cellColumn.Item().Text(item.MenuItem?.Name ?? "Menu Item").Bold();

                                    if (item.MenuItem?.Caretaker is not null)
                                    {
                                        cellColumn.Item().Text($"Restaurant: {item.MenuItem.Caretaker.FullName}")
                                            .FontSize(9)
                                            .FontColor(Colors.Grey.Darken2);
                                    }

                                    if (item.SelectedCustomizations.Any())
                                    {
                                        cellColumn.Item().PaddingTop(3).Text("Customizations:")
                                            .FontSize(9)
                                            .Bold();

                                        foreach (var customization in item.SelectedCustomizations)
                                        {
                                            var priceText = customization.PriceChange != 0
                                                ? $" ({customization.PriceChange:0.00})"
                                                : "";

                                            cellColumn.Item().Text($"- {customization.GroupTitle}: {customization.OptionName}{priceText}")
                                                .FontSize(9);
                                        }
                                    }
                                    else
                                    {
                                        cellColumn.Item().PaddingTop(3).Text("Customizations: None")
                                            .FontSize(9)
                                            .FontColor(Colors.Grey.Darken2);
                                    }
                                });

                                table.Cell().Element(BodyCell).Text(item.Quantity.ToString());
                                table.Cell().Element(BodyCell).Text(item.BaseUnitPrice.ToString("0.00"));
                                table.Cell().Element(BodyCell).Text(item.FinalUnitPrice.ToString("0.00"));
                                table.Cell().Element(BodyCell).Text(item.LineTotal.ToString("0.00"));
                            }
                        });

                        column.Item()
                            .AlignRight()
                            .PaddingTop(14)
                            .Text($"Grand Total: {_order.TotalPrice:0.00}")
                            .Bold()
                            .FontSize(16)
                            .FontColor(Colors.Red.Darken4);

                        column.Item().PaddingTop(18).Text("Receipt Note")
                            .Bold()
                            .FontSize(14);

                        column.Item().Text(
                            "This receipt was generated dynamically from order, customer, restaurant, menu item, and customization data stored in the system. It is created programmatically for the selected order.");
                    });

                page.Footer()
                    .AlignCenter()
                    .Text("TasteAtDoor - Dynamic Receipt PDF")
                    .FontSize(9)
                    .FontColor(Colors.Grey.Darken1);
            });
        }

        private static IContainer HeaderCell(IContainer container)
        {
            return container
                .Background(Colors.Red.Darken4)
                .DefaultTextStyle(x => x.FontColor(Colors.White).Bold())
                .Padding(6)
                .Border(1)
                .BorderColor(Colors.Red.Darken4);
        }

        private static IContainer BodyCell(IContainer container)
        {
            return container
                .Padding(6)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2);
        }
    }
}