using MedReport.Client.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;

namespace MedReport.Client.Services
{
    public static class ReportService
    {
        // -------------------------------------------------------------------------
        // CORE ENGINE FUNCTION: Generator Laporan Medis (Telah Disinkronkan)
        // Parameter disederhanakan karena teks klinis kini melekat pada dataPasien.
        // -------------------------------------------------------------------------
        public static void Generate(
            ReportDataModel dataPasien,
            byte[] gambarTandaTangan,
            byte[] logoBytes)
        {
            // -------------------------------------------------------------------------
            // TAHAP 1: MANAJEMEN PENYIMPANAN & IZIN OS (PERMISSION SAFETY)
            // -------------------------------------------------------------------------
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MedicalApp_Api", "Reports");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, $"Laporan_Endoskopi_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            // -------------------------------------------------------------------------
            // TAHAP 2: RENDERING PDF (QUEST PDF ENGINE)
            // -------------------------------------------------------------------------
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        // PENGATURAN HALAMAN DASAR
                        page.Size(PageSizes.A4);
                        page.Margin(1, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11));

                        // --- KOP SURAT (HEADER) ---
                        page.Header().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(10).Row(row =>
                        {
                            if (logoBytes != null)
                            {
                                row.ConstantItem(60).Image(logoBytes);
                            }

                            row.RelativeItem().PaddingLeft(10).Column(col =>
                            {
                                col.Item().Text("AIRWAY MANAGEMENT REPORT").FontSize(18).SemiBold().FontColor(Colors.Blue.Medium);
                                col.Item().Text(dataPasien.Hospital ?? "Rumah Sakit Tidak Diketahui").FontSize(11).Italic();
                                col.Item().Text(dataPasien.Address ?? "Alamat Rumah Sakit Tidak Diketahui").FontSize(9).FontColor(Colors.Grey.Darken2);
                            });
                        });

                        // --- BADAN LAPORAN (CONTENT) ---
                        page.Content().PaddingVertical(10).Column(col =>
                        {
                            col.Spacing(10);

                            // BLOK 1: TABEL IDENTITAS PASIEN
                            col.Item().Background(Colors.Grey.Lighten4).Padding(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                QuestPDF.Infrastructure.IContainer CellStyle(QuestPDF.Infrastructure.IContainer container)
                                {
                                    return container.Border(1).BorderColor(Colors.Black).Padding(5);
                                }

                                table.Cell().Element(CellStyle).Text(t => { t.Span("ID Pasien: ").SemiBold(); t.Span(dataPasien.IdPasien ?? "-"); });
                                table.Cell().Element(CellStyle).Text(t => { t.Span("Nama Pasien: ").SemiBold(); t.Span(dataPasien.Nama ?? "-"); });

                                // PERBAIKAN 1: Panggil properti format string terlindung (.TanggalLahirFormatted)
                                table.Cell().Element(CellStyle).Text(t => { t.Span("Tgl Lahir: ").SemiBold(); t.Span(dataPasien.TanggalLahirFormatted); });
                                table.Cell().Element(CellStyle).Text(t => { t.Span("Gender: ").SemiBold(); t.Span(dataPasien.Gender ?? "-"); });

                                table.Cell().Element(CellStyle).Column(colBox =>
                                {
                                    colBox.Item().Text("Obat Premedikasi:").SemiBold();
                                    colBox.Item().Text(dataPasien.ObatPremedikasi ?? "-");
                                });

                                table.Cell().Element(CellStyle).Column(colBox =>
                                {
                                    colBox.Item().Text("Alat:").SemiBold();
                                    colBox.Item().Text(dataPasien.Alat ?? "-");
                                });
                            });

                            // BLOK 2: DIAGNOSIS DAN KELUHAN
                            col.Item().PaddingTop(10).Row(row =>
                            {
                                row.Spacing(20);

                                row.RelativeItem().Column(diagCol =>
                                {
                                    diagCol.Item().PaddingBottom(3).Text("Diagnosis:").FontSize(12).SemiBold().Underline();
                                    diagCol.Item().Text(dataPasien.Diagnosis ?? "-");
                                });

                                row.RelativeItem().Column(kelCol =>
                                {
                                    kelCol.Item().PaddingBottom(3).Text("Keluhan:").FontSize(12).SemiBold().Underline();
                                    kelCol.Item().Text(dataPasien.Keluhan ?? "-");
                                });
                            });

                            // BLOK 3: CATATAN PEMERIKSAAN
                            col.Item().PaddingTop(15).Column(procCol =>
                            {
                                procCol.Item().PaddingBottom(5).Text("Catatan Pemeriksaan:").FontSize(12).SemiBold().Underline();

                                procCol.Item().Row(row =>
                                {
                                    row.Spacing(20);

                                    row.RelativeItem().Column(leftCol =>
                                    {
                                        leftCol.Item().Text("Anamnesa:").SemiBold();
                                        leftCol.Item().PaddingBottom(10).Text(string.IsNullOrWhiteSpace(dataPasien.Keluhan) ? "-" : dataPasien.Keluhan);

                                        leftCol.Item().Text("Hasil:").SemiBold();
                                        leftCol.Item().Text(string.IsNullOrWhiteSpace(dataPasien.Diagnosis) ? "-" : dataPasien.Diagnosis);
                                    });

                                    row.RelativeItem().Column(rightCol =>
                                    {
                                        rightCol.Item().Text("Kesimpulan:").SemiBold();
                                        // Catatan tambahan jika rekam medis menggunakan field khusus di masa mendatang
                                        rightCol.Item().PaddingBottom(10).Text("-");

                                        rightCol.Item().Text("Saran:").SemiBold();
                                        rightCol.Item().Text("-");
                                    });
                                });
                            });

                            // BLOK 4: GALERI FOTO ENDOSKOPI (Membaca file fisik langsung dari data model terikat)
                            if (dataPasien.FotoEndoskopi != null && dataPasien.FotoEndoskopi.Count > 0)
                            {
                                col.Item().EnsureSpace(50).PaddingTop(15).PaddingBottom(5).Text("Browse Image:").FontSize(12).SemiBold().Underline();

                                col.Item().Table(photoGrid =>
                                {
                                    photoGrid.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    foreach (var foto in dataPasien.FotoEndoskopi)
                                    {
                                        if (File.Exists(foto.OriginalPath))
                                        {
                                            try
                                            {
                                                // Load file biner secara on-demand langsung saat penyusunan PDF
                                                byte[] imgBytes = File.ReadAllBytes(foto.OriginalPath);
                                                photoGrid.Cell().Padding(5).Image(imgBytes);
                                            }
                                            catch
                                            {
                                                // Skip jika ada file gambar yang mendadak rusak/terkunci sistem luar
                                            }
                                        }
                                    }
                                });
                            }

                            // BLOK 5: AREA TANDA TANGAN DOKTER (MURNI KOSONG JIKA NULL)
                            col.Item().ShowEntire().ExtendVertical().AlignBottom().PaddingTop(30).Row(row =>
                            {
                                row.RelativeItem();

                                row.ConstantItem(150).Column(sigCol =>
                                {
                                    string waktuSekarang = DateTime.Now.ToString("dd MMMM yyyy, HH:mm", new System.Globalization.CultureInfo("id-ID"));
                                    sigCol.Item().AlignCenter().Text(waktuSekarang).FontSize(10);

                                    if (gambarTandaTangan != null && gambarTandaTangan.Length > 0)
                                    {
                                        sigCol.Item().PaddingVertical(5).Height(50).AlignCenter().Image(gambarTandaTangan).FitHeight();
                                    }
                                    else
                                    {
                                        sigCol.Item().Height(60);
                                    }

                                    sigCol.Item().AlignCenter().Text($"({dataPasien.Dokter ?? "Nama Dokter"})").SemiBold().FontSize(10);
                                });
                            });
                        });

                        // --- FOOTER (Kaki Halaman) ---
                        page.Footer().PaddingVertical(5).AlignCenter().Text(x =>
                        {
                            x.CurrentPageNumber().FontSize(10);
                        });
                    });
                })
                .GeneratePdf(filePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Gagal merender Report PDF (QuestPDF Error). \n\nDetail teknis: {ex.Message}");
            }

            // -------------------------------------------------------------------------
            // TAHAP 3: MENJALANKAN FILE HASIL KE PENGGUNA (OS EXECUTION)
            // -------------------------------------------------------------------------
            try
            {
                var p = new System.Diagnostics.Process();
                p.StartInfo = new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true };
                p.Start();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                throw new Exception(
                    "Laporan PDF SUKSES DIBUAT!\n\n" +
                    "Namun, laporan tidak dapat dibuka otomatis karena PC ini belum memiliki aplikasi pembaca PDF (seperti Adobe Acrobat/Edge).\n\n" +
                    $"Silakan buka manual di folder:\n{folderPath}");
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Laporan PDF SUKSES DIBUAT di:\n{folderPath}\n\n" +
                    $"Gagal membuka file secara otomatis. Detail teknis: {ex.Message}");
            }
        }
    }
}