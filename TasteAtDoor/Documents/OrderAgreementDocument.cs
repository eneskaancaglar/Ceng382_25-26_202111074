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

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Column(column =>
                    {
                        column.Item().Text("Taste At Door - Service Agreement")
                            .FontSize(20)
                            .Bold();

                        column.Item().Text($"Agreement for Order #{_order.Id}")
                            .FontSize(12);

                        column.Item().PaddingTop(5).Text($"Generated on: {DateTime.Now:dd.MM.yyyy HH:mm}");
                    });

                page.Content()
                    .PaddingVertical(15)
                    .Column(column =>
                    {
                        column.Spacing(12);

                        column.Item().Text("Customer Information").Bold().FontSize(14);
                        column.Item().Text($"Customer Name: {_order.ApplicationUser?.FullName ?? "Unknown"}");
                        column.Item().Text($"Customer Email: {_order.ApplicationUser?.Email ?? "No email"}");

                        column.Item().PaddingTop(10).Text("Order Information").Bold().FontSize(14);
                        column.Item().Text($"Order ID: {_order.Id}");
                        column.Item().Text($"Order Date: {_order.OrderDate:dd.MM.yyyy HH:mm}");
                        column.Item().Text($"Order Status: {_order.Status}");
                        column.Item().Text($"Total Price: {_order.TotalPrice:0.00}");

                        column.Item().PaddingTop(10).Text("Ordered Items").Bold().FontSize(14);

                        foreach (var item in _order.OrderItems)
                        {
                            column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(itemColumn =>
                            {
                                itemColumn.Item().Text(item.MenuItem?.Name ?? "Menu Item").Bold();
                                itemColumn.Item().Text($"Quantity: {item.Quantity}");
                                itemColumn.Item().Text($"Base Unit Price: {item.BaseUnitPrice:0.00}");
                                itemColumn.Item().Text($"Final Unit Price: {item.FinalUnitPrice:0.00}");
                                itemColumn.Item().Text($"Line Total: {item.LineTotal:0.00}");

                                if (item.SelectedCustomizations.Any())
                                {
                                    itemColumn.Item().PaddingTop(4).Text("Customizations:").Bold();

                                    foreach (var customization in item.SelectedCustomizations)
                                    {
                                        var extra = customization.PriceChange != 0
                                            ? $" ({customization.PriceChange:0.00})"
                                            : "";

                                        itemColumn.Item().Text($"- {customization.GroupTitle}: {customization.OptionName}{extra}");
                                    }
                                }
                                else
                                {
                                    itemColumn.Item().PaddingTop(4).Text("Customizations: None");
                                }
                            });
                        }

                        column.Item().PaddingTop(14).Text("Agreement Terms").Bold().FontSize(14);
                        column.Item().Text("1. This document is generated automatically based on the selected order.");
                        column.Item().Text("2. The customer confirms that the listed items and selected customizations belong to this order.");
                        column.Item().Text("3. The caterer agrees to prepare and deliver the order information as recorded in the system.");
                        column.Item().Text("4. This document is for project demonstration purposes and represents a dynamically generated agreement PDF.");

                        column.Item().PaddingTop(20).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Customer Signature").Bold();
                                c.Item().PaddingTop(18).LineHorizontal(1);
                            });

                            row.ConstantItem(40);

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Caterer Signature").Bold();
                                c.Item().PaddingTop(18).LineHorizontal(1);
                            });
                        });
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Taste At Door - Dynamic Agreement PDF");
                    });
            });
        }
    }
}