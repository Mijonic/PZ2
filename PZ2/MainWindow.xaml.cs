using PZ2.Model;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Diagnostics;
using Point = System.Windows.Point;

namespace PZ2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public static PowerEntity[,] abstractGrid = new PowerEntity[300, 300];
        public static List<PowerEntity> entities = new List<PowerEntity>();
        public static List<LineEntity> lines = new List<LineEntity>();


        public static List<Tuple<List<Tuple<int, int>>, LineEntity>> allPaths = new List<Tuple<List<Tuple<int, int>>, LineEntity>>();
       
        public static List<Polyline> allPolylines = new List<Polyline>();

        public static Tuple<Ellipse, Brush> paintedElementA = null;
        public static Tuple<Ellipse, Brush> paintedElementB = null;

        public static List<Tuple<int, int>> intersectionPoints = new List<Tuple<int, int>>();
        public static bool allowIntersetions = false;

        public static HashSet<string> allVisitedPointsByPath = new HashSet<string>();


        public static double xMinMaxDifference;
        public static double yMinMaxDifference;
        public static double  maxX, minX, maxY, minY;

        public static double canvasWidth;
        public static double canvasHeight;

        public static int gridSize = 299;
     

        public MainWindow()
        {
            InitializeComponent();

            canvasWidth = this.mainCanvas.Width;
            canvasHeight = this.mainCanvas.Height;

            LoadDataFromFile();
            DrawPaths();


            DrawIntersectionPoints();

            DrawPoints();








        }



        

        public static void LoadDataFromFile()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("Geographic.xml");

            XmlNodeList nodeList;

            double newX;
            double newY;


            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Substations/SubstationEntity");
            foreach (XmlNode node in nodeList)
            {

                SubstationEntity sub = new SubstationEntity();

                sub.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                sub.Name = node.SelectSingleNode("Name").InnerText;
                sub.X = double.Parse(node.SelectSingleNode("X").InnerText);
                sub.Y = double.Parse(node.SelectSingleNode("Y").InnerText);

                ToLatLon(sub.X, sub.Y, 34, out newX, out newY);

                sub.X = newX;
                sub.Y = newY;

                entities.Add(sub);


                
            }

          

           

            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Nodes/NodeEntity");
            foreach (XmlNode node in nodeList)
            {

                NodeEntity nodeobj = new NodeEntity();

                nodeobj.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                nodeobj.Name = node.SelectSingleNode("Name").InnerText;
                nodeobj.X = double.Parse(node.SelectSingleNode("X").InnerText);
                nodeobj.Y = double.Parse(node.SelectSingleNode("Y").InnerText);

                ToLatLon(nodeobj.X, nodeobj.Y, 34, out newX, out newY);

                nodeobj.X = newX;
                nodeobj.Y = newY;


                entities.Add(nodeobj);
            }

          

            
            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Switches/SwitchEntity");
            foreach (XmlNode node in nodeList)
            {

                SwitchEntity switchobj = new SwitchEntity();


                switchobj.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                switchobj.Name = node.SelectSingleNode("Name").InnerText;
                switchobj.X = double.Parse(node.SelectSingleNode("X").InnerText);
                switchobj.Y = double.Parse(node.SelectSingleNode("Y").InnerText);
                switchobj.Status = node.SelectSingleNode("Status").InnerText;

                ToLatLon(switchobj.X, switchobj.Y, 34, out newX, out newY);

                switchobj.X = newX;
                switchobj.Y = newY;

                entities.Add(switchobj);
            }


          

           

            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Lines/LineEntity");
            foreach (XmlNode node in nodeList)
            {

                LineEntity l = new LineEntity();

                l.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                l.Name = node.SelectSingleNode("Name").InnerText;
                if (node.SelectSingleNode("IsUnderground").InnerText.Equals("true"))
                {
                    l.IsUnderground = true;
                }
                else
                {
                    l.IsUnderground = false;
                }
                l.R = float.Parse(node.SelectSingleNode("R").InnerText);
                l.ConductorMaterial = node.SelectSingleNode("ConductorMaterial").InnerText;
                l.LineType = node.SelectSingleNode("LineType").InnerText;
                l.ThermalConstantHeat = long.Parse(node.SelectSingleNode("ThermalConstantHeat").InnerText);
                l.FirstEnd = long.Parse(node.SelectSingleNode("FirstEnd").InnerText);
                l.SecondEnd = long.Parse(node.SelectSingleNode("SecondEnd").InnerText);



                bool firstEnd = false;
                bool secondEnd = false;

                foreach(PowerEntity entity in entities)
                {
                    if (entity.Id == l.FirstEnd)
                        firstEnd = true;

                    if (entity.Id == l.SecondEnd)
                        secondEnd = true;

                }

                if (firstEnd && secondEnd)
                    lines.Add(l);

              
            }


            maxX = entities[0].X;
            minX = entities[0].X;
            maxY = entities[0].Y;
            minY = entities[0].Y;

            foreach(PowerEntity entity in entities)
            {    

                if (entity.X > maxX)
                    maxX = entity.X;

                if (entity.X < minX)
                    minX = entity.X;

                if (entity.Y > maxY)
                    maxY = entity.Y;

                if (entity.Y < minY)
                    minY = entity.Y;
            }


            xMinMaxDifference = maxX - minX;
            yMinMaxDifference = maxY - minY;


            PopulateGrid();

           

        }

        public static void PopulateGrid()
        {
        
            int scaledX, scaledY;

            foreach(PowerEntity entity in entities)
            {
                scaledX = CalculateScaledXCoordinate(entity.X);
                scaledY = CalculateScaledYCoordinate(entity.Y);

                if ( abstractGrid[scaledX, scaledY] == null)
                {
                    abstractGrid[scaledX, scaledY] = entity;
                   

                }else
                {
                    int cnt = 0;
                    int columnCnt = -1;
                    int size = 0;

                    bool foundSpace = false;

                    while (true)
                    {



                       
                        int index = scaledX - 1 - cnt;
                        for (int k = columnCnt; k < (2 + size) - cnt; k++)
                        {
                            if (index % gridSize >= 0 && ((scaledY + k) % gridSize) >= 0)
                            {
                                

                                if (index <= gridSize && scaledY + k <= gridSize && abstractGrid[index, scaledY + k] == null)
                                {
                                    abstractGrid[index, scaledY + k] = entity;
                                  
                                    foundSpace = true;
                                    break;
                                }
                            }


                        }

                        if (foundSpace)
                            break;

                        index = scaledX;
                        for (int k = columnCnt; k < (2 + size) - cnt; k++)
                        {
                            if (index % gridSize >= 0 && ((scaledY + k) % gridSize) >= 0)
                            {


                                if (index <= gridSize && scaledY + k <= gridSize && abstractGrid[index, scaledY + k] == null)
                                {
                                    abstractGrid[index, scaledY + k] = entity;
                                   
                                    foundSpace = true;
                                    break;
                                }
                            }


                        }


                        if (foundSpace)
                            break;


                        index = scaledX + 1 + cnt;
                        for (int k = columnCnt; k < (2 + size) - cnt; k++)
                        {
                            if (index % gridSize >= 0 && ((scaledY + k) % gridSize) >= 0)
                            {
                               
                                if (index <= gridSize && scaledY + k <= gridSize && abstractGrid[index, scaledY + k] == null)
                                {
                                    abstractGrid[index, scaledY + k] = entity;
                                    
                                    foundSpace = true;
                                    break;
                                }
                            }


                        }


                        if (foundSpace)
                            break;


                        columnCnt--;
                        cnt++;
                        size += 2;


                    }
                }
            }

          
        }


        public static void DrawPoints()
        {

          
            double x, y;
           
       

            for( int i = 0; i < gridSize; i++)
            {
                for(int j = 0; j < gridSize; j++)
                {

                    

                    if(abstractGrid[i,j] != null)
                    {
                      

                        x = CalculateXScaledToCanvas(i);
                        y = CalculateYScaledToCanvas(j);



                        Ellipse ellipse = new Ellipse();

                        ellipse.Width = 3;
                        ellipse.Height = 3;

                        if (abstractGrid[i, j] is SubstationEntity)
                        {

                            ellipse.Fill = Brushes.Red;                           
                            ellipse.ToolTip = String.Format("Substation - {0} - {1}", abstractGrid[i,j].Id, abstractGrid[i, j].Name);
                        }
                        else if (abstractGrid[i, j] is NodeEntity)
                        {
                            ellipse.Fill = Brushes.Green;
                            ellipse.ToolTip = String.Format("Node - {0} - {1}", abstractGrid[i,j].Id, abstractGrid[i, j].Name);
                        }
                        else
                        {

                            ellipse.Fill = Brushes.Blue;
                            ellipse.ToolTip = String.Format("Switch - {0} - {1}", abstractGrid[i, j].Id, abstractGrid[i, j].Name);
                        }
                    

                        Canvas.SetBottom(ellipse, x);
                        Canvas.SetLeft(ellipse, y);
                       
                        ((MainWindow)Application.Current.MainWindow).mainCanvas.Children.Add(ellipse);

                       
                    }
                }

            }

         

         
        }

        public static void DrawIntersectionPoints()
        {
            double x, y;

            foreach(Tuple<int, int> point in intersectionPoints )
            {
                x = CalculateXScaledToCanvas(point.Item1) + 0.5;
                y = CalculateYScaledToCanvas(point.Item2) + 0.5;


                Ellipse ellipse = new Ellipse();

                ellipse.Width = 2;
                ellipse.Height = 2;

                ellipse.Fill = Brushes.LightGray;


                Canvas.SetBottom(ellipse, x);
                Canvas.SetLeft(ellipse, y);
                ((MainWindow)Application.Current.MainWindow).mainCanvas.Children.Add(ellipse);
            }
        }

        public static void DrawPaths()
        {

            FindAllPaths();

            CreatePolylines();
           
            foreach(Polyline polyline in allPolylines)
            {
                ((MainWindow)Application.Current.MainWindow).mainCanvas.Children.Add(polyline);
            }
        

        }


        public static void CreatePolylines()
        {
            foreach(Tuple<List<Tuple<int, int>>, LineEntity> path in allPaths)
            {

                Polyline polyline = new Polyline();

                polyline.StrokeThickness = 1;
                polyline.Stroke = Brushes.Black;
                polyline.ToolTip = String.Format("Line - {0} - {1}", path.Item2.Id, path.Item2.Name);



                if (path.Item1 != null)
                {

                    foreach (Tuple<int, int> positions in path.Item1)
                    {


                        double ratio = canvasHeight / gridSize;
                        double x = (positions.Item2 * ratio) + 1.5;

                        ratio = canvasWidth / gridSize;
                        double y = canvasWidth - (positions.Item1 * ratio) - 1.5;


                        polyline.Points.Add(new System.Windows.Point(x, y));
                    }

                    allPolylines.Add(polyline);
                }
            }
        }
     

        public static void FindAllPaths()
        {
            PowerEntity startElement = null;
            PowerEntity endElement = null;

            List<LineEntity> remainingLines = new List<LineEntity>();

            bool startingPoints = true;

            LineEntity startLine = lines.Find(l => l.Id == 35980);
            lines.Insert(0, startLine);


            foreach (LineEntity line in lines)
            {

                if (startingPoints == false && line.Id.Equals(35980))
                    continue;

                startElement = entities.Find(entity => entity.Id.Equals(line.FirstEnd));
                endElement = entities.Find(entity => entity.Id.Equals(line.SecondEnd));


                List<Tuple<int, int>> path = BFS(startElement, endElement);
                
                if(path == null)
                {
                    remainingLines.Add(line);
                }
                else
                {
                    
                    allPaths.Add(new Tuple<List<Tuple<int, int>>, LineEntity>(path, line));         
                    
                    startingPoints = false;
                }               

            }
           
            allowIntersetions = true;


            foreach (LineEntity line in remainingLines)
            {

                if (startingPoints == false && line.Id.Equals(35980))
                    continue;

                startElement = entities.Find(entity => entity.Id.Equals(line.FirstEnd));
                endElement = entities.Find(entity => entity.Id.Equals(line.SecondEnd));


                List<Tuple<int, int>> path = BFS(startElement, endElement);

                if (path != null) 
                {

                    allPaths.Add(new Tuple<List<Tuple<int, int>>, LineEntity>(path, line));
                    
                    startingPoints = false;
                }

            }

        




        }


        public static List<Tuple<int, int>> BFS(PowerEntity startElement, PowerEntity endElement)
        {
            Tuple<int, int> startPoint = new Tuple<int, int>(FindPositionAtGrid(startElement.Id).Item1, FindPositionAtGrid(startElement.Id).Item2);
            Tuple<int, int> endPoint = new Tuple<int, int>(FindPositionAtGrid(endElement.Id).Item1, FindPositionAtGrid(endElement.Id).Item2);

            Tuple<int, int> currentPoint;

            bool foundPath = false;

            Queue<Tuple<int, int>> queue = new Queue<Tuple<int, int>>();
            List<Tuple<int, int>> path = new List<Tuple<int, int>>();

            

            bool[,] visited = new bool[300, 300];
            

            Dictionary<Tuple<int, int>, Tuple<int, int>> previousPoint = new Dictionary<Tuple<int, int>, Tuple<int, int>>();

            queue.Enqueue(startPoint);
            visited[startPoint.Item1, startPoint.Item2] = true;

            while (queue.Count > 0)
            {
                currentPoint = queue.Dequeue();

                if (abstractGrid[currentPoint.Item1, currentPoint.Item2] != null)
                {
                    if (abstractGrid[currentPoint.Item1, currentPoint.Item2].Id == endElement.Id)
                    {
                        foundPath = true;
                        break;
                    }
                }

              
                CheckCurrentElementChildrens(currentPoint, queue,  visited,  previousPoint);

            }





            if (foundPath)
            {

                path.Add(endPoint);
             
                allVisitedPointsByPath.Add(endPoint.Item1.ToString() + endPoint.Item2.ToString());
               


                Tuple<int, int> pointToCheck = endPoint;
                while (previousPoint.ContainsKey(pointToCheck))
                {

                    if(allowIntersetions && CheckIntersection(previousPoint[pointToCheck].Item1, previousPoint[pointToCheck].Item2))
                    {
                        intersectionPoints.Add(new Tuple<int, int>(previousPoint[pointToCheck].Item1, previousPoint[pointToCheck].Item2));
                    }

                    path.Add(previousPoint[pointToCheck]);
                  

                    allVisitedPointsByPath.Add(previousPoint[pointToCheck].Item1.ToString() + previousPoint[pointToCheck].Item2.ToString());
                    pointToCheck = previousPoint[pointToCheck];
                }

                path.Add(startPoint);
              
                allVisitedPointsByPath.Add(startPoint.Item1.ToString() + startPoint.Item2.ToString());

            
                return path;
            }



            return null;

        }

        public static void CheckCurrentElementChildrens(Tuple<int, int> currentElement, Queue<Tuple<int, int>> queue,  bool[,] visitedPositions,  Dictionary<Tuple<int, int>, Tuple<int, int>> storePoint)
        {
            int[] iarr = { -1, +1, 0, 0 };
            int[] jarr = {  0,  0, -1, +1};



            for (int i = 0; i < 4; i++)
            {
              
                int newX = currentElement.Item1 - iarr[i];
                int newY = currentElement.Item2 - jarr[i];


                if(!allowIntersetions)
                {

                    if (IsChildPositionValid(newX, newY) && !visitedPositions[newX, newY] && !CheckIntersection(newX, newY))
                    {

                        queue.Enqueue(new Tuple<int, int>(newX, newY));
                        visitedPositions[newX, newY] = true;

                        storePoint[new Tuple<int, int>(newX, newY)] = currentElement;
                    }
                    


                }else
                {    

                    if (IsChildPositionValid(newX, newY) && !visitedPositions[newX, newY])
                    {


                        if (CheckOverlap(currentElement.Item1, currentElement.Item2, newX, newY))
                            break;


                        queue.Enqueue(new Tuple<int, int>(newX, newY));
                        visitedPositions[newX, newY] = true;

                        storePoint[new Tuple<int, int>(newX, newY)] = currentElement;
                    }

                
                }
              

            }
        }

        public static bool CheckIntersection(int potentialNextX, int potentialNextY)
        {
            
            return allVisitedPointsByPath.Contains(potentialNextX.ToString() + potentialNextY.ToString());

        }

        public static bool CheckOverlap(int currentX, int currentY, int newX, int newY)
        {

            if( allVisitedPointsByPath.Contains(currentX.ToString() + currentY.ToString()) && allVisitedPointsByPath.Contains(newX.ToString() + newY.ToString())) 
                return true;
            else
                return false;


        }

        public static bool IsChildPositionValid(int newX, int newY)
        {
            return (newX > gridSize || newY > gridSize || newX < 0 || newY < 0) ? false : true;
        }

        public static Tuple<int, int> FindPositionAtGrid(long id)
        {


            for(int i = 0; i < 300; i++)
            {
                for(int j = 0; j < 300; j++)
                {
                    if(abstractGrid[i, j] != null && abstractGrid[i,j].Id == id)
                    {
                        return new Tuple<int, int>(i, j);
                    }
                }
            }

       

            return null;
        }

        public static int CalculateScaledXCoordinate(double oldX)
        {
            return Convert.ToInt32((((oldX - minX) * gridSize) / xMinMaxDifference));
           
        }

        public static int CalculateScaledYCoordinate(double oldY)
        {
            return Convert.ToInt32((((oldY - minY) * gridSize) / yMinMaxDifference));
        }

        private void MainCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {

            if(paintedElementA != null)
            {
                Ellipse ellipseA = paintedElementA.Item1;
                ellipseA.Fill = paintedElementA.Item2;

              
            }

            if (paintedElementB != null)
            {
                Ellipse ellipseB = paintedElementB.Item1;
                ellipseB.Fill = paintedElementB.Item2;

               
            }



            if (e.OriginalSource is Polyline)
            {


                MessageBoxResult result = MessageBox.Show("Do you want to change color of connected nodes to Orange? ", "PZ2", MessageBoxButton.YesNoCancel);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        {
                            Tuple<double, double> pointA, pointB;



                            Polyline polyline = (Polyline)e.OriginalSource;

                            pointA = new Tuple<double, double>(polyline.Points[0].X, polyline.Points[0].Y);
                            pointB = new Tuple<double, double>(polyline.Points[polyline.Points.Count - 1].X, polyline.Points[polyline.Points.Count - 1].Y);


                            var element1 = mainCanvas.InputHitTest(new System.Windows.Point(pointA.Item1, pointA.Item2));
                            var element2 = mainCanvas.InputHitTest(new System.Windows.Point(pointB.Item1, pointB.Item2));

                            if (element1 is Ellipse && element2 is Ellipse)
                            {

                                paintedElementA = new Tuple<Ellipse, Brush>(((Ellipse)element1), ((Ellipse)element1).Fill);

                                ((Ellipse)element1).Fill = Brushes.Orange;
                               




                                paintedElementB = new Tuple<Ellipse, Brush>(((Ellipse)element2), ((Ellipse)element2).Fill);
                                ((Ellipse)element2).Fill = Brushes.Orange;
                              



                            }
                            break;
                        }
                    case MessageBoxResult.No:
                        break;
                    case MessageBoxResult.Cancel: 
                        break;
                }

                

               
            }
        }   

        public static double CalculateXScaledToCanvas(double oldX)
        {
            double ratio = canvasWidth / gridSize;
            return oldX * ratio;
        }

        public static double CalculateYScaledToCanvas(double oldY)
        {
            double ratio = canvasHeight / gridSize;
            return oldY * ratio;
        }

 

        //From UTM to Latitude and longitude in decimal
        public static void ToLatLon(double utmX, double utmY, int zoneUTM, out double latitude, out double longitude)
        {
            bool isNorthHemisphere = true;

            var diflat = -0.00066286966871111111111111111111111111;
            var diflon = -0.0003868060578;

            var zone = zoneUTM;
            var c_sa = 6378137.000000;
            var c_sb = 6356752.314245;
            var e2 = Math.Pow((Math.Pow(c_sa, 2) - Math.Pow(c_sb, 2)), 0.5) / c_sb;
            var e2cuadrada = Math.Pow(e2, 2);
            var c = Math.Pow(c_sa, 2) / c_sb;
            var x = utmX - 500000;
            var y = isNorthHemisphere ? utmY : utmY - 10000000;

            var s = ((zone * 6.0) - 183.0);
            var lat = y / (c_sa * 0.9996);
            var v = (c / Math.Pow(1 + (e2cuadrada * Math.Pow(Math.Cos(lat), 2)), 0.5)) * 0.9996;
            var a = x / v;
            var a1 = Math.Sin(2 * lat);
            var a2 = a1 * Math.Pow((Math.Cos(lat)), 2);
            var j2 = lat + (a1 / 2.0);
            var j4 = ((3 * j2) + a2) / 4.0;
            var j6 = ((5 * j4) + Math.Pow(a2 * (Math.Cos(lat)), 2)) / 3.0;
            var alfa = (3.0 / 4.0) * e2cuadrada;
            var beta = (5.0 / 3.0) * Math.Pow(alfa, 2);
            var gama = (35.0 / 27.0) * Math.Pow(alfa, 3);
            var bm = 0.9996 * c * (lat - alfa * j2 + beta * j4 - gama * j6);
            var b = (y - bm) / v;
            var epsi = ((e2cuadrada * Math.Pow(a, 2)) / 2.0) * Math.Pow((Math.Cos(lat)), 2);
            var eps = a * (1 - (epsi / 3.0));
            var nab = (b * (1 - epsi)) + lat;
            var senoheps = (Math.Exp(eps) - Math.Exp(-eps)) / 2.0;
            var delt = Math.Atan(senoheps / (Math.Cos(nab)));
            var tao = Math.Atan(Math.Cos(delt) * Math.Tan(nab));

            longitude = ((delt * (180.0 / Math.PI)) + s) + diflon;
            latitude = ((lat + (1 + e2cuadrada * Math.Pow(Math.Cos(lat), 2) - (3.0 / 2.0) * e2cuadrada * Math.Sin(lat) * Math.Cos(lat) * (tao - lat)) * (tao - lat)) * (180.0 / Math.PI)) + diflat;
        }


    }
}
