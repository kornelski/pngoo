using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace PNGoo
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
        /// Fired when a particular file has been processed, even if compression failed
        /// </summary>
        public event EventHandler FileProcessed;

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
            foreach (string filePath in FilePaths)
            {
                try
                {
                    PNGCompressor pngCompressor = compress(filePath);
                    byte[] fileToWrite = pngCompressor.CompressedFile;
                    string outputDirectory = OutputDirectory;
                    // we may be getting a jpg as input, make sure we output png
                    string fileName = Path.GetFileNameWithoutExtension(filePath) + ".png";

                    // if the compressed file is larger than the original, keep the original (unless told otherwise)
                    // TODO: stop this from outputting non-pngs as pngs
                    if (!OutputIfLarger && pngCompressor.CompressedFile.Length > pngCompressor.OriginalFile.Length)
                    {
                        fileToWrite = pngCompressor.OriginalFile;
                    }

                    // we're going to output to the same directory, overwriting files if needed
                    if (outputDirectory == null)
                    {
                        outputDirectory = Path.GetDirectoryName(filePath);
                    }

                    string outputFilePath = outputDirectory + "/" + fileName;

                    // output the file
                    File.WriteAllBytes(outputDirectory + "/" + fileName, fileToWrite);
                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Compress an image according to given settings
        /// </summary>
        /// <param name="filePath">Path of the file to compress</param>
        /// <returns>Compressed PNG Object</returns>
        private PNGCompressor compress(string filePath)
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
        private PNGCompressor compressIndexed(byte[] fileData)
        {
            PNGQuant pngQuant = new PNGQuant(fileData);
            pngQuant.CompressionSettings = this.CompressionSettings.Indexed;
            pngQuant.Start();
            return pngQuant;
        }

        private void OnFileProcessed(/* need event args */)
        {
        }
    }
}
