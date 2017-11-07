//=========================================================
// Author   : Richard Chin
// Date     : November 2017
//========================================================= 

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace PathFinder
{
    /**************************************************************************
    * Represents a node in our system 
    ***************************************************************************/
    [Serializable]
    public class PathNode
    {
        public PathNode()
        {
            Id = -1;
        }

        // Constructor
        public PathNode(int id, int x = 1, int y = 1)
        {
            Id = id;
            SetPosition(x, y);
        }

        // Colors
        public Color colorSelected = Color.Gold;
        public Color colorStartNode = Color.RoyalBlue;
        public Color colorEndNode   = Color.Crimson;
        public Color colorVisited   = Color.DimGray;
        public Color colorBorder    = Color.FromArgb(86, 88, 87);
        public Color colorDefault   = Color.FromArgb(216, 212, 207);

        protected Rectangle _boundingRectangle;
        protected Size nodeSize = new Size(35, 35);

        // ====================================
        // MAIN PROPERTIES OF OUR NODE
        // ====================================

        // Coordinates of this node 
        public int Id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public Size NodeSize { get => nodeSize; set => nodeSize = value; }
        public bool IsSelected { get; set; }

        // Returns the bounding rectangle of this node
        public Rectangle BoundingRect { get { return _boundingRectangle;  } }

        // Pathfinding
        public bool StartNode { get; set; } = false;        // 
        public bool EndNode { get; set; } = false;          // 
        public bool Visited { get; set; } = false;          // indicates if this node has already been included in the search
        public float Score { get; set; } = float.MaxValue;  // give the maximum distance score to start
        public int PreviousNodeId { get; set; } = -1;       // previous node that we came from



        /**************************************************************************
        * recalculates the bounding rectangle for our current location 
        ***************************************************************************/
        void updateBoundingRect()
        {
            _boundingRectangle = new Rectangle(X - nodeSize.Width / 2, Y - nodeSize.Height / 2, nodeSize.Width, nodeSize.Height);
        }


        /**************************************************************************
        * Sets the location of our node on the canvas 
        ***************************************************************************/
        public void SetPosition(Point p) { SetPosition(p.X, p.Y); }
        public void SetPosition(int x, int y)
        {
            X = x;
            Y = y;
            updateBoundingRect();
        }

        /**************************************************************************
        * Returns the location of our position on the canvas 
        ***************************************************************************/
        public Point GetPosition()
        {
            return new Point(X, Y);
        }
        
        // test if the given point in within our node
        public bool HitTest(Point p)
        {
            return _boundingRectangle.Contains(p);
        }


        /**************************************************************************
        * Render this node to the given graphics object 
        ***************************************************************************/
        public void Draw(ref Graphics g, bool showlabels = true)
        {
            // labels
            // Label graphic objects
            Font labelFont = new Font("Tahoma", 10.0F);
            Brush labelBrush = new SolidBrush(Color.Khaki);

            // Initalise pen and its colour
            // The color will depend on its current state. Lets determine its colour based on state precidence
            Pen myPen;
            Color penColor = colorDefault;
            Color penBorder = colorBorder;

            if (Visited)
                penColor = colorVisited;

            if (EndNode)
                penColor = colorEndNode;

            if (StartNode)
                penColor = colorStartNode;

            if (IsSelected)
                penBorder = colorSelected;


            // Draw our node as a circle
            Brush myBrush = new SolidBrush(penColor);
            myPen = new Pen(penBorder);
            myPen.Width = 3.0F;

            int circumference = NodeSize.Width;
            int radius = circumference / 2;

            g.DrawEllipse(myPen, X - radius, Y - radius, circumference, circumference);

            circumference = (circumference * 1) / 2;
            radius = circumference / 2;
            g.FillEllipse(myBrush, X - radius, Y - radius, circumference, circumference);

            // Draw the node's score
            // Display, just about the bounding rectangle
            if (showlabels)
            {
                if (this.Score < float.MaxValue)
                {
                    //String label = $"Id: {this.Id}\r\nScore: {this.Score:0.#}";
                    String label = $"Score: {this.Score:0.#}";
                    SizeF labelSize = g.MeasureString(label, labelFont);

                    g.DrawString(label,
                                    labelFont,
                                    labelBrush,
                                    X - (this.nodeSize.Width / 2) - (int)labelSize.Width,
                                    Y - (this.NodeSize.Height / 2) - (int)labelSize.Height - 4);
                }
            }

            myPen.Dispose();
            labelFont.Dispose();
            labelBrush.Dispose();
        }

        public override bool Equals(object obj)
        {
            var pathnode = obj as PathNode;
            return pathnode != null &&
                   Id == pathnode.Id;
        }

        public override int GetHashCode()
        {
            return 2108858624 + Id.GetHashCode();
        }
    }
}
