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

        /// <summary>
        /// The rectangle extruded 5 cm into a solid box. Volume = 4 × 3 × 5 = 60 cm³ — the
        /// end-to-end fidelity check (sketch profile → solid) when round-tripped.
        /// </summary>
        public static InventorDocument BoxPart()
        {
            InventorDocument doc = RectanglePart();
            doc.DisplayName = "box";
            doc.Features.Add(new InventorExtrude
            {
                Name = "Extrude1",
                SketchIndex = 0,
                ProfileIndex = 0,
                Operation = InventorOperation.NewBody,
                Direction = InventorExtentDirection.Positive,
                Distance = 5,
            });
            return doc;
        }

        /// <summary>
        /// An offset square section revolved a full turn about its own centerline (the sketch's
        /// Y axis), making a washer/tube: R=4, r=2, h=2 cm → volume 24π ≈ 75.4 cm³.
        /// </summary>
        public static InventorDocument RevolvePart()
        {
            var doc = new InventorDocument { DisplayName = "revolve", Kind = InventorDocumentKind.Part };
            doc.Parameters.Add(new InventorParameter { Name = "side", Expression = "20 mm", Unit = "mm" });

            var sketch = new InventorSketch { Name = "Section" };
            const long l0 = 1, l1 = 2, l2 = 3, l3 = 4, axis = 5;
            sketch.Curves.Add(Line(l0, 2, 0, 4, 0));   // bottom
            sketch.Curves.Add(Line(l1, 4, 0, 4, 2));   // outer
            sketch.Curves.Add(Line(l2, 4, 2, 2, 2));   // top
            sketch.Curves.Add(Line(l3, 2, 2, 2, 0));   // inner
            InventorCurve centerline = Line(axis, 0, 0, 0, 2);
            centerline.Centerline = true;
            sketch.Curves.Add(centerline);

            Coincide(sketch, l0, InventorCurvePointRole.End, l1, InventorCurvePointRole.Start);
            Coincide(sketch, l1, InventorCurvePointRole.End, l2, InventorCurvePointRole.Start);
            Coincide(sketch, l2, InventorCurvePointRole.End, l3, InventorCurvePointRole.Start);
            Coincide(sketch, l3, InventorCurvePointRole.End, l0, InventorCurvePointRole.Start);
            sketch.Constraints.Add(OnCurve(InventorConstraintKind.Horizontal, l0));
            sketch.Constraints.Add(OnCurve(InventorConstraintKind.Horizontal, l2));
            sketch.Constraints.Add(OnCurve(InventorConstraintKind.Vertical, l1));
            sketch.Constraints.Add(OnCurve(InventorConstraintKind.Vertical, l3));
            sketch.Constraints.Add(Fix(l0, InventorCurvePointRole.Start));
            sketch.Dimensions.Add(Distance(l0, InventorCurvePointRole.Start, l0, InventorCurvePointRole.End, "side"));
            sketch.Dimensions.Add(Distance(l3, InventorCurvePointRole.Start, l3, InventorCurvePointRole.End, "side"));

            // Pin the centerline (vertical on the Y axis, length "side").
            sketch.Constraints.Add(OnCurve(InventorConstraintKind.Vertical, axis));
            sketch.Constraints.Add(Fix(axis, InventorCurvePointRole.Start));
            sketch.Dimensions.Add(Distance(axis, InventorCurvePointRole.Start, axis, InventorCurvePointRole.End, "side"));

            doc.Sketches.Add(sketch);
            doc.Features.Add(new InventorRevolve
            {
                Name = "Revolve1",
                SketchIndex = 0,
                ProfileIndex = 0,
                Operation = InventorOperation.NewBody,
                AngleRadians = 0, // full revolution
            });
            return doc;
        }

        /// <summary>Three boxes in a row: the box extrude patterned 3× along X. Volume 3 × 60 = 180 cm³.</summary>
        public static InventorDocument RectPatternPart()
        {
            InventorDocument doc = MakeBox("rect-pattern", 0);
            var pattern = new InventorRectangularPattern
            {
                Name = "Pattern1",
                CountX = 3,
                CountY = 1,
                StepX = new double[] { 6, 0, 0 },
                StepY = new double[] { 0, 0, 0 },
            };
            pattern.SourceFeatureIndices.Add(0); // the extrude
            doc.Features.Add(pattern);
            return doc;
        }

        /// <summary>The box mirrored across the YZ plane (x=0): two boxes. Volume 2 × 60 = 120 cm³.</summary>
        public static InventorDocument MirrorPart()
        {
            InventorDocument doc = MakeBox("mirror", 0);
            var mirror = new InventorMirror
            {
                Name = "Mirror1",
                PlaneOrigin = new double[] { 0, 0, 0 },
                PlaneNormal = new double[] { 1, 0, 0 },
            };
            mirror.SourceFeatureIndices.Add(0);
            doc.Features.Add(mirror);
            return doc;
        }

        /// <summary>An offset box patterned 4× around the Z axis. Volume 4 × 60 = 240 cm³.</summary>
        public static InventorDocument CircularPatternPart()
        {
            InventorDocument doc = MakeBox("circular-pattern", 10);
            var pattern = new InventorCircularPattern
            {
                Name = "Pattern1",
                Count = 4,
                AngleRadians = 0, // full revolution
                AxisPoint = new double[] { 0, 0, 0 },
                AxisDir = new double[] { 0, 0, 1 },
            };
            pattern.SourceFeatureIndices.Add(0);
            doc.Features.Add(pattern);
            return doc;
        }

        // A fully-constrained 4×3 cm rectangle offset dx in X, extruded 5 cm into a 60 cm³ box.
        private static InventorDocument MakeBox(string name, double dx)
        {
            var doc = new InventorDocument { DisplayName = name, Kind = InventorDocumentKind.Part };
            doc.Parameters.Add(new InventorParameter { Name = "width", Expression = "40 mm", Unit = "mm" });
            doc.Parameters.Add(new InventorParameter { Name = "height", Expression = "30 mm", Unit = "mm" });

            var sketch = new InventorSketch { Name = "Rectangle" };
            const long l0 = 1, l1 = 2, l2 = 3, l3 = 4;
            sketch.Curves.Add(Line(l0, dx, 0, dx + 4, 0));
            sketch.Curves.Add(Line(l1, dx + 4, 0, dx + 4, 3));
            sketch.Curves.Add(Line(l2, dx + 4, 3, dx, 3));
            sketch.Curves.Add(Line(l3, dx, 3, dx, 0));
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

            doc.Features.Add(new InventorExtrude
            {
                Name = "Extrude1",
                SketchIndex = 0,
                ProfileIndex = 0,
                Operation = InventorOperation.NewBody,
                Direction = InventorExtentDirection.Positive,
                Distance = 5,
            });
            return doc;
        }

        /// <summary>
        /// An assembly of two instances of the same box component (deduped to one exported .opd),
        /// the second offset 10 cm in X. Exercises the multi-file export and occurrence transforms.
        /// </summary>
        public static InventorDocument AssemblyDoc()
        {
            InventorDocument box = BoxPart(); // shared by both occurrences (same object → one file)
            var asm = new InventorDocument { DisplayName = "assembly", Kind = InventorDocumentKind.Assembly };
            asm.Occurrences.Add(new InventorOccurrence { Name = "Box:1", Component = box });
            asm.Occurrences.Add(new InventorOccurrence
            {
                Name = "Box:2",
                Component = box,
                Position = new double[] { 10, 0, 0 },
            });
            return asm;
        }

        /// <summary>A part carrying a single datum: a work plane offset 5 cm above XY.</summary>
        public static InventorDocument DatumPlanePart()
        {
            var doc = new InventorDocument { DisplayName = "datum-plane", Kind = InventorDocumentKind.Part };
            doc.WorkPlanes.Add(new InventorWorkPlane
            {
                Name = "Offset Plane",
                Origin = new double[] { 0, 0, 5 },
                XAxis = new double[] { 1, 0, 0 },
                YAxis = new double[] { 0, 1, 0 },
            });
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
