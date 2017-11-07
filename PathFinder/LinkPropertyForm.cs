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
    public partial class LinkPropertyForm : Form
    {
        public NodeLink Link { get; set; }

        public LinkPropertyForm()
        {
            InitializeComponent();
        }

        private void LinkPropertyForm_Load(object sender, EventArgs e)
        {
            textBox1.Text = Link.Distance.ToString();

            cbDirection.SelectedIndex = (Link.Direction == NodeLink.DirectionType.uniDirectional) ? 0 : 1;

            switch (Link.DistType)
            {
                case NodeLink.DistanceType.AutoDistance:
                    cbDistance.SelectedIndex = 0;
                    break;
                case NodeLink.DistanceType.BlockingDistance:
                    cbDistance.SelectedIndex = 1;
                    break;
                case NodeLink.DistanceType.UserDefinedDistance:
                    cbDistance.SelectedIndex = 2;
                    break;
                default:
                    break;
            }
        }

        private void cbDistance_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch(cbDistance.SelectedIndex)
            {
                case 0: // auto
                    {
                        Link.DistType = NodeLink.DistanceType.AutoDistance;
                        textBox1.Enabled = false;
                    }
                    break;
                case 1: // block
                    {
                        Link.DistType = NodeLink.DistanceType.BlockingDistance;
                        textBox1.Enabled = false;
                    }
                    break;
                case 2: // user-defined
                    {
                        Link.DistType = NodeLink.DistanceType.UserDefinedDistance;
                        textBox1.Enabled = true;
                    }
                    break;
                default:
                    break;
            }
        }


        /**************************************************************************
        * Changes the link direction 
        ***************************************************************************/
        private void cbDirection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbDirection.SelectedIndex == 0)
                Link.Direction = NodeLink.DirectionType.uniDirectional;
            else
                Link.Direction = NodeLink.DirectionType.biDirectional;
        }

        /// <summary>
        /// Changes the distance
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            float d;
            if( float.TryParse(textBox1.Text, out d) )
            {
                Link.Distance = d;
            }
        }
    }
}
