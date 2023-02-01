using FileGeneratorLibrary;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FileGeneratorWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const long ONE_HUNDRED_GB = 100 * 1024 * 1024 * 1024L;
        private const long ONE_GB = 1024 * 1024 * 1024;
        private const long ONE_MB = 1024 * 1024;

        private readonly FileGenerator _generator = new FileGenerator();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void SaveDialogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.Filter = "Text (*.txt)|*.txt";
                dialog.ShowDialog();
                string filePath = System.IO.Path.GetFullPath(dialog.FileName);
                TextBoxPath.Text = filePath;
            }
            catch (Exception ex)
            {
                ResultBox.Text = ex.Message.ToString();
            }
        }

        private async void RunGeneratingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ResultBox.Text = "Running...";

                SaveDialogButton.IsEnabled = false;
                RunGeneratingButton.IsEnabled = false;

                long fileMaxSize = GetMaxFileSize();

                if (fileMaxSize < ONE_MB || fileMaxSize > ONE_HUNDRED_GB)
                {
                    throw new Exception("Max file size must be from range (1 MB - 100 GB)");
                }

                string outputPath = TextBoxPath.Text;
                await _generator.Generate(outputPath, fileMaxSize, CancellationToken.None);

                ResultBox.Text = $"Finished.\n Generated data in: {TextBoxPath.Text}";
            }
            catch (Exception ex)
            {
                ResultBox.Text = ex.Message.ToString();
            }
            finally
            {
                SaveDialogButton.IsEnabled = true;
                RunGeneratingButton.IsEnabled = true;
            }
        }

        private long GetMaxFileSize()
        {
            var fileSize = long.Parse(TextBoxMaxFileSize.Text);

            var fileUnit = ComboBoxFileSizeUnit.Text;

            if (fileUnit == "MB")
            {
                fileSize *= ONE_MB;
            }
            else
            {
                fileSize *= ONE_GB;
            }

            return fileSize;
        }
    }
}
