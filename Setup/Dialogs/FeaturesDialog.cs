using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using WixSharp;

using WixSharp.UI.Forms;

namespace Setup.Dialogs
{
                        public partial class FeaturesDialog : ManagedForm, IManagedDialog     {
        /*https://msdn.microsoft.com/en-us/library/aa367536(v=vs.85).aspx
         * ADDLOCAL - list of features to install
         * REMOVE - list of features to uninstall
         * ADDDEFAULT - list of features to set to their default state
         * REINSTALL - list of features to repair*/

        FeatureItem[] features;

                                public FeaturesDialog()
        {
            InitializeComponent();
            label1.MakeTransparentOn(banner);
            label2.MakeTransparentOn(banner);
        }

        void FeaturesDialog_Load(object sender, System.EventArgs e)
        {
            string drawTextOnlyProp = Runtime.Session.Property("WixSharpUI_TreeNode_TexOnlyDrawing");

            bool drawTextOnly = true;

            if (drawTextOnlyProp.IsNotEmpty())
            {
                if (string.Compare(drawTextOnlyProp, "false", true) == 0)
                    drawTextOnly = false;
            }
            else
            {
                float dpi = CreateGraphics().DpiY;
                if (dpi == 96)                     drawTextOnly = false;
            }

            ReadOnlyTreeNode.Behavior.AttachTo(featuresTree, drawTextOnly);

            banner.Image = Runtime.Session.GetResourceBitmap("WixUI_Bmp_Banner") ??
                           Runtime.Session.GetResourceBitmap("WixSharpUI_Bmp_Banner");

            BuildFeaturesHierarchy();

            if (banner.Image != null)
                ResetLayout();
        }

        void ResetLayout()
        {
                                    
            float ratio = (float)banner.Image.Width / (float)banner.Image.Height;
            topPanel.Height = (int)(banner.Width / ratio);
            topBorder.Top = topPanel.Height + 1;

            var upShift = (int)(next.Height * 2.3) - bottomPanel.Height;
            bottomPanel.Top -= upShift;
            bottomPanel.Height += upShift;

            middlePanel.Top = topBorder.Bottom + 5;
            middlePanel.Height = (bottomPanel.Top - 5) - middlePanel.Top;

            featuresTree.Width = (int)((middlePanel.Width / 3.0) * 1.75);

            descriptionPanel.Left = featuresTree.Right + 10;
            descriptionPanel.Width = middlePanel.Width - descriptionPanel.Left - 10;

            featuresTree.Nodes[0].EnsureVisible();
        }

                                public static List<string> UserSelectedItems { get; private set; }

                                public static List<string> InitialUserSelectedItems { get; private set; }

        void BuildFeaturesHierarchy()
        {
            features = Runtime.Session.Features;

                        var rootItems = features.Where(x => x.ParentName.IsEmpty())
                                    .OrderBy(x => x.RawDisplay)
                                    .ToArray();

            var itemsToProcess = new Queue<FeatureItem>(rootItems); 
            while (itemsToProcess.Any())
            {
                var item = itemsToProcess.Dequeue();

                                var viewModel = new ReadOnlyTreeNode
                {
                    Text = item.Title,
                    Tag = item,                     IsReadOnly = item.DisallowAbsent,
                    DefaultChecked = item.DefaultIsToBeInstalled(),
                    Checked = item.DefaultIsToBeInstalled()
                };

                item.ViewModel = viewModel;

                if (item.Parent != null && item.Display != FeatureDisplay.hidden)
                    (item.Parent.ViewModel as TreeNode).Nodes.Add(viewModel); 
                
                                features.Where(x => x.ParentName == item.Name)
                        .ForEach(c =>
                                 {
                                     c.Parent = item;                                      itemsToProcess.Enqueue(c);                                  });

                if (UserSelectedItems != null)
                    viewModel.Checked = UserSelectedItems.Contains((viewModel.Tag as FeatureItem).Name);

                if (item.Display == FeatureDisplay.expand)
                    viewModel.Expand();
            }

                        rootItems.Where(x => x.Display != FeatureDisplay.hidden)
                     .Select(x => x.ViewModel)
                     .Cast<TreeNode>()
                     .ForEach(node => featuresTree.Nodes.Add(node));

            InitialUserSelectedItems = features.Where(x => x.IsViewChecked())
                                               .Select(x => x.Name)
                                               .OrderBy(x => x)
                                               .ToList();

            isAutoCheckingActive = true;
        }

        void SaveUserSelection()
        {
            UserSelectedItems = features.Where(x => x.IsViewChecked())
                                        .Select(x => x.Name)
                                        .OrderBy(x => x)
                                        .ToList();
        }

        void back_Click(object sender, System.EventArgs e)
        {
            SaveUserSelection();
            Shell.GoPrev();
        }

        void next_Click(object sender, System.EventArgs e)
        {
                        
            bool userChangedFeatures = UserSelectedItems?.JoinBy(",") != InitialUserSelectedItems.JoinBy(",");

            if (userChangedFeatures)
            {
                string itemsToInstall = features.Where(x => x.IsViewChecked())
                                                .Select(x => x.Name)
                                                .JoinBy(",");

                string itemsToRemove = features.Where(x => !x.IsViewChecked())
                                               .Select(x => x.Name)
                                               .JoinBy(",");

                if (itemsToRemove.Any())
                    Runtime.Session["REMOVE"] = itemsToRemove;

                if (itemsToInstall.Any())
                    Runtime.Session["ADDLOCAL"] = itemsToInstall;
            }
            else
            {
                Runtime.Session["REMOVE"] = "";
                Runtime.Session["ADDLOCAL"] = "";
            }

            SaveUserSelection();
            Shell.GoNext();
        }

        void cancel_Click(object sender, System.EventArgs e)
        {
            Shell.Cancel();
        }

        void featuresTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            description.Text = e.Node.FeatureItem().Description.LocalizeWith(Runtime.Localize);
        }

        bool isAutoCheckingActive = false;

        void featuresTree_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (isAutoCheckingActive)
            {
                isAutoCheckingActive = false;
                bool newState = e.Node.Checked;
                var queue = new Queue<TreeNode>();
                queue.EnqueueRange(e.Node.Nodes.ToArray());

                while (queue.Any())
                {
                    var node = queue.Dequeue();
                    node.Checked = newState;
                    queue.EnqueueRange(node.Nodes.ToArray());
                }

                if (e.Node.Checked)
                {
                    var parent = e.Node.Parent;
                    while (parent != null)
                    {
                        parent.Checked = true;
                        parent = parent.Parent;
                    }
                }

                isAutoCheckingActive = true;
            }
        }

        void reset_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            isAutoCheckingActive = false;
            features.ForEach(f => f.ResetViewChecked());
            isAutoCheckingActive = true;
        }
    }
}