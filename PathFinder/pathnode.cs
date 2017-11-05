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
    * Defines a path from one node to another 
    ***************************************************************************/
    [Serializable]
    public class PathLink
    {
        public PathLink()
        {
            _distance = 1.0F;
        }
        public PathLink(float d = 1.0f)
        {
            Distance = d;
        }

        private float _distance;

        public float Distance { get => _distance; set => _distance = value; }
    }

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
        public Color colorDefault   = Color.WhiteSmoke;
        public Color colorSelected  = Color.Gold;
        public Color colorStartNode = Color.RoyalBlue;
        public Color colorEndNode   = Color.Crimson;
        public Color colorVisited   = Color.DimGray;

        protected Rectangle _boundingRectangle;
        protected Size nodeSize = new Size(25, 25);

        // Holds the path links between this node and another
        // [Key]: Id of attached node
        // [Value]: path structure
        protected Dictionary<int, PathLink> _nodeLinks = new Dictionary<int, PathLink>();

        // Returns the collection of paths from this node
        public Dictionary<int, PathLink> NodePaths { get => _nodeLinks; }


        // ====================================
        // MAIN PROPERTIES OF OUR NODE
        // ====================================

        // Coordinates of this node 
        public int Id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public Size NodeSize { get => nodeSize; set => nodeSize = value; }
        public bool IsSelected { get; set; }

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

        public bool HasLinks()
        {
            return (_nodeLinks.Count > 0);
        }

        public void DeleteAllLinks()
        {
            _nodeLinks.Clear();
        }


        /**************************************************************************
        * [A] --> [B]
        ***************************************************************************/
        public void AddLinkTo(ref PathNode n, float distance = 1.0F)
        {
            // calculate distance between nodes
            float dist = (float)Math.Sqrt(Math.Abs(this.X - n.X) + Math.Abs(this.Y - n.Y));
            PathLink newlink = new PathLink(dist);

            _nodeLinks[n.Id] = newlink;
        }

        public void DeleteNodeLink(int id)
        {
            _nodeLinks.Remove(id);
        }


        /**************************************************************************
        * Returns the distance between this node and the given node. 
        * 
        * REMARKS:
        *   The given node *must* be a defined linked node.
        *   If it is not, then float.MaxValue is returned.
        ***************************************************************************/
        public float GetDistanceFrom(ref PathNode b)
        {
            if (b == null)
                return float.MaxValue;

            PathLink aPath;
            if (_nodeLinks.TryGetValue(b.Id, out aPath))
                return aPath.Distance;

            // If we are measuring distance to ourself (same node), then return zero.
            if (this == b)
                return 0.0f;

            return float.MaxValue;
        }


        // test if the given point in within our node
        public bool HitTest(Point p)
        {
            return _boundingRectangle.Contains(p);
        }


        /**************************************************************************
        * Render this node to the given graphics object 
        ***************************************************************************/
        public void Draw(ref Graphics g)
        {
            // labels
            // Label graphic objects
            Font labelFont = new Font("Tahoma", 10.0F);
            Brush labelBrush = new SolidBrush(Color.Khaki);

            // Initalise pen and its colour
            // The color will depend on its current state. Lets determine its colour based on state precidence
            Pen myPen;
            Color penColor = colorDefault;

            if (Visited)
                penColor = colorVisited;

            if (EndNode)
                penColor = colorEndNode;

            if (StartNode)
                penColor = colorStartNode;

            if (IsSelected)
                penColor = colorSelected;


            // Draw our node as a circle
            myPen = new Pen(penColor);
            myPen.Width = 4.0F;
            g.DrawEllipse(myPen, X - nodeSize.Width / 2, Y - nodeSize.Height / 2, nodeSize.Width, nodeSize.Height);

            // Draw the node's score
            // Display, just about the bounding rectangle
            if (this.Score < float.MaxValue)
            {
                String label = $"Id: {this.Id}\r\nScore: {this.Score:0.#}";
                SizeF labelSize = g.MeasureString(label, labelFont);

                g.DrawString(label,
                                labelFont,
                                labelBrush,
                                X - (this.nodeSize.Width / 2) - (int)labelSize.Width,
                                Y - (this.NodeSize.Height / 2) - (int)labelSize.Height - 4);
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
