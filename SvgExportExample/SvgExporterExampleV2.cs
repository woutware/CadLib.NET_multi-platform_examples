#region (C) Wout Ware 2026
//
// File: SvgExporterExampleV2.cs
// Author: Wout de Zeeuw
// Creation: 1/22/2026
//
// (c) 2026 Wout Ware All Rights Reserved.
//
#endregion

using System;
using System.Collections.Generic;
#if !MULTIPLATFORM
using System.Drawing.Printing;
#endif
using System.IO;

using WW.Cad.Base;
using WW.Cad.Drawing;
using WW.Cad.Drawing.Wireframe;
using WW.Cad.IO;
using WW.Cad.Model;
using WW.Cad.Model.Objects;
using WW.Cad.Model.Tables;
#if MULTIPLATFORM
using WW.Drawing.Printing;
#endif
using WW.Math;
using WW.Math.Geometry;

namespace WW.Cad.Examples {
    /// <summary>
    /// This class demonstrates how to export an AutoCAD file to SVG (both model space and paper space layouts).
    /// The <see cref="SvgExporter"/> is used in paper mode in this example.
    /// </summary>
    public class SvgExporterExampleV2 {
        // Exports an AutoCAD file to SVG. For each layout a page in the SVG file is created.
        public static void ExportToSvg(string filename, SvgExportOptions options = null) {
            DxfModel model = CadReader.Read(
                filename, 
                new ReadConfig { ReadUnknownEntityHandling = ReadUnknownEntityHandling.LoadAsUnknownEntity }
            );
            model.LoadExternalReferences();
            ExportToSvg(model, options);
        }

        // Exports the specified layout of an AutoCAD file to SVG.
        public static void ExportToSvg(DxfModel model, SvgExportOptions options = null) {
            if (options == null) {
                options = SvgExportOptions.Default;
            }
            string filename = Path.GetFileName(model.Filename);
            string dir = Path.GetDirectoryName(model.Filename);
            string filenameNoExt = Path.GetFileNameWithoutExtension(filename);
            string outputFilename = options.GetOutputFilename(dir, filenameNoExt, ".svg");

            using (FileStream stream = File.Create(outputFilename)) {
                SvgExporter svgExporter = new SvgExporter(stream);

                AddLayoutToSvgExporter(svgExporter, model, null, options);
            }
        }

        // For each layout, add a page to the SVG file.
        // Optionally specify a modelView (for model space only).
        // Optionally specify a layout.
        private static void AddLayoutToSvgExporter(
            SvgExporter svgExporter, DxfModel model, DxfView modelView, SvgExportOptions options
        ) {
            if (options == null) {
                options = SvgExportOptions.Default;
            }
            svgExporter.PlotOptions = options;
            Bounds3D bounds;
            const float defaultMarginInInches = 0.5f;
            float marginInInches = 0f;
            PaperSize paperSize = null;
            bool useModelView = false;
            bool emptyLayout = false;
            DxfLayout layout;
            if (options.Layout != null) {
                layout = options.Layout;
            } else {
                layout = model.Header.ShowModelSpace ? model.ModelLayout : model.ActiveLayout;
            }

            DrawableStore drawableStore = new DrawableStore();
            var drawable = DrawableUtil.CreateDrawables(drawableStore, options.GraphicsConfig, model, layout);
            DrawableDrawContext drawContext = new DrawableDrawContext(model, layout, options.GraphicsConfig) { IsPlot = true };
            var drawableInstance = DrawableUtil.CreateDrawableInstance(drawContext, drawable);

            if (!layout.PaperSpace) {
                // Model space.
                bounds = new Bounds3D();
                drawable.GetWorldBounds(drawContext, bounds, false);

                if (bounds.Initialized) {
                    paperSize = GetPaperSize(bounds, options.ModelSpacePaperKind, options.ModelSpaceOrientation);
                } else {
                    emptyLayout = true;
                }
                marginInInches = defaultMarginInInches;
                useModelView = modelView != null;
            } else {
                // Paper space layout.
                Bounds2D plotAreaBounds = layout.GetPlotAreaBounds(options.GetPlotArea);
                bounds = new Bounds3D();
                emptyLayout = !plotAreaBounds.Initialized;
                if (plotAreaBounds.Initialized) {
                    double customScaleFactor = 1d;
                    if (
                        (layout.PlotLayoutFlags & PlotLayoutFlags.UseStandardScale) == 0 &&
                        (layout.PlotArea == PlotArea.LayoutInformation) &&
                        (layout.CustomPrintScaleNumerator != 0d && layout.CustomPrintScaleDenominator != 0d)
                    ) {
                        customScaleFactor = layout.CustomPrintScaleNumerator / layout.CustomPrintScaleDenominator;
                    }
                    bounds.Update((Point3D)(Vector3D)((Vector2D)plotAreaBounds.Min / customScaleFactor));
                    bounds.Update((Point3D)(Vector3D)((Vector2D)plotAreaBounds.Max / customScaleFactor));

                    if (layout.PlotArea == PlotArea.LayoutInformation) {
                        switch (layout.PlotPaperUnits) {
                            case PlotPaperUnits.Millimeters:
                                paperSize = new PaperSize(Guid.NewGuid().ToString(), (int)(plotAreaBounds.Delta.X * 100d / 25.4d), (int)(plotAreaBounds.Delta.Y * 100d / 25.4d));
                                break;
                            case PlotPaperUnits.Inches:
                                paperSize = new PaperSize(Guid.NewGuid().ToString(), (int)(plotAreaBounds.Delta.X * 100d), (int)(plotAreaBounds.Delta.Y * 100d));
                                break;
                            case PlotPaperUnits.Pixels:
                                // No physical paper units. Fall back to fitting layout into a known paper size.
                                break;
                        }
                    }

                    if (paperSize == null) {
                        paperSize = GetPaperSize(bounds, options.PaperSpaceDefaultPaperKind, options.PaperSpaceDefaultOrientation);
                        marginInInches = defaultMarginInInches;
                    }
                }
            }

            if (!emptyLayout) {
                bool paperMode = true;
                double scaleFactor;
                Matrix4D to2DTransform;
                if (paperMode) {
                    // Paper mode, creates SVG with a specific paper size in cm and coordinates in 100th of cm.
                    svgExporter.PaperSize = paperSize;

                    // Lengths in inches.
                    float pageWidthInInches = paperSize.Width / 100f;
                    float pageHeightInInches = paperSize.Height / 100f;

                    // SvgExporter is in paper mode, so SVG units are in 100ths of cm.
                    const double inchToHundredthCm = 2.54 * 100;

                    if (useModelView) {
                        to2DTransform = modelView.GetMappingTransform(
                            new Rectangle2D(
                                marginInInches * inchToHundredthCm,
                                marginInInches * inchToHundredthCm,
                                (pageWidthInInches - marginInInches) * inchToHundredthCm,
                                (pageHeightInInches - marginInInches) * inchToHundredthCm),
                            true);
                        scaleFactor = double.NaN; // Not needed for model space.
                    } else {
                        to2DTransform = DxfUtil.GetScaleTransform(
                            bounds.Corner1,
                            bounds.Corner2,
                            new Point3D(bounds.Center.X, bounds.Corner2.Y, 0d),
                            new Point3D(new Vector3D(marginInInches, pageHeightInInches - marginInInches, 0d) * inchToHundredthCm),
                            new Point3D(new Vector3D(pageWidthInInches - marginInInches, marginInInches, 0d) * inchToHundredthCm),
                            new Point3D(new Vector3D(pageWidthInInches / 2d, marginInInches, 0d) * inchToHundredthCm),
                            out scaleFactor
                        );
                    }
                } else {
                    // Pixel mode, creates SVG with pixels as units.
                    Size2D sizeInPixels;
                    double maxSizeInPixels = 1000;

                    var boundsDelta = bounds.Delta;
                    if (boundsDelta.X > boundsDelta.Y) {
                        sizeInPixels = new Size2D(maxSizeInPixels, (maxSizeInPixels * boundsDelta.Y) / boundsDelta.X);
                    } else {
                        sizeInPixels = new Size2D((maxSizeInPixels * boundsDelta.X) / boundsDelta.Y, maxSizeInPixels);
                    }

                    // This sets the SvgExporter in pixel mode.
                    svgExporter.SizeInPixels = new System.Drawing.Size((int)System.Math.Ceiling(sizeInPixels.X), (int)System.Math.Ceiling(sizeInPixels.Y));

                    const double marginInPixels = 5d;

                    if (useModelView) {
                        to2DTransform = modelView.GetMappingTransform(
                            new Rectangle2D(
                                marginInPixels,
                                marginInPixels,
                                sizeInPixels.X - marginInPixels,
                                sizeInPixels.Y - marginInPixels),
                            true);
                        scaleFactor = double.NaN; // Not needed for model space.
                    } else {
                        to2DTransform = DxfUtil.GetScaleTransform(
                            bounds.Corner1,
                            bounds.Corner2,
                            new Point3D(bounds.Center.X, bounds.Corner2.Y, 0d),
                            new Point3D(new Vector3D(marginInPixels, sizeInPixels.Y - marginInPixels, 0d)),
                            new Point3D(new Vector3D(sizeInPixels.X - marginInPixels, marginInPixels, 0d)),
                            new Point3D(new Vector3D(sizeInPixels.X / 2d, marginInPixels, 0d)),
                            out scaleFactor
                        );
                    }
                }

                if (layout == null || !layout.PaperSpace) {
                    svgExporter.Draw(drawableInstance, model, options.GraphicsConfig, to2DTransform);
                } else {
                    svgExporter.Draw(drawableInstance, model, layout, null, options.GraphicsConfig, to2DTransform, scaleFactor);
                }
            }
        }

        private static PaperSize GetPaperSize(Bounds3D bounds, PaperKind paperKind, PlotOrientation orientation) {
            PaperSize paperSize = PaperSizes.GetPaperSize(paperKind);
            if (orientation == PlotOrientation.Auto) {
                if (bounds.Delta.X > bounds.Delta.Y) {
                    paperSize = new PaperSize($"{paperSize.PaperName}, rotated", paperSize.Height, paperSize.Width);
                }
            } else if (orientation == PlotOrientation.Portrait) {
                if (paperSize.Width > paperSize.Height) {
                    paperSize = new PaperSize($"{paperSize.PaperName}", paperSize.Height, paperSize.Width);
                }
            } else if (orientation == PlotOrientation.Landscape) {
                if (paperSize.Width < paperSize.Height) {
                    paperSize = new PaperSize($"{paperSize.PaperName}", paperSize.Height, paperSize.Width);
                }
            }
            return paperSize;
        }
    }
}
