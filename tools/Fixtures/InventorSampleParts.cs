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

        /// <summary>
        /// The box with a 0.5 cm fillet rounding one vertical edge (at x=4, y=3, z 0→5). The edge
        /// descriptor (midpoint + direction) binds to the recomputed body at recompute (ADR-0040).
        /// Volume ≈ 60 − 0.5²(1−π/4)·5 ≈ 59.73 cm³.
        /// </summary>
        public static InventorDocument FilletedBoxPart()
        {
            InventorDocument doc = BoxPart();
            doc.DisplayName = "filleted-box";
            var fillet = new InventorFillet { Name = "Fillet1", RadiusCm = 0.5 };
            fillet.Edges.Add(VerticalEdge(4, 3));
            doc.Features.Add(fillet);
            return doc;
        }

        /// <summary>The box with a 0.5 cm chamfer on the same vertical edge. Volume ≈ 60 − 0.5²/2·5.</summary>
        public static InventorDocument ChamferedBoxPart()
        {
            InventorDocument doc = BoxPart();
            doc.DisplayName = "chamfered-box";
            var chamfer = new InventorChamfer { Name = "Chamfer1", DistanceCm = 0.5 };
            chamfer.Edges.Add(VerticalEdge(4, 3));
            doc.Features.Add(chamfer);
            return doc;
        }

        /// <summary>
        /// The box shelled to 0.5 cm walls with the top face (z=5) removed (open box). Cavity
        /// 3×2×4.5 = 27 → volume 60 − 27 = 33 cm³.
        /// </summary>
        public static InventorDocument ShelledBoxPart()
        {
            InventorDocument doc = BoxPart();
            doc.DisplayName = "shelled-box";
            var shell = new InventorShell { Name = "Shell1", ThicknessCm = 0.5 };
            shell.RemovedFaces.Add(TopFace());
            doc.Features.Add(shell);
            return doc;
        }

        /// <summary>
        /// The box with a Ø1 cm blind hole, 2 cm deep, drilled into the top face. Volume
        /// 60 − π·0.5²·2 ≈ 58.43 cm³.
        /// </summary>
        public static InventorDocument HoledBoxPart()
        {
            InventorDocument doc = BoxPart();
            doc.DisplayName = "holed-box";
            doc.Features.Add(new InventorHole
            {
                Name = "Hole1",
                PlacementFace = TopFace(),
                DiameterCm = 1,
                DepthCm = 2,
                ThroughAll = false,
            });
            return doc;
        }

        // The box's vertical edge at corner (x, y), running z 0→5: midpoint + Z direction.
        private static InventorEdgeDescriptor VerticalEdge(double x, double y) => new InventorEdgeDescriptor
        {
            Midpoint = new double[] { x, y, 2.5 },
            Direction = new double[] { 0, 0, 1 },
        };

        // The box's top face (z=5): centroid + outward +Z normal.
        private static InventorFaceDescriptor TopFace() => new InventorFaceDescriptor
        {
            Centroid = new double[] { 2, 1.5, 5 },
            Normal = new double[] { 0, 0, 1 },
        };

        /// <summary>
        /// A 1 cm-radius circle profile swept 10 cm straight along +Z, making a cylinder.
        /// Volume = π·1²·10 ≈ 31.42 cm³ (faceted in the reader).
        /// </summary>
        public static InventorDocument SweepPart()
        {
            var doc = new InventorDocument { DisplayName = "sweep", Kind = InventorDocumentKind.Part };
            doc.Sketches.Add(CircleSketch("Profile", 1, 0));
            var sweep = new InventorSweep
            {
                Name = "Sweep1",
                ProfileSketchIndex = 0,
                ProfileIndex = 0,
                Operation = InventorOperation.NewBody,
            };
            sweep.Path.Add(new double[] { 0, 0, 0 });
            sweep.Path.Add(new double[] { 0, 0, 10 });
            doc.Features.Add(sweep);
            return doc;
        }

        /// <summary>
        /// A loft between two coaxial 1 cm-radius circles 10 cm apart, making a cylinder.
        /// Volume ≈ 31.42 cm³ (faceted).
        /// </summary>
        public static InventorDocument LoftPart()
        {
            var doc = new InventorDocument { DisplayName = "loft", Kind = InventorDocumentKind.Part };
            doc.Sketches.Add(CircleSketch("Bottom", 1, 0));
            doc.Sketches.Add(CircleSketch("Top", 1, 10));
            var loft = new InventorLoft { Name = "Loft1", Operation = InventorOperation.NewBody };
            loft.Sections.Add(new InventorLoftSection { SketchIndex = 0, ProfileIndex = 0 });
            loft.Sections.Add(new InventorLoftSection { SketchIndex = 1, ProfileIndex = 0 });
            doc.Features.Add(loft);
            return doc;
        }

        /// <summary>
        /// A half-disc (a diameter line closed by a semicircular arc, r=2 cm) extruded 5 cm into a
        /// half-cylinder. Volume = (π·2²/2)·5 = 10π ≈ 31.42 cm³ — exercises an arc in a profile.
        /// </summary>
        public static InventorDocument ArcExtrudePart()
        {
            var doc = new InventorDocument { DisplayName = "arc-extrude", Kind = InventorDocumentKind.Part };

            var sketch = new InventorSketch { Name = "HalfDisc" };
            const long l0 = 1, a1 = 2;
            sketch.Curves.Add(Line(l0, -2, 0, 2, 0)); // the diameter
            sketch.Curves.Add(new InventorCurve
            {
                Id = a1,
                Kind = InventorCurveKind.Arc,
                Center = new double[] { 0, 0 },
                Start = new double[] { 2, 0 },
                End = new double[] { -2, 0 },
                Ccw = true, // upper semicircle
            });
            // The arc's center/start/end fully fix it: pin both diameter ends + the centre, and
            // coincide the arc ends to the diameter ends. (Oblikovati's radius dim is circle-only.)
            Coincide(sketch, l0, InventorCurvePointRole.End, a1, InventorCurvePointRole.Start);
            Coincide(sketch, a1, InventorCurvePointRole.End, l0, InventorCurvePointRole.Start);
            sketch.Constraints.Add(Fix(l0, InventorCurvePointRole.Start));
            sketch.Constraints.Add(Fix(l0, InventorCurvePointRole.End));
            sketch.Constraints.Add(Fix(a1, InventorCurvePointRole.Center));

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
        /// A closed fit-spline through four fixed points (a rounded blob). All fit points are
        /// fixed, so it is DOF 0, and the closed spline is one closed profile.
        /// </summary>
        public static InventorDocument SplineSketchPart()
        {
            var doc = new InventorDocument { DisplayName = "spline", Kind = InventorDocumentKind.Part };
            var sketch = new InventorSketch { Name = "Blob" };
            const long s0 = 1;
            var spline = new InventorCurve { Id = s0, Kind = InventorCurveKind.Spline, Closed = true, Fit = true };
            spline.SplinePoints.Add(new double[] { 0, 0 });
            spline.SplinePoints.Add(new double[] { 4, 0 });
            spline.SplinePoints.Add(new double[] { 4, 3 });
            spline.SplinePoints.Add(new double[] { 0, 3 });
            sketch.Curves.Add(spline);
            for (int i = 0; i < spline.SplinePoints.Count; i++)
            {
                var fix = new InventorSketchConstraint { Kind = InventorConstraintKind.Fix };
                fix.Points.Add(new InventorPointRef(s0, InventorCurvePointRole.SplinePoint, i));
                sketch.Constraints.Add(fix);
            }

            doc.Sketches.Add(sketch);
            return doc;
        }

        /// <summary>
        /// A closed control-point spline (fit = false) over four fixed control points. DOF 0, one
        /// closed profile — the control-polygon counterpart of <see cref="SplineSketchPart"/>.
        /// </summary>
        public static InventorDocument ControlPointSplinePart()
        {
            var doc = new InventorDocument { DisplayName = "control-spline", Kind = InventorDocumentKind.Part };
            var sketch = new InventorSketch { Name = "CtrlBlob" };
            const long s0 = 1;
            var spline = new InventorCurve { Id = s0, Kind = InventorCurveKind.Spline, Closed = true, Fit = false };
            spline.SplinePoints.Add(new double[] { 0, 0 });
            spline.SplinePoints.Add(new double[] { 4, 0 });
            spline.SplinePoints.Add(new double[] { 4, 3 });
            spline.SplinePoints.Add(new double[] { 0, 3 });
            sketch.Curves.Add(spline);
            for (int i = 0; i < spline.SplinePoints.Count; i++)
            {
                var fix = new InventorSketchConstraint { Kind = InventorConstraintKind.Fix };
                fix.Points.Add(new InventorPointRef(s0, InventorCurvePointRole.SplinePoint, i));
                sketch.Constraints.Add(fix);
            }

            doc.Sketches.Add(sketch);
            return doc;
        }

        /// <summary>
        /// A sampler that exercises every remaining geometric constraint and the angle dimension,
        /// with geometry that satisfies them: two collinear equal-length lines, two concentric
        /// equal-radius circles, a line tangent to a circle, and a 45° angle dimension. Validated
        /// by the open check (it is not a single closed profile).
        /// </summary>
        public static InventorDocument ConstraintSamplerPart()
        {
            var doc = new InventorDocument { DisplayName = "constraint-sampler", Kind = InventorDocumentKind.Part };
            doc.Parameters.Add(new InventorParameter { Name = "ang", Expression = "45 deg", Unit = "deg" });
            var sketch = new InventorSketch { Name = "Sampler" };
            const long l0 = 1, l1 = 2, c0 = 3, c1 = 4, lt = 5, ct = 6, la = 7, lsa = 8, lsb = 9, lx = 10;
            sketch.Curves.Add(Line(l0, 0, 0, 2, 0));
            sketch.Curves.Add(Line(l1, 3, 0, 5, 0));         // collinear + equal length to l0
            sketch.Curves.Add(Circle(c0, 0, 5, 1));
            sketch.Curves.Add(Circle(c1, 0, 5, 1));          // concentric + equal radius to c0
            sketch.Curves.Add(Line(lt, 10, 1, 14, 1));
            sketch.Curves.Add(Circle(ct, 12, 3, 2));         // tangent: line y=1 is 2 below centre
            sketch.Curves.Add(Line(la, 0, 0, 2, 2));         // 45° to l0
            sketch.Curves.Add(Line(lsa, -3, 4, -3, 6));      // symmetric pair about lx
            sketch.Curves.Add(Line(lsb, 3, 4, 3, 6));
            sketch.Curves.Add(Line(lx, 0, 3, 0, 7));         // vertical symmetry axis at x=0

            sketch.Constraints.Add(Between(InventorConstraintKind.Collinear, l0, l1));
            sketch.Constraints.Add(Between(InventorConstraintKind.EqualLength, l0, l1));
            sketch.Constraints.Add(Between(InventorConstraintKind.Concentric, c0, c1));
            sketch.Constraints.Add(Between(InventorConstraintKind.EqualRadius, c0, c1));
            sketch.Constraints.Add(Between(InventorConstraintKind.Tangent, lt, ct));

            // Ground the tangent line (pins both endpoints).
            var ground = new InventorSketchConstraint { Kind = InventorConstraintKind.Ground };
            ground.Points.Add(new InventorPointRef(lt, InventorCurvePointRole.Start));
            ground.Points.Add(new InventorPointRef(lt, InventorCurvePointRole.End));
            sketch.Constraints.Add(ground);

            // Symmetry: the two pair lines' start points are symmetric about lx.
            var symmetry = new InventorSketchConstraint { Kind = InventorConstraintKind.Symmetry };
            symmetry.Points.Add(new InventorPointRef(lsa, InventorCurvePointRole.Start));
            symmetry.Points.Add(new InventorPointRef(lsb, InventorCurvePointRole.Start));
            symmetry.Curves.Add(lx);
            sketch.Constraints.Add(symmetry);

            var angle = new InventorSketchDimension { Kind = InventorDimensionKind.Angle, Expression = "ang" };
            angle.Curves.Add(l0);
            angle.Curves.Add(la);
            sketch.Dimensions.Add(angle);

            doc.Sketches.Add(sketch);
            return doc;
        }

        private static InventorCurve Circle(long id, double cx, double cy, double radius) => new InventorCurve
        {
            Id = id,
            Kind = InventorCurveKind.Circle,
            Center = new[] { cx, cy },
            Radius = radius,
        };

        private static InventorSketchConstraint Between(InventorConstraintKind kind, long a, long b)
        {
            var c = new InventorSketchConstraint { Kind = kind };
            c.Curves.Add(a);
            c.Curves.Add(b);
            return c;
        }

        /// <summary>A full ellipse (major axis along X, radii 4×2 cm) with a fixed centre.</summary>
        public static InventorDocument EllipsePart()
        {
            var doc = new InventorDocument { DisplayName = "ellipse", Kind = InventorDocumentKind.Part };
            var sketch = new InventorSketch { Name = "Ellipse" };
            const long e0 = 1;
            sketch.Curves.Add(new InventorCurve
            {
                Id = e0,
                Kind = InventorCurveKind.Ellipse,
                Center = new double[] { 0, 0 },
                MajorAxis = new double[] { 1, 0 },
                MajorRadius = 4,
                MinorRadius = 2,
            });
            sketch.Constraints.Add(Fix(e0, InventorCurvePointRole.Center));
            doc.Sketches.Add(sketch);
            return doc;
        }

        /// <summary>A half elliptical arc (radii 4×2 cm, 0→π about the major X axis), centre fixed.</summary>
        public static InventorDocument EllipticalArcPart()
        {
            var doc = new InventorDocument { DisplayName = "elliptical-arc", Kind = InventorDocumentKind.Part };
            var sketch = new InventorSketch { Name = "EllArc" };
            const long e0 = 1;
            sketch.Curves.Add(new InventorCurve
            {
                Id = e0,
                Kind = InventorCurveKind.EllipticalArc,
                Center = new double[] { 0, 0 },
                MajorAxis = new double[] { 1, 0 },
                MajorRadius = 4,
                MinorRadius = 2,
                StartAngle = 0,
                EndAngle = System.Math.PI,
            });
            sketch.Constraints.Add(Fix(e0, InventorCurvePointRole.Center));
            doc.Sketches.Add(sketch);
            return doc;
        }

        // A fixed circle of the given radius (cm) on a plane offset originZ along Z, with a
        // diameter dimension. The 2D center is the origin, so it sits at (0,0,originZ) in model space.
        private static InventorSketch CircleSketch(string name, double radius, double originZ)
        {
            var sketch = new InventorSketch { Name = name, Origin = new double[] { 0, 0, originZ } };
            const long c0 = 1;
            sketch.Curves.Add(new InventorCurve
            {
                Id = c0,
                Kind = InventorCurveKind.Circle,
                Center = new double[] { 0, 0 },
                Radius = radius,
            });
            sketch.Constraints.Add(Fix(c0, InventorCurvePointRole.Center));
            var dim = new InventorSketchDimension
            {
                Kind = InventorDimensionKind.Diameter,
                Expression = (radius * 20) + " mm",
            };
            dim.Curves.Add(c0);
            sketch.Dimensions.Add(dim);
            return sketch;
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
