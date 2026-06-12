using MedReport.Client.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MedReport.Client.Services
{
    public static class ReportService
    {
        // SOLUSI 1: Ubah penanda fungsi menjadi Asynchronous (Task) agar tidak memblokir UI Thread utama
        public static async Task GenerateAsync(
            ReportDataModel dataPasien,
            byte[] gambarTandaTangan,
            byte[] logoBytes)
        {
            // TAHAP 1: MANAJEMEN PENYIMPANAN & IZIN OS
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MedicalApp_Api", "Reports");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, $"Laporan_Endoskopi_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            // TAHAP 2: RENDERING PDF (QUEST PDF ENGINE - NON BLOCKING THREAD)
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;

                // Bungkus proses kalkulasi berat QuestPDF ke dalam Task.Run agar berjalan di ThreadPool background
                await Task.Run(() =>
                {
                    Document.Create(container =>
                    {
                        container.Page(page =>
                        {
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
                                            // PERBAIKAN: Ambil dari data ObatPremedikasi (atau field kesimpulan jika Anda membuat TextBox baru nanti)
                                            rightCol.Item().PaddingBottom(10).Text(string.IsNullOrWhiteSpace(dataPasien.ObatPremedikasi) ? "-" : dataPasien.ObatPremedikasi);

                                            rightCol.Item().Text("Saran:").SemiBold();
                                            // PERBAIKAN: Ambil dari data Alat (atau field saran jika Anda membuat TextBox baru nanti)
                                            rightCol.Item().Text(string.IsNullOrWhiteSpace(dataPasien.Alat) ? "-" : dataPasien.Alat);
                                        });
                                    });
                                });

                                // BLOK 4: GALERI FOTO ENDOSKOPI (STREAMING IMAGE COMPRESSION)
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
                                                    // SOLUSI 2: STREAMING BINARY. Jangan timbun semua byte array di RAM secara serentak.
                                                    // Gunakan FileStream untuk membaca data gambar secara bertahap saat mesin membutuhkan proses rendering saja.
                                                    using (FileStream fs = new FileStream(foto.OriginalPath, FileMode.Open, FileAccess.Read))
                                                    {
                                                        photoGrid.Cell().Padding(5).Image(fs);
                                                    }
                                                }
                                                catch
                                                {
                                                    // Skip jika file mendadak dikunci OS
                                                }
                                            }
                                        }
                                    });
                                }

                                // BLOK 5: AREA TANDA TANGAN DOKTER
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

                            // --- FOOTER ---
                            page.Footer().PaddingVertical(5).AlignCenter().Text(x =>
                            {
                                x.CurrentPageNumber().FontSize(10);
                            });
                        });
                    })
                    .GeneratePdf(filePath);
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Gagal merender Report PDF (QuestPDF Error). \n\nDetail teknis: {ex.Message}");
            }

            // TAHAP 3: MENJALANKAN FILE HASIL KE PENGGUNA (OS EXECUTION)
            try
            {
                // Gunakan Process secara asinkron tanpa memblokir benang UI
                await Task.Run(() =>
                {
                    var p = new System.Diagnostics.Process();
                    p.StartInfo = new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true };
                    p.Start();
                });
            }
            catch (System.ComponentModel.Win32Exception)
            {
                throw new Exception(
                    "Laporan PDF SUKSES DIBUAT!\n\n" +
                    "Namun, laporan tidak dapat dibuka otomatis karena PC ini belum memiliki aplikasi pembaca PDF (seperti Adobe Acrobat/Edge).\n\n" +
                    $"Silakan buka manual di folder:\n{folderPath}");
            }
        }
    }
}