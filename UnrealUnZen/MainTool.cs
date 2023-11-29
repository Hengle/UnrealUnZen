using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using UEcastocLib;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;

namespace UnrealUnZen
{
    public partial class MainTool : Form
    {
        private CommonOpenFileDialog UnpackFolderBrowserDialog = new CommonOpenFileDialog();
        WorkInProgressForm workInProgressForm = new WorkInProgressForm();

        public MainTool()
        {
            UnpackFolderBrowserDialog.IsFolderPicker = true;
            UnpackFolderBrowserDialog.Title = 
                "Select a folder into which to unpack the package files.\n" +
                "Another folder with the name of the selected package will be created automatically.";

            UCasDataParser.FileUnpacked += workInProgressForm.OnFileUnpacked;
            UCasDataParser.FinishedUnpacking += workInProgressForm.OnFinishedUnpacking;

            Packer.ManifestFileProcessed += workInProgressForm.OnManifestFileProcessed;
            Packer.FilePacked += workInProgressForm.OnFilePacked;
            Packer.FinishedPacking += workInProgressForm.OnFinishedPacking;

            InitializeComponent();
        }

        string UTocFileAddress = "";
        UTocData UTocFile = new UTocData();

        private void Form1_Load(object sender, EventArgs e)
        {
            RepackMethodCMB.SelectedIndex = 0;
            UTocVerCMB.SelectedIndex = 0;
        }
        public static TreeNode MakeTreeFromPaths(List<string> paths, string rootNodeName = "", char separator = '/')
        {
            var rootNode = new TreeNode(rootNodeName);
            foreach (var path in paths.Where(x => !string.IsNullOrEmpty(x.Trim())))
            {
                var currentNode = rootNode;
                var pathItems = path.Split(separator);
                foreach (var item in pathItems)
                {
                    var tmp = currentNode.Nodes.Cast<TreeNode>().Where(x => x.Text.Equals(item));
                    currentNode = tmp.Count() > 0 ? tmp.Single() : currentNode.Nodes.Add(item);
                }
            }
            return rootNode;
        }
        private void OpenTocBTN_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "UToc files(*.utoc)|*.utoc";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                UTocFileAddress = ofd.FileName;
                UTocFile = UTocDataParser.ParseUtocFile(UTocFileAddress, Helpers.HexStringToByteArray(AESKey.Text));
                List<string> pathes = new List<string>();

                for (int i = 0; i < UTocFile.Files.Count; i++)
                {
                    pathes.Add(UTocFile.Files[i].FilePath);
                }

                ArchiveViewTV.Nodes.Clear();
                ArchiveViewTV.Nodes.Add(MakeTreeFromPaths(pathes, Path.GetFileNameWithoutExtension(UTocFileAddress), '\\'));
                OpenTocBTN.Text = "Load TOC (Loaded " + Path.GetFileNameWithoutExtension(UTocFileAddress) + ")";
                UnpackBTN.Enabled = true;
                RepackBTN.Enabled = true;
                saveManifestToolStripMenuItem.Enabled = true;
                fixManifestToolStripMenuItem.Enabled = true;
            }
        }
        private void UnpackBTN_Click(object sender, EventArgs e)
        {
            UnpackFolderBrowserDialog.InitialDirectory = UTocFileAddress;

            if (UnpackFolderBrowserDialog.ShowDialog() != CommonFileDialogResult.Ok) return;

            var outFolderName = Path.GetFileNameWithoutExtension(UTocFileAddress) + "_Export";
            var unpackDirectoryPath = Path.Combine(UnpackFolderBrowserDialog.FileName, outFolderName);
            Directory.CreateDirectory(unpackDirectoryPath);

            Task.Factory.StartNew(() => UTocFile.UnpackUcasFiles(Path.ChangeExtension(UTocFileAddress, ".ucas"),
                unpackDirectoryPath, RegexUnpack.Text));

            workInProgressForm.ShowDialog();
        }

        private void RepackBTN_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog repackFolderDialog = new CommonOpenFileDialog();
            repackFolderDialog.IsFolderPicker = true;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "utoc file|*.utoc";

            if (repackFolderDialog.ShowDialog() != CommonFileDialogResult.Ok) return;

            saveFileDialog.InitialDirectory = Directory.GetParent(repackFolderDialog.FileName)?.ToString();
            saveFileDialog.FileName = Path.GetFileNameWithoutExtension(repackFolderDialog.FileName);

            if (saveFileDialog.ShowDialog() != DialogResult.OK) return;

            var compressionMethod = RepackMethodCMB.GetItemText(RepackMethodCMB.SelectedItem);
            var repackFolder = repackFolderDialog.FileName;
            var outFile = saveFileDialog.FileName;
            var aesKeyText = AESKey.Text;

            Task.Factory.StartNew(() => {
                Packer.PackGameFiles(UTocFileAddress, UTocFile, repackFolder, outFile, compressionMethod, aesKeyText);
            });

            workInProgressForm.ShowDialog();
        }

        private void MountPointTXB_TextChanged(object sender, EventArgs e)
        {
            Constants.MountPoint = MountPointTXB.Text;
        }

        private void saveManifestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.Filter = "Manifest Json File|*.json";
            saveFile.FileName = Path.GetFileNameWithoutExtension(UTocFileAddress);
            if (saveFile.ShowDialog() == DialogResult.OK)
            {
                Manifest manifest = UTocFile.ConstructManifest(Path.ChangeExtension(UTocFileAddress, ".ucas"));
                File.WriteAllText(saveFile.FileName, JsonConvert.SerializeObject(manifest, Formatting.Indented));
                MessageBox.Show("Done!");
            }
        }

        private void fixManifestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Json Manifest File|*.json";
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok && openFileDialog.ShowDialog() == DialogResult.OK)
            {
                Manifest manifest = Packer.JsonToManifest(openFileDialog.FileName);
                foreach (var f in manifest.Files.ToList())
                {
                    if (!File.Exists(Path.Combine(dialog.FileName, f.Filepath.Replace("/", "\\"))) && f.Filepath != "dependencies")
                    {
                        manifest.Files.Remove(f);
                        manifest.Deps.ChunkIDToDependencies.Remove(ulong.Parse(f.ChunkID.Substring(0, 16), System.Globalization.NumberStyles.HexNumber));
                    }
                }
                File.WriteAllText(openFileDialog.FileName + ".Fixed_json", JsonConvert.SerializeObject(manifest, Formatting.Indented));
                MessageBox.Show("Done!");
            }
        }

        private void repackUsingCustomManifestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            OpenFileDialog jsonManifest = new OpenFileDialog();
            jsonManifest.Filter = "Manifest Json File|*.json";
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "utoc file|*.utoc";
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok && jsonManifest.ShowDialog() == DialogResult.OK && saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                Manifest manifest = Packer.JsonToManifest(dialog.FileName);

                int gameFilesPackedTotal = Packer.PackGameFiles(dialog.FileName, manifest, saveFileDialog.FileName, RepackMethodCMB.GetItemText(RepackMethodCMB.SelectedItem), AESKey.Text);
                if (gameFilesPackedTotal != 0)
                    MessageBox.Show(gameFilesPackedTotal + " file(s) packed!");
            }
        }

        private void HelpFilter_Click(object sender, EventArgs e)
        {
            MessageBox.Show("this will filter the files to extract using the W wildcards separated by comma or semicolon, example {}.mp3,{}.txt;{}myname{}\r\nuse {} instead of * to avoid issues on Windows");
        }
    }
}
