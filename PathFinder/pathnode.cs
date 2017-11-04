using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathFinder
{

    /**************************************************************************
    * Represents a node in our system 
    ***************************************************************************/
    public class aPathNode
    {
        public aPathNode(int id, int x = 1, int y = 1)
        {
            Id = id;
            SetPosition(x, y);
        }

        private Rectangle _boundingRectangle;
        private Size nodeSize = new Size(25, 25);

        // 
        private HashSet<int> _nodelinks = new HashSet<int>();


        // Coordinates of this node 
        public int Id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public Size NodeSize { get => nodeSize; set => nodeSize = value; }

        public bool IsSelected { get; set; }

        // readonly 
        public HashSet<int> NodeLinks {
            get { return _nodelinks;  }
        }

        void updateBoundingRect()
        {
            _boundingRectangle = new Rectangle(X - nodeSize.Width / 2, Y - nodeSize.Height / 2, nodeSize.Width, nodeSize.Height);
        }

        public void SetPosition(Point p)
        {
            SetPosition(p.X, p.Y);
        }

        public void SetPosition(int x, int y)
        {
            X = x;
            Y = y;
            updateBoundingRect();
        }

        public Point GetPosition()
        {
            return new Point(X, Y);
        }

        public bool HasNodeLinks()
        {
            return (_nodelinks.Count > 0);
        }

        public void DeleteAllLinks()
        {
            _nodelinks.Clear();
        }

        public void AddNodeLink(ref aPathNode n)
        {
            AddNodeLink(n.Id);
        }

        public void AddNodeLink(int id)
        {
            _nodelinks.Add(id);
        }

        public void DeleteNodeLink(int id)
        {
            _nodelinks.Remove(id);
        }

        // test if the given point in within our node
        public bool HitTest(Point p)
        {
            return _boundingRectangle.Contains(p);
        }

        // Render this node to the given graphic object
        public void Draw(ref Graphics g)
        {
            Pen myPen;
            if (IsSelected)
                myPen = new Pen(Color.Gold);
            else
                myPen = new Pen(Color.White);

            g.DrawEllipse(myPen, X - nodeSize.Width / 2, Y - nodeSize.Height / 2, nodeSize.Width, nodeSize.Height);
            myPen.Dispose();
        }

        public override bool Equals(object obj)
        {
            var pathnode = obj as aPathNode;
            return pathnode != null &&
                   Id == pathnode.Id;
        }

        public override int GetHashCode()
        {
            return 2108858624 + Id.GetHashCode();
        }
    }
}
