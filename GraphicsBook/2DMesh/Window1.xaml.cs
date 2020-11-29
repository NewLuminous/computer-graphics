﻿using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;

using System.Diagnostics;

namespace GraphicsBook
{
    /// <summary>
    /// Display and interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        GraphPaper gp = null;

        Random autoRand = new System.Random();
        List<Dot> myDots = null;
        Key lastKey;
        Dot startingVertex = null;
        Dictionary<Tuple<Dot, Dot>, Segment> graph;
        List<int> myTriangle = null;
        Dictionary<Tuple<int, int, int>, Polygon> myTriangles;

        // Are we ready for interactions like slider-changes to alter the 
        // parts of our display (like polygons or images or arrows)? Probably not until those things 
        // have been constructed!
        bool ready = false;

        // Code to create and display objects goes here.
        public Window1()
        {
            InitializeComponent();
            InitializeCommands();

            // Now add some graphical items in the main drawing area, whose name is "Paper"
            gp = this.FindName("Paper") as GraphPaper;
            

            // Track mouse activity in this window
            MouseLeftButtonDown += MyMouseButtonDown;
            MouseLeftButtonUp += MyMouseButtonUp;
            MouseMove += MyMouseMove;

            myDots = new List<Dot>();
            graph = new Dictionary<Tuple<Dot, Dot>, Segment>();
            myTriangle = new List<int>();
            myTriangles = new Dictionary<Tuple<int, int, int>, Polygon>();

            ready = true; // Now we're ready to have sliders and buttons influence the display.
        }

        public void ResetEdgesClick(object sender, RoutedEventArgs e)
        {
            Debug.Print("ResetEdgesClick clicked!\n");
            if (ready)
            {
                foreach (Segment edge in graph.Values)
                {
                    if (gp.Children.Contains(edge))
                    {
                        gp.Children.Remove(edge);
                    }
                }
                graph = new Dictionary<Tuple<Dot, Dot>, Segment>();
            }
            e.Handled = true; // don't propagate the click any further
        }

        public void ResetTrianglesClick(object sender, RoutedEventArgs e)
        {
            Debug.Print("ResetTrianglesClick clicked!\n");
            if (ready)
            {
                foreach (Polygon triangle in myTriangles.Values)
                {
                    if (gp.Children.Contains(triangle))
                    {
                        gp.Children.Remove(triangle);
                    }
                }
                myTriangles = new Dictionary<Tuple<int, int, int>, Polygon>();
            }
            e.Handled = true; // don't propagate the click any further
        }

        public void ResetVerticesClick(object sender, RoutedEventArgs e)
        {
            Debug.Print("ResetVerticesClick clicked!\n");
            if (ready)
            {
                ResetEdgesClick(sender, e);
                foreach (Dot dot in myDots)
                {
                    gp.Children.Remove(dot);
                }
                myDots = new List<Dot>();
            }
            e.Handled = true; // don't propagate the click any further
        }

        #region Interaction handling -- sliders and buttons

        /* Click-handling in the main graph-paper window */
        public void MyMouseButtonUp(object sender, RoutedEventArgs e)
        {
            if (sender != this) return;
            System.Windows.Input.MouseButtonEventArgs ee =
              (System.Windows.Input.MouseButtonEventArgs)e;
            Debug.Print("MouseUp at " + ee.GetPosition(this));
            e.Handled = true;
        }

        protected double Distance(Point a, Point b)
        {
            return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        }

        protected Dot NearestDot(Point clickPoint)
        {
            if (myDots.Count == 0) return null;
            int idx = 0;
            for (int i = 1; i < myDots.Count; ++i)
            {
                if (Distance(clickPoint, myDots[i].Position) < Distance(clickPoint, myDots[idx].Position))
                {
                    idx = i;
                }
            }
            return myDots[idx];
        }

        protected void AddDot(Dot dot)
        {
            myDots.Add(dot);
            myDots[myDots.Count - 1].Stroke = Brushes.Transparent;
            gp.Children.Add(myDots[myDots.Count - 1]);
        }

        protected void AddEdge(Dot dot)
        {
            Segment edge;
            if (graph.TryGetValue(new Tuple<Dot, Dot>(startingVertex, dot), out edge))
            {
                gp.Children.Remove(edge);
                graph.Remove(new Tuple<Dot, Dot>(startingVertex, dot));
                graph.Remove(new Tuple<Dot, Dot>(dot, startingVertex));
            }
            else
            {
                edge = new Segment(startingVertex, dot);
                edge.Stroke = Brushes.Black;
                gp.Children.Add(edge);
                graph.Add(new Tuple<Dot, Dot>(startingVertex, dot), edge);
                graph.Add(new Tuple<Dot, Dot>(dot, startingVertex), edge);
            }
        }

        public void MyMouseButtonDown(object sender, RoutedEventArgs e)
        {
            if (sender != this) return;
            if (ready)
            {
                System.Windows.Input.MouseButtonEventArgs ee = 
                    (System.Windows.Input.MouseButtonEventArgs)e;
                Debug.Print("MouseDown at " + ee.GetPosition(this));

                Point clickPoint = ee.GetPosition(gp);

                if (lastKey == Key.LeftShift || lastKey == Key.RightShift)
                {
                    Dot dot = NearestDot(clickPoint);
                    if (dot != null)
                    {
                        if (startingVertex != null)
                        {
                            startingVertex.Stroke = Brushes.Transparent;
                        }
                        startingVertex = dot;
                        startingVertex.Stroke = Brushes.Red;
                        lastKey = Key.None;
                    }
                }
                else if (lastKey == Key.LeftCtrl || lastKey == Key.RightCtrl)
                {
                    Dot dot = NearestDot(clickPoint);
                    if (dot != null)
                    {
                        int idx = myDots.IndexOf(dot);
                        if (!myTriangle.Contains(idx))
                        {
                            myTriangle.Add(idx);
                            dot.Stroke = Brushes.Blue;
                            if (myTriangle.Count == 3)
                            {
                                myTriangle.Sort();
                                Polygon polygon;
                                if (myTriangles.TryGetValue(new Tuple<int, int, int>(myTriangle[0], myTriangle[1], myTriangle[2]), out polygon)) {
                                    gp.Children.Remove(polygon);
                                    myTriangles.Remove(new Tuple<int, int, int>(myTriangle[0], myTriangle[1], myTriangle[2]));
                                }
                                else
                                {
                                    polygon = new Polygon();
                                    foreach (int i in myTriangle)
                                    {
                                        polygon.Points.Add(myDots[i].Position);
                                    }
                                    polygon.Stroke = Brushes.Black;
                                    polygon.StrokeThickness = 0.25;

                                    gp.Children.Add(polygon);
                                    myTriangles.Add(new Tuple<int, int, int>(myTriangle[0], myTriangle[1], myTriangle[2]), polygon);

                                }
                                foreach (int i in myTriangle)
                                {
                                    myDots[i].Stroke = Brushes.Transparent;
                                }
                                myTriangle = new List<int>();
                            }

                            lastKey = Key.None;
                        }
                    }
                }
                else
                {
                    if (startingVertex == null)
                    {
                        AddDot(new Dot(clickPoint));
                    }
                    else
                    {
                        Dot dot = NearestDot(clickPoint);
                        if (!dot.IsMouseOver)
                        {
                            dot = new Dot(clickPoint);
                            AddDot(dot);
                        }
                        if (dot != startingVertex)
                        {
                            AddEdge(dot);
                        }

                        startingVertex.Stroke = Brushes.Transparent;
                        startingVertex = null;
                    }
                }
            }
            e.Handled = true;
        }


        public void MyMouseMove(object sender, MouseEventArgs e)
        {
            if (sender != this) return;
            System.Windows.Input.MouseEventArgs ee =
              (System.Windows.Input.MouseEventArgs)e;
            // Uncommment following line to get a flood of mouse-moved messages. 
            // Debug.Print("MouseMove at " + ee.GetPosition(this));
            e.Handled = true;
        }


        void slider1change(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Debug.Print("Slider changed, ready = " + ready + ", and val = " + e.NewValue + ".\n");
            e.Handled = true;
            // Be sure to not respond to slider-moves until all objects have been constructed. 
            if (ready)
            {/*
                PointCollection p = myTriangle.Points.Clone();
                Debug.Print(myTriangle.Points.ToString());
                Point u = p[0];
                u.X = e.NewValue;
                p[0] = u;
                myTriangle.Points = p;*/
            }
        }

        public void b2Click(object sender, RoutedEventArgs e)
        {
            Debug.Print("Button two clicked!\n");
            e.Handled = true; // don't propagate the click any further
        }
        #endregion

        #region Menu, command, and keypress handling

        protected static RoutedCommand ExitCommand;

        protected void InitializeCommands()
        {
            InputGestureCollection inp = new InputGestureCollection();
            inp.Add(new KeyGesture(Key.X, ModifierKeys.Control));
            ExitCommand = new RoutedCommand("Exit", typeof(Window1), inp);
            CommandBindings.Add(new CommandBinding(ExitCommand, CloseApp));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, CloseApp));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.New, NewCommandHandler));
        }

        void NewCommandHandler(Object sender, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show("You selected the New command",
                                Title,
                                MessageBoxButton.OK,
                                MessageBoxImage.Exclamation);

        }

        // Announce keypresses, EXCEPT for CTRL, ALT, SHIFT, CAPS-LOCK, and "SYSTEM" (which is how Windows 
        // seems to refer to the "ALT" keys on my keyboard) modifier keys
        // Note that keypresses that represent commands (like ctrl-N for "new") get trapped and never get
        // to this handler.
        void KeyDownHandler(object sender, KeyEventArgs e)
        {
            if ((e.Key != Key.LeftCtrl) &&
                (e.Key != Key.RightCtrl) &&
                (e.Key != Key.LeftAlt) &&
                (e.Key != Key.RightAlt) &&
                (e.Key != Key.System) &&
                (e.Key != Key.Capital) &&
                (e.Key != Key.LeftShift) &&
                (e.Key != Key.RightShift))
            {
                MessageBox.Show(String.Format("[{0}]  {1} received @ {2}",
                                        e.Key,
                                        e.RoutedEvent.Name,
                                        DateTime.Now.ToLongTimeString()),
                                Title,
                                MessageBoxButton.OK,
                                MessageBoxImage.Exclamation);
            }

            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                if (startingVertex == null) lastKey = e.Key;
            }
            else
            {
                lastKey = e.Key;
            }
        }

        void CloseApp(Object sender, ExecutedRoutedEventArgs args)
        {
            if (MessageBoxResult.Yes ==
                MessageBox.Show("Really Exit?",
                                Title,
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question)
               ) Close();
        }
        #endregion //Menu, command and keypress handling

        #region Image, Mesh, and Quiver, construction helpers
        private byte[, ,] createStripeImageArray()
        {
            int width = 128;
            int height = 128;
            byte[ , , ] pixelArray = new byte[width, height, 4];

            for (int y = 0; y < height; ++y)
            {
                int yIndex = y * width;
                for (int x = 0; x < width; ++x)
                {
                    byte b = (byte)(32 * (Math.Round((x + 2 * y) / 32.0)));
                    pixelArray[x, y, 0] = b;
                    pixelArray[x, y, 1] = b;
                    pixelArray[x, y, 2] = b;
                    pixelArray[x, y, 3] = 255;
                }
            }
            return pixelArray;
        }

        private int vectorIndex(int row, int col, int nrows, int ncols)
        {
            return col + row * ncols;
        }

        private Mesh createSampleMesh()
        {
            int nrows = 4;
            int ncols = 6;
            int nverts = nrows * ncols;
            int nedges = nrows * (ncols - 1) + ncols * (nrows - 1);
            int baseX = -40;
            int baseY = 55;
            Point[] verts = new Point[nverts];
            int[,] edges = new int[nedges, 2];

            for (int y = 0; y < nrows; y++)
            {
                for (int x = 0; x < ncols; x++)
                {
                    verts[vectorIndex(y, x, nrows, ncols)] =
                        new Point(baseX + 10 * x, baseY + 10 * y + 5 * Math.Sin(2 * Math.PI * x / (ncols - 1)));
                }
            }

            int count = 0;
            for (int y = 0; y < nrows; y++)
            {
                for (int x = 0; x < ncols - 1; x++)
                {
                    edges[count, 0] = vectorIndex(y, x, nrows, ncols);
                    edges[count, 1] = vectorIndex(y, x + 1, nrows, ncols);
                    count++;
                }
            }
            for (int x = 0; x < ncols; x++)
            {
                for (int y = 0; y < nrows - 1; y++)
                {
                    edges[count, 0] = vectorIndex(y, x, nrows, ncols);
                    edges[count, 1] = vectorIndex(y + 1, x, nrows, ncols);
                    count++;
                }
            }
            Debug.Print("count = " + count + "\n");
            return new Mesh(nverts, count, verts, edges);
        }

        private Quiver makeQuiver()
        {
            int count = 10;
            Point[] verts = new Point[count];
            Vector[] arrows = new Vector[count];
            for (int i = 0; i < count; i++)
            {
                double th = 2 * Math.PI * i / count;
                verts[i] = new Point(-40 + 5 * Math.Cos(th), -40 + 5 * Math.Sin(th));
                arrows[i] = new Vector(20 * Math.Cos(th), 20 * Math.Sin(th));
            }
            return new Quiver(verts, arrows);
        }
        #endregion
    }
}