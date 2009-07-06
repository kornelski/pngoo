﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Diagnostics;

namespace PNGoo.BatchOperations
{
    public class BatchFileCompressor
    {
        /// <summary>
        /// Paths of files to compress
        /// </summary>
        public string[] FilePaths;

        /// <summary>
        /// Directory to output compressed files. Set as null to output to original files' directory.
        /// </summary>
        public string OutputDirectory = "";

        /// <summary>
        /// Should the file be output even if it's larger? Otherwise the original will be output
        /// </summary>
        public bool OutputIfLarger = false;

        /// <summary>
        /// Setting to use to compress files
        /// </summary>
        public CompressionSettings CompressionSettings;

        /// <summary>
        /// Create new batch file compressor
        /// </summary>
        public BatchFileCompressor() {}

        /// <summary>
        /// Begin compressing files
        /// </summary>
        public void Start()
        {
            if (FilePaths == null || OutputDirectory == String.Empty)
            {
                throw new Exception("FilePaths / OutputDirectory not set");
            }

            // loop through our files
            for (int i = 0; i < FilePaths.Length; i++)
            {
                string filePath = FilePaths[i];

                try
                {
                    Compressor.PNGCompressor pngCompressor = compress(filePath);
                    // this stores the compressor that produced the smallest file
                    Compressor.PNGCompressor winningCompressor = pngCompressor;
                    byte[] fileToWrite = pngCompressor.CompressedFile;
                    string outputDirectory = OutputDirectory;
                    // we may be getting a jpg as input, make sure we output png
                    string fileName = Path.GetFileNameWithoutExtension(filePath) + ".png";

                    // if the compressed file is larger than the original, keep the original (unless told otherwise)
                    if (!OutputIfLarger &&
                        Compressor.PNGCompressor.IsPng(pngCompressor.OriginalFile) &&
                        pngCompressor.CompressedFile.Length >= pngCompressor.OriginalFile.Length)
                    {
                        fileToWrite = pngCompressor.OriginalFile;
                        // there was no winning compressor
                        winningCompressor = null;
                    }

                    // we're going to output to the same directory, overwriting files if needed
                    if (outputDirectory == null)
                    {
                        outputDirectory = Path.GetDirectoryName(filePath);
                    }

                    // build the file path
                    string outputFilePath = System.IO.Path.Combine(outputDirectory, fileName);

                    // output the file
                    File.WriteAllBytes(outputFilePath, fileToWrite);

                    // fire the success event
                    FileProcessSuccessEventArgs e = new FileProcessSuccessEventArgs(filePath, outputFilePath, i, winningCompressor);
                    OnFileProcessSuccess(e);
                }
                catch (Exception e)
                {
                    // fire the fail event
                    FileProcessFailEventArgs eventArgs = new FileProcessFailEventArgs(filePath, i, e);
                    OnFileProcessFail(eventArgs);
                }
            }
        }

        /// <summary>
        /// Compress an image according to given settings
        /// </summary>
        /// <param name="filePath">Path of the file to compress</param>
        /// <returns>Compressed PNG Object</returns>
        private Compressor.PNGCompressor compress(string filePath)
        {
            // read file
            byte[] fileData = File.ReadAllBytes(filePath);

            // select which compressor to use
            switch (this.CompressionSettings.OutputType)
            {
                case CompressionSettings.PNGType.Indexed:
                    return compressIndexed(fileData);
                default:
                    throw new Exception("Invalid output type");
            }
        }

        /// <summary>
        /// Compress an image according to given indexed settings
        /// </summary>
        /// <param name="fileData">Byte data of the file to compress</param>
        /// <returns>Compressed PNGCompressor</returns>
        private Compressor.PNGCompressor compressIndexed(byte[] fileData)
        {
            Compressor.PNGQuant pngQuant = new Compressor.PNGQuant(fileData);
            pngQuant.CompressionSettings = this.CompressionSettings.Indexed;
            pngQuant.Start();
            return pngQuant;
        }

        /// <summary>
        /// Fire the 'FileProcessSuccess' event
        /// </summary>
        /// <param name="e">Event args</param>
        private void OnFileProcessSuccess(FileProcessSuccessEventArgs e)
        {
            FileProcessSuccess(this, e);
        }

        /// <summary>
        /// Fire the 'FileProcessFail' event
        /// </summary>
        /// <param name="e">Event args</param>
        private void OnFileProcessFail(FileProcessFailEventArgs e)
        {
            FileProcessFail(this, e);
        }

        /// <summary>
        /// Fire the 'Complete' event
        /// </summary>
        private void OnComplete()
        {
            Complete(this, EventArgs.Empty);
        }

        /// <summary>
        /// Fired when a file in the batch processes successfully
        /// </summary>
        public event FileProcessSuccessEventHandler FileProcessSuccess = delegate { };

        /// <summary>
        /// Fired when a file in the batch processes unsuccessfully
        /// </summary>
        public event FileProcessFailEventHandler FileProcessFail = delegate { };

        /// <summary>
        /// Fired when all files in the batch queue have processed
        /// </summary>
        public event EventHandler Complete = delegate { };
    }
}
