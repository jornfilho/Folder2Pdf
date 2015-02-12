using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace Folder2Pdf
{
    class Program
    {
        private static PdfDocument _pdf;
        private static PdfPage _pdfPage;
        private static XGraphics _graph;
        private static XFont _font;
        private const int LineHeight = 20;
        private const int LineWidth = 85;
        private const int PageHeight = 850;
        private static double _pageHeightPoint;
        private static double _pageWidthPoint;

        private static string _baseFolder;
        private static string _destinationFolder;
        private static string _successLog;
        private static string _errorLog;
        private static IList<string> _allowedExtensions;
            
        [STAThread]
        static void Main(string[] args)
        {
            #region SelectStartFolder
            while (true)
            {
                Console.WriteLine("Selecione a pasta de origem:");

                if (SelectStartFolder())
                    break;

                Console.WriteLine("A seleção da pasta de origem é obrigatória.\n");
            }
            Console.Write(""); 
            #endregion

            #region SelectDestinationFolder
            while (true)
            {
                Console.WriteLine("Selecione a pasta de destino:");

                if (SelectDestinationFolder())
                    break;

                Console.WriteLine("A seleção da pasta de destino é obrigatória.\n");
            } 
            #endregion
            
            Console.WriteLine("Iniciando o processo de conversão");

            try
            {
                InitializeApplication();
                PathLasso(_baseFolder, _destinationFolder);
                RemoveEmptyDirectory(_destinationFolder);
                DumpLog();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Erro ao converter pdf");
            }
        }

        private static bool SelectStartFolder()
        {
            var fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() != DialogResult.OK && !String.IsNullOrEmpty(fbd.SelectedPath)) 
                return false;

            _baseFolder = fbd.SelectedPath;
            return true;
        }

        private static bool SelectDestinationFolder()
        {
            var fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() != DialogResult.OK && !String.IsNullOrEmpty(fbd.SelectedPath))
                return false;

            _destinationFolder = fbd.SelectedPath;
            return true;
        }

        private static void InitializePdf()
        {
            _pdf = new PdfDocument();
            //_pdf.Info.Title = "TXT to PDF";
            _pdfPage = _pdf.AddPage();
            _graph = XGraphics.FromPdfPage(_pdfPage);
            _font = new XFont("Verdana", 12, XFontStyle.Regular);
            _pageHeightPoint = _pdfPage.Height.Point;
            _pageWidthPoint = _pdfPage.Width.Point;
        }

        private static void InitializeApplication()
        {
            _allowedExtensions = new List<string>
            {
                ".bowerrc",
                ".conf",
                ".css",
                ".gitignore",
                ".gitmodules",
                ".htaccess",
                ".html",
                ".inc",
                ".js",
                ".json",
                ".less",
                ".map",
                ".markdown",
                ".md",
                ".npmignore",
                ".nuspec",
                ".php",
                ".pl",
                ".project",
                ".scss",
                ".txt",
                ".xml",
                ".yaml",
                ".yml",
                ".cs"
            };
        }

        

        private static void PathLasso(string baseFolder, string destinationFolder)
        {
            try
            {
                MirrorDirectory(baseFolder, ref destinationFolder);

                foreach (string f in Directory.GetFiles(baseFolder))
                {
                    var ext = new FileInfo(f).Extension;
                    if (!_allowedExtensions.Any(e => e.Equals(ext)))
                        continue;

                    ReadFile(f, destinationFolder);
                }
                foreach (string d in Directory.GetDirectories(baseFolder))
                {
                    PathLasso(d,destinationFolder);
                }
            }
            catch (Exception excpt)
            {
                Debug.WriteLine(excpt.Message);
            }
        }

        private static void MirrorDirectory(string baseFolder, ref string destinationFolder)
        {
            try
            {
                if (baseFolder.Equals(_baseFolder)) 
                    return;

                var baseDirectory = new DirectoryInfo(baseFolder);
                if (!baseDirectory.Exists) 
                    return;

                var baseDirectoryName = baseDirectory.Name;

                if (new DirectoryInfo(destinationFolder + "\\" + baseDirectoryName).Exists) 
                    return;

                Directory.CreateDirectory(destinationFolder + "\\" + baseDirectoryName);
                destinationFolder = destinationFolder + "\\" + baseDirectoryName;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        private static void RemoveEmptyDirectory(string baseFolder)
        {
            foreach (var directory in Directory.GetDirectories(baseFolder))
            {
                RemoveEmptyDirectory(directory);
                if (Directory.GetFiles(directory).Length == 0 &&
                    Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory, false);
                }
            }
        }

        private static void ReadFile(string filePath, string destinationFolder)
        {
            InitializePdf();
            TextReader readFile = new StreamReader(filePath);
            var yPoint = 20;

            while (true)
            {
                var line = readFile.ReadLine();
                if (line == null)
                    break;

                if (line.Length > LineWidth)
                {
                    while (line.Length > LineWidth)
                    {
                        var subline = line.Substring(0, LineWidth);
                        line = line.Substring(LineWidth, line.Length - LineWidth);

                        WriteLine(subline, ref yPoint);
                    }

                    WriteLine(line, ref yPoint);
                }
                else
                    WriteLine(line, ref yPoint);
            }

            var fileInfo = new FileInfo(filePath);
            string pdfFilename = destinationFolder+"\\"+fileInfo.Name+".pdf";
            _pdf.Save(pdfFilename);
            readFile.Close();
        }

        private static void WriteLine(string line, ref int yPoint)
        {
            _graph.DrawString(line, _font, XBrushes.Black, new XRect(LineHeight, yPoint, _pageWidthPoint, _pageHeightPoint), XStringFormats.TopLeft);
            yPoint = yPoint + LineHeight;

            if (yPoint <= PageHeight) 
                return;

            _pdfPage = _pdf.AddPage();
            _graph = XGraphics.FromPdfPage(_pdfPage);
            yPoint = LineHeight;
            _pageHeightPoint = _pdfPage.Height.Point;
            _pageWidthPoint = _pdfPage.Width.Point;
        }

        private static void DumpLog()
        {
            return;
            StreamWriter sw = new StreamWriter(_destinationFolder + "\\_allowedExtensions.txt", true);
            foreach (var ext in _allowedExtensions.OrderBy(e=> e))
            {
                sw.WriteLine(ext);
            }
            sw.Flush();
            sw.Close();
        }
    }
}
