using HelixToolkit.Wpf;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System.Security.Policy;
using System.Numerics;
using System.Diagnostics;
using System.Xml.Linq;

/**
 * Author: Hamidreza Hoshyarmanesh (Feb 2023)
 * This code reads/loads the 3d models of all the parts of the n42 robot and opens them in the robot_viewport.
 * The relationships among the joints and links of the robot are determined and the movement of the robot is demonstrated in 3D space
**/

namespace n42_Robot_PROTO_III
{

    class Joint
    {
        public Model3D model3d = null;
        public float angle = 0;
        public float angleMin = -180;
        public float angleMax = +180;
        public float rotPointX = 0;
        public float rotPointY = 0;
        public float rotPointZ = 0;
        public float rotAxisX = 0;
        public float rotAxisY = 0;
        public float rotAxisZ = 0;

        public float transAxisX = 0;
        public float transAxisY = 0;
        public float transAxisZ = 0;
        public float transMin = -100;
        public float transMax = +100; 

        public Joint(Model3D pModel)
        {
            model3d = pModel;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow_Visualization : Window
    {
        Model3DGroup robot = new Model3DGroup();
        //Model3D Frame;
        //Model3D RotLink;
        //Model3D NeedleHolder;
        //Model3D Nut;
        //Model3D RCM_Central_Link;
        //Model3D RCM_Parallelogram;
        //Model3D RCM_SwingArm_Back;
        //Model3D RCM_SwingArm_Front;
        //Model3D Workspace;

        GeometryModel3D currentSelectedModel = null;
        Color currentColor = Colors.White;
        public Model3D robot_Model { get; set; }
        public object TbX_Val { get; private set; }
        public object TbY_Val { get; private set; }
        public object TbZ_Val { get; private set; }

        public Model3D model3d = null;
        ModelVisual3D visual;
        ModelVisual3D visual2;
        List<Joint> joints = null;
        bool isAnimating = false;
        Model3D sphere = null;
        Model3D end_effector = null;
        string basePath = "";
        Transform3DGroup F1;
        Transform3DGroup F2;
        Transform3DGroup F3;
        Transform3DGroup F4;
        Transform3DGroup F5;
        Transform3DGroup F6;
        Transform3DGroup F7;
        Transform3DGroup F8;
        Transform3DGroup F9;
        RotateTransform3D R;
        TranslateTransform3D T;
        Vector3D reachingPoint;
        int movements = 10;
        float SamplingDistance = 0.15f;
        float DistanceThreshold = 1f;
        float LearningRate = 0.01f;


        System.Windows.Forms.Timer timer1;
        public static T Clamp<T>(T value, T min, T max)
            where T : System.IComparable<T>
        {
            T result = value;
            if (value.CompareTo(max) > 0)
                result = max;
            if (value.CompareTo(min) < 0)
                result = min;
            return result;
        }

        public MainWindow_Visualization()
        {
            InitializeComponent();
            basePath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName + "\\Robot\\";
            List<string> model_paths = new List<string>();

            model_paths.Add("ROT_Link.STL");
            model_paths.Add("Nut.STL");
            model_paths.Add("RCM_CenterLink.STL");
            model_paths.Add("RCM_SwingArm_Back.STL");
            model_paths.Add("RCM_Parallelogram_Up.STL");
            model_paths.Add("RCM_Parallelogram_Down.STL");
            model_paths.Add("RCM_SwingArm_Front.STL");
            model_paths.Add("NeedleHolder.STL");
            model_paths.Add("Needle.STL");
            model_paths.Add("Workspace.STL");
            model_paths.Add("Frame.STL");

            Initialize_Environment(model_paths);

            /** Debug sphere to check in which point the joint is rotating**/
            var builder = new MeshBuilder(true, true);
            var builder2 = new MeshBuilder(true, true);
            var position = new Point3D(0, 0, 0);
            builder.AddSphere(position, 5, 15, 15);
            builder2.AddSphere(position, 5, 15, 15);
            sphere = new GeometryModel3D(builder.ToMesh(), Materials.Brown);
            end_effector = new GeometryModel3D(builder.ToMesh(), Materials.Brown);
            visual = new ModelVisual3D();
            visual.Content = sphere;
            visual2 = new ModelVisual3D();
            visual2.Content = end_effector;

            robot_viewport.RotateGesture = new MouseGesture(MouseAction.RightClick);
            robot_viewport.PanGesture = new MouseGesture(MouseAction.LeftClick);
            robot_viewport.Children.Add(visual);
            robot_viewport.Children.Add(visual2);
            robot_viewport.Camera.LookDirection = new Vector3D(100, -100, -100);
            robot_viewport.Camera.UpDirection = new Vector3D(0, 0, 1);
            robot_viewport.Camera.Position = new Point3D(-10000, 10000, 10000);

            float[] angles = { joints[0].angle, joints[1].angle, joints[2].angle, joints[3].angle, joints[4].angle, joints[5].angle,
                                joints[6].angle, joints[7].angle, joints[8].angle };

            timer1 = new System.Windows.Forms.Timer();
            timer1.Interval = 1;
            timer1.Tick += new System.EventHandler(timer1_Tick);

        }


        private void ViewPort3D_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition(robot_viewport);
            PointHitTestParameters hitParams = new PointHitTestParameters(mousePos);
            VisualTreeHelper.HitTest(robot_viewport, null, ResultCallback, hitParams);
        }

        private void ViewPort3D_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Perform the hit test on the mouse's position relative to the viewport.
            HitTestResult result = VisualTreeHelper.HitTest(robot_viewport, e.GetPosition(robot_viewport));

            // mesh_result will be the mesh hit by the mouse
            RayMeshGeometry3DHitTestResult mesh_result = result as RayMeshGeometry3DHitTestResult;

            if (currentSelectedModel != null)
                unselectModel();

            if (mesh_result != null)
            {
                // mesh_result.ModelHit --> is the 3D model hit by the mouse
                selectModel(mesh_result.ModelHit);
            }
        }

        /// <summary>
        /// select the 3D model hit by the mouse and change its color to "#ff3333"
        /// </summary>
        private void selectModel(Model3D pModel)
        {
            try
            {
                Model3DGroup models = ((Model3DGroup)pModel);
                // currentSelectedModel --> the 3D model already hit by the mouse
                currentSelectedModel = models.Children[0] as GeometryModel3D;
            }
            catch (Exception exc)
            {
                currentSelectedModel = (GeometryModel3D)pModel;
            }
            if (currentSelectedModel != robot.Children[10])
            {
                //currentColor = changeModelColor(currentSelectedModel, ColorHelper.HexToColor("#ff3333"));
                currentColor = changeModelColor(currentSelectedModel, ColorHelper.HexToColor("#FF38BF9D"));

            }
        }

        private void unselectModel()
        {
            changeModelColor(currentSelectedModel, currentColor);
        }


        public HitTestResultBehavior ResultCallback(HitTestResult result)
        {
            // Did we hit 3D?
            RayHitTestResult rayResult = result as RayHitTestResult;
            if (rayResult != null)
            {
                // Did we hit a MeshGeometry3D?
                RayMeshGeometry3DHitTestResult rayMeshResult = rayResult as RayMeshGeometry3DHitTestResult;
                robot.Transform = new TranslateTransform3D(new Vector3D(rayResult.PointHit.X, rayResult.PointHit.Y, rayResult.PointHit.Z));

                if (rayMeshResult != null)
                {
                    // Yes we did!
                }
            }

            return HitTestResultBehavior.Continue;
        }




        private Color changeModelColor(Joint pJoint, Color newColor)
        {
            Model3DGroup models = ((Model3DGroup)pJoint.model3d);
            return changeModelColor(models.Children[0] as GeometryModel3D, newColor);
        }


        private Color changeModelColor(GeometryModel3D pModel, Color newColor)
        {
            if (pModel == null)
                return currentColor;

            Color previousColor = Colors.Black;

            MaterialGroup mg = (MaterialGroup)pModel.Material;
            if (mg.Children.Count > 0)
            {
                try
                {
                    previousColor = ((EmissiveMaterial)mg.Children[0]).Color;
                    ((EmissiveMaterial)mg.Children[0]).Color = newColor;
                    ((DiffuseMaterial)mg.Children[1]).Color = newColor;
                }
                catch (Exception exc)
                {
                    previousColor = currentColor;
                }
            }

            return previousColor;
        }
        // ****************************************************************************************************************
        // ****************************************************************************************************************
        private Model3DGroup Initialize_Environment(List<string> model_paths)
        {
            try
            {
                ModelImporter importer = new ModelImporter();

                joints = new List<Joint>();

                System.Windows.Media.Media3D.Material mat_White = MaterialHelper.CreateMaterial(
                                                                  new SolidColorBrush(Color.FromRgb(253, 253, 253)));

                // Load the STL components (dimensions in mm) and assign white color to all ... All model3Ds are listed in [joints]
                foreach (string modelName in model_paths)
                {
                    var base_materialGroup = new MaterialGroup();
                    Color baseColor = Colors.White;
                    EmissiveMaterial base_emissMat = new EmissiveMaterial(new SolidColorBrush(baseColor));
                    DiffuseMaterial base_diffMat = new DiffuseMaterial(new SolidColorBrush(baseColor));
                    SpecularMaterial base_specMat = new SpecularMaterial(new SolidColorBrush(baseColor), 200);
                    base_materialGroup.Children.Add(base_emissMat);
                    base_materialGroup.Children.Add(base_diffMat);
                    base_materialGroup.Children.Add(base_specMat);

                    //if (modelName != "Workspace.STL")
                    //{
                    var link = importer.Load(basePath + modelName);
                    GeometryModel3D base_model = link.Children[0] as GeometryModel3D;
                    base_model.Material = base_materialGroup;
                    base_model.BackMaterial = base_materialGroup;
                    joints.Add(new Joint(link));
                    //}
                }

                // Assemble the STL parts and create the robot as a Modele3DGroup
                var ROT_Link = joints[0];
                this.robot.Children.Add(joints[0].model3d);

                var Nut = joints[1];
                this.robot.Children.Add(joints[1].model3d);

                var RCM_CenterLink = joints[2];
                this.robot.Children.Add(joints[2].model3d);

                var RCM_SwingArm_Back = joints[3];
                this.robot.Children.Add(joints[3].model3d);

                var RCM_Parallelogram_Up = joints[4];
                this.robot.Children.Add(joints[4].model3d);

                var RCM_Parallelogram_Down = joints[5];
                this.robot.Children.Add(joints[5].model3d);

                var RCM_SwingArm_Front = joints[6];
                this.robot.Children.Add(joints[6].model3d);

                var NeedleHolder = joints[7];
                this.robot.Children.Add(joints[7].model3d);

                var Needle = joints[8];
                this.robot.Children.Add(joints[8].model3d);

                var Frame = joints[10];
                this.robot.Children.Add(joints[10].model3d);

                // Create the workspace and make it transparent (alpha = 20%))
                var Workspace = importer.Load(basePath + model_paths[9]);
                this.robot.Children.Add(new Joint(Workspace).model3d);
                GeometryModel3D Workspace_gem = Workspace.Children[0] as GeometryModel3D;
                var materialGroup = new MaterialGroup();
                //Color mainColor = Colors.White;
                Color transp = new Color()
                {
                    A = 20,
                    R = Colors.Transparent.R,
                    G = Colors.Transparent.G,
                    B = Colors.Transparent.B
                };
                EmissiveMaterial emissMat = new EmissiveMaterial(new SolidColorBrush(transp));
                //DiffuseMaterial diffMat = new DiffuseMaterial(new SolidColorBrush(transp));
                //SpecularMaterial specMat = new SpecularMaterial(new SolidColorBrush(transp), 20);
                materialGroup.Children.Add(emissMat);
                //materialGroup.Children.Add(diffMat);
                //materialGroup.Children.Add(specMat);
                Workspace_gem.Material = materialGroup;
                Workspace_gem.BackMaterial = materialGroup;

                // Robot is complete ... assign it to the public variable
                this.robot_Model = robot;
                overall_grid.DataContext = this;

                // Rotate the robot
                //RotateTransform3D robotTransformZ = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0));
                //robotTransformZ.CenterX = 0;
                //robotTransformZ.CenterY = 0;
                //robotTransformZ.CenterZ = 0;
                //myTransform3DGroup.Children.Add(robotTransformZ);

                //// Add the robot rotation to the Transform3DGroup
                Transform3DGroup myTransform3DGroup = new Transform3DGroup();
                RotateTransform3D robotTransformX = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 88));
                robotTransformX.CenterX = 0;
                robotTransformX.CenterY = 0;
                robotTransformX.CenterZ = 0;
                myTransform3DGroup.Children.Add(robotTransformX);
                robot.Transform = myTransform3DGroup;


                joints[0].angleMin = -40;
                joints[0].angleMax = +40;
                joints[0].rotAxisX = 1;
                joints[0].rotAxisY = 0;
                joints[0].rotAxisZ = 0;
                joints[0].rotPointX = 0;
                joints[0].rotPointY = 0;
                joints[0].rotPointZ = 0;

                joints[1].transMin = 0; // ? Determined by DOF2 slider
                joints[1].transMax = 0; // ? Determined by DOF2 slider

                joints[2].transMin = 0; // ? Determined by DOF2 slider
                joints[2].transMax = 0; // ? Determined by DOF2 slider
                joints[2].angleMin = 0; // ? Determined by DOF2 slider
                joints[2].angleMax = 0; // ? Determined by DOF2 slider
                joints[2].rotAxisX = 0;
                joints[2].rotAxisY = 0;
                joints[2].rotAxisZ = 1;
                joints[2].rotPointX = joints[1].transAxisX - 10;
                joints[2].rotPointY = 0;
                joints[2].rotPointZ = 0;

                joints[3].angleMin = -45;
                joints[3].angleMax = +45;
                joints[3].rotAxisX = 0;
                joints[3].rotAxisY = 0;
                joints[3].rotAxisZ = 1;
                joints[3].rotPointX = 68.5F;
                joints[3].rotPointY = 0;
                joints[3].rotPointZ = 0;

                joints[6].angleMin = -45;
                joints[6].angleMax = +45;
                joints[6].rotAxisX = 0;
                joints[6].rotAxisY = 0;
                joints[6].rotAxisZ = 1;
                joints[6].rotPointX = 118.5F;
                joints[6].rotPointY = 0;
                joints[6].rotPointZ = 0;

                joints[7].angleMin = -29.24f;
                joints[7].angleMax = +50.17f;
                joints[7].rotAxisX = 0;
                joints[7].rotAxisY = 0;
                joints[7].rotAxisZ = 1;
                joints[7].rotPointX = 167.7F;
                joints[7].rotPointY = 0;
                joints[7].rotPointZ = 0;

                joints[8].transMin = 0;
                joints[8].transMax = +150f;
                joints[8].angleMin = 0;
                joints[8].angleMax = +150f;

            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("Robot Build Error:" + e.StackTrace);
            }
            return robot;
        }

        private void joint_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isAnimating)
                return;
            if (joints == null)
                return;
            joints[0].angle = (float)joint_DOF1.Value;
            joints[1].transAxisX = -(((float)joint_DOF2.Value) - 22.0f) / 1.5f;
            joints[7].angle = (float)joint_DOF2.Value - 23;
            joints[8].transAxisY = (float)Needle.Value;
            execute_fk();
        }

        private void execute_fk()
        {
            /** Debug sphere, it takes the x,y,z of the textBoxes and update its position
             * This is useful when using x,y,z in the "new Point3D(x,y,z)* when defining a new RotateTransform3D() to check where the joints is actually  rotating */
            float[] angles = { joints[0].angle, joints[1].angle, joints[2].angle, joints[3].angle, joints[4].angle, joints[5].angle,
                               joints[6].angle, joints[7].angle, joints[8].transAxisY };
            //float[] translations = {joints[0].translation, joints[1].translation, joints[2].translation, joints[3].translation, joints[4].translation, joints[5].translation,
            //                       joints[6].translation, joints[7].translation, joints[8].translation};
            ForwardKinematics(angles);
        }

        public Matrix4x4 ForwardKinematics(float[] angles)
        {
            //The ROT_Link only has rotation and positioned at the origin, so the only transform in the transformGroup is the rotation R


            F1 = new Transform3DGroup();
            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(joints[0].rotAxisX, joints[0].rotAxisY, joints[0].rotAxisZ), angles[0]), new Point3D(joints[0].rotPointX, joints[0].rotPointY, joints[0].rotPointZ));
            F1.Children.Add(R);

            F8 = new Transform3DGroup();
            T = new TranslateTransform3D(0, 0, 0);
            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(joints[7].rotAxisX, joints[7].rotAxisY, joints[7].rotAxisZ), angles[7]), new Point3D(joints[7].rotPointX, joints[7].rotPointY, joints[7].rotPointZ));
            F8.Children.Add(T);
            F8.Children.Add(R);
            F8.Children.Add(F1);

            //as before
            F4 = new Transform3DGroup();
            T = new TranslateTransform3D(0, 0, 0);
            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(joints[3].rotAxisX, joints[3].rotAxisY, joints[3].rotAxisZ), angles[7]), new Point3D(joints[3].rotPointX, joints[3].rotPointY, joints[3].rotPointZ));
            F4.Children.Add(T);
            F4.Children.Add(R);
            F4.Children.Add(F1);

            F7 = new Transform3DGroup();
            T = new TranslateTransform3D(0, 0, 0);
            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(joints[6].rotAxisX, joints[6].rotAxisY, joints[6].rotAxisZ), angles[7]), new Point3D(joints[6].rotPointX, joints[6].rotPointY, joints[6].rotPointZ));
            F7.Children.Add(T);
            F7.Children.Add(R);
            F7.Children.Add(F1);

            // Nut linear movement 
            F2 = new Transform3DGroup();
            T = new TranslateTransform3D(joints[1].transAxisX, joints[1].transAxisY, joints[1].transAxisZ);
            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 0), 0), new Point3D(0, 0, 0));
            F2.Children.Add(T);
            F2.Children.Add(R);
            F2.Children.Add(F1);

            // RCM Centerlink rotation with the DOF2 slider (angles[7])
            // Parabola equation
            float angle_joint2 = -(0.95f * (float)(Math.Pow((((angles[7]) + 23) / 20), 2f))) + 1.5f;

            F3 = new Transform3DGroup();
            T = new TranslateTransform3D(0, 0, 0);
            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(joints[2].rotAxisX, joints[2].rotAxisY, joints[2].rotAxisZ), angle_joint2), new Point3D(joints[2].rotPointX, joints[2].rotPointY, joints[2].rotPointZ));
            F3.Children.Add(T);
            F3.Children.Add(R);
            F3.Children.Add(F2);

            //Parabolic movement
            float angle_joint4 = -(float)(Math.Pow((angles[7] + 26) / 14, 2f)) + 2.5f;
            F5 = new Transform3DGroup();
            T = new TranslateTransform3D(0, angle_joint4, 0);
            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 0), 0), new Point3D(joints[4].rotPointX, joints[4].rotPointY, joints[4].rotPointZ));
            F5.Children.Add(T);
            F5.Children.Add(R);
            F5.Children.Add(F2);

            float angle_joint5 = -(0.5f * (float)(Math.Pow((((angles[7]) + 26) / 20), 2f))) + 2f;
            F6 = new Transform3DGroup();
            T = new TranslateTransform3D(0, angle_joint5, 0);
            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 0), 0), new Point3D(joints[5].rotPointX, joints[5].rotPointY, joints[5].rotPointZ));
            F6.Children.Add(T);
            F6.Children.Add(R);
            F6.Children.Add(F2);

            F9 = new Transform3DGroup();
            //T = new TranslateTransform3D((joints[8].transAxisY) * Math.Cos(67.0 * Math.PI / 180.0), (-joints[8].transAxisY) * Math.Sin(67.0 * Math.PI / 180.0), 0);
            T = new TranslateTransform3D((angles[8]) * Math.Cos(67.0 * Math.PI / 180.0), (-angles[8]) * Math.Sin(67.0 * Math.PI / 180.0), 0);
            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 0), 0), new Point3D(0, 0, 0));
            F9.Children.Add(T);
            F9.Children.Add(R);
            F9.Children.Add(F8);

            joints[0].model3d.Transform = F1; //ROT_Link joint (DOF1)
            joints[1].model3d.Transform = F2; //NUT (translation)
            joints[2].model3d.Transform = F3; //RCM_CenterLink (rotation)
            joints[3].model3d.Transform = F4; //RCM_SwingArm_Back
            joints[4].model3d.Transform = F5; //RCM_Parallelogram_Up
            joints[5].model3d.Transform = F6; //RCM_Parallelogram_Down
            joints[6].model3d.Transform = F7; //RCM_SwingArm_Front
            joints[7].model3d.Transform = F8; //Needle Holder (DOF2)
            joints[8].model3d.Transform = F9; //The Needle (translation)

            // joints[7].model3d.Transform = F1; //Cables

            // joints[8].model3d.Transform = F2; //Cables

            // joints[6].model3d.Transform = F3; //The ABB writing
            // joints[9].model3d.Transform = F3; //Cables

            // Dean's FK implementation
            double theta_1 = angles[0]; // rotation DOF in deg
            double theta_2 = angles[7] + 23; // translation DOF in deg
            double theta_2_prime = (90 - theta_2) * Math.PI / 180;

            double theta_2_trans = 143 - (114.22 / Math.Sin(theta_2_prime)) * Math.Sin(((180 - Math.Asin(41.57 / 114.22 * Math.Sin(theta_2_prime)) * 180 / Math.PI - theta_2_prime * 180 / Math.PI) * Math.PI / 180));
            // double theta_3 = joints[8].transAxisY; // needle insertion DOF
            double theta_3 = angles[8];

            Matrix4x4 R0 = Matrix4x4.CreateFromYawPitchRoll(0, Convert.ToSingle((-theta_1) * Math.PI / 180), 0);
            Vector3D T0 = new Vector3D(theta_2_trans - 24.5, 0, 0);
            R0.M14 = Convert.ToSingle(T0.X);
            R0.M24 = Convert.ToSingle(T0.Y);
            R0.M34 = Convert.ToSingle(T0.Z);

            Matrix4x4 R1 = Matrix4x4.CreateFromYawPitchRoll(Convert.ToSingle((theta_2) * Math.PI / 180), 0, 0);
            Vector3D T1 = new Vector3D(193 - theta_2_trans, 0, 0);
            R1.M14 = Convert.ToSingle(T1.X);
            R1.M24 = Convert.ToSingle(T1.Y);
            R1.M34 = Convert.ToSingle(T1.Z);

            Matrix4x4 R2 = Matrix4x4.CreateFromYawPitchRoll(0, 0, 0);
            Vector3D T2 = new Vector3D(0, 0, -theta_3);
            R2.M14 = Convert.ToSingle(T2.X);
            R2.M24 = Convert.ToSingle(T2.Y);
            R2.M34 = Convert.ToSingle(T2.Z);


            Matrix4x4 E_final = R0 * R1 * R2;
            //Matrix4x4 Rx = Matrix4x4.CreateFromYawPitchRoll(0, -(float)Math.PI/4, 0);
            //Matrix4x4 Ry = Matrix4x4.CreateFromYawPitchRoll(-(float)Math.PI / 4, 0, 0);
            //Matrix4x4 Rz = Matrix4x4.CreateFromYawPitchRoll(0, 0, -(float)Math.PI / 4);

            // Console.WriteLine(theta_2_trans);
            Tx.Content = E_final.M14;
            Ty.Content = E_final.M24;
            Tz.Content = E_final.M34;

            Vector3D end_eff_coord = new Vector3D(E_final.M14, E_final.M24, E_final.M34);
            end_effector.Transform = new TranslateTransform3D(end_eff_coord);
            return E_final;

        }
        private Vector3D Rot2Euler(Matrix4x4 R) {
            double E1;
            double E2;
            double E3;
            double dlta;

            if (R.M13 == 1 || R.M13 == -1)
            {
                //special case
                E3 = 0; //set arbitrarily
                dlta = Math.Atan2(R.M12, R.M13);

                if (R.M13 == -1)
                {
                    E2 = Math.PI / 2;
                    E1 = E3 + dlta;
                }
                else
                {
                    E2 = -Math.PI / 2;
                    E1 = -E3 + dlta;
                }
            }
            else
            {
                E2 = -Math.Asin(R.M13);
                E1 = Math.Atan2(R.M23 / Math.Cos(E2), R.M33 / Math.Cos(E2));
                E3 = Math.Atan2(R.M12 / Math.Cos(E2), R.M11 / Math.Cos(E2));
            }

           return new Vector3D(E1, E2, E3);
    }
        private void GoToPosition(object sender, RoutedEventArgs e)
        {
            if (timer1.Enabled)
            {
                button.Content = "Go to Position";
                isAnimating = false;
                timer1.Stop();
                movements = 0;
            }
            else
            {
                sphere.Transform = new TranslateTransform3D(reachingPoint);
                movements = 50000;
                button.Content = "STOP";
                isAnimating = true;
                timer1.Start();
            }
        }

        public void timer1_Tick(object sender, EventArgs e)
        {
            float[] angles = { (float)joint_DOF1.Value, 0, 0, 0, 0, 0, 0, (float)joint_DOF2.Value-23, (float)Needle.Value };
            // Console.WriteLine(angles[2]);
            angles = IK_numerical(reachingPoint, angles);
            joint_DOF1.Value = angles[0];
            joint_DOF2.Value = angles[7]+23;
            Needle.Value = angles[8];

            if ((--movements) <= 0)
            {
                button.Content = "Go to position";
                isAnimating = false;
                Console.WriteLine("Stopped");
                timer1.Stop();
            }
        }

        public float[] IK_numerical(Vector3D target, float[] angles)
        {
            if (DistanceFromTarget(target, angles) < DistanceThreshold)
            {
                movements = 0;
                return angles;
            }

            float[] oldAngles = { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };
            angles.CopyTo(oldAngles, 0);
            for (int i = 0; i <= 8; i++)
            {
                
                if (i == 0 || i == 7 || i == 8)
                {
                    // Gradient descent
                    float gradient = PartialGradient(target, angles, i);
                    angles[i] -= LearningRate * gradient;
                    //Console.WriteLine(angles[i]);

                    // Clamp
                    angles[i] = Clamp(angles[i], joints[i].angleMin, joints[i].angleMax);
                    if (i == 8)
                    {
                        //Console.WriteLine(angles[i]);
                    }

                    // Early termination
                    if (DistanceFromTarget(target, angles) < DistanceThreshold || checkAngles(oldAngles, angles))
                    {
                        movements = 0;
                        return angles;
                    }
                }
            }

            return angles;
        }

        // public float[] IK_analytical()
        //{

        //    return angles;
        //}

        public float DistanceFromTarget(Vector3D target, float[] angles)
        {
            Matrix4x4 E_final = ForwardKinematics(angles);
            Vector3D point = new Vector3D(E_final.M14, E_final.M24, E_final.M34);
            return (float)Math.Sqrt(Math.Pow((point.X - target.X), 2.0) + Math.Pow((point.Y - target.Y), 2.0) + Math.Pow((point.Z - target.Z), 2.0));
        }

        public bool checkAngles(float[] oldAngles, float[] angles)
        {
            for (int i = 0; i <= 8; i++)
            {
                if (oldAngles[i] != angles[i])
                    return false;
            }

            return true;
        }

        public float PartialGradient(Vector3D target, float[] angles, int i)
        {
            // Saves the angle,
            // it will be restored later
            float angle = angles[i];

            // Gradient : [F(x+SamplingDistance) - F(x)] / h
            float f_x = DistanceFromTarget(target, angles);
            
            angles[i] += SamplingDistance;

            float f_x_plus_d = DistanceFromTarget(target, angles);

            float gradient = (f_x_plus_d - f_x) / SamplingDistance;
            if (i == 0 || i == 7 || i == 8)
            {
                Console.WriteLine(i);
                Console.WriteLine(gradient);
            }
            // Restores
            angles[i] = angle;

            return gradient;
        }

        private void ReachingPoint_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {

                reachingPoint = new Vector3D(Double.Parse(TbX.Text), Double.Parse(TbY.Text), Double.Parse(TbZ.Text));
                sphere.Transform = new TranslateTransform3D(reachingPoint);
            }
            catch (Exception exc)
            {

            }
        }

        private Matrix4x4 getTransformMatrix(float[] angles)
        {
            Matrix4x4 newTransMatrix = Matrix4x4.CreateFromYawPitchRoll(angles[0], angles[1], angles[7]);
            return newTransMatrix;
        }



    }
}
