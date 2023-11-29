using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UEcastocLib;
using static UEcastocLib.Packer;
using static UEcastocLib.UCasDataParser;

namespace UnrealUnZen
{
    public partial class WorkInProgressForm : Form
    {
        public WorkInProgressForm()
        {
            InitializeComponent();
        }

        public void OnFileUnpacked(FileUnpackedEventArguments fileUnpackedEventArguments)
        {
            Invoke((MethodInvoker)delegate
            {
                ulong Gigabyte = (ulong)Math.Pow(2.0, 30.0);
                var currentFileNumber = fileUnpackedEventArguments.CurrentFileNumber;
                var totalFilesNumber = fileUnpackedEventArguments.TotalFilesNumber;
                var filesUnpackedSize = fileUnpackedEventArguments.FilesUnpackedSize;
                var allFilesSize = fileUnpackedEventArguments.AllFilesSize;
                double filesUnpackedSizeGB = (double)filesUnpackedSize / Gigabyte;
                double allFilesSizeGB = (double)allFilesSize / Gigabyte;

                ProgressLabel.Text = 
                    $"Unpacked {currentFileNumber} out of {totalFilesNumber} files\n" +
                    $"{filesUnpackedSizeGB:0.##} / {allFilesSizeGB:0.##} GB";
            });
        }

        public void OnFinishedUnpacking(int filesUnpacked)
        {
            MessageBox.Show(filesUnpacked + " file(s) extracted!");
            Invoke((MethodInvoker)Close);
        }

        public void OnManifestFileProcessed(FileProcessedEventArguments fileProcessedEventArguments)
        {
            Invoke((MethodInvoker)delegate
            {
                ulong Gigabyte = (ulong)Math.Pow(2.0, 30.0);
                var currentFileNumber = fileProcessedEventArguments.CurrentFileNumber;
                var totalFilesNumber = fileProcessedEventArguments.TotalFilesNumber;
                var filesUnpackedSize = fileProcessedEventArguments.FilesUnpackedSize;
                var allFilesSize = fileProcessedEventArguments.AllFilesSize;
                double filesUnpackedSizeGB = (double)filesUnpackedSize / Gigabyte;
                double allFilesSizeGB = (double)allFilesSize / Gigabyte;

                ProgressLabel.Text =
                    $"Processed {currentFileNumber} out of {totalFilesNumber} manifest files";
            });
        }

        public void OnFilePacked(FileProcessedEventArguments fileProcessedEventArguments)
        {
            Invoke((MethodInvoker)delegate
            {
                ulong Gigabyte = (ulong)Math.Pow(2.0, 30.0);
                var currentFileNumber = fileProcessedEventArguments.CurrentFileNumber;
                var totalFilesNumber = fileProcessedEventArguments.TotalFilesNumber;
                var filesUnpackedSize = fileProcessedEventArguments.FilesUnpackedSize;
                var allFilesSize = fileProcessedEventArguments.AllFilesSize;
                double filesUnpackedSizeGB = (double)filesUnpackedSize / Gigabyte;
                double allFilesSizeGB = (double)allFilesSize / Gigabyte;

                ProgressLabel.Text =
                    $"Unpacked {currentFileNumber} out of {totalFilesNumber} files\n" +
                    $"{filesUnpackedSizeGB:0.##} / {allFilesSizeGB:0.##} GB";
            });
        }

        public void OnFinishedPacking(int filesPacked)
        {
            MessageBox.Show(filesPacked + " file(s) extracted!");
            Invoke((MethodInvoker)Close);
        }
    }
}
