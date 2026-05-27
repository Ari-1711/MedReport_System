using MedReport.Client.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace MedReport.Client.Services
{
    public static class ReportService
    {
        // -------------------------------------------------------------------------
        // CORE ENGINE FUNCTION: Generator Laporan Medis
        // Menggunakan tipe data primitif dan biner (byte[]) sebagai parameter. 
        // Ini adalah prinsip "Decoupling": Mesin pembuat PDF tidak boleh tahu-menahu 
        // soal elemen UI WPF. Mesin ini murni hanya menerima data mentah dan merakitnya.
        // -------------------------------------------------------------------------
        public static void Generate
            (ReportDataModel dataPasien,
            (string Anamnesa, string Hasil, string Kesimpulan, string Saran) catatan,
            List<byte[]> fotoEndoskopiBytes,
            byte[] gambarTandaTangan,
            byte[] logoBytes)
        {
            // -------------------------------------------------------------------------
            // TAHAP 1: MANAJEMEN PENYIMPANAN & IZIN OS (PERMISSION SAFETY)
            // -------------------------------------------------------------------------
            // MENCEGAH UNAUTHORIZED ACCESS EXCEPTION
            // Menyimpan di folder 'LocalApplicationData' (AppData\Local).
            // Folder ini selalu diizinkan oleh Windows untuk ditulis tanpa perlu akses 'Run as Administrator'.
            // Jika Anda menyimpan di drive C:\ atau Program Files, Windows Defender bisa memblokir aplikasi.
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MedicalApp_Api", "Reports");

            // Buat folder brankas laporan jika belum ada
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Penamaan file unik menggunakan stempel waktu untuk mencegah file tertimpa (Overwrite)
            string filePath = Path.Combine(folderPath, $"Laporan_Endoskopi_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            // -------------------------------------------------------------------------
            // TAHAP 2: RENDERING PDF (QUEST PDF ENGINE)
            // -------------------------------------------------------------------------
            try
            {
                // Deklarasi lisensi wajib untuk menghindari Watermark QuestPDF
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
                        // Menggunakan BorderBottom agar kop surat memiliki garis bawah pemisah yang rapi
                        page.Header().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(10).Row(row =>
                        {
                            if (logoBytes != null)
                            {
                                // Render logo jika diatur oleh perawat
                                row.ConstantItem(60).Image(logoBytes);
                            }

                            // Identitas Rumah Sakit (Fluid Layout)
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
                            // Menggunakan background abu-abu muda agar blok identitas terpisah secara visual dari data klinis
                            col.Item().Background(Colors.Grey.Lighten4).Padding(10).Table(table =>
                            {
                                // Rasio tabel dibagi 2 kolom sama besar (50:50)
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                // HELPER FUNGSI LOKAL: Desain border standar untuk semua sel tabel
                                QuestPDF.Infrastructure.IContainer CellStyle(QuestPDF.Infrastructure.IContainer container)
                                {
                                    return container.Border(1).BorderColor(Colors.Black).Padding(5);
                                }

                                // Pengisian Sel Tabel (Menggunakan t.Span untuk menggabungkan teks Bold dan Normal dalam 1 baris)
                                table.Cell().Element(CellStyle).Text(t => { t.Span("ID Pasien: ").SemiBold(); t.Span(dataPasien.Patient.IdPasien ?? "-"); });
                                table.Cell().Element(CellStyle).Text(t => { t.Span("Nama Pasien: ").SemiBold(); t.Span(dataPasien.Patient.Nama ?? "-"); });
                                table.Cell().Element(CellStyle).Text(t => { t.Span("Tgl Lahir: ").SemiBold(); t.Span(dataPasien.Patient.TanggalLahir?.ToString("dd/MM/yyyy") ?? "-"); });
                                table.Cell().Element(CellStyle).Text(t => { t.Span("Gender: ").SemiBold(); t.Span(dataPasien.Patient.Gender ?? "-"); });

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
                                row.Spacing(20); // Gutter (jarak antar kolom) 20px mencegah teks saling bertabrakan

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
                                        leftCol.Item().PaddingBottom(10).Text(string.IsNullOrWhiteSpace(catatan.Anamnesa) ? "-" : catatan.Anamnesa);

                                        leftCol.Item().Text("Hasil:").SemiBold();
                                        leftCol.Item().Text(string.IsNullOrWhiteSpace(catatan.Hasil) ? "-" : catatan.Hasil);
                                    });

                                    row.RelativeItem().Column(rightCol =>
                                    {
                                        rightCol.Item().Text("Kesimpulan:").SemiBold();
                                        rightCol.Item().PaddingBottom(10).Text(string.IsNullOrWhiteSpace(catatan.Kesimpulan) ? "-" : catatan.Kesimpulan);

                                        rightCol.Item().Text("Saran:").SemiBold();
                                        rightCol.Item().Text(string.IsNullOrWhiteSpace(catatan.Saran) ? "-" : catatan.Saran);
                                    });
                                });
                            });

                            // BLOK 4: GALERI FOTO ENDOSKOPI
                            if (fotoEndoskopiBytes != null && fotoEndoskopiBytes.Count > 0)
                            {
                                // LAYOUT SAFETY: EnsureSpace(50)
                                // Memaksa teks judul pindah ke halaman baru jika ruang di halaman saat ini 
                                // kurang dari 50 point. Mencegah judul tertinggal sendirian tanpa foto di bawahnya.
                                col.Item().EnsureSpace(50).PaddingTop(15).PaddingBottom(5).Text("Browse Image:").FontSize(12).SemiBold().Underline();

                                col.Item().Table(photoGrid =>
                                {
                                    photoGrid.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    foreach (var imgData in fotoEndoskopiBytes)
                                    {
                                        photoGrid.Cell().Padding(5).Image(imgData);
                                    }
                                });
                            }

                            // BLOK 5: AREA TANDA TANGAN DOKTER
                            // LAYOUT SAFETY: ShowEntire()
                            // Memastikan seluruh blok tanda tangan dirender utuh. Jika terpotong pergantian halaman,
                            // QuestPDF akan secara otomatis memindahkan seluruh blok ini ke halaman berikutnya.
                            col.Item().ShowEntire().ExtendVertical().AlignBottom().PaddingTop(30).Row(row =>
                            {
                                // RelativeItem kosong bertindak sebagai per elastis yang mendorong blok tanda tangan ke pojok kanan
                                row.RelativeItem();

                                row.ConstantItem(150).Column(sigCol =>
                                {
                                    string waktuSekarang = DateTime.Now.ToString("dd MMMM yyyy, HH:mm", new System.Globalization.CultureInfo("id-ID"));
                                    sigCol.Item().AlignCenter().Text(waktuSekarang).FontSize(10);

                                    if (gambarTandaTangan != null)
                                    {
                                        // FitHeight() memastikan coretan tanda tangan tidak melar (distorsi) secara proporsional
                                        sigCol.Item().PaddingVertical(5).Height(50).AlignCenter().Image(gambarTandaTangan).FitHeight();
                                    }
                                    else
                                    {
                                        sigCol.Item().PaddingVertical(15).AlignCenter().Text("(Belum Tanda Tangan)").FontSize(9).Italic();
                                    }

                                    sigCol.Item().AlignCenter().Text($"({dataPasien.Dokter ?? "Nama Dokter"})").SemiBold().FontSize(10);
                                });
                            });
                        });

                        // --- FOOTER (Kaki Halaman) ---
                        // Fitur otomatis QuestPDF untuk menghitung dan mencetak nomor halaman
                        page.Footer().PaddingVertical(5).AlignCenter().Text(x =>
                        {
                            x.CurrentPageNumber().FontSize(10);
                        });
                    });
                })
                .GeneratePdf(filePath); // Proses akhir: tulis kode biner struktur PDF ke Harddisk
            }
            catch (Exception ex)
            {
                // Tangkap error jika PC kehabisan memori saat merender, atau file dikunci aplikasi lain
                throw new Exception($"Gagal merender Report PDF (QuestPDF Error). \n\nDetail teknis: {ex.Message}");
            }

            // -------------------------------------------------------------------------
            // TAHAP 3: MENJALANKAN FILE HASIL KE PENGGUNA (OS EXECUTION)
            // -------------------------------------------------------------------------
            try
            {
                // Memerintahkan sistem operasi (Windows) untuk membuka file PDF dengan aplikasi Default Reader
                var p = new System.Diagnostics.Process();
                p.StartInfo = new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true };
                p.Start();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // MENCEGAT ERROR SPESIFIK OS: 
                // Terjadi jika PC perawat/rumah sakit belum menginstall Adobe Acrobat / Foxit / Edge
                // Jangan tutupi fakta bahwa laporan berhasil dibuat. Beritahu mereka lokasinya.
                throw new Exception(
                    "Laporan PDF SUKSES DIBUAT!\n\n" +
                    "Namun, laporan tidak dapat dibuka otomatis karena PC ini belum memiliki aplikasi pembaca PDF (seperti Adobe Acrobat/Edge).\n\n" +
                    $"Silakan buka manual di folder:\n{folderPath}");
            }
            catch (Exception ex)
            {
                // Mencegat error umum lainnya (misal: sistem operasi korup)
                throw new Exception(
                    $"Laporan PDF SUKSES DIBUAT di:\n{folderPath}\n\n" +
                    $"Gagal membuka file secara otomatis. Detail teknis: {ex.Message}");
            }
        }
    }
}