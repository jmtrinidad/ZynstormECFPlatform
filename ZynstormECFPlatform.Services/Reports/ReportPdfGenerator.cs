using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Services.Reports;

public static class ReportPdfGenerator
{
    static ReportPdfGenerator()
    {
        // QuestPDF requires setting the license type. Under their community license guidelines,
        // it is free for individuals and companies under $1M USD annual revenue.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] GenerateDailyReportPdf(Client client, List<EcfDocument> documents, DateTime start, DateTime end)
    {
        var rangeStartFormatted = start.ToDrTime().ToString("dd/MM/yyyy hh:mm tt");
        var rangeEndFormatted = end.ToDrTime().ToString("dd/MM/yyyy hh:mm tt");

        int totalCount = documents.Count;
        int acceptedCount = documents.Count(d => d.EcfStatusId == 10);
        int rejectedCount = documents.Count(d => d.EcfStatusId == 11);
        int errorCount = documents.Count(d => d.EcfStatusId == 12);

        var acceptedDocs = documents.Where(d => d.EcfStatusId == 10).ToList();
        decimal subTotalAcumulado = acceptedDocs.Sum(d => d.SubTotal);
        decimal itbisAcumulado = acceptedDocs.Sum(d => d.Itbistotal);
        decimal totalAcumulado = acceptedDocs.Sum(d => d.Total);

        var typeGroups = documents
            .GroupBy(d => d.EcfTypeId)
            .Select(g => new
            {
                TypeId = g.Key,
                Name = GetEcfTypeName(g.Key),
                Count = g.Count(),
                Total = g.Sum(d => d.Total)
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        var recentDocs = documents
            .OrderByDescending(d => d.RegisteredAt)
            .Take(5)
            .ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor(Colors.Grey.Darken3));

                // Page Header
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Zynstorm ECF").Bold().FontSize(22).FontColor(Color.FromHex("#1E3A8A"));
                        col.Item().Text($"Cliente: {client.Name}").Bold().FontSize(12).FontColor(Colors.Grey.Darken2);
                        col.Item().Text($"RNC: {client.Rnc}").FontSize(10).FontColor(Colors.Grey.Darken1);
                    });

                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text("RESUMEN DIARIO").Bold().FontSize(16).FontColor(Color.FromHex("#0F172A"));
                        col.Item().Text("Comprobantes Electrónicos").FontSize(10).FontColor(Colors.Grey.Darken1);
                        col.Item().Text($"Período: {rangeStartFormatted} - {rangeEndFormatted}").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });

                // Page Content
                page.Content().Column(col =>
                {
                    col.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten3);

                    // KPIs Grid
                    col.Item().PaddingTop(15).Row(row =>
                    {
                        row.RelativeItem().PaddingRight(5).Component(new StatCard("Emitidos", totalCount.ToString(), "#475569"));
                        row.RelativeItem().PaddingLeft(2).PaddingRight(2).Component(new StatCard("Aceptados", acceptedCount.ToString(), "#10B981"));
                        row.RelativeItem().PaddingLeft(2).PaddingRight(2).Component(new StatCard("Rechazados", rejectedCount.ToString(), "#F59E0B"));
                        row.RelativeItem().PaddingLeft(5).Component(new StatCard("Con Error", errorCount.ToString(), "#EF4444"));
                    });

                    // Economic Summary
                    col.Item().PaddingTop(15).Column(econ =>
                    {
                        econ.Item().Text("CONSOLIDADO ECONÓMICO (ACEPTADOS)").Bold().FontSize(11).FontColor(Colors.Grey.Darken2);
                        econ.Item().PaddingTop(5).Background(Color.FromHex("#F8FAFC")).Border(1).BorderColor(Color.FromHex("#E2E8F0")).Padding(12).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Subtotal").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"RD$ {subTotalAcumulado:N2}").Bold().FontSize(13).FontColor(Colors.Grey.Darken3);
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("ITBIS").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"RD$ {itbisAcumulado:N2}").Bold().FontSize(13).FontColor(Colors.Grey.Darken3);
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Monto Total Facturado").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"RD$ {totalAcumulado:N2}").Bold().FontSize(15).FontColor(Color.FromHex("#1E3A8A"));
                            });
                        });
                    });

                    // Breakdown Table
                    col.Item().PaddingTop(20).Text("DISTRIBUCIÓN POR TIPO DE COMPROBANTE").Bold().FontSize(11).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background(Color.FromHex("#1E3A8A")).Padding(6).Text("Tipo de e-CF").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Color.FromHex("#1E3A8A")).Padding(6).AlignCenter().Text("Cantidad").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Color.FromHex("#1E3A8A")).Padding(6).AlignRight().Text("Monto Total").Bold().FontColor(Colors.White).FontSize(9);
                        });

                        if (!typeGroups.Any())
                        {
                            table.Cell().ColumnSpan(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(10).AlignCenter().Text("Sin actividad registrada en este período.").FontSize(9).Italic();
                        }
                        else
                        {
                            foreach (var group in typeGroups)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(6).Text($"{group.Name} ({group.TypeId})").FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(6).AlignCenter().Text(group.Count.ToString()).FontSize(9).Bold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(6).AlignRight().Text($"RD$ {group.Total:N2}").FontSize(9).Bold();
                            }
                        }
                    });

                    // Recent Invoices Table
                    col.Item().PaddingTop(20).Text("ÚLTIMOS DOCUMENTOS PROCESADOS").Bold().FontSize(11).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1.5f);
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background(Color.FromHex("#0F172A")).Padding(6).Text("e-NCF").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Color.FromHex("#0F172A")).Padding(6).Text("Receptor").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Color.FromHex("#0F172A")).Padding(6).AlignRight().Text("Total").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Color.FromHex("#0F172A")).Padding(6).AlignRight().Text("Estado").Bold().FontColor(Colors.White).FontSize(9);
                        });

                        if (!recentDocs.Any())
                        {
                            table.Cell().ColumnSpan(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(10).AlignCenter().Text("Sin actividad registrada en este período.").FontSize(9).Italic();
                        }
                        else
                        {
                            foreach (var doc in recentDocs)
                            {
                                string statusText = doc.EcfStatus?.Name ?? "Pendiente";
                                string statusColorHex = doc.EcfStatusId switch
                                {
                                    10 => "#10B981", // Success
                                    11 => "#F59E0B", // Warning
                                    12 => "#EF4444", // Danger
                                    _ => "#475569"
                                };

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(6).Text(doc.Ncf).FontFamily("Courier").FontSize(9).Bold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(6).Text(doc.CustomerName ?? "-").FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(6).AlignRight().Text($"RD$ {doc.Total:N2}").FontSize(9).Bold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(6).AlignRight().Text(statusText).FontSize(8).Bold().FontColor(Color.FromHex(statusColorHex));
                            }
                        }
                    });
                });

                // Page Footer
                page.Footer().Column(foot =>
                {
                    foot.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten3);
                    foot.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text("Este es un reporte diario automatizado emitido por Zynstorm ECF.").FontSize(7).FontColor(Colors.Grey.Lighten1);
                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Página ").FontSize(8).FontColor(Colors.Grey.Lighten1);
                            x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Lighten1);
                            x.Span(" de ").FontSize(8).FontColor(Colors.Grey.Lighten1);
                            x.TotalPages().FontSize(8).FontColor(Colors.Grey.Lighten1);
                        });
                    });
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerateWeeklyReportPdf(Client client, List<EcfDocument> documents, DateTime start, DateTime end)
    {
        var rangeStartFormatted = start.ToDrTime().ToString("dd/MM/yyyy hh:mm tt");
        var rangeEndFormatted = end.ToDrTime().ToString("dd/MM/yyyy hh:mm tt");

        int totalCount = documents.Count;
        int acceptedCount = documents.Count(d => d.EcfStatusId == 10);
        int rejectedCount = documents.Count(d => d.EcfStatusId == 11);
        int errorCount = documents.Count(d => d.EcfStatusId == 12);

        var acceptedDocs = documents.Where(d => d.EcfStatusId == 10).ToList();
        decimal subTotalAcumulado = acceptedDocs.Sum(d => d.SubTotal);
        decimal itbisAcumulado = acceptedDocs.Sum(d => d.Itbistotal);
        decimal totalAcumulado = acceptedDocs.Sum(d => d.Total);

        var typeGroups = documents
            .GroupBy(d => d.EcfTypeId)
            .Select(g => new
            {
                TypeId = g.Key,
                Name = GetEcfTypeName(g.Key),
                Count = g.Count(),
                Total = g.Sum(d => d.Total)
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        var topBuyers = acceptedDocs
            .GroupBy(d => new { d.CustomerRnc, d.CustomerName })
            .Select(g => new
            {
                Name = g.Key.CustomerName,
                Rnc = g.Key.CustomerRnc,
                Total = g.Sum(d => d.Total)
            })
            .OrderByDescending(x => x.Total)
            .Take(3)
            .ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor(Colors.Grey.Darken3));

                // Page Header
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Zynstorm ECF").Bold().FontSize(22).FontColor(Color.FromHex("#1E3A8A"));
                        col.Item().Text($"Cliente: {client.Name}").Bold().FontSize(12).FontColor(Colors.Grey.Darken2);
                        col.Item().Text($"RNC: {client.Rnc}").FontSize(10).FontColor(Colors.Grey.Darken1);
                    });

                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text("REPORTE SEMANAL").Bold().FontSize(16).FontColor(Color.FromHex("#0F172A"));
                        col.Item().Text("Resumen Ejecutivo").FontSize(10).FontColor(Colors.Grey.Darken1);
                        col.Item().Text($"Período: {rangeStartFormatted} - {rangeEndFormatted}").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });

                // Page Content
                page.Content().Column(col =>
                {
                    col.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten3);

                    // KPIs Grid
                    col.Item().PaddingTop(15).Row(row =>
                    {
                        row.RelativeItem().PaddingRight(5).Component(new StatCard("Emitidos", totalCount.ToString(), "#475569"));
                        row.RelativeItem().PaddingLeft(2).PaddingRight(2).Component(new StatCard("Aceptados", acceptedCount.ToString(), "#10B981"));
                        row.RelativeItem().PaddingLeft(2).PaddingRight(2).Component(new StatCard("Rechazados", rejectedCount.ToString(), "#F59E0B"));
                        row.RelativeItem().PaddingLeft(5).Component(new StatCard("Con Error", errorCount.ToString(), "#EF4444"));
                    });

                    // Economic Summary
                    col.Item().PaddingTop(15).Column(econ =>
                    {
                        econ.Item().Text("CONSOLIDADO FINANCIERO SEMANAL").Bold().FontSize(11).FontColor(Colors.Grey.Darken2);
                        econ.Item().PaddingTop(5).Background(Color.FromHex("#1E3A8A")).Border(1).BorderColor(Color.FromHex("#1E3A8A")).Padding(12).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Subtotal Semanal").FontSize(9).FontColor(Colors.Blue.Lighten4);
                                c.Item().Text($"RD$ {subTotalAcumulado:N2}").Bold().FontSize(13).FontColor(Colors.White);
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("ITBIS Semanal").FontSize(9).FontColor(Colors.Blue.Lighten4);
                                c.Item().Text($"RD$ {itbisAcumulado:N2}").Bold().FontSize(13).FontColor(Colors.White);
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Monto Semanal Facturado").FontSize(9).FontColor(Colors.Blue.Lighten4);
                                c.Item().Text($"RD$ {totalAcumulado:N2}").Bold().FontSize(16).FontColor(Colors.White);
                            });
                        });
                    });

                    // Breakdown Table
                    col.Item().PaddingTop(20).Text("ACTIVIDAD POR TIPO DE COMPROBANTE").Bold().FontSize(11).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background(Color.FromHex("#1E293B")).Padding(6).Text("Tipo de e-CF").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Color.FromHex("#1E293B")).Padding(6).AlignCenter().Text("Emitidos").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Color.FromHex("#1E293B")).Padding(6).AlignRight().Text("Monto Total").Bold().FontColor(Colors.White).FontSize(9);
                        });

                        if (!typeGroups.Any())
                        {
                            table.Cell().ColumnSpan(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(10).AlignCenter().Text("Sin actividad registrada en este período.").FontSize(9).Italic();
                        }
                        else
                        {
                            foreach (var group in typeGroups)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(6).Text($"{group.Name} ({group.TypeId})").FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(6).AlignCenter().Text(group.Count.ToString()).FontSize(9).Bold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(6).AlignRight().Text($"RD$ {group.Total:N2}").FontSize(9).Bold();
                            }
                        }
                    });

                    // Top Customers Table
                    col.Item().PaddingTop(20).Text("TOP 3 COMPRADORES DE LA SEMANA").Bold().FontSize(11).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background(Color.FromHex("#0F172A")).Padding(6).Text("Razón Social").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Color.FromHex("#0F172A")).Padding(6).Text("RNC").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Color.FromHex("#0F172A")).Padding(6).AlignRight().Text("Total Facturado").Bold().FontColor(Colors.White).FontSize(9);
                        });

                        if (!topBuyers.Any())
                        {
                            table.Cell().ColumnSpan(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(10).AlignCenter().Text("Sin cobros/facturas aceptadas en esta semana.").FontSize(9).Italic();
                        }
                        else
                        {
                            foreach (var buyer in topBuyers)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(6).Text(buyer.Name ?? "-").FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(6).Text(buyer.Rnc ?? "-").FontFamily("Courier").FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(6).AlignRight().Text($"RD$ {buyer.Total:N2}").FontSize(9).Bold().FontColor(Color.FromHex("#1E3A8A"));
                            }
                        }
                    });
                });

                // Page Footer
                page.Footer().Column(foot =>
                {
                    foot.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten3);
                    foot.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text("Este es un reporte semanal ejecutivo automatizado emitido por Zynstorm ECF.").FontSize(7).FontColor(Colors.Grey.Lighten1);
                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Página ").FontSize(8).FontColor(Colors.Grey.Lighten1);
                            x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Lighten1);
                            x.Span(" de ").FontSize(8).FontColor(Colors.Grey.Lighten1);
                            x.TotalPages().FontSize(8).FontColor(Colors.Grey.Lighten1);
                        });
                    });
                });
            });
        }).GeneratePdf();
    }

    private static string GetEcfTypeName(int ecfTypeId)
    {
        return ecfTypeId switch
        {
            31 => "Factura de Crédito Fiscal Electrónica",
            32 => "Factura de Consumo Electrónica",
            33 => "Nota de Débito Electrónica",
            34 => "Nota de Crédito Electrónica",
            41 => "Compras Electrónicas",
            43 => "Gastos Menores Electrónicos",
            44 => "Regímenes Especiales Electrónica",
            45 => "Gubernamental Electrónica",
            _ => "Comprobante Fiscal Electrónico"
        };
    }
}

// QuestPDF component to draw stat cards
public class StatCard : IComponent
{
    private string Title { get; }
    private string Value { get; }
    private string ColorHex { get; }

    public StatCard(string title, string value, string colorHex)
    {
        Title = title;
        Value = value;
        ColorHex = colorHex;
    }

    public void Compose(IContainer container)
    {
        container
            .Background(Color.FromHex("#F8FAFC"))
            .Border(1)
            .BorderColor(Color.FromHex("#E2E8F0"))
            .Row(row =>
            {
                row.ConstantItem(4).Background(Color.FromHex(ColorHex));
                row.RelativeItem().Padding(8).Column(col =>
                {
                    col.Item().Text(Title.ToUpper()).Bold().FontSize(7).FontColor(Colors.Grey.Lighten1);
                    col.Item().Text(Value).Bold().FontSize(16).FontColor(Color.FromHex(ColorHex));
                });
            });
    }
}
