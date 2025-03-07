using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;  
using Tesseract;

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
            string pattern = @"\d+/[A-Z]{1,5}/[A-Z]{1,5}/\d{4}";

            try
            {
                using (var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default))
                {
                    var imageFiles = Directory.GetFiles(folderPath);
                    foreach (var filePath in imageFiles)
                    {
                        string extension = Path.GetExtension(filePath).ToLowerInvariant();
                        if (extension == ".jpg" || extension == ".jpeg" || extension == ".png"
                            || extension == ".tif" || extension == ".tiff" || extension == ".bmp")
                        {
                            try
                            {
                                using (var pix = Pix.LoadFromFile(filePath))
                                {
                                    using (var page = engine.Process(pix))
                                    {
                                        string ocrText = page.GetText();
                                        ocrText = ocrText.ToUpperInvariant();
                                        var matches = Regex.Matches(ocrText, pattern);

                                        if (matches.Count > 0)
                                        {
                                            lstResults.Items.Add($"Plik: {Path.GetFileName(filePath)}");
                                            foreach (Match m in matches)
                                            {
                                                lstResults.Items.Add($"  -> Numer faktury: {m.Value}");
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception exFile)
                            {
                                lstResults.Items.Add($"Błąd pliku {Path.GetFileName(filePath)}: {exFile.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Błąd Tesseract: {ex.Message}");
            }
        }
    }
}
