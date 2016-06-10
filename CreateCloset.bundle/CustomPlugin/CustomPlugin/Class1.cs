using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime; 
using Autodesk.AutoCAD.EditorInput; 
using Autodesk.AutoCAD.DatabaseServices; 
using Autodesk.AutoCAD.ApplicationServices; 
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;

namespace CustomPlugin
{
    public class MyCommands
    {
        #region Closet
        public enum WoodGrainAlignment
        {
            Horizontal = 0,
            Vertical = 1
        }

        public class PanelDetails
        {
            public String _desc;
            public String _dimension;
            public int _quantity;

            public PanelDetails(String desc, String dimension, int quantity)
            {
                _desc = desc;
                _dimension = dimension;
                _quantity = quantity;
            }
        }

        static double W = 6.0 * 12; // Total Width in inches
        static double D = 3.0 * 12; // Total Depth in inches
        static double H = 8.0 * 12; // Total Height in inches
        static double t = 2.0; // Ply thick in inches
        static double doorH = H * 0.4;
        static int N = 3; // Number of drawer rows
        static bool splitDrawers = true;
        static List<PanelDetails> _panels = new List<PanelDetails>();

        static String blockName = String.Empty;

        static int plyIndex = 1;

        // Prompts user to input dimensions of the closet
        // Creates a closet using those dimensions 
        [CommandMethod("CreateCloset")]
        public static void CreateCloset()
        {
            _panels.Clear();

            Document activeDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;

            Database db = activeDoc.Database;

            Editor ed = activeDoc.Editor;

            PromptDoubleOptions pdo = new PromptDoubleOptions("Total closet width in feet : ");
            PromptDoubleResult pdr = ed.GetDouble(pdo);
            if (pdr.Status != PromptStatus.OK)
                return;
            W = pdr.Value * 12.0;

            pdo = new PromptDoubleOptions("Total closet depth in feet : ");
            pdr = ed.GetDouble(pdo);
            if (pdr.Status != PromptStatus.OK)
                return;
            D = pdr.Value * 12.0;

            pdo = new PromptDoubleOptions("Total closet height in feet : ");
            pdr = ed.GetDouble(pdo);
            if (pdr.Status != PromptStatus.OK)
                return;
            H = pdr.Value * 12.0;

            pdo = new PromptDoubleOptions("Ply thickness in inches : ");
            pdr = ed.GetDouble(pdo);
            if (pdr.Status != PromptStatus.OK)
                return;
            t = pdr.Value;

            pdo = new PromptDoubleOptions("Door height as percentage of total closet height : ");
            pdr = ed.GetDouble(pdo);
            if (pdr.Status != PromptStatus.OK)
                return;
            doorH = H * pdr.Value * 0.01;

            PromptIntegerOptions pio = new PromptIntegerOptions("Number of drawer rows : ");
            PromptIntegerResult pir = ed.GetInteger(pio);
            if (pir.Status != PromptStatus.OK)
                return;
            N = pir.Value;

            pio = new PromptIntegerOptions("Split drawers ? (1 / 0) : ");
            pir = ed.GetInteger(pio);
            if (pir.Status != PromptStatus.OK)
                return;
            splitDrawers = (pir.Value != 0);

            ed.WriteMessage(String.Format("\nTotal closet width  : {0}", W));
            ed.WriteMessage(String.Format("\nTotal closet depth  : {0}", D));
            ed.WriteMessage(String.Format("\nTotal closet height : {0}", H));
            ed.WriteMessage(String.Format("\nPly thickness       : {0}", t));
            ed.WriteMessage(String.Format("\nDoor height : {0}", doorH));
            ed.WriteMessage(String.Format("\nNumber of drawer rows :  {0}", N));
            ed.WriteMessage(String.Format("\nSplit drawers ?  : {0}", splitDrawers ? "yes" : "no"));
           
            CreateHandleBlock();

            // Left
            PlyYZ(new Point2d[] { new Point2d(0.0, 0.0), 
                                            new Point2d(D-t, 0.0),
                                            new Point2d(D-t, H-2*t),
                                            new Point2d(0.0, H-2*t)
                                            }, new Point3d(0.0, t, t), WoodGrainAlignment.Vertical);
            _panels.Add(new PanelDetails("Left panel", String.Format("{0} x {1}", (D-t), H-2*t), 1));

            // Right
            PlyYZ(new Point2d[] { new Point2d(0.0, 0.0), 
                                            new Point2d(D-t, 0.0),
                                            new Point2d(D-t, H-2*t),
                                            new Point2d(0.0, H-2*t)
                                            }, new Point3d(W - t, t, t), WoodGrainAlignment.Vertical);
            _panels.Add(new PanelDetails("Right panel", String.Format("{0} x {1}", (D - t), H - 2 * t), 1));

            // Top
            PlyXY(new Point2d[] { new Point2d(0.0, 0.0), 
                                            new Point2d(W, 0.0),
                                            new Point2d(W, D-t),
                                            new Point2d(0.0, D-t)
                                            }, new Point3d(0.0, t, H - t), WoodGrainAlignment.Horizontal);
            _panels.Add(new PanelDetails("Top panel", String.Format("{0} x {1}", W, (D - t)), 1));

            // Bottom
            PlyXY(new Point2d[] { new Point2d(0.0, 0.0), 
                                            new Point2d(W, 0.0),
                                            new Point2d(W, D-t),
                                            new Point2d(0.0, D-t)
                                            }, new Point3d(0.0, t, 0.0), WoodGrainAlignment.Horizontal);
            _panels.Add(new PanelDetails("Bottom panel", String.Format("{0} x {1}", W, (D - t)), 1));

            // Back
            PlyXZ(new Point2d[] { new Point2d(0.0, 0.0), 
                                            new Point2d(-W+2*t, 0.0),
                                            new Point2d(-W+2*t, H-2*t),
                                            new Point2d(0.0, H-2*t)
                                            }, new Point3d(t, D - t, t), WoodGrainAlignment.Vertical);
            _panels.Add(new PanelDetails("Back panel", String.Format("{0} x {1}", W+2*t, H-2*t), 1));

            // Front twin doors
            // Left
            ObjectId leftDoorId = PlyXZ(new Point2d[] { new Point2d(0.0, 0.0), 
                                            new Point2d(-W * 0.5, 0.0),
                                            new Point2d(-W * 0.5, doorH),
                                            new Point2d(0.0, doorH)
                                            }, new Point3d(0.0, 0.0, H - doorH), WoodGrainAlignment.Vertical);
            _panels.Add(new PanelDetails("Front twin doors", String.Format("{0} x {1}", W * 0.5, doorH), 2));

            // Swing the door open
            // comment it to shut the drawer
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity ent = tr.GetObject(leftDoorId, OpenMode.ForWrite) as Entity;
                ent.TransformBy(Matrix3d.Rotation(-135 * Math.PI / 180.0, Vector3d.ZAxis, new Point3d(0.0, 0.0, H - doorH)));
                tr.Commit();
            }

            // Right
            ObjectId rightDoorId = PlyXZ(new Point2d[] { new Point2d(0.0, 0.0), 
                                            new Point2d(-W * 0.5, 0.0),
                                            new Point2d(-W * 0.5, doorH),
                                            new Point2d(0.0, doorH)
                                            }, new Point3d(W * 0.5, 0.0, H - doorH), WoodGrainAlignment.Vertical);

            // Swing the door open
            // comment it to shut the drawer
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity ent = tr.GetObject(rightDoorId, OpenMode.ForWrite) as Entity;
                ent.TransformBy(Matrix3d.Rotation(135 * Math.PI / 180.0, Vector3d.ZAxis, new Point3d(W, 0.0, H - doorH)));
                tr.Commit();
            }

            // Inner shelf divider
            PlyXY(new Point2d[] { new Point2d(0.0, 0.0), 
                                            new Point2d(W-2*t, 0.0),
                                            new Point2d(W-2*t, D-2*t),
                                            new Point2d(0.0, D-2*t)
                                            }, new Point3d(t, t, H - doorH), WoodGrainAlignment.Horizontal);
            _panels.Add(new PanelDetails("Inner shelf divider", String.Format("{0} x {1}", W - 2 * t, D - 2 * t), 1));
            CreateDrawerBlock(splitDrawers);

            InsertDrawers();
        }

        public static ObjectId PlyYZ(Point2d[] pts, Point3d destPt, WoodGrainAlignment grain)
        {
            return CreateExtrusion(pts, destPt, Vector3d.XAxis, grain);
        }

        public static ObjectId PlyXY(Point2d[] pts, Point3d destPt, WoodGrainAlignment grain)
        {
            return CreateExtrusion(pts, destPt, Vector3d.ZAxis, grain);
        }

        public static ObjectId PlyXZ(Point2d[] pts, Point3d destPt, WoodGrainAlignment grain)
        {
            return CreateExtrusion(pts, destPt, Vector3d.YAxis, grain);
        }

        public static ObjectId CreateExtrusion(Point2d[] pts, Point3d destPt, Vector3d normal, WoodGrainAlignment grain)
        {
            ObjectId entId = ObjectId.Null;

            Document activeDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database db = activeDoc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                BlockTableRecord btr = null;
                if (String.IsNullOrEmpty(blockName))
                    btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                else
                {
                    if (bt.Has(blockName))
                        btr = tr.GetObject(bt[blockName], OpenMode.ForWrite) as BlockTableRecord;
                    else
                    {
                        btr = new BlockTableRecord();
                        btr.Name = blockName;
                        bt.UpgradeOpen();
                        bt.Add(btr);
                        tr.AddNewlyCreatedDBObject(btr, true);
                    }
                }

                Solid3d extrudedSolid = new Solid3d();
                using (Autodesk.AutoCAD.DatabaseServices.Polyline outline = new Autodesk.AutoCAD.DatabaseServices.Polyline())
                {
                    outline.SetDatabaseDefaults();
                    outline.Normal = normal;

                    int cnt = 0;
                    foreach (Point2d pt in pts)
                    {
                        outline.AddVertexAt(cnt, pt, 0, 0, 0);
                        cnt++;
                    }
                    outline.Closed = true;

                    Extents3d exts = outline.GeometricExtents;
                    Point3d minPt = exts.MinPoint;
                    Point3d maxPt = exts.MaxPoint;

                    double p1 = maxPt.X - minPt.X;
                    double p2 = maxPt.Y - minPt.Y;
                    double p3 = maxPt.Z - minPt.Z;

                    double pmin = 0.0;
                    if (p1 == 0)
                    {
                        pmin = Math.Min(p2, p3);
                    }
                    if (p2 == 0)
                    {
                        pmin = Math.Min(p1, p3);
                    }
                    if (p3 == 0)
                    {
                        pmin = Math.Min(p1, p2);
                    }
                    double pmax = Math.Max(Math.Max(p1, p2), p3);

                    extrudedSolid.RecordHistory = true;

                    plyIndex++;

                    Vector3d heightVector = outline.Normal * t;

                    SweepOptions sweepOptions = new SweepOptions();

                    SweepOptionsBuilder builder = new SweepOptionsBuilder(sweepOptions);

                    extrudedSolid.CreateExtrudedSolid(outline, heightVector, sweepOptions);
                }

                entId = btr.AppendEntity(extrudedSolid);
                tr.AddNewlyCreatedDBObject(extrudedSolid, true);

                extrudedSolid.TransformBy(Matrix3d.Displacement(destPt.GetAsVector()));

                tr.Commit();
            }

            return entId;
        }

        public static void CreateDrawerBlock(bool isSplit)
        {
            double drawerW = W;
            double drawerH = (H - doorH) / N;
            // Front Panel
            if (isSplit)
            {
                drawerW = W * 0.5;

                // Inner Drawer shelf divider
                PlyYZ(new Point2d[] { new Point2d(0.0, 0.0), 
                                            new Point2d(D-2*t, 0.0),
                                            new Point2d(D-2*t, (H - doorH)-t),
                                            new Point2d(0.0, (H - doorH)-t)
                                            }, new Point3d(W * 0.5 - t * 0.5, t, t), WoodGrainAlignment.Vertical);
                _panels.Add(new PanelDetails("Drawer shelf divider", String.Format("{0} x {1}", D - 2 * t, (H - doorH)-t), 1));
            }

            // Add panels that are created to the Drawer block for reuse
            blockName = "Drawer";

            // Front
            PlyXZ(new Point2d[] {   new Point2d(0.0, 0.0), 
                                    new Point2d(-drawerW, 0.0),
                                    new Point2d(-drawerW, drawerH),
                                    new Point2d(0.0, drawerH)
                                }, new Point3d(0.0, 0.0, 0.0), WoodGrainAlignment.Horizontal);
            _panels.Add(new PanelDetails("Drawer front panel", String.Format("{0} x {1}", drawerW, drawerH), splitDrawers ? N * 2 : N));

            // Left 
            PlyYZ(new Point2d[] {   new Point2d(0.0, 0.0), 
                                    new Point2d(D-2*t, 0.0),
                                    new Point2d(D-2*t, drawerH-3*t),
                                    new Point2d(0.0, drawerH-3*t)
                                    }, new Point3d(t, t, 2 * t), WoodGrainAlignment.Horizontal);
            _panels.Add(new PanelDetails("Drawer left panel", String.Format("{0} x {1}", D - 2 * t, drawerH - 3 * t), splitDrawers ? N * 2 : N));

            // Right
            PlyYZ(new Point2d[] { new Point2d(0.0, 0.0), 
                                            new Point2d(D-2*t, 0.0),
                                            new Point2d(D-2*t, drawerH-3*t),
                                            new Point2d(0.0, drawerH-3*t)
                                            }, new Point3d(drawerW - 2 * t, t, 2 * t), WoodGrainAlignment.Horizontal);
            _panels.Add(new PanelDetails("Drawer right panel", String.Format("{0} x {1}", D - 2 * t, drawerH - 3 * t), splitDrawers ? N * 2 : N));

            // Bottom
            PlyXY(new Point2d[] { new Point2d(0.0, 0.0), 
                                            new Point2d(drawerW-2*t, 0.0),
                                            new Point2d(drawerW-2*t, D-2*t),
                                            new Point2d(0.0, D-2*t)
                                            }, new Point3d(t, t, t), WoodGrainAlignment.Horizontal);
            _panels.Add(new PanelDetails("Drawer bottom panel", String.Format("{0} x {1}", drawerW - 2 * t, D - 2 * t), splitDrawers ? N * 2 : N));

            // Back
            PlyXZ(new Point2d[] { new Point2d(0.0, 0.0), 
                                            new Point2d(-drawerW+4*t, 0.0),
                                            new Point2d(-drawerW+4*t, drawerH-3*t),
                                            new Point2d(0.0, drawerH-3*t)
                                            }, new Point3d(2 * t, D - 2 * t, 2 * t), WoodGrainAlignment.Horizontal);
            _panels.Add(new PanelDetails("Drawer back panel", String.Format("{0} x {1}", drawerW + 4 * t, drawerH - 3 * t), splitDrawers ? N * 2 : N));

            _panels.Add(new PanelDetails("Drawer handles", "-", splitDrawers ? N * 2 : N));

            blockName = String.Empty;
        }

        public static void InsertDrawers()
        {
            Document activeDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database db = activeDoc.Database;

            //double drawerW = W;
            double drawerH = (H - doorH) / N;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                for (int cnt = 0; cnt < N; cnt++)
                {
                    // 2*t-(D/N) * (N-cnt) for placing the drawers in open position.
                    // 0.0 for shut drawers.

                    Point3d pos1 = new Point3d(0.0, 2 * t - (D / N) * (N - cnt), drawerH * cnt);
                    BlockReference bref1 = new BlockReference(pos1, bt["Drawer"]);
                    DBObjectCollection explodeSet1 = new DBObjectCollection();
                    bref1.Explode(explodeSet1);
                    foreach (DBObject dbObj in explodeSet1)
                    {
                        btr.AppendEntity(dbObj as Entity);
                        tr.AddNewlyCreatedDBObject(dbObj, true);
                    }

                    // Handle
                    if (splitDrawers)
                    {
                        BlockReference bref11 = new BlockReference(new Point3d(0.25 * W, 2 * t - (D / N) * (N - cnt), drawerH * cnt + drawerH * 0.5), bt["Handle"]);
                        btr.AppendEntity(bref11 as Entity);
                        tr.AddNewlyCreatedDBObject(bref11, true);
                    }
                    else
                    {
                        BlockReference bref11 = new BlockReference(new Point3d(0.5 * W, 2 * t - (D / N) * (N - cnt), drawerH * cnt + drawerH * 0.5), bt["Handle"]);
                        btr.AppendEntity(bref11 as Entity);
                        tr.AddNewlyCreatedDBObject(bref11, true);
                    }

                    if (splitDrawers)
                    {
                        // 2*t-(D/N) * (N-cnt) for placing the drawers in open position.
                        // 0.0 for shut drawers.

                        Point3d pos2 = new Point3d(W * 0.5, 2 * t - (D / N) * (N - cnt), drawerH * cnt);
                        BlockReference bref2 = new BlockReference(pos2, bt["Drawer"]);
                        DBObjectCollection explodeSet2 = new DBObjectCollection();
                        bref2.Explode(explodeSet2);
                        foreach (DBObject dbObj in explodeSet2)
                        {
                            btr.AppendEntity(dbObj as Entity);
                            tr.AddNewlyCreatedDBObject(dbObj, true);
                        }

                        // Handle
                        BlockReference bref22 = new BlockReference(new Point3d(W * 0.75, 2 * t - (D / N) * (N - cnt), drawerH * cnt + drawerH * 0.5), bt["Handle"]);
                        btr.AppendEntity(bref22 as Entity);
                        tr.AddNewlyCreatedDBObject(bref22, true);
                    }
                }
                tr.Commit();
            }
        }

        public static void CreateHandleBlock()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = null;

                if (bt.Has("Handle"))
                    btr = tr.GetObject(bt["Handle"], OpenMode.ForWrite) as BlockTableRecord;
                else
                {
                    btr = new BlockTableRecord();
                    btr.Name = "Handle";
                    bt.UpgradeOpen();
                    bt.Add(btr);
                    tr.AddNewlyCreatedDBObject(btr, true);
                }

                using (Autodesk.AutoCAD.DatabaseServices.Polyline outline = new Autodesk.AutoCAD.DatabaseServices.Polyline())
                {
                    outline.SetDatabaseDefaults();
                    outline.Normal = Vector3d.XAxis;

                    outline.AddVertexAt(0, new Point2d(0.0, 0.0), 0, 0, 0);
                    outline.AddVertexAt(1, new Point2d(0.0, 0.5 * t), 0, 0, 0);
                    outline.AddVertexAt(2, new Point2d(-t, 0.5 * t), 0, 0, 0);
                    outline.AddVertexAt(3, new Point2d(-t, 0.75 * t), 0, 0, 0);
                    outline.AddVertexAt(4, new Point2d(-1.25 * t, t), 0, 0, 0);
                    outline.AddVertexAt(5, new Point2d(-1.75 * t, t), 0, 0, 0);
                    outline.AddVertexAt(6, new Point2d(-2.0 * t, 0.75 * t), 0, 0, 0);
                    outline.AddVertexAt(7, new Point2d(-2.0 * t, 0.0), 0, 0, 0);
                    outline.Closed = true;

                    RevolveOptions opts = new RevolveOptions();
                    RevolveOptionsBuilder rob = new RevolveOptionsBuilder(opts);
                    rob.CloseToAxis = true;
                    rob.DraftAngle = 0;
                    rob.TwistAngle = 0;

                    Solid3d handle = new Solid3d(); ;
                    handle.CreateRevolvedSolid(outline, new Point3d(0, 0, 0), Vector3d.YAxis, 2.0 * Math.PI, 0, rob.ToRevolveOptions());
                    handle.ColorIndex = 0;

                    btr.AppendEntity(handle);
                    tr.AddNewlyCreatedDBObject(handle, true);

                    tr.Commit();
                }
            }
        }
        #endregion
    }
}