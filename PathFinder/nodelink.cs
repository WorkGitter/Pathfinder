using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Drawing2D;
using System.Diagnostics;

namespace PathFinder
{
    /**************************************************************************
    * Link between two nodes 
    ***************************************************************************/
    [Serializable]
    public class NodeLink
    {
        public enum DirectionType
        {
            biDirectional,
            uniDirectional
        }

        public NodeLink()
        {
            StartNodeId = EndNodeId = -1;
            Distance = 1.0F;
            Direction = DirectionType.biDirectional;
        }

        public NodeLink(PathNode start, PathNode end, DirectionType t = DirectionType.uniDirectional)
        {
            StartNodeId = start.Id;
            EndNodeId = end.Id;
            Direction = t;
            UpdateConnection(start, end);
        }

        public int StartNodeId { get; set; }
        public int EndNodeId { get; set; }
        public float Distance { get; set; }
        public DirectionType Direction { get; set; }

        private Point _startPoint { get; set; }
        private Point _endPoint { get; set; }

        // selection rectangle
        private Rectangle _selectionRect = new Rectangle();

        public override bool Equals(object obj)
        {
            var link = obj as NodeLink;
            return link != null &&
                   StartNodeId == link.StartNodeId &&
                   EndNodeId == link.EndNodeId;
        }

        public override int GetHashCode()
        {
            var hashCode = 1961402845;
            hashCode = hashCode * -1521134295 + StartNodeId.GetHashCode();
            hashCode = hashCode * -1521134295 + EndNodeId.GetHashCode();
            return hashCode;
        }


        /**************************************************************************
        * Used to set the location points of the connecting nodes. 
        * Also used to calculate the distance between them.
        ***************************************************************************/
        public void UpdateConnection(PathNode startNode, PathNode endNode)
        {
            // These nodes *must* already be referenced here;
            if ((startNode.Id != StartNodeId) || (EndNodeId != endNode.Id))
            {
                Debug.Assert(false);
                return;
            }

            _startPoint = startNode.GetPosition();
            _endPoint = endNode.GetPosition();

            int horzDist = (_endPoint.X - _startPoint.X);
            int vertDist = (_endPoint.Y - _startPoint.Y);

            Distance = (float)Math.Sqrt(horzDist * horzDist + vertDist * vertDist);

            // update selection rect
            Point middle = new Point(_startPoint.X + (horzDist / 2), _startPoint.Y + (vertDist / 2));
            _selectionRect.X = middle.X - 5;
            _selectionRect.Y = middle.Y - 5;
            _selectionRect.Height = 10;
            _selectionRect.Width = 10;
        }


        /*********************************************************************
         * EXPERIMENTAL
         * I wanted to colour code the connecting links based on its distance.
         * I havent got it working properly, so commenting out for now.
         * 
         * NB: Dont like the idea of continually creating new Pen resource. Wonder
         * how this affect performance?
         * 
         * 
         *
         
        // DRAW THE NODE LINK LINES
        //
        float maxdist = (float)Math.Sqrt((pbCanvas.Height * pbCanvas.Height) + (pbCanvas.Width * pbCanvas.Width));

        // Calculate colour of link, based on its distance
        float ratio = 1.0f - (link.Value.Distance / maxdist);
        if (ratio < 0.0) ratio = 0.0f;
        if (ratio > 1.0f) ratio = 1.0f;

        Pen colourPen = new Pen(Color.FromArgb((int)((1 - ratio) * 255), (int)(ratio * 255), 0));

        colourPen.CustomEndCap = deepArrow;
        colourPen.Width = 3.0F;

        if (link.Value.Direction == NodeLink.DirectionType.biDirectional)
            colourPen.CustomStartCap = deepArrow;

        graphics.DrawLine(colourPen, sN.GetPosition(), eN.GetPosition());
        colourPen.Dispose();
        *********************************************************************/
        /**************************************************************************
        * Renders this line to the screen 
        ***************************************************************************/
        public void Draw(ref Graphics graphics, bool showlabels = true )
        {
            // graphic objects
            using (Font labelFont = new Font("Tahoma", 10.0F))
            using (Brush labelBrush = new SolidBrush(Color.LightSeaGreen))
            using (Brush selectionBrush = new SolidBrush(Color.Gold))
            using (Pen linePen = new Pen(Color.OliveDrab))
            {
                AdjustableArrowCap deepArrow = new AdjustableArrowCap(6, 6, true);

                linePen.CustomEndCap = deepArrow;
                linePen.Width = 1.5F;
                if (Direction == DirectionType.biDirectional)
                {
                    linePen.CustomStartCap = deepArrow;
                    linePen.Color = Color.MediumAquamarine;
                }

                // Draw line between the two nodes.
                // We have two options.
                // The first and easiest is to just draw the line between the two points.
                // The send option, calculates where the line intersects the node's bounding circle.
                // We then draw only to that point.
                // I have tried the first, now I will tackle the second.

                // get noode points
                Point sPoint = GetIntersectionPoint(_startPoint, _endPoint);
                Point ePoint = GetIntersectionPoint(_endPoint, _startPoint);

                graphics.DrawLine(linePen, sPoint, ePoint);
                graphics.FillRectangle(new SolidBrush(linePen.Color), _selectionRect);



                // Draws the path label. This will be the distance between nodes, as stored in the label.
                // We want our string to be positions midway between the nodes.
                //
                // (b)
                //    \[label]
                //     \
                //     (a)
                // 
                if (showlabels)
                {
                    String label = $"{Distance:0.#}";
                    SizeF labelSize = graphics.MeasureString(label, labelFont);

                    int midX = Math.Abs(ePoint.X - sPoint.X) / 2 + Math.Min(sPoint.X, ePoint.X);
                    int midY = Math.Abs(ePoint.Y - sPoint.Y) / 2 + Math.Min(sPoint.Y, ePoint.Y);
                    Point labelPoint = new Point(midX - (int)labelSize.Width, midY - (int)labelSize.Height);

                    graphics.DrawString(label, labelFont, labelBrush, labelPoint);
                }

            } // using

        } // Draw


        /**************************************************************************
        * Returns the intersection point for the given node 
        ***************************************************************************/
        private Point GetIntersectionPoint(Point nodePoint, Point p)
        {
            int xDist = p.X - nodePoint.X;              // horizontal distance between points
            int yDist = p.Y - nodePoint.Y;              // vertial distance between points
            double d = Math.Sqrt((xDist * xDist) + (yDist * yDist)); // 

            // get radius of our node
            double r = (new PathNode()).NodeSize.Width / 2.0;


            Point iPoint = new Point();
            iPoint.X = (int)((xDist * r) / d) + nodePoint.X;
            iPoint.Y = (int)((yDist * r) / d) + nodePoint.Y;

            return iPoint;

        } // GetInsersectionPoint


    }

}
