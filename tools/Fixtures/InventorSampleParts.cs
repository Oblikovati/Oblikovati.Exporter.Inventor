// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.Inventor.Model;

namespace Oblikovati.Exporter.Inventor.Fixtures
{
    /// <summary>
    /// Representative Inventor-neutral documents, shared by the unit tests and the golden
    /// round-trip so both assert and open the exact same inputs. M1 covers the document
    /// envelope and model parameters; sketch/feature fixtures arrive in later milestones.
    /// </summary>
    public static class InventorSampleParts
    {
        /// <summary>A bare part: just the envelope and units. The smallest valid .opd.</summary>
        public static InventorDocument EmptyPart() => new InventorDocument
        {
            DisplayName = "empty",
            Kind = InventorDocumentKind.Part,
        };

        /// <summary>
        /// A part carrying parameters with a formula reference ("height = width * 2"),
        /// exercising inline units and cross-parameter references through the emitter.
        /// </summary>
        public static InventorDocument ParametricPart()
        {
            var doc = new InventorDocument
            {
                DisplayName = "parametric",
                Kind = InventorDocumentKind.Part,
            };
            doc.Parameters.Add(new InventorParameter { Name = "width", Expression = "40 mm", Unit = "mm" });
            doc.Parameters.Add(new InventorParameter { Name = "height", Expression = "width * 2", Unit = "mm" });
            return doc;
        }

        /// <summary>
        /// A fully-constrained (DOF 0) rectangle: four coincident, horizontal/vertical lines on
        /// the XY plane, a corner fixed, and width/height dimensions driven by parameters. Sized
        /// 4×3 cm (= 40×30 mm). Exercises the whole sketch pipeline through a closed profile.
        /// </summary>
        public static InventorDocument RectanglePart()
        {
            var doc = new InventorDocument { DisplayName = "rectangle", Kind = InventorDocumentKind.Part };
            doc.Parameters.Add(new InventorParameter { Name = "width", Expression = "40 mm", Unit = "mm" });
            doc.Parameters.Add(new InventorParameter { Name = "height", Expression = "30 mm", Unit = "mm" });

            var sketch = new InventorSketch { Name = "Rectangle" };
            const long l0 = 1, l1 = 2, l2 = 3, l3 = 4;
            sketch.Curves.Add(Line(l0, 0, 0, 4, 0));   // bottom
            sketch.Curves.Add(Line(l1, 4, 0, 4, 3));   // right
            sketch.Curves.Add(Line(l2, 4, 3, 0, 3));   // top
            sketch.Curves.Add(Line(l3, 0, 3, 0, 0));   // left

            Coincide(sketch, l0, InventorCurvePointRole.End, l1, InventorCurvePointRole.Start);
            Coincide(sketch, l1, InventorCurvePointRole.End, l2, InventorCurvePointRole.Start);
            Coincide(sketch, l2, InventorCurvePointRole.End, l3, InventorCurvePointRole.Start);
            Coincide(sketch, l3, InventorCurvePointRole.End, l0, InventorCurvePointRole.Start);

            sketch.Constraints.Add(OnCurve(InventorConstraintKind.Horizontal, l0));
            sketch.Constraints.Add(OnCurve(InventorConstraintKind.Horizontal, l2));
            sketch.Constraints.Add(OnCurve(InventorConstraintKind.Vertical, l1));
            sketch.Constraints.Add(OnCurve(InventorConstraintKind.Vertical, l3));
            sketch.Constraints.Add(Fix(l0, InventorCurvePointRole.Start));

            sketch.Dimensions.Add(Distance(l0, InventorCurvePointRole.Start, l0, InventorCurvePointRole.End, "width"));
            sketch.Dimensions.Add(Distance(l3, InventorCurvePointRole.Start, l3, InventorCurvePointRole.End, "height"));

            doc.Sketches.Add(sketch);
            return doc;
        }

        /// <summary>A fully-constrained (DOF 0) circle: center fixed, diameter driven by a parameter.</summary>
        public static InventorDocument CirclePart()
        {
            var doc = new InventorDocument { DisplayName = "circle", Kind = InventorDocumentKind.Part };
            doc.Parameters.Add(new InventorParameter { Name = "dia", Expression = "40 mm", Unit = "mm" });

            var sketch = new InventorSketch { Name = "Circle" };
            const long c0 = 1;
            sketch.Curves.Add(new InventorCurve
            {
                Id = c0,
                Kind = InventorCurveKind.Circle,
                Center = new double[] { 0, 0 },
                Radius = 2,
            });
            sketch.Constraints.Add(Fix(c0, InventorCurvePointRole.Center));
            var dim = new InventorSketchDimension { Kind = InventorDimensionKind.Diameter, Expression = "dia" };
            dim.Curves.Add(c0);
            sketch.Dimensions.Add(dim);

            doc.Sketches.Add(sketch);
            return doc;
        }

        private static InventorCurve Line(long id, double x0, double y0, double x1, double y1) => new InventorCurve
        {
            Id = id,
            Kind = InventorCurveKind.Line,
            Start = new[] { x0, y0 },
            End = new[] { x1, y1 },
        };

        private static void Coincide(
            InventorSketch sketch, long a, InventorCurvePointRole ar, long b, InventorCurvePointRole br)
        {
            var c = new InventorSketchConstraint { Kind = InventorConstraintKind.Coincident };
            c.Points.Add(new InventorPointRef(a, ar));
            c.Points.Add(new InventorPointRef(b, br));
            sketch.Constraints.Add(c);
        }

        private static InventorSketchConstraint OnCurve(InventorConstraintKind kind, long curve)
        {
            var c = new InventorSketchConstraint { Kind = kind };
            c.Curves.Add(curve);
            return c;
        }

        private static InventorSketchConstraint Fix(long curve, InventorCurvePointRole role)
        {
            var c = new InventorSketchConstraint { Kind = InventorConstraintKind.Fix };
            c.Points.Add(new InventorPointRef(curve, role));
            return c;
        }

        private static InventorSketchDimension Distance(
            long a, InventorCurvePointRole ar, long b, InventorCurvePointRole br, string expression)
        {
            var d = new InventorSketchDimension { Kind = InventorDimensionKind.Distance, Expression = expression };
            d.Points.Add(new InventorPointRef(a, ar));
            d.Points.Add(new InventorPointRef(b, br));
            return d;
        }
    }
}
