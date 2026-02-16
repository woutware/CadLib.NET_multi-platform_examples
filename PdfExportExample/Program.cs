using System;

using WW.Cad.Drawing;
using WW.Cad.Examples;
using WW.Cad.IO;
using WW.Cad.Model;
using WW.Cad.Model.Entities;
using WW.Drawing.Printing;
using WW.Math;

namespace PdfExportExample {
    class Program {
        static void Main(string[] args) {
            // To get the trial software working, please open file MyWWLicense.cs
            // in the root directory and follow the instructions.
            WW.MyWWLicense.Set();

            CreateAndWriteCadDrawing();
            string filename = "Test.dwg";

            PlotOptions options = new PlotOptions {
                ModelSpacePaperSize = PaperSizes.GetPaperSize(PaperKind.A4)
            };
            options.GraphicsConfig.TryDrawingTextAsText = true;

            bool useClassicPdfExporter = true;

            if (useClassicPdfExporter) {
                WW.Cad.Examples.PdfExporterExample.ExportToPdf(filename);
            } else {
                // This V2 PDF exporter is newer, but the classic exporter is more tried and true.
                WW.Cad.Examples.PdfExporterExampleV2.ExportToPdf(filename);
            }

            Console.WriteLine($"Written dwg and pdf files to directory: {Environment.CurrentDirectory}.");
            Console.WriteLine("Press enter.");
            Console.ReadLine();
        }

        // Create a very simple AutoCAD drawing.
        private static void CreateAndWriteCadDrawing() {
            DxfModel model = new DxfModel();

            model.Entities.Add(new DxfLine(new Point2D(1, 0), new Point2D(3, 2)));
            model.Entities.Add(
                new DxfDimension.Aligned(model.CurrentDimensionStyle) {
                    DimensionLineLocation = new Point3D(2, 4, 0),
                    ExtensionLine1StartPoint = new Point3D(1, 0, 0),
                    ExtensionLine2StartPoint = new Point3D(3, 2, 0)
                }
            );
            model.Entities.Add(
                new DxfMText("This is some sample text", new Point3D(0, -1, 0), 0.4d) {
                    Color = EntityColors.Blue
                }
            );

            DwgWriter.Write(@"test.dwg", model);
        }
    }
}