using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using Tesseract;
using Aspose.Pdf;
using Aspose.Pdf.Devices;

namespace szukaniewzorca
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtFolderPath.Text = fbd.SelectedPath;
                }
            }
        }

        private void btnStartOcr_Click(object sender, RoutedEventArgs e)
        {
            lstResults.Items.Clear();

            string folderPath = txtFolderPath.Text;
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                System.Windows.MessageBox.Show("Proszę wybrać poprawny folder.");
                return;
            }

            string tessDataPath = @"C:\Program Files\Tesseract-OCR\tessdata";

            // Wzorzec dla numeru faktury (przynajmniej dwa slashe)
            string invoicePattern = @"[A-Za-z0-9]+(?:\/[A-Za-z0-9]+){2,}";

            // Wzorzec dla daty w jednym z czterech określonych formatów
            string datePattern1 = @"(?:\d{2}[-.]\d{2}[-.]\d{4})"; // dd-mm-yyyy lub dd.mm.yyyy
            string datePattern2 = @"(?:\d{2}[-.]\d{4}[-.]\d{2})"; // mm-dd-yyyy lub mm.dd.yyyy
            string datePattern3 = @"(?:\d{4}[-.]\d{2}[-.]\d{2})"; // yyyy-mm-dd lub yyyy.mm.dd

            try
            {
                using (var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default))
                {
                    var files = Directory.GetFiles(folderPath);
                    foreach (var filePath in files)
                    {
                        string extension = Path.GetExtension(filePath).ToLowerInvariant();

                        if (extension == ".pdf")
                        {
                            // Konwertuj PDF na obrazy
                            string imageFolder = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(filePath));
                            Directory.CreateDirectory(imageFolder); // Utwórz folder na obrazy, jeśli nie istnieje
                            ConvertPdfToImages(filePath, imageFolder);

                            // Przeprowadz OCR na wygenerowanych obrazach
                            var imageFiles = Directory.GetFiles(imageFolder);
                            foreach (var imageFile in imageFiles)
                            {
                                if (imageFile.ToLowerInvariant().EndsWith(".png") || imageFile.ToLowerInvariant().EndsWith(".jpg"))
                                {
                                    ProcessImageWithTesseract(imageFile, engine, invoicePattern, datePattern1, datePattern2, datePattern3);
                                }
                            }
                        }
                        else if (extension == ".jpg" || extension == ".jpeg" || extension == ".png"
                            || extension == ".tif" || extension == ".tiff" || extension == ".bmp")
                        {
                            ProcessImageWithTesseract(filePath, engine, invoicePattern, datePattern1, datePattern2, datePattern3);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Błąd Tesseract: {ex.Message}");
            }
        }

        private void ConvertPdfToImages(string pdfFilePath, string outputFolder)
        {
            Document pdfDocument = new Document(pdfFilePath);
            Resolution resolution = new Resolution(300);
            PngDevice pngDevice = new PngDevice(500, 700, resolution);

            for (int pageNumber = 1; pageNumber <= pdfDocument.Pages.Count; pageNumber++)
            {
                string outputImagePath = Path.Combine(outputFolder, $"page_{pageNumber}.png");
                pngDevice.Process(pdfDocument.Pages[pageNumber], outputImagePath);
            }
        }

        private void ProcessImageWithTesseract(string imageFilePath, TesseractEngine engine, string invoicePattern, string datePattern1, string datePattern2, string datePattern3)
        {
            try
            {
                using (var pix = Pix.LoadFromFile(imageFilePath))
                {
                    using (var page = engine.Process(pix))
                    {
                        string ocrText = page.GetText();
                        ocrText = ocrText.ToUpperInvariant();

                        // Sprawdzenie, czy są co najmniej dwa slashe w tekście
                        int slashCount = ocrText.Count(c => c == '/');
                        if (slashCount >= 2)
                        {
                            lstResults.Items.Add($"Plik: {Path.GetFileName(imageFilePath)}");

                            // Znalezienie numerów faktur
                            var invoiceMatches = Regex.Matches(ocrText, invoicePattern);
                            foreach (Match m in invoiceMatches)
                            {
                                lstResults.Items.Add($"   Numer faktury: {m.Value}");
                            }

                            // Szukanie frazy "data wystawienia"
                            var dataWystawieniaMatch = Regex.Match(ocrText, @"DATA WYSTAWIENIA");
                            if (dataWystawieniaMatch.Success)
                            {
                                // Znalezienie daty w pobliżu napisu "data wystawienia" (np. do 50 znaków po frazie)
                                int startIndex = dataWystawieniaMatch.Index + dataWystawieniaMatch.Length;
                                string substringAfterDataWystawienia = ocrText.Substring(startIndex, Math.Min(50, ocrText.Length - startIndex)); // Ograniczysz długość przeszukiwanego tekstu

                                // Szukaj daty tylko w tej części tekstu
                                var dateMatches1 = Regex.Matches(substringAfterDataWystawienia, datePattern1);
                                var dateMatches2 = Regex.Matches(substringAfterDataWystawienia, datePattern2);
                                var dateMatches3 = Regex.Matches(substringAfterDataWystawienia, datePattern3);

                                // Wypisz znalezione daty
                                foreach (Match date in dateMatches1)
                                {
                                    lstResults.Items.Add($"   Data: {date.Value}");
                                }
                                foreach (Match date in dateMatches2)
                                {
                                    lstResults.Items.Add($"   Data: {date.Value}");
                                }
                                foreach (Match date in dateMatches3)
                                {
                                    lstResults.Items.Add($"   Data: {date.Value}");
                                }
                            }
                           
                        }
                    }
                }
            }
            catch (Exception exFile)
            {
                lstResults.Items.Add($"Błąd pliku {Path.GetFileName(imageFilePath)}: {exFile.Message}");
            }
        }


    }
}
