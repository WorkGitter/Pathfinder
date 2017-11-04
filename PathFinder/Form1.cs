using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PathFinder
{
    public partial class Form1 : Form
    {
        private int _nextkeyid = 0;
        private Dictionary<int, aPathNode> _nodecollection = new Dictionary<int, aPathNode>();

        // mouse movement
        private bool _actionMovingNode = false;
        private bool _actionNextLinkNode = false;
        private Point _mouseposition;



        // selection
        // ids of items selected.
        // Items can be selected by holding down the Ctrl key while clicking
        private HashSet<int> _selectionNodes = new HashSet<int>();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        /**************************************************************************
        * Returns a new node 
        * Automatically assigns a unique id
        ***************************************************************************/
        private aPathNode CreateNewNode(int x = 1, int y = 1)
        {
            return CreateNewNode(new Point(x, y));
        }

        private aPathNode CreateNewNode(Point p)
        {
            return new aPathNode(_nextkeyid++, p.X, p.Y);
        }

        private aPathNode GetNodeWithId(int id)
        {
            aPathNode node;
            if (_nodecollection.TryGetValue(id, out node))
                return node;

            return null;
        }

        //
        private void pbCanvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            foreach(var node in _nodecollection)
            {
                node.Value.Draw(ref graphics);

                // draw linked node arrows
                if(node.Value.HasNodeLinks())
                {
                    Point a = node.Value.GetPosition();
                    foreach(var edgeId in node.Value.NodeLinks)
                    {
                        aPathNode edge = GetNodeWithId(edgeId);
                        if (edge == null)
                            continue;

                        Point b = edge.GetPosition();
                        graphics.DrawLine(new Pen(Color.Blue), a, b);
                    }
                }
            }


            // 
            if (_actionNextLinkNode)
            {
                Pen linepen = new Pen(Color.LightYellow);
                foreach (var startnodeid in _selectionNodes)
                {
                    aPathNode startnode = _nodecollection[startnodeid];
                    e.Graphics.DrawLine(linepen, startnode.GetPosition(), _mouseposition);
                }

                linepen.Dispose();
            }

        }


        // Add a new node
        private void pbCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            if(pbCanvas.ClientRectangle.Contains(e.Location))
            {
                _mouseposition = e.Location;
                // see if we have a hit on any existing nodes
                int hittestid = -1;
                foreach (var node in _nodecollection)
                {
                    if(node.Value.HitTest(e.Location))
                    {
                        hittestid = node.Value.Id;
                        break;
                    }
                }

                // TODO Describ
                if (hittestid >= 0)
                {
                    if (Control.ModifierKeys.HasFlag(Keys.Control))
                    {
                        aPathNode n = _nodecollection[hittestid];
                        addSelectedItem(ref n);
                        _nodecollection.Remove(hittestid);
                        _nodecollection.Add(hittestid, n);

                        _actionNextLinkNode = true;

                        pbCanvas.Invalidate();
                    }
                    else
                    {
                        // if no control keys pressed, then select only our item.
                        aPathNode n = _nodecollection[hittestid];

                        clearSelectedItems();
                        addSelectedItem(ref n);
                        _nodecollection.Remove(hittestid);
                        _nodecollection.Add(hittestid, n);

                        _actionMovingNode = true;
                        pbCanvas.Invalidate();
                    }

                    return;
                }

                clearSelectedItems();

                // add new node if Shift Key is pressed
                if (Control.ModifierKeys.HasFlag(Keys.Shift))
                {
                    aPathNode newnode = CreateNewNode(e.Location);
                    _nodecollection.Add(newnode.Id, newnode);
                }
                pbCanvas.Invalidate();
            }
        }

        private void addSelectedItem(ref aPathNode node)
        {
            _selectionNodes.Add(node.Id);
            node.IsSelected = true;
        }

        private void removeSelectedItem(ref aPathNode node)
        {
            _selectionNodes.Remove(node.Id);
            node.IsSelected = false;
        }

        private void clearSelectedItems()
        {
            if (_selectionNodes.Count == 0)
                return; 

            foreach (var node in _nodecollection)
            {
                node.Value.IsSelected = false;
            }

            _selectionNodes.Clear();
        }

        private void pbCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_actionNextLinkNode)
            {
                _mouseposition = e.Location;
                pbCanvas.Invalidate();
            }

            if(_actionMovingNode)
            {
                _mouseposition = e.Location;

                if(_selectionNodes.Count == 1)
                {
                    // get node of selected item and update
                    aPathNode n = _nodecollection[_selectionNodes.First()];
                    n.SetPosition(_mouseposition);
                    pbCanvas.Invalidate();
                }
            }
        }

        private void pbCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (_actionNextLinkNode)
            {
                _actionNextLinkNode = false;
                // see if we have a hit on any existing nodes
                int hittestid = -1;
                foreach (var node in _nodecollection)
                {
                    if (node.Value.HitTest(e.Location))
                    {
                        hittestid = node.Value.Id;
                        break;
                    }
                }

                if (hittestid >= 0)
                {
                    // TODO: Wrap in safe function
                    aPathNode endnode = _nodecollection[hittestid];

                    // add our edge node to our selection
                    foreach (var startnodeid in _selectionNodes)
                    {
                        aPathNode startnode = _nodecollection[startnodeid];
                        startnode.AddNodeLink(ref endnode);
                    }

                }

                pbCanvas.Invalidate();
            }

            if (_actionMovingNode)
            {
                _actionMovingNode = false;
            }

        }
    }
}
