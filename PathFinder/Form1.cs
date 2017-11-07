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
using System.Drawing.Drawing2D;
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
        private bool _actionSelection = false;
        private Point _mouseposition;

        // selection
        // ids of items selected.
        // Items can be selected by holding down the Ctrl key while clicking
        private HashSet<int> _selectionNodes = new HashSet<int>();

        // selection rectangle
        private Rectangle _selectionrect = new Rectangle();

        // Path finding variables

        // stores the nodes (id) that we have not visited.
        private HashSet<int> _unvisitedNodesSet = new HashSet<int>();
        private int _startNodeId = -1;
        private int _endNodeId = -1;

        // options
        private bool _showlabels = true;


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
        private void doSelectItem(ref PathNode node)
        {
            if (node == null)
                return;

            _selectionNodes.Add(node.Id);
            node.IsSelected = true;
        }

        private void doSelectItem(int nodeId)
        {
            PathNode aNode = null;
            if (!_nodecollection.TryGetValue(nodeId, out aNode))
                return;

            doSelectItem(ref aNode);
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

        private NodeLink HitTest_Link(Point p)
        {
            NodeLink hit = null;
            foreach(var link in _linkcollection)
            {
                if (link.Value.HitTest(p))
                    return link.Value;
            }
            return hit;
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
                    doSelectItem(ref n);
                }
                else
                {
                    // selection rectangle
                    _actionSelection = true;
                    _selectionrect = new Rectangle(_mouseposition.X, _mouseposition.Y, 1, 1);
                }

                pbCanvas.Invalidate();
            }

        } // private void pbCanvas_MouseDown(object sender, MouseEventArgs e)

        private void pbCanvas_MouseMove(object sender, MouseEventArgs e)
        {

            // Moving selected items
            // We could have multiple selected items, so move relative to the current cursor.
            if(_actionMovingNode)
            {
                _actionSelection = false;
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

                    // update links
                    updateLinkDistancesForNode(selId);

                    pbCanvas.Invalidate();
                }

                return;
            }

            // Drawing connecting lines
            if (_actionConnectingPath || (_mousedown && _selectionNodes.Count > 0))
            {
                _actionSelection = false;
                _actionConnectingPath = true;
                _mouseposition = e.Location;
                pbCanvas.Invalidate();
                return;
            }

            // bounding selection rectangle
            _selectionrect = new Rectangle(
                                        Math.Min(_mouseposition.X, e.Location.X),
                                        Math.Min(_mouseposition.Y, e.Location.Y),
                                        Math.Abs(_mouseposition.X - e.Location.X),
                                        Math.Abs(_mouseposition.Y - e.Location.Y));
            pbCanvas.Invalidate();
        }

        private void pbCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            _mousedown = false;

            // We need to create and define a path link between our active nodes
            if (_actionConnectingPath)
            {
                _actionConnectingPath = false;

                bool bothdirections = Control.ModifierKeys.HasFlag(Keys.Control);

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
                        addLinksForNode(startnodeid, hittestid, bothdirections);
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

            // SELECTION
            if(_actionSelection)
            {
                _actionSelection = false;

                // select all items within this bounding rectangle
                foreach (var node in _nodecollection)
                {
                    if(_selectionrect.IntersectsWith(node.Value.BoundingRect))
                    {
                        doSelectItem(node.Value.Id);
                    }
                }

                pbCanvas.Invalidate();
            }

        } // pbCanvas_MouseUp



        /**************************************************************************
        * Background grid 'dots' 
        ***************************************************************************/
        private void draw_Grid(ref Graphics graphics)
        {
            int width = pbCanvas.Width;
            int height = pbCanvas.Height;
            Pen dotPen = new Pen(Color.FromArgb(64,64,64));

            for(int j = 0; j < height; j++)
            {
                j += 20;
                for (int i = 0; i < width; i++)
                {
                    i += 20;

                    graphics.DrawRectangle(dotPen, i, j, 1, 1);
                }
            }

            dotPen.Dispose();
        }

        /**************************************************************************
        * PictureBox paint handler 
        * Responsible for rendering all items on the graph to the screen
        ***************************************************************************/
        private void pbCanvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Pen drawingLinePen = new Pen(Color.Coral);
            Pen solutionLinePen = new Pen(Color.FromArgb(255, 22, 84));
            Pen selectionPen = new Pen(Color.Yellow);

            // directional pens
            Pen biDirectionalLinePen = new Pen(Color.MediumAquamarine);
            Pen uniDirectionalLinePen = new Pen(Color.OliveDrab);

            AdjustableArrowCap deepArrow = new AdjustableArrowCap(4, 4, false);
            uniDirectionalLinePen.CustomEndCap = deepArrow;
            biDirectionalLinePen.CustomStartCap = deepArrow;
            biDirectionalLinePen.CustomEndCap = deepArrow;


            biDirectionalLinePen.Width = 2.0F;
            uniDirectionalLinePen.Width = 2.0F;
            drawingLinePen.Width = 3.0F;
            solutionLinePen.Width = 3.0F;

            // Label graphic objects
            Font labelFont = new Font("Tahoma", 10.0F);
            Brush labelBrush = new SolidBrush(Color.LightSeaGreen);

            // Draw the background grid
            draw_Grid(ref graphics);

            foreach (var link in _linkcollection)
            {
                link.Value.Draw(ref graphics, _showlabels);
            }

            // Draw our nodes
            foreach (var node in _nodecollection)
                node.Value.Draw(ref graphics, _showlabels);


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
                String label = null;
                bool isBothDir = Control.ModifierKeys.HasFlag(Keys.Control);

                if (isBothDir)
                    label = "<< >>";
                else
                    label = ">>";

                SizeF labelSize = graphics.MeasureString(label, labelFont);

                foreach (var startnodeid in _selectionNodes)
                {
                    PathNode startnode = _nodecollection[startnodeid];
                    if (startnode == null)
                        continue;

                    Point sP = startnode.GetPosition();
                    Point eP = _mouseposition;
                    graphics.DrawLine(drawingLinePen, sP, eP);

                    int midX = Math.Abs(eP.X - sP.X) * 4 / 5 + Math.Min(sP.X, eP.X);
                    int midY = Math.Abs(eP.Y - sP.Y) * 4 / 5 + Math.Min(sP.Y, eP.Y);
                    Point labelPoint = new Point(midX - (int)labelSize.Width, midY - (int)labelSize.Height - 10);

                    graphics.DrawString(label, labelFont, labelBrush, labelPoint);
                }
            }
            else
            {
                if(_mousedown && _actionSelection)
                {
                    graphics.DrawRectangle(selectionPen, _selectionrect);
                }
            }

            // clean up 
            solutionLinePen.Dispose();
            drawingLinePen.Dispose();
            uniDirectionalLinePen.Dispose();
            biDirectionalLinePen.Dispose();
            labelFont.Dispose();
            labelBrush.Dispose();

        } // pbCanvas_Paint


        /**************************************************************************
        * Calculates and displays the travelled distance for our solution path 
        ***************************************************************************/
        private void DisplayResult()
        {
            float totaldistance = 0;
            if (_endNodeId >= 0)
            {
                PathNode currentnode = GetNodeWithId(_endNodeId);
                if (currentnode != null)
                {
                    while (currentnode.PreviousNodeId >= 0)
                    {
                        // Quite liking the power of Linq, so using it here to get the distance from the link
                        // between the current node and previous node.
                        float dist = _linkcollection.Where(
                                p =>
                               ((p.Key.Key == currentnode.Id) && (p.Key.Value == currentnode.PreviousNodeId)) ||
                               ((p.Key.Key == currentnode.PreviousNodeId) && (p.Key.Value == currentnode.Id))

                                ).Select(s => s.Value.Distance ).FirstOrDefault();

                        totaldistance += dist;


                        PathNode prevNode = GetNodeWithId(currentnode.PreviousNodeId);
                        if (prevNode == null)
                            break;

                        currentnode = prevNode;
                    }
                }
            }

            toolStripStatusLabelResult.Text = "Solution Distance: " + totaldistance.ToString();
        }

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

            DisplayResult();

            pbCanvas.Invalidate();

        } //DIJKSTRAToolStripMenuItem_Click



        /**************************************************************************
        * A* Pathfinding algorithm.
        * 
        * The A* algorithm is based on Dijkstra. It uses a heuristic value to allow
        * for faster solutions.
        * 
        * f(x) = g(x) + h(x)
        * 
        * The Dijkstra's algoritm is basically A*, but with a heuristic value of zero.
        * I know of two simple ways calculating h(x). One is the 'Manhatten' score. 
        * The other is the 'Euclidean' score.
        * 
        * I think for this demo, the 'Euclidean' is more appropriate.
        ***************************************************************************/
        private void ASTAR_ToolStripMenuItem_Click(object sender, EventArgs e)
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
            PathNode endNode = _nodecollection[_endNodeId];
            PathNode startNode = _nodecollection[_startNodeId];
            startNode.Score = 0;

            Debug.Assert(endNode != null);
            Debug.Assert(startNode != null);

            bool finished = false;
            while (!finished)
            {
                // For all our branch paths from our test node, calculate the 
                // distance score. If the score is less than current, then replace.
                foreach (var p in _linkcollection.Where(p => (p.Key.Key == startNode.Id) || (p.Key.Value == startNode.Id)).Select(r => r.Value))
                {
                    // Check the direction of our link.
                    // If it is flows both ways, then its ok.
                    // If it flows against the direction that we are moving, then skip.
                    if (!((p.Direction == NodeLink.DirectionType.biDirectional) || (p.StartNodeId == startNode.Id)))
                        continue;

                    // if our link is blocked, then do not try to use
                    if (p.DistType == NodeLink.DistanceType.BlockingDistance)
                        continue;

                    // The ids of our source and destination nodes can be either of the two id properties.
                    // Figure out which one is our active node, and then assign the other to our variable.
                    // [a] <-------> [b]
                    int branchNodeId = p.EndNodeId;
                    if (branchNodeId == startNode.Id)
                        branchNodeId = p.StartNodeId;

                    PathNode branchNode = GetNodeWithId(branchNodeId);
                    if (branchNode == null)
                        continue;

                    // Heuristic Score
                    // Use the Euclidean technique. This basically gets the absolute distance from
                    // both nodes.
                    float heuristic = calculateDistanceBetweenNodes(branchNode, endNode);

                    // The 'pending' score of this branch node is now a combination of
                    // preceding node's score, the path distance (or weight) and the heuristic.
                    float newScore = (p.Distance + startNode.Score) + heuristic;

                    // update this node to the new score
                    if (branchNode.Score > newScore)
                    {
                        branchNode.Score = newScore;             // update to smaller score
                        branchNode.PreviousNodeId = startNode.Id;    // add link to the path we came from
                    }

                    // Found the end node!
                    if (_endNodeId == branchNodeId)
                        finished = true;
                }

                // remove this node from our visited list
                _unvisitedNodesSet.Remove(startNode.Id);
                startNode.Visited = true;
                if (_unvisitedNodesSet.Count == 0)
                    finished = true;

                // Choose the next node with the smallest score from our
                // remaining nodes
                float minScore = float.MaxValue;
                foreach (var s in _unvisitedNodesSet)
                {
                    PathNode pn = GetNodeWithId(s);
                    if (pn.Score < minScore)
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

            DisplayResult();

            pbCanvas.Invalidate();
        } // ASTAR_ToolStripMenuItem_Click


        /**************************************************************************
        * Save state to a binary file 
        ***************************************************************************/
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new SaveFileDialog();
            dlg.Filter = "PathFinder File (*.path)|*.path";
            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            List<Object> objects = new List<object>();
            foreach (var t in _nodecollection)
                objects.Add(t.Value);

            foreach (var s in _linkcollection)
                objects.Add(s.Value);

            BinaryFormatter formatter = new BinaryFormatter();
            using (var filestream = new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                try
                {
                    formatter.Serialize(filestream, objects);
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

            List<Object> objects = new List<object>();
            BinaryFormatter formatter = new BinaryFormatter();
            using (var filestream = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                try
                {
                    objects = (List<Object>)formatter.Deserialize(filestream);
                }
                catch (SerializationException ex)
                {
                    MessageBox.Show($"Failed to open. Reason:{ex.Message}");
                    return;
                }
            }

            clearAllLinks();
            _nodecollection.Clear();
            resetNodeStates();
            clearSelectedItems();
            _startNodeId = _endNodeId = -1;

            int maxid = -1;
            foreach (var objt in objects)
            {
                if(objt.GetType() == typeof(PathNode))
                {
                    PathNode node = (PathNode)objt;
                    _nodecollection[node.Id] = node;

                    if (node.Id > maxid)
                        maxid = node.Id;

                    continue;
                }

                if(objt.GetType() == typeof(NodeLink))
                {
                    NodeLink link = (NodeLink)objt;
                    _linkcollection[new KeyValuePair<int, int>(link.StartNodeId, link.EndNodeId)] = link;
                    continue;
                }
            }

            // ensure that our id generator is updated.
            _nextkeyid = maxid++;


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
            doSelectItem(ref newnode);
            pbCanvas.Invalidate();
        }


        /**************************************************************************
        * Delete selected nodes 
        ***************************************************************************/
        private void deleteSelectedNodes()
        {
            foreach (var selid in _selectionNodes)
            {
                deleteLinksForNode(selid);
                _nodecollection.Remove(selid);
            }

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

        #region PATTERN_GENERATOR

        /**************************************************************************
        * Generate square grid 
        ***************************************************************************/
        private void generateGridToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int x = pbCanvas.Width;
            int y = pbCanvas.Height;
           
            int[,] grid = new int[x, y];

            int nodeWidth = (new PathNode()).NodeSize.Width;
            int padding = nodeWidth * 2;
            int xcount = x / (nodeWidth + padding);
            int ycount = y / (nodeWidth + padding);
            int xspacing = (x - (((nodeWidth + padding) * xcount) - padding)) / 2;
            int yspacing = (y - (((nodeWidth + padding) * ycount) - padding)) / 2;

            // generate nodes
            for (int j = 0; j < ycount; j++)
            {
                for (int i = 0; i < xcount; i++)
                {
                    PathNode newnode = CreateNewNode(   (i * (nodeWidth + padding)) + (nodeWidth / 2) + xspacing,
                                                        (j * (nodeWidth + padding)) + (nodeWidth / 2) + yspacing);
                    _nodecollection[newnode.Id] = newnode;
                    grid[i, j] = newnode.Id;
                }
            }

            // Add Horizontal Links
            for (int j = 0; j < ycount; j++)
            {
                for (int i = 0; i < xcount - 1; i++)
                {
                    addLinksForNode(grid[i, j], grid[i + 1, j], true);
                }
            }

            // Add Vertical Links
            for (int i = 0; i < xcount; i++) 
            {
                for (int j = 0; j < ycount - 1; j++)
                {
                    addLinksForNode(grid[i, j], grid[i, j + 1], true);
                }
            }

            pbCanvas.Invalidate();
        } // generateGridToolStripMenuItem_Click


        
        /**************************************************************************
        * Circular Pattern  
        ***************************************************************************/
        private void generateCircularPatternToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int width = pbCanvas.Width;
            int height = pbCanvas.Height;

            int canvasradius = (int)Math.Min(width, height) / 2;

            int centreX = width / 2;
            int centreY = height / 2;

            int nodewidth = (new PathNode()).NodeSize.Width;
            int padding = nodewidth * 2;

            canvasradius -= (nodewidth);

            for (int k = 0; k < 2; k++)
            {

                double c = Math.PI * canvasradius * 2;
                int count = (int)(c / (nodewidth + padding));
                double angledelta = 360 / count;

                double angle = 0;
                int previd = -1;
                for (int n = 0; n < count; n++)
                {
                    angle += angledelta;
                    double rad = Math.PI * angle / 180.0;

                    int nX = (int)(Math.Cos(rad) * canvasradius);
                    int nY = (int)(Math.Sin(rad) * canvasradius);

                    PathNode newnode = CreateNewNode(nX + centreX, nY + centreY);
                    _nodecollection[newnode.Id] = newnode;

                    if (previd >= 0)
                    {
                        addLinksForNode(previd, newnode.Id, true);
                    }
                    previd = newnode.Id;
                }

                canvasradius /= 2;
            }

            pbCanvas.Invalidate();
        }

        #endregion

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
        private void addLinksForNode(int startnodeid, int endnodeid, bool bidirectional = false)
        {
            // get the nodes, so we can calculate the distance between
            PathNode a = GetNodeWithId(startnodeid);
            PathNode b = GetNodeWithId(endnodeid);
            if ((a == null) || (b == null))
                return;

            NodeLink newlink = new NodeLink(a, b, bidirectional ? NodeLink.DirectionType.biDirectional : NodeLink.DirectionType.uniDirectional);

            // Add to collection
            // use the [] operator to prevent any exception thrown. If we have duplicates, then
            // ok with overwriting the previous.
            _linkcollection[ new KeyValuePair<int, int>(startnodeid, endnodeid) ] = newlink;
        }

        /// <summary>
        /// Recalculate node distances
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
                if ((s == null) || (e == null))
                    continue;

                a.UpdateConnection(s, e);
            }
        }
        #endregion

        private void showLabelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _showlabels = !_showlabels;
            showLabelsToolStripMenuItem.Checked = _showlabels;
            pbCanvas.Invalidate();
        }


        /**************************************************************************
        * mouse double-click handler 
        * 
        * - Add new node on whitespace
        * - Open object property dialog if object clicked
        ***************************************************************************/
        private void pbCanvas_DoubleClick(object sender, EventArgs e)
        {
            Point mouseP = Cursor.Position;
            mouseP = pbCanvas.PointToClient(mouseP);

            // This returns the id of the node. -1 otherwise.
            // If we have clicked in any whitespace, add new item

            NodeLink link = null;
            int hittestid = HitTest_NodeId(mouseP);
            if (hittestid < 0)
            {
                link = HitTest_Link(mouseP);

                if (link == null)
                {
                    // Add to canvas
                    clearSelectedItems();
                    PathNode newnode = CreateNewNode(mouseP);
                    _nodecollection.Add(newnode.Id, newnode);
                    doSelectItem(ref newnode);
                    pbCanvas.Invalidate();
                    return;
                }
            }

            if(link != null)
            {
                var form = new LinkPropertyForm();
                form.Link = link;
                form.ShowDialog();
            }
        }


        /**************************************************************************
        * Refresh 
        ***************************************************************************/
        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //_startNodeId = _endNodeId = -1;
            clearSelectedItems();
            resetNodeStates();

            // recalculate the distances
            foreach (var links in _linkcollection)
            {
                updateLinkDistancesForNode(links.Key.Key);
            }

            pbCanvas.Invalidate();
        }

    } // class Form1

}
