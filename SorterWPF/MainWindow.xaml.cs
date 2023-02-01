using Microsoft.Win32;
using SorterLibrary;
using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Shapes;

namespace SorterWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {        
        private readonly Sorter _sorter = new Sorter();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenDialogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.ShowDialog();
                string filePath = System.IO.Path.GetFullPath(dialog.FileName);
                TextBoxPath.Text = filePath;
            }
            catch (Exception ex)
            {
                ResultBox.Text = ex.Message.ToString();                
            }
        }

        private async void RunSortingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ResultBox.Text = "Sorting...";

                OpenDialogButton.IsEnabled = false;
                RunSortingButton.IsEnabled = false;

                var fileLocation = System.IO.Path.GetDirectoryName(TextBoxPath.Text);

                string outputPath = TextBoxPath.Text + "_output.txt";
                await _sorter.Run(TextBoxPath.Text, outputPath, fileLocation, CancellationToken.None);

                ResultBox.Text = $"Finished.\n Sorted data in: {outputPath}";
            }
            catch (Exception ex)
            {
                ResultBox.Text = ex.Message.ToString(); 
            }
            finally
            {
                OpenDialogButton.IsEnabled = true;
                RunSortingButton.IsEnabled = true;
            }            
        }
    }
}
