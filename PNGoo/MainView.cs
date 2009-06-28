using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Drawing.Imaging;

namespace PNGoo
{
    public partial class MainView : Form
    {
        private delegate void passMsg(string msg);

        /// <summary>
        /// Settings to use for batch file processing
        /// </summary>
        private static CompressionSettings compressionSettings = new CompressionSettings();

        public MainView()
        {
            InitializeComponent();
        }

        private void MainView_Load(object sender, EventArgs e)
        {
            // add compression types
            string[] enumNames = Enum.GetNames(typeof(CompressionSettings.PNGType));
            this.pngTypeComboBox.Items.AddRange(enumNames);
            this.pngTypeComboBox.SelectedIndex = 0;
        }

        private void addFilesButton_Click(object sender, EventArgs e)
        {
            if (addFilesDialog.ShowDialog() == DialogResult.OK)
            {
                foreach (string file in addFilesDialog.FileNames)
                {
                    addFileToBatch(file);
                }
            }
        }

        /// <summary>
        /// Adds a file to fileBatchDataGridView.
        /// TODO: Prevent adding duplicates
        /// </summary>
        /// <param name="path">Path to file</param>
        /// <param name="display">Displayed 'path'</param>
        private void addFileToBatch(string path, string display)
        {
            FileInfo fileInfo = new FileInfo(path);
            // we don't accept directories (yet)
            if (fileInfo.Attributes == FileAttributes.Directory)
            {
                return;
            }
            // search for duplicates of the current file
            foreach (DataGridViewRow row in fileBatchDataGridView.Rows)
            {

                if (row.Cells["RealFileColumn"].Value.ToString() == path)
                {
                    return;
                }
            }
            long fileSize = new FileInfo(path).Length;
            double fileK = fileSize / 1024;
            fileK = Math.Round(fileK, 2);
            fileBatchDataGridView.Rows.Add(display, path, fileK + "k", "", "Uncompressed");
        }
        /// <summary>
        /// Adds a file to fileBatchDataGridView.
        /// </summary>
        /// <param name="path">Path to file</param>
        private void addFileToBatch(string path)
        {
            addFileToBatch(path, path);
        }

        private void MainView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                e.Effect = DragDropEffects.All;
            }
        }

        private void MainView_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                addFileToBatch(file);
            }
        }

        private void pasteMenuItem_Click(object sender, EventArgs e)
        {
            System.Collections.Specialized.StringCollection files = Clipboard.GetFileDropList();
            foreach (string file in files)
            {
                addFileToBatch(file);
            }
        }

        /// <summary>
        /// Pastes files from the clipboard into the datagrid
        /// </summary>
        private void pasteFiles()
        {
            if (canPasteFiles())
            {
                System.Collections.Specialized.StringCollection files = Clipboard.GetFileDropList();
                foreach (string file in files)
                {
                    addFileToBatch(file);
                }
            }
        }

        /// <summary>
        /// Can files be pasted from the clipboard?
        /// </summary>
        /// <returns>true if files can be pasted</returns>
        private bool canPasteFiles()
        {
            return Clipboard.ContainsFileDropList();
        }

        private void removeItemButton_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in fileBatchDataGridView.SelectedRows)
            {
                fileBatchDataGridView.Rows.Remove(row);
            }
        }

        private void fileBatchDataGridView_SelectionChanged(object sender, EventArgs e)
        {
            removeItemButton.Enabled = (fileBatchDataGridView.SelectedRows.Count != 0);
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pasteFiles();
        }

        private void MainView_Activated(object sender, EventArgs e)
        {
            bool canPaste = canPasteFiles();
            pasteMenuItem.Enabled = canPaste;
            pasteToolStripMenuItem.Enabled = canPaste;
        }

        private void outputDirectoryBrowseButton_Click(object sender, EventArgs e)
        {
            if (outputFolderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                outputDirectoryTextBox.Text = outputFolderBrowserDialog.SelectedPath;
            }
        }

        private void colourSettingsButton_Click(object sender, EventArgs e)
        {
            ColourSettings colourSettings;
            string filePath;

            DataGridViewSelectedRowCollection selectedRows = fileBatchDataGridView.SelectedRows;

            // bug out if there aren't any rows selected
            if (selectedRows.Count == 0)
            {
                MessageBox.Show(
                    "To prevew colour settings, you must first select an image to preview from the list below",
                    "No Image Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
                return;
            }
            filePath = fileBatchDataGridView.SelectedRows[0].Cells["RealFileColumn"].Value.ToString();
            try
            {
                colourSettings = new ColourSettings(filePath);
                colourSettings.CompressionSettings = compressionSettings.Indexed;
                colourSettings.ShowDialog();
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "The selected image could not be loaded.",
                    "Invalid Image",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
                return;
            }

            if (colourSettings.DialogResult == DialogResult.OK)
            {
                compressionSettings.Indexed = colourSettings.CompressionSettings;
            }
            
        }

        private void overwriteCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            outputDirectoryTextBox.Enabled =
                outputDirectoryBrowseButton.Enabled =
                !overwriteCheckBox.Checked;
        }

        private void pngTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            compressionSettings.OutputType = (CompressionSettings.PNGType)pngTypeComboBox.SelectedIndex;
        }

        private void goButton_Click(object sender, EventArgs e)
        {
            BatchFileCompressor batch = new BatchFileCompressor();
            batch.OutputDirectory = outputDirectoryTextBox.Text;
            batch.OutputIfLarger = overwriteIfLargerCheckBox.Checked;
            batch.CompressionSettings = compressionSettings;
            batch.FilePaths = getFileList();

            // if we're wanting to overwrite the original, set the output dir to null
            if (overwriteCheckBox.Checked)
            {
                batch.OutputDirectory = null;
            }

            batch.Start();
        }

        /// <summary>
        /// Gets the list of files added to the box
        /// </summary>
        /// <returns>List of files</returns>
        private string[] getFileList()
        {
            string[] fileList = new string[fileBatchDataGridView.Rows.Count];
            int i = 0;

            foreach (DataGridViewRow row in fileBatchDataGridView.Rows)
            {
                fileList[i++] = row.Cells["RealFileColumn"].Value.ToString();
            }
            return fileList;
        }

    }
}
