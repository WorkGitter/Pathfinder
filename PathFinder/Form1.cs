//=========================================================
// Author   : Richard Chin
// Date     : November 2017
//========================================================= 

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PathFinder
{
    public partial class Form1 : Form
    {
        // main collection of nodes
        // [node id] --> [node structure]
        private int _nextkeyid = 0;
        private Dictionary<int, PathNode> _nodecollection = new Dictionary<int, PathNode>();

        // main collection of node links
        // [src-trgt node id pair] --> [link object]
        private Dictionary<KeyValuePair<int, int>, NodeLink> _linkcollection = new Dictionary<KeyValuePair<int, int>, NodeLink>();


        // mouse movement
        private bool _mousedown = false;
        private bool _actionMovingNode = false;
        private bool _actionConnectingPath = false;
        private Point _mouseposition;

        // selection
        // ids of items selected.
        // Items can be selected by holding down the Ctrl key while clicking
        private HashSet<int> _selectionNodes = new HashSet<int>();

        // Path finding variables

        // stores the nodes (id) that we have not visited.
        private HashSet<int> _unvisitedNodesSet = new HashSet<int>();
        private int _startNodeId = -1;
        private int _endNodeId = -1;



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
        private PathNode CreateNewNode(int x = 1, int y = 1)
        {
            return CreateNewNode(new Point(x, y));
        }

        private PathNode CreateNewNode(Point p)
        {
            return new PathNode(_nextkeyid++, p.X, p.Y);
        }

        /////////////////////////////////////////////
        /// <summary>
        /// Finds the node with the given Id.
        /// </summary>
        /// <param name="id">Id of node</param>
        /// <returns>null if none found</returns>
        ////////////////////////////////////////////
        private PathNode GetNodeWithId(int id)
        {
            PathNode node;
            if (_nodecollection.TryGetValue(id, out node))
                return node;

            return null;
        }


        /**************************************************************************
        * Adds the given node to our selected list 
        ***************************************************************************/
        private void addSelectedItem(ref PathNode node)
        {
            if (node == null)
                return;

            _selectionNodes.Add(node.Id);
            node.IsSelected = true;
        }

        private void removeSelectedItem(ref PathNode node)
        {
            if (node== null)
                return;

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

        private void selectAllItems()
        {
            foreach (var node in _nodecollection)
            {
                node.Value.IsSelected = true;
                _selectionNodes.Add(node.Value.Id);
            }
        }


        /**************************************************************************
        * Returns the node id that is at the given position 
        * If no node present, it returns -1.
        ***************************************************************************/
        private int HitTest_NodeId(Point p)
        {
            int hittestid = -1;
            foreach (var node in _nodecollection)
            {
                if (node.Value.HitTest(p))
                {
                    hittestid = node.Value.Id;
                    break;
                }
            }
            return hittestid;
        }


        /**************************************************************************
        * Handler when the mouse button is pressed. 
        * 
        * We use this to :
        * - select an items (Ctrl+Click)
        * - Deselect (Whitespace Click)
        * - Add new node (Shift+Click)
        * 
        ***************************************************************************/
        private void pbCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            _mouseposition = e.Location;

            if (e.Button == MouseButtons.Left)
            {
                // flag to indicate the mouse button is down
                _mousedown = true; 

                // No need to continue if the mouse click is outside the bounds of our canvas
                if (!pbCanvas.ClientRectangle.Contains(e.Location))
                    return;


                // Only select the current item, except if the Ctrl key is pressed.
                // We can use the Ctrl key to multi-select nodes.
                if (!Control.ModifierKeys.HasFlag(Keys.Control))
                    clearSelectedItems();

                // Bounds check for any nodes under the mouse position
                // This returns the id of the node. -1 otherwise.
                // If we have clicked in any whitespace, then de-select any existing items.
                int hittestid = HitTest_NodeId(_mouseposition);
                
                // We have an item that needs selecting.
                if (hittestid >= 0)
                {
                    // if no control keys pressed, then select only our item.
                    if (!Control.ModifierKeys.HasFlag(Keys.Shift))
                        _actionMovingNode = true;

                    PathNode n = _nodecollection[hittestid];
                    addSelectedItem(ref n);
                }

                pbCanvas.Invalidate();
            }

        } // private void pbCanvas_MouseDown(object sender, MouseEventArgs e)

        private void pbCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            // Drawing connecting lines
            if (_actionConnectingPath || (_mousedown && !_actionMovingNode))
            {
                _actionConnectingPath = true;
                _mouseposition = e.Location;
                pbCanvas.Invalidate();
            }

            // Moving selected items
            // We could have multiple selected items, so move relative to the current cursor.
            if(_actionMovingNode)
            {
                int offsetX = e.X - _mouseposition.X;
                int offsetY = e.Y - _mouseposition.Y;
                _mouseposition = e.Location;

                foreach (var selId in _selectionNodes)
                {
                    PathNode selNode = _nodecollection[selId];
                    Point p = selNode.GetPosition();
                    p.X += offsetX;
                    p.Y += offsetY;

                    selNode.SetPosition(p);
                    pbCanvas.Invalidate();
                }
            }
        }

        private void pbCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            _mousedown = false;

            // We need to create and define a path link between our active nodes
            if (_actionConnectingPath)
            {
                _actionConnectingPath = false;

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

                // If we have a hit. We need to create a new link between the two
                if (hittestid >= 0)
                {
                    foreach (var startnodeid in _selectionNodes)
                    {
                        addLinksForNode(startnodeid, hittestid);
                    }
                }

                pbCanvas.Invalidate();
            } // if (_actionConnectingPath)


            // MOVE
            // For items that have been moved, we need to update the distances for any links.
            if (_actionMovingNode)
            {
                _actionMovingNode = false;

                // update path distances of any items that were moved.
                foreach (var startnodeid in _selectionNodes)
                {
                    updateLinkDistancesForNode(startnodeid);
                }

                pbCanvas.Invalidate();
            } // if (_actionMovingNode)

        } // pbCanvas_MouseUp


        /**************************************************************************
        * PictureBox paint handler 
        * 
        * Responsible for rendering the graph to the screen.
        * 
        ***************************************************************************/
        private void pbCanvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Pen pathLinePen = new Pen(Color.WhiteSmoke);
            Pen drawingLinePen = new Pen(Color.Coral);
            Pen solutionLinePen = new Pen(Color.RoyalBlue);

            pathLinePen.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;
            pathLinePen.Width = 3.0F;
            drawingLinePen.Width = 3.0F;
            solutionLinePen.Width = 4.0F;

            // Label graphic objects
            Font labelFont = new Font("Tahoma", 10.0F);
            Brush labelBrush = new SolidBrush(Color.LightSeaGreen);


            // Draw the node link lines
            foreach(var link in _linkcollection)
            {
                PathNode sN = GetNodeWithId(link.Value.StartNodeId);
                PathNode eN = GetNodeWithId(link.Value.EndNodeId);
                if ((sN == null) || (eN == null))
                    continue;

                graphics.DrawLine(pathLinePen, sN.GetPosition(), eN.GetPosition());

                // Draws the path label. This will be the distance between nodes, as stored in the label.
                // We want our string to be positions midway between the nodes.
                //
                // (b)
                //    \[label]
                //     \
                //     (a)
                // 
                String label = $"{link.Value.Distance:0.#}";
                SizeF labelSize = graphics.MeasureString(label, labelFont);

                int midX = Math.Abs(eN.X - sN.X) / 2 + Math.Min(sN.X, eN.X);
                int midY = Math.Abs(eN.Y - sN.Y) / 2 + Math.Min(sN.Y, eN.Y);
                Point labelPoint = new Point(midX - (int)labelSize.Width, midY - (int)labelSize.Height);

                graphics.DrawString(label, labelFont, labelBrush, labelPoint);
            }

            // Draw our nodes
            foreach (var node in _nodecollection)
            {
                node.Value.Draw(ref graphics);
            }

            // SOLUTION PATH LINK
            // If we have a solution path, then draw the lines here, using a different 
            // colour ?
            if(_endNodeId >=0)
            {
                PathNode currentnode = GetNodeWithId(_endNodeId);
                if (currentnode != null)
                {
                    while (currentnode.PreviousNodeId >= 0)
                    {
                        PathNode prevNode = GetNodeWithId(currentnode.PreviousNodeId);
                        if (prevNode == null)
                            break;

                        graphics.DrawLine(solutionLinePen, prevNode.GetPosition(), currentnode.GetPosition());
                        currentnode = prevNode;
                    }
                }
            }


            // Draw connecting lines.
            // These are dynamic drawing lines that follow the mouse.
            if (_actionConnectingPath)
            {
                foreach (var startnodeid in _selectionNodes)
                {
                    PathNode startnode = _nodecollection[startnodeid];
                    if (startnode == null)
                        continue;

                    graphics.DrawLine(drawingLinePen, startnode.GetPosition(), _mouseposition);
                }
            }

            // clean up 
            solutionLinePen.Dispose();
            drawingLinePen.Dispose();
            pathLinePen.Dispose();
            labelFont.Dispose();
            labelBrush.Dispose();

        } // pbCanvas_Paint


        /**************************************************************************
        * Set the selected node to the START node 
        ***************************************************************************/
        private void toolStripMenuSTART_Click(object sender, EventArgs e)
        {
            int nodeid = HitTest_NodeId(_mouseposition);
            if (nodeid == -1)
                return;

            // remove previous start node and then set the current
            PathNode prevnode = (_startNodeId >= 0) ? GetNodeWithId(_startNodeId) : null;
            PathNode node = GetNodeWithId(nodeid);

            _startNodeId = nodeid;
            if (prevnode != null) prevnode.StartNode = false;
            if (node != null) node.StartNode = true;

            clearSelectedItems();
            resetNodeStates();
            pbCanvas.Invalidate(); // show change
        }

        private void toolStripMenuEND_Click(object sender, EventArgs e)
        {
            int nodeid = HitTest_NodeId(_mouseposition);
            if (nodeid == -1)
                return;

            // remove previous start node and then set the current
            PathNode prevnode = (_endNodeId >= 0) ? GetNodeWithId(_endNodeId) : null;
            PathNode node = GetNodeWithId(nodeid);

            _endNodeId = nodeid;
            if (prevnode != null) prevnode.EndNode = false;
            if (node != null) node.EndNode = true;

            clearSelectedItems();
            resetNodeStates();
            pbCanvas.Invalidate(); // show change
        }


        /**************************************************************************
        * Removes all items from the canvas 
        ***************************************************************************/
        private void ToolStripMenuItemCLEAR_Click(object sender, EventArgs e)
        {
            clearAllLinks();
            clearSelectedItems();

            _nextkeyid = 0;
            _nodecollection.Clear();
            _endNodeId = _startNodeId = -1;

            pbCanvas.Invalidate();
        }


        /**************************************************************************
        * Return all the nodes to the state before search is performed. 
        * Returns true, if a start and end node is defined.
        ***************************************************************************/
        private bool resetNodeStates()
        {
            bool validstartnode = false;
            bool validendnode = false;
            foreach (var n in _nodecollection)
            {
                n.Value.Visited         = false;            // clear visited status
                n.Value.Score           = float.MaxValue;   // reset score
                n.Value.PreviousNodeId  = -1;               // reset our solution path link
                _unvisitedNodesSet.Add(n.Key);              // populate unvisited collection set

                if (n.Key == _startNodeId)
                    validstartnode = true;

                if (n.Key == _endNodeId)
                    validendnode = true;
            }
            return validstartnode && validendnode;
        }

        /**************************************************************************
        *  Runs the Dijkstra pathfinding algorithm.
        ***************************************************************************/
        private void DIJKSTRAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool validendpoints = resetNodeStates();
            if (!validendpoints)
            {
                MessageBox.Show("Please set your start and end nodes", "Information Needed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // ALGORITHM

            // Start at the beginning.
            // The start node will always have a distance of 0 to itself.
            PathNode startNode = _nodecollection[_startNodeId];
            startNode.Score = 0;

            bool finished = false;
            while(!finished)
            {
                // For all our branch paths from our test node, calculate the 
                // distance score. If the score is less than current, then replace.
                foreach (var p in _linkcollection.Where( p => (p.Key.Key == startNode.Id) || (p.Key.Value == startNode.Id) ).Select( r => r.Value))
                {
                    // Check the direction of our link.
                    // If it is flows both ways, then its ok.
                    // If it flows against the direction that we are moving, then skip.
                    if (!((p.Direction == NodeLink.DirectionType.biDirectional) || (p.StartNodeId == startNode.Id)))
                        continue;

                    // The ids of our source and destination nodes can be either of the two id properties.
                    // Figure out which one is our active node, and then assign the other to our variable.
                    // [a] <-------> [b]
                    int endNodeId = p.EndNodeId;
                    if (endNodeId == startNode.Id)
                        endNodeId = p.StartNodeId;

                    PathNode endNode = GetNodeWithId(endNodeId);
                    if(endNode == null)
                        continue;

                    float newScore = p.Distance + startNode.Score;

                    // update this node to the new score
                    if (endNode.Score > newScore)
                    {
                        endNode.Score = newScore;             // update to smaller score
                        endNode.PreviousNodeId = startNode.Id;    // add link to the path we came from
                    }
                }

                // remove this node from our visited list
                _unvisitedNodesSet.Remove(startNode.Id);
                startNode.Visited = true;
                if (_unvisitedNodesSet.Count == 0)
                    finished = true;

                // Choose the next node with the smallest score from our
                // remaining nodes
                float minScore = float.MaxValue;
                foreach(var s in _unvisitedNodesSet)
                {
                    PathNode pn = GetNodeWithId(s);
                    if(pn.Score < minScore)
                    {
                        startNode = pn;
                        minScore = pn.Score;
                    }
                }

                // If all the remaining items do not have a score (is infinity)
                // then stop. This is probaly due to no solution available, or 
                if (minScore == float.MaxValue)
                    finished = true;

            } // while(!finished)

            pbCanvas.Invalidate();

        } //DIJKSTRAToolStripMenuItem_Click



        /**************************************************************************
        * Save state to a binary file 
        ***************************************************************************/
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new SaveFileDialog();
            dlg.Filter = "PathFinder File (*.path)|*.path";
            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            BinaryFormatter formatter = new BinaryFormatter();
            using (var filestream = new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                try
                {
                    formatter.Serialize(filestream, _nodecollection);
                }
                catch (SerializationException ex)
                {
                    MessageBox.Show($"Failed to save. Reason:{ex.Message}");
                    return;
                }
            }
        }


        /**************************************************************************
        * Restore from a previously saved binary file 
        ***************************************************************************/
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "PathFinder File (*.path)|*.path";
            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            BinaryFormatter formatter = new BinaryFormatter();
            using (var filestream = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                try
                {
                    _nodecollection = (Dictionary<int, PathNode>)formatter.Deserialize(filestream);
                }
                catch (SerializationException ex)
                {
                    MessageBox.Show($"Failed to open. Reason:{ex.Message}");
                    return;
                }
            }

            resetNodeStates();
            clearSelectedItems();
            pbCanvas.Invalidate();
        }


        /**************************************************************************
        * Deletes the selected nodes 
        ***************************************************************************/
        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            deleteSelectedNodes();
        }

        private void deletePathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var selid in _selectionNodes)
            {
                deleteLinksForNode(selid);
            }
            pbCanvas.Invalidate();
        }

        /**************************************************************************
        * Inserts a new node 
        * Keyboard shortcut - INS
        ***************************************************************************/
        private void insertNewNodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            insertNewNode();
        }


        /**************************************************************************
        * Insert a new node to the canvas 
        ***************************************************************************/
        private void insertNewNode()
        {
            // Add new node to the centre of the canvas. But we can be a bit more clever
            // than that. To prevent obscuring an existing item, lets shift to a new position
            // if it is occupied.
            Point p = new Point(pbCanvas.Width / 2, pbCanvas.Height / 2);
            int hittest = HitTest_NodeId(p);
            while(hittest >= 0)
            {
                p.Offset(25, 25);
                hittest = HitTest_NodeId(p);
            }

            // Add to canvas
            clearSelectedItems();
            PathNode newnode = CreateNewNode(p);
            _nodecollection.Add(newnode.Id, newnode);
            addSelectedItem(ref newnode);
            pbCanvas.Invalidate();
        }


        /**************************************************************************
        * Delete selected nodes 
        ***************************************************************************/
        private void deleteSelectedNodes()
        {
            foreach (var selid in _selectionNodes)
                _nodecollection.Remove(selid);

            clearSelectedItems();
            pbCanvas.Invalidate();
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Insert:
                    insertNewNode();
                    break;
                case Keys.Delete:
                    deleteSelectedNodes();
                    break;
                case Keys.A:
                    if (Control.ModifierKeys.HasFlag(Keys.Control))
                    { // select all 
                        selectAllItems();
                        pbCanvas.Invalidate();
                    }
                    break;
                default:
                    break;
            }
        }


        /**************************************************************************
        * Generate square grid 
        ***************************************************************************/
        private void generateGridToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int x = pbCanvas.Width;
            int y = pbCanvas.Height;
            //int yoffset = y / 25;
            //int xoffset = x / 25;


            PathNode anode = new PathNode();
            int offset = anode.NodeSize.Height + 50;

            int nX = offset;
            int nY = offset;
            PathNode prevnode = null;
            while (nY < y)
            {
                nX = offset;
                while (nX < x)
                {
                    PathNode newnode = CreateNewNode(nX, nY);
                    _nodecollection.Add(newnode.Id, newnode);

                    if (prevnode != null)
                    {
                        /// TODO
                    }

                    prevnode = newnode;

                    nX += offset;
                }
                nY += offset;
            }

            pbCanvas.Invalidate();
        } // generateGridToolStripMenuItem_Click



        #region NODE LINK MANAGEMENT

        /// <summary>
        /// Remove all currently defined node links
        /// </summary>
        private void clearAllLinks()
        {
            _linkcollection.Clear();
        }

        /// <summary>
        /// removes any link paths for the given node
        /// </summary>
        /// <remarks>Dictionary<KeyValuePair<int, int>, NodeLink></remarks>
        /// <param name="nodeid">node id</param>
        private void deleteLinksForNode(int nodeid)
        {
            // use LINQ to get a list of any links that has attachment to this node.
            foreach(var n in _linkcollection.Where( p => (p.Key.Key == nodeid) || (p.Key.Value == nodeid) ).ToList())
            {
                _linkcollection.Remove(n.Key);
            }
        }

        /// <summary>
        /// calculates the distance between two given nodes
        /// </summary>
        /// <remarks>uses pythagorus's theorem</remarks>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private float calculateDistanceBetweenNodes(PathNode a, PathNode b)
        {
            int width = Math.Abs(a.X - b.X);
            int height = Math.Abs(a.Y - b.Y);

            return (float)Math.Sqrt( width * width + height * height);
        }

        /// <summary>
        /// add a link to two nodes
        /// </summary>
        /// <param name="startnodeid"></param>
        /// <param name="endnodeid"></param>
        private void addLinksForNode(int startnodeid, int endnodeid)
        {
            // get the nodes, so we can calculate the distance between
            PathNode a = GetNodeWithId(startnodeid);
            PathNode b = GetNodeWithId(endnodeid);
            if ((a == null) || (b == null))
                return;

            float dist = calculateDistanceBetweenNodes(a, b);
            NodeLink newlink = new NodeLink();
            newlink.StartNodeId = startnodeid;
            newlink.EndNodeId = endnodeid;
            newlink.Distance = dist;

            // Add to collection
            // use the [] operator to prevent any exception thrown. If we have duplicates, then
            // ok with overwriting the previous.
            _linkcollection[ new KeyValuePair<int, int>(startnodeid, endnodeid) ] = newlink;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startnodeid"></param>
        private void updateLinkDistancesForNode(int startnodeid)
        {
            NodeLink a = null;
            var nodecollection = _linkcollection.Where(p => (p.Key.Key == startnodeid) || (p.Key.Value == startnodeid)).Select( k => k.Key).ToList();

            foreach (var n in nodecollection)
            {
                if (!_linkcollection.TryGetValue(n, out a))
                    continue;

                PathNode s = GetNodeWithId(a.StartNodeId);
                PathNode e = GetNodeWithId(a.EndNodeId);
                float dist = calculateDistanceBetweenNodes(s, e);

                // update this link's distance
                a.Distance = dist;
            }
        }

        #endregion




    } // class Form1

}
