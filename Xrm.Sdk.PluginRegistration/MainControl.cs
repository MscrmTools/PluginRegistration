// =====================================================================
//
//  This file is part of the Microsoft Dynamics CRM SDK code samples.
//
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  This source code is intended only as a supplement to Microsoft
//  Development Tools and/or on-line documentation.  See these other
//  materials for detailed information regarding Microsoft code samples.
//
//  THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//  PARTICULAR PURPOSE.
//
// =====================================================================

namespace Xrm.Sdk.PluginRegistration
{
    using McTools.Xrm.Connection;
    using Xrm.Sdk.PluginRegistration.Controls;
    using Forms;
    using Xrm.Sdk.PluginRegistration.Helpers;
    using Wrappers;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Drawing;
    using System.Text;
    using System.Windows.Forms;
    using XrmToolBox.Extensibility;
    using XrmToolBox.Extensibility.Interfaces;
    using XrmToolBox.Extensibility.Args;

    public partial class MainControl : PluginControlBase, IStatusBarMessenger
    {
        private const string SYSTEM_ERROR_MESSAGE = "The selected item is required for the Microsoft Dynamics CRM system to work correctly.";
        private const string SYSTEM_ERROR_CAPTION = "Microsoft Dynamics CRM";
        private CrmViewType m_currentView;
        private ConnectionDetail m_con;
        private CrmOrganization m_org;
        private static CrmEntitySorter m_entitySorter;
        private Dictionary<Guid, Guid> m_stepEntityMap = new Dictionary<Guid, Guid>();
        private Dictionary<string, CrmTreeNode> m_rootNodeList = null;
        private Dictionary<Guid, Guid> m_viewNodeList = null;
        private Dictionary<Guid, Guid> m_stepParentList = null;
        private ProgressIndicator m_progressIndicator = null;

        public MainControl()
        {
            InitializeComponent();

            #region Load the Images & Icons from the Resource File
            Dictionary<CrmTreeNodeImageType, Image> nodeImageList = null;
            try
            {
                nodeImageList = CrmResources.LoadImage(
                    CrmTreeNodeImageType.Assembly, CrmTreeNodeImageType.Image,
                    CrmTreeNodeImageType.Message, CrmTreeNodeImageType.MessageEntity,
                    CrmTreeNodeImageType.StepDisabled, CrmTreeNodeImageType.StepEnabled,
                    CrmTreeNodeImageType.ServiceEndpoint);

                toolServiceEndpointRegister.Image = nodeImageList[CrmTreeNodeImageType.ServiceEndpoint];
                mnuContextNodeServiceEndpointRegister.Image = toolServiceEndpointRegister.Image;
                mnuContextGeneralServiceEndpointRegister.Image = toolServiceEndpointRegister.Image;

                toolAssemblyRegister.Image = nodeImageList[CrmTreeNodeImageType.Assembly];
                mnuContextNodeAssemblyRegister.Image = toolAssemblyRegister.Image;
                mnuContextGeneralAssemblyRegister.Image = toolAssemblyRegister.Image;

                toolStepRegister.Image = nodeImageList[CrmTreeNodeImageType.StepEnabled];
                mnuContextNodeStepRegister.Image = toolStepRegister.Image;
                mnuContextGeneralStepRegister.Image = toolStepRegister.Image;

                toolImageRegister.Image = nodeImageList[CrmTreeNodeImageType.Image];
                mnuContextNodeImageRegister.Image = toolImageRegister.Image;
                mnuContextGeneralImageRegister.Image = toolImageRegister.Image;

                toolViewAssembly.Image = toolAssemblyRegister.Image;
                toolViewEntity.Image = nodeImageList[CrmTreeNodeImageType.MessageEntity];
                toolViewMessage.Image = nodeImageList[CrmTreeNodeImageType.Message];

                imlEnableImages.Images.Add("enableStep", nodeImageList[CrmTreeNodeImageType.StepEnabled]);
                imlEnableImages.Images.Add("disableStep", nodeImageList[CrmTreeNodeImageType.StepDisabled]);

                UpdateEnableButton(true);
            }
            catch (Exception)
            {
                if (nodeImageList != null)
                {
                    foreach (Image img in nodeImageList.Values)
                    {
                        if (img != null)
                        {
                            img.Dispose();
                        }
                    }
                }

                throw;
            }

            Dictionary<string, Image> imageList = null;
            try
            {
                imageList = CrmResources.LoadImage(
                    "ImportExport", 
                    "EditLabel", 
                    "Update", 
                    "Register", 
                    "Refresh", 
                    "Delete",
                    "Import", 
                    "Export", 
                    "View", 
                    "Search", 
                    "Errors", 
                    "InstallProfiler", 
                    "EnableProfiler", 
                    "DisableProfiler",
                    "UninstallProfiler", 
                    "Debug", 
                    "Close");

                toolRegister.Image = imageList["Register"];
                toolView.Image = imageList["View"];

                toolUpdate.Image = imageList["Update"];
                mnuContextNodeUpdate.Image = toolUpdate.Image;

                toolUnregister.Image = imageList["Delete"];
                mnuContextNodeUnregister.Image = toolUnregister.Image;

                toolSearch.Image = imageList["Search"];
                mnuContextNodeSearch.Image = toolSearch.Image;
                mnuContextGeneralSearch.Image = toolSearch.Image;

                toolRefresh.Image = imageList["Refresh"];
                mnuContextNodeRefresh.Image = toolRefresh.Image;
                mnuContextGeneralRefresh.Image = toolRefresh.Image;

                toolProfilerDebug.Image = imageList["Debug"];
                toolClose.Image = imageList["Close"];

                imlEnableImages.Images.Add("installProfiler", imageList["InstallProfiler"]);
                imlEnableImages.Images.Add("enableProfiler", imageList["EnableProfiler"]);
                imlEnableImages.Images.Add("disableProfiler", imageList["DisableProfiler"]);
                imlEnableImages.Images.Add("uninstallProfiler", imageList["UninstallProfiler"]);
            }
            catch (Exception)
            {
                foreach (Image img in imageList.Values)
                {
                    img.Dispose();
                }

                throw;
            }
            #endregion

            //Set the view types on the menu items. The Designer's auto code generation keeps overwriting this
            toolViewAssembly.Tag = CrmViewType.Assembly;
            toolViewEntity.Tag = CrmViewType.Entity;
            toolViewMessage.Tag = CrmViewType.Message;

            if (m_entitySorter == null)
            {
                m_entitySorter = new CrmEntitySorter();
            }
            trvPlugins.CrmTreeNodeSorter = m_entitySorter;

            //this.RefreshProfilerGeneralMenu();

            //Update the shortcut keys
            mnuContextNodeAssemblyRegister.ShortcutKeys = toolAssemblyRegister.ShortcutKeys;
            mnuContextGeneralAssemblyRegister.ShortcutKeys = toolAssemblyRegister.ShortcutKeys;

            mnuContextNodeStepRegister.ShortcutKeys = toolStepRegister.ShortcutKeys;
            mnuContextGeneralStepRegister.ShortcutKeys = toolStepRegister.ShortcutKeys;

            mnuContextNodeImageRegister.ShortcutKeys = toolImageRegister.ShortcutKeys;
            mnuContextGeneralImageRegister.ShortcutKeys = toolImageRegister.ShortcutKeys;

            mnuContextNodeRefresh.ShortcutKeys = mnuContextGeneralRefresh.ShortcutKeys;

            mnuContextNodeSearch.ShortcutKeys = mnuContextGeneralSearch.ShortcutKeys;

            mnuContextNodeSearch.ShortcutKeys = mnuContextGeneralSearch.ShortcutKeys;

            //Setup splitter panel 2 min distance because form designer keeps putting properties in incorrect order
            splitterDisplay.Panel2MinSize = 230;

            ConnectionUpdated += OrganizationControl_ConnectionUpdated;

            m_progressIndicator = new ProgressIndicator(new Action<StatusBarMessageEventArgs>((message) =>
            {
                if (SendMessageToStatusBar != null)
                {
                    SendMessageToStatusBar(this, message);
                }
            }));
        }

        public event EventHandler<StatusBarMessageEventArgs> SendMessageToStatusBar;

        void OrganizationControl_ConnectionUpdated(object sender, ConnectionUpdatedEventArgs e)
        {
            var instruction = new WorkAsyncInfo()
            {
                Message = "Loading assemblies information...",
                Work = (worker, argument) =>
                {
                    argument.Result = new CrmOrganization(ConnectionDetail, m_progressIndicator);
                },
                PostWorkCallBack = (argument) =>
                {
                    Init((CrmOrganization)argument.Result);
                }
            };
            
            WorkAsync(instruction);
        }

        public void Init(CrmOrganization org)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (org.ConnectionDetail == null)
            {
                throw new ArgumentNullException("org.ConnectionDetail");
            }

            m_org = org;
            m_con = org.ConnectionDetail;
            m_currentView = CrmViewType.Assembly;
            LoadNodes();
        }

        #region Control Event Handlers
        private void toolAssemblyRegister_Click(object sender, EventArgs e)
        {
            PluginRegistrationForm regForm = new PluginRegistrationForm(Organization, this, null);
            regForm.ShowDialog(ParentForm);
        }
        private void toolServiceEndpointRegister_Click(object sender, EventArgs e)
        {
            //ServiceBusConfigForm serviceBusConfigForm = new ServiceBusConfigForm(this.Organization, this, null);
            //serviceBusConfigForm.ShowDialog();
        }
        private void toolStepRegister_Click(object sender, EventArgs e)
        {
            //Check if we can extract a plugin from the Tree
            CrmPlugin plugin = null;
            CrmServiceEndpoint serviceEndpoint = null;

            if (trvPlugins.SelectedNode != null)
            {
                Guid pluginId;
                Guid serviceEndpointId = Guid.Empty;
                switch (trvPlugins.SelectedNode.NodeType)
                {
                    case CrmTreeNodeType.Assembly:
                    case CrmTreeNodeType.Message:
                    case CrmTreeNodeType.MessageEntity:

                        pluginId = Guid.Empty;
                        break;

                    case CrmTreeNodeType.Plugin:
                    case CrmTreeNodeType.WorkflowActivity:

                        pluginId = trvPlugins.SelectedNode.NodeId;
                        break;
                    case CrmTreeNodeType.Step:

                        pluginId = ((CrmPluginStep)trvPlugins.SelectedNode).PluginId;
                        break;
                    case CrmTreeNodeType.Image:

                        pluginId = ((CrmPluginImage)trvPlugins.SelectedNode).PluginId;
                        break;
                    case CrmTreeNodeType.ServiceEndpoint:

                        pluginId = ((CrmServiceEndpoint)trvPlugins.SelectedNode).PluginId;
                        serviceEndpointId = ((CrmServiceEndpoint)trvPlugins.SelectedNode).NodeId;
                        break;
                    default:
                        throw new NotImplementedException("NodeType = " + trvPlugins.SelectedNode.NodeType.ToString());
                }

                if (Guid.Empty != pluginId)
                {
                    plugin = m_org.Plugins[pluginId];
                }
                if (Guid.Empty != serviceEndpointId)
                {
                    serviceEndpoint = m_org.ServiceEndpoints[serviceEndpointId];
                }
            }

            StepRegistrationForm regForm = new StepRegistrationForm(Organization, this, plugin, null, serviceEndpoint);
            regForm.ShowDialog();
        }

        private void trvPlugins_SelectionChanged(object sender, CrmTreeNodeTreeEventArgs e)
        {
            SelectItem(e.Node);
        }

        private void toolUnregister_Click(object sender, EventArgs e)
        {
            if (trvPlugins.SelectedNode == null)
            {
                return;
            }
            else if (IsNodeSystemItem(trvPlugins.SelectedNode))
            {
                ShowSystemItemError("It cannot be unregistered.");
                return;
            }

            //If this is the profiler plug-in, then it needs to be unregistered.
            CrmPlugin plugin = trvPlugins.SelectedNode as CrmPlugin;
            if (null != plugin && plugin.IsProfilerPlugin)
            {
                toolProfilerInstall_Click(sender, e);
                return;
            }

            Guid nodeId = trvPlugins.SelectedNode.NodeId;
            if (MessageBox.Show("Are you sure you want to unregister this item?", "Unregister",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                Enabled = false;
                // this.MainForm.EnableToolBar(false);

                try
                {
                    StringBuilder builder = new StringBuilder();
                    foreach (KeyValuePair<string, int> stat in RegistrationHelper.Unregister(m_org, (ICrmEntity)trvPlugins.SelectedNode))
                    {
                        builder.AppendLine(string.Format("{0} {1} Unregistered Successfully", stat.Value, stat.Key));
                    }

                    trvPlugins.RemoveNode(trvPlugins.SelectedNode.NodeId);

                    MessageBox.Show(builder.ToString(), "Unregister", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    ErrorMessageForm.ShowErrorMessageBox(this, "Unable to unregister this item an error occurred.", "Unregister Error", ex);
                }
                finally
                {
                    if (trvPlugins.HasNode(nodeId))
                    {
                        trvPlugins.RefreshNode(nodeId, true);
                        SelectItem(trvPlugins.SelectedNode);
                    }

                    Enabled = true;
                    // this.MainForm.EnableToolBar(true);
                }

                trvPlugins.Focus();
            }
            else
            {
                trvPlugins.Focus();
                return;
            }
        }

        private void toolEnable_Click(object sender, EventArgs e)
        {
            if (trvPlugins.SelectedNode.NodeType != CrmTreeNodeType.Step)
            {
                return;
            }
            else if (IsNodeSystemItem(trvPlugins.SelectedNode))
            {
                ShowSystemItemError("The step cannot be enabled or disabled.");
                return;
            }

            CrmPluginStep step = (CrmPluginStep)trvPlugins.SelectedNode;
            string captionItem, messageItem;
            if (step.Enabled)
            {
                captionItem = "Disable";
                messageItem = "disable";
            }
            else
            {
                captionItem = "Enable";
                messageItem = "enable";
            }

            if (MessageBox.Show(string.Format("Are you sure you want to {0} this step?", messageItem),
                string.Format("{0} Step", captionItem),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.No)
            {
                return;
            }

            Enabled = false;
            try
            {
                RegistrationHelper.UpdateStepStatus(m_org, step.StepId, !step.Enabled);
                step.Enabled = !step.Enabled;
                UpdateEnableButton(step.Enabled);
                trvPlugins.RefreshNode(trvPlugins.SelectedNode.NodeId);

                MessageBox.Show(string.Format("Step {0}d successfully.", messageItem),
                    string.Format("{0} Step", captionItem), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ErrorMessageForm.ShowErrorMessageBox(this,
                    string.Format("Unable to {0} this item at ths time. An error occurred.", messageItem),
                    string.Format("{0} Step", captionItem), ex);
            }
            finally
            {
                Enabled = true;
            }
        }

        private void toolUpdate_Click(object sender, EventArgs e)
        {
            if (IsNodeSystemItem(trvPlugins.SelectedNode))
            {
                ShowSystemItemError("The assembly cannot be updated.");
                return;
            }

            switch (trvPlugins.SelectedNode.NodeType)
            {
                case CrmTreeNodeType.ServiceEndpoint:
                    {
                        //ServiceBusConfigForm serForm = new ServiceBusConfigForm(this.Organization, this, (CrmServiceEndpoint)trvPlugins.SelectedNode);
                        //serForm.ShowDialog();
                    }
                    break;
                case CrmTreeNodeType.Assembly:
                    {
                        PluginRegistrationForm regForm = new PluginRegistrationForm(Organization, this, (CrmPluginAssembly)trvPlugins.SelectedNode);
                        regForm.ShowDialog(ParentForm);
                    }
                    break;
                case CrmTreeNodeType.Step:
                    {
                        CrmPluginStep step = (CrmPluginStep)trvPlugins.SelectedNode;
                        CrmPlugin plugin = m_org[step.AssemblyId][step.PluginId];

                        CrmServiceEndpoint serviceEndpoint = null;
                        if (step.ServiceBusConfigurationId != Guid.Empty)
                        {
                            serviceEndpoint = m_org.ServiceEndpoints[step.ServiceBusConfigurationId];
                        }

                        StepRegistrationForm regForm = new StepRegistrationForm(Organization, this, plugin, step, serviceEndpoint);
                        regForm.ShowDialog();
                    }
                    break;
                case CrmTreeNodeType.Image:
                    {
                        ImageRegistrationForm regForm = new ImageRegistrationForm(m_org, this,
                            trvPlugins.RootNodes, (CrmPluginImage)trvPlugins.SelectedNode, trvPlugins.SelectedNode.NodeId);
                        regForm.ShowDialog();
                    }
                    break;
                default:
                    throw new NotImplementedException("NodeType = " + trvPlugins.SelectedNode.NodeType.ToString());
            }

            ICrmTreeNode node = trvPlugins.SelectedNode;
            if (node != null)
            {
                trvPlugins_SelectionChanged(sender, new CrmTreeNodeTreeEventArgs(node, TreeViewAction.Unknown));
            }
        }

        private int m_dataRowIndex = -1;
        private Guid m_dataRowId = Guid.Empty;
        private void grvData_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == m_dataRowIndex)
            {
                return;
            }
            m_dataRowIndex = e.RowIndex;

            Guid rowId = (Guid)grvData.Rows[m_dataRowIndex].Cells["Id"].Value;
            m_dataRowId = rowId;

            //Check for special cases
            switch (trvPlugins.SelectedNode.NodeType)
            {
                case CrmTreeNodeType.Message:
                case CrmTreeNodeType.MessageEntity:

                    //The Id presented in the DataGrid is not the id of the node in the tree
                    if (m_viewNodeList.ContainsKey(rowId))
                    {
                        m_dataRowId = m_viewNodeList[rowId];
                    }
                    break;
            }
        }

        private void grvData_DoubleClick(object sender, EventArgs e)
        {
            if (trvPlugins.HasNode(m_dataRowId))
            {
                trvPlugins.SelectedNode = trvPlugins[m_dataRowId];
            }
        }

        private void toolImageRegister_Click(object sender, EventArgs e)
        {
            Guid nodeId = Guid.Empty;
            if (trvPlugins.SelectedNode != null && trvPlugins.SelectedNode.NodeType != CrmTreeNodeType.Image)
            {
                nodeId = trvPlugins.SelectedNode.NodeId;
            }

            // TODO : Ajith
            // Do Validations if the Image is valid on the Step -Message and then Launch the Wizard

            var regForm = new ImageRegistrationForm(m_org, this, trvPlugins.RootNodes, null, nodeId);
            regForm.ShowDialog();
        }

        private void toolView_Click(object sender, EventArgs e)
        {
            if (sender != null && sender.GetType().IsSubclassOf(typeof(ToolStripItem)))
            {
                ToolStripItem item = (ToolStripItem)sender;
                if (item.Tag != null && item.Tag.GetType() == typeof(CrmViewType))
                {
                    propGridEntity.SelectedObject = null;
                    LoadNodes((CrmViewType)item.Tag);
                }
            }
        }

        private void toolRefresh_Click(object sender, EventArgs e)
        {
            var instruction = new WorkAsyncInfo()
            {
                Message = "Refreshing assemblies information...",
                Work = (worker, argument) =>
                {
                    try
                    {
                        OrganizationHelper.RefreshConnection(m_org, OrganizationHelper.LoadMessages(m_org), m_progressIndicator);
                        Invoke(new Action(() =>
                        {
                            propGridEntity.SelectedObject = null;
                            LoadNodes();
                        }));
                    }
                    catch (Exception ex)
                    {
                        Invoke(new Action(() =>
                        {
                            ErrorMessageForm.ShowErrorMessageBox(this, "Unable to the refresh the organization. Connection must close.", "Connection Error", ex);
                        }));
                    }
                }
            };
            WorkAsync(instruction);
        }

        private void trvPlugins_DoubleClick(object sender, CrmTreeNodeEventArgs e)
        {
            if (toolUpdate.Visible && toolUpdate.Enabled)
            {
                trvPlugins.SelectedNode = e.Node;
                if (toolUpdate.Visible && toolUpdate.Enabled)
                {
                    toolUpdate_Click(sender, e);
                }
            }
            else
            {
                //If the selected step is the Plug-in Profiler plug-in, the PluginRegistrationForm should be displayed.
                //This will allow the consumer the ability to change the isolation mode of the Plug-in Profiler.
                CrmPlugin plugin = trvPlugins.SelectedNode as CrmPlugin;
                if (null != plugin && plugin.IsProfilerPlugin)
                {
                    //Display the Plug-in Registration form
                    using (PluginRegistrationForm form = new PluginRegistrationForm(Organization, this,
                        Organization.Assemblies[plugin.AssemblyId]))
                    {
                        form.ShowDialog(ParentForm);
                    }

                    //Update the tree based on the selected node
                    ICrmTreeNode node = trvPlugins.SelectedNode;
                    if (node != null)
                    {
                        trvPlugins_SelectionChanged(sender, new CrmTreeNodeTreeEventArgs(node, TreeViewAction.Unknown));
                    }
                }
            }
        }

        private void trvPlugins_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && toolUpdate.Enabled && toolUpdate.Visible)
            {
                toolUpdate_Click(sender, e);
            }
        }

        private void toolSearch_Click(object sender, EventArgs e)
        {
            var searchForm = new SearchForm(Organization, this, trvPlugins.RootNodes, trvPlugins.SelectedNode);
            searchForm.StartPosition = FormStartPosition.CenterParent;
            searchForm.ShowDialog(this);
        }

        private void trvPlugins_NodeRemoved(object sender, CrmTreeNodeEventArgs e)
        {
            switch (e.Node.NodeType)
            {
                case CrmTreeNodeType.Step:
                    {
                        Guid stepId = e.Node.NodeId;
                        if (null != m_stepParentList && m_stepParentList.ContainsKey(stepId))
                        {
                            CrmTreeNode node = (CrmTreeNode)trvPlugins[m_stepParentList[stepId]];
                            node.RemoveChild(stepId);
                            m_stepParentList.Remove(stepId);

                            RemoveCrmTreeNodesCascadeUp(node);
                        }
                    }
                    break;
                default:
                    return;
            }

            if (null == trvPlugins.SelectedNode)
            {
                propGridEntity.SelectedObject = null;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (trvPlugins != null && trvPlugins.SelectedNode != null)
                {
                    switch (trvPlugins.SelectedNode.NodeType)
                    {
                        case CrmTreeNodeType.Assembly:
                            CrmPluginAssembly assembly = (CrmPluginAssembly)trvPlugins.SelectedNode;
                            RegistrationHelper.UpdateAssembly(m_org, assembly.Description, assembly.AssemblyId);
                            trvPlugins.RefreshNode(assembly.AssemblyId, false, false);
                            MessageBox.Show("Assembly has been updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            break;
                        case CrmTreeNodeType.Plugin:
                        case CrmTreeNodeType.WorkflowActivity:
                            CrmPlugin plugin = (CrmPlugin)trvPlugins.SelectedNode;
                            RegistrationHelper.UpdatePlugin(m_org, plugin);
                            trvPlugins.RefreshNode(plugin.PluginId, false, false);
                            MessageBox.Show("Plug-in has been updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            break;
                        default:
                            MessageBox.Show("A valid object has not been selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            break;
                    }
                }
                else
                {
                    MessageBox.Show("A valid object has not been selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                ErrorMessageForm.ShowErrorMessageBox(this, "Unable to Update the Assembly /Plugin due to an error.", "Update", ex);
            }
        }

        private void toolProfilerInstall_Click(object sender, EventArgs e)
        {
            //if (true/*!OrganizationHelper.IsProfilerSupported*/)
            //{
            //    return;
            //}

            //if (null != this.m_org.ProfilerPlugin)
            //{
            //    if (DialogResult.Yes != MessageBox.Show(this, "This will delete all previously collected profiling sessions. Continue?", "Profiler Installation",
            //        MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2))
            //    {
            //        return;
            //    }

            //    try
            //    {
            //        //Guid nodeId = OrganizationHelper.UninstallProfiler(this.m_org);
            //        if (trvPlugins.HasNode(nodeId))
            //        {
            //            trvPlugins.RemoveNode(nodeId);
            //        }

            //        trvPlugins.RefreshNodes(false);
            //    }
            //    catch (Exception ex)
            //    {
            //        ErrorMessage.ShowErrorMessageBox(this, "Unable to Uninstall the Profiler", "Profiler Installation Error", ex);
            //        return;
            //    }

            //    MessageBox.Show(this, "Profiler Uninstalled Successfully", "Profiler Installation",
            //        MessageBoxButtons.OK, MessageBoxIcon.Information);
            //}
            //else
            //{
            //    try
            //    {
            //        //trvPlugins.AddNode(Guid.Empty, OrganizationHelper.InstallProfiler(this.m_org));
            //    }
            //    catch (Exception ex)
            //    {
            //        ErrorMessage.ShowErrorMessageBox(this, "Unable to Install the Profiler", "Profiler Installation Error", ex);
            //        return;
            //    }

            //    MessageBox.Show(this, "Profiler Installed Successfully", "Profiler Installation",
            //        MessageBoxButtons.OK, MessageBoxIcon.Information);
            //}

            //if (null != trvPlugins.SelectedNode)
            //{
            //    this.SelectItem(trvPlugins.SelectedNode);
            //}
            //this.RefreshProfilerGeneralMenu();
        }

        private void toolProfilerEnable_Click(object sender, EventArgs e)
        {
            //if (true/*!OrganizationHelper.IsProfilerSupported*/)
            //{
            //    return;
            //}

            //CrmPluginStep step = trvPlugins.SelectedNode as CrmPluginStep;
            //if (null == step)
            //{
            //    return;
            //}

            //if (step.IsProfiled)
            //{
            //    //Update the status on the server
            //    //OrganizationHelper.DisableProfiler(step);

            //    //Retrieve the new status for the step
            //    OrganizationHelper.RefreshStep(step.Organization, step);
            //    if (null != step.Organization.ProfilerPlugin)
            //    {
            //        OrganizationHelper.RefreshPlugin(step.Organization, step.Organization.ProfilerPlugin);
            //    }

            //    //Update the profiler status
            //    step.ProfilerStepId = null;

            //    //Refresh the node on the tree
            //    trvPlugins.RefreshNode(step.NodeId);
            //}
            //else
            //{
            //    //Update the status on the server
            //    //Guid profilerStepId = OrganizationHelper.EnableProfiler(step);

            //    //Retrieve the new status for the step (first ensure that the profiler plug-in is known).
            //    if (null == step.Organization.ProfilerPlugin)
            //    {
            //        this.toolRefresh_Click(sender, e);
            //        return;
            //    }
            //    else
            //    {
            //        OrganizationHelper.RefreshPlugin(step.Organization, step.Organization.ProfilerPlugin);
            //        OrganizationHelper.RefreshStep(step.Organization, step);

            //        //Update the profiler status
            //        step.ProfilerStepId = profilerStepId;

            //        //Refresh the node on the tree
            //        trvPlugins.RefreshNode(step.NodeId);
            //        trvPlugins.RefreshNode(step.Organization.ProfilerPlugin.NodeId);
            //    }
            //}

            //this.SelectItem(step);
        }

        private void toolProfilerDebug_Click(object sender, EventArgs e)
        {
            //using (DebugPluginForm form = new DebugPluginForm(this.m_org))
            //{
            //    form.Text = "Debug Existing Plug-in";
            //    form.ShowDialog(this);
            //}
        }
        #endregion

        #region Properties
        public IComparer<ICrmTreeNode> CrmTreeNodeSorter
        {
            get
            {
                return m_entitySorter;
            }
        }

        public ConnectionDetail Connection
        {
            get
            {
                return m_con;
            }
        }

        public CrmOrganization Organization
        {
            get
            {
                return m_org;
            }
        }

        public bool IsAutoExpanded
        {
            get
            {
                return trvPlugins.AutoExpand;
            }
        }

        #endregion

        #region Public Methods
        public void AddServiceEndpoint(CrmServiceEndpoint serviceEndpoint)
        {
            if (serviceEndpoint == null)
            {
                throw new ArgumentNullException("serviceEndpoint");
            }

            if (m_currentView == CrmViewType.Assembly)
            {
                trvPlugins.AddNode(Guid.Empty, serviceEndpoint);
            }
        }
        public void AddAssembly(CrmPluginAssembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            if (m_currentView == CrmViewType.Assembly)
            {
                trvPlugins.AddNode(Guid.Empty, assembly);
            }
        }

        public void RefreshServiceEndpoint(CrmServiceEndpoint serviceEndpoint)
        {
            if (serviceEndpoint == null)
            {
                throw new ArgumentNullException("serviceEndpoint");
            }

            trvPlugins.RefreshNode(serviceEndpoint.NodeId, true);
            SelectItem(trvPlugins.SelectedNode);
        }
        public void RefreshAssembly(CrmPluginAssembly assembly, bool reloadChildren)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            if (m_currentView == CrmViewType.Assembly)
            {
                trvPlugins.RefreshNode(assembly.NodeId, reloadChildren);
                SelectItem(trvPlugins.SelectedNode);
            }
        }

        public void RemoveAssembly(Guid assemblyId)
        {
            if (m_currentView == CrmViewType.Assembly)
            {
                trvPlugins.RemoveNode(assemblyId);
                SelectItem(trvPlugins.SelectedNode);
            }
        }

        public void AddPlugin(CrmPlugin reg)
        {
            if (reg == null)
            {
                throw new ArgumentNullException("reg");
            }

            if (m_currentView == CrmViewType.Assembly)
            {
                trvPlugins.AddNode(reg.AssemblyId, reg);
                if (trvPlugins.SelectedNode != null && trvPlugins.SelectedNode.NodeId == reg.AssemblyId)
                {
                    SelectItem(trvPlugins.SelectedNode);
                }
            }
        }

        public void RefreshPlugin(CrmPlugin plugin)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException("plugin");
            }

            if (m_currentView == CrmViewType.Assembly)
            {
                trvPlugins.RefreshNode(plugin.NodeId, true);
                SelectItem(trvPlugins.SelectedNode);
            }
        }

        public void RemovePlugin(Guid pluginId)
        {
            if (m_currentView == CrmViewType.Assembly)
            {
                trvPlugins.RemoveNode(pluginId);
                SelectItem(trvPlugins.SelectedNode);
            }
        }

        public void AddStep(CrmPluginStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException("step");
            }

            Guid parentId;
            switch (m_currentView)
            {
                case CrmViewType.Assembly:
                    if (step.ServiceBusConfigurationId == Guid.Empty)
                    {
                        parentId = step.PluginId;
                    }
                    else
                    {
                        parentId = step.ServiceBusConfigurationId;
                    }

                    break;
                case CrmViewType.Entity:
                case CrmViewType.Message:
                    parentId = CreateCrmTreeNodes(m_currentView, step.MessageId, step.MessageEntityId, true).NodeId;

                    //Add to the step parent id list
                    if (!m_stepParentList.ContainsKey(step.StepId))
                    {
                        m_stepParentList.Add(step.StepId, parentId);
                    }

                    //Add to the CrmTreeNode
                    ((CrmTreeNode)trvPlugins[parentId]).AddChild(step);
                    break;
                default:
                    throw new NotImplementedException("View = " + m_currentView.ToString());
            }

            trvPlugins.AddNode(parentId, step);
            if (trvPlugins.SelectedNode != null && trvPlugins.SelectedNode.NodeId == parentId)
            {
                SelectItem(trvPlugins.SelectedNode);
            }
        }

        public void RefreshStep(CrmPluginStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException("step");
            }

            trvPlugins.RefreshNode(step.NodeId, true);
            SelectItem(trvPlugins.SelectedNode);
        }

        public void RemoveStep(Guid stepId)
        {
            CrmPluginStep step = (CrmPluginStep)trvPlugins[stepId];

            trvPlugins.RemoveNode(stepId);
            SelectItem(trvPlugins.SelectedNode);
        }

        public void AddImage(CrmPluginImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }

            trvPlugins.AddNode(image.StepId, image);
            if (trvPlugins.SelectedNode != null && trvPlugins.SelectedNode.NodeId == image.StepId)
            {
                SelectItem(trvPlugins.SelectedNode);
            }
        }

        public void RefreshImage(CrmPluginImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }

            trvPlugins.RefreshNode(image.NodeId, true);
            SelectItem(trvPlugins.SelectedNode);
        }

        public void RemoveImage(Guid imageId)
        {
            trvPlugins.RemoveNode(imageId);
            SelectItem(trvPlugins.SelectedNode);
        }

        public bool IsNodeSystemItem(ICrmTreeNode node)
        {
            CrmTreeNode internalNode = node as CrmTreeNode;
            if (null != internalNode)
            {
                return true;
            }

            ICrmEntity entity = node as ICrmEntity;
            if (null != entity)
            {
                return entity.IsSystemCrmEntity;
            }
            else
            {
                return false;
            }
        }

        public void ShowSystemItemError(string text)
        {
            if (text == null)
            {
                MessageBox.Show(SYSTEM_ERROR_MESSAGE, SYSTEM_ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show(string.Format("{0}\n{1}", SYSTEM_ERROR_MESSAGE, text),
                    SYSTEM_ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void SelectNode(Guid nodeId)
        {
            if (!trvPlugins.HasNode(nodeId))
            {
                throw new ArgumentException("Node is not in the tree", "nodeId");
            }

            trvPlugins.SelectedNode = trvPlugins[nodeId];
        }

        public void UpdateAutoExpand(bool newValue)
        {
            trvPlugins.AutoExpand = newValue;
        }
        #endregion

        #region Private Helper Methods
        private void SelectItem(ICrmTreeNode node)
        {
            //Reset the visibility and enabled properties because we don't what is enabled
            toolUpdate.Visible = false;
            mnuContextNodeUpdate.Visible = false;

            toolEnable.Visible = false;
            mnuContextNodeEnable.Visible = false;

            RefreshProfilerNodeMenu(null);

            bool isSystemNode = IsNodeSystemItem(trvPlugins.SelectedNode);
            if (node == null)
            {
                mnuContextNodeUnregister.Enabled = false;
                toolUnregister.Enabled = false;
                mnuContextNodeUnregister.Enabled = false;
                return;
            }
            else
            {
                //It should only be possible to unregister non-system components
                mnuContextNodeUnregister.Enabled = !isSystemNode;
                toolUnregister.Enabled = !isSystemNode;
            }

            DataTable gridTable = null;
            switch (node.NodeType)
            {
                case CrmTreeNodeType.ServiceEndpoint:
                    {
                        CrmServiceEndpoint serviceEndpoint = (CrmServiceEndpoint)node;

                        toolUpdate.Visible = true;
                        mnuContextNodeUpdate.Visible = true;
                        btnSave.Enabled = false;
                        //Load the data table and display information
                        gridTable = OrganizationHelper.CreateDataTable<CrmPluginStep>(CrmPluginStep.Columns, serviceEndpoint.Steps);
                    }
                    break;
                case CrmTreeNodeType.Assembly:
                    if (!isSystemNode)
                    {
                        CrmPluginAssembly assembly = (CrmPluginAssembly)node;

                        toolUpdate.Visible = true;
                        mnuContextNodeUpdate.Visible = true;
                        btnSave.Enabled = true;
                        //Load the data table and display information
                        gridTable = OrganizationHelper.CreateDataTable<CrmPlugin>(CrmPlugin.Columns, assembly.Plugins);
                    }
                    break;
                case CrmTreeNodeType.Plugin:
                case CrmTreeNodeType.WorkflowActivity:
                    {
                        CrmPlugin plugin = (CrmPlugin)node;
                        btnSave.Enabled = true;
                        //Load the data table and display information
                        gridTable = OrganizationHelper.CreateDataTable<CrmPluginStep>(CrmPluginStep.Columns, plugin.Steps);
                    }
                    break;
                case CrmTreeNodeType.Step:
                    {
                        CrmPluginStep step = (CrmPluginStep)node;
                        btnSave.Enabled = false;
                        UpdateEnableButton(step.Enabled);

                        if (true/*!OrganizationHelper.IsProfilerSupported*/ ||
                            !(step.IsProfiled || step.Organization.Plugins[step.PluginId].IsProfilerPlugin))
                        {
                            toolEnable.Visible = true;
                            mnuContextNodeEnable.Visible = true;
                        }

                        toolUpdate.Visible = true;
                        mnuContextNodeUpdate.Visible = true;

                        RefreshProfilerNodeMenu(step);

                        //Load the data table and display information
                        gridTable = OrganizationHelper.CreateDataTable<CrmPluginImage>(CrmPluginImage.Columns, step.Images);
                    }
                    break;
                case CrmTreeNodeType.Image:
                    {
                        toolUpdate.Visible = true;
                        mnuContextNodeUpdate.Visible = true;
                        CrmPluginImage image = (CrmPluginImage)node;
                        btnSave.Enabled = false;
                        //Load the data table and display information
                        gridTable = null;
                    }
                    break;
                case CrmTreeNodeType.Message:
                case CrmTreeNodeType.MessageEntity:
                    {
                        toolUnregister.Enabled = false;
                        mnuContextNodeUnregister.Enabled = false;
                        btnSave.Enabled = false;
                        CrmTreeNode treeNode = (CrmTreeNode)node;
                        switch (treeNode.ChildNodeType)
                        {
                            case CrmTreeNodeType.Message:
                                {
                                    gridTable = OrganizationHelper.CreateDataTable<CrmMessage>(CrmMessage.Columns,
                                        (CrmMessage[])treeNode.ToEntityArray(CrmTreeNodeType.Message));
                                }
                                break;
                            case CrmTreeNodeType.MessageEntity:
                                {
                                    gridTable = OrganizationHelper.CreateDataTable<CrmMessageEntity>(CrmMessageEntity.Columns,
                                        (CrmMessageEntity[])treeNode.ToEntityArray(CrmTreeNodeType.MessageEntity));
                                }
                                break;
                            case CrmTreeNodeType.Step:
                                {
                                    gridTable = OrganizationHelper.CreateDataTable<CrmPluginStep>(CrmPluginStep.Columns,
                                        (CrmPluginStep[])treeNode.ToEntityArray(CrmTreeNodeType.Step));
                                }
                                break;
                            default:
                                gridTable = null;
                                break;
                        }
                    }
                    break;
                default:

                    throw new NotImplementedException("NodeType = " + node.NodeType.ToString());
            }

            //Update the properties grid
            {
                CrmTreeNode treeNode = node as CrmTreeNode;
                if (null == treeNode)
                {
                    propGridEntity.SelectedObject = node;
                }
                else
                {
                    propGridEntity.SelectedObject = treeNode.Entity;
                }
            }

            m_dataRowIndex = -1;
            m_dataRowId = Guid.Empty;
            if (gridTable == null)
            {
                grpGrid.Visible = false;
                splitterDisplay.Height = grpGrid.Bottom - splitterDisplay.Top;
            }
            else
            {
                grpGrid.Visible = true;
                splitterDisplay.Height = grpGrid.Top - grpPlugins.Margin.Bottom - grpGrid.Margin.Top - splitterDisplay.Top;

                //Create the list of values

                //Create the new DataSet
                DataSet set = new DataSet("Grid");
                set.Tables.Add(gridTable);

                grvData.DataSource = set.DefaultViewManager;
                grvData.DataMember = gridTable.TableName;
                grvData.Columns["Id"].Visible = false;
            }
        }

        private void LoadNodes()
        {
            LoadNodes(m_currentView);
        }

        private void LoadNodes(CrmViewType view)
        {
            ToolStripItem currentCheckedItem = null;
            ToolStripItem newCheckedItem = null;

            foreach (ToolStripItem item in toolView.DropDownItems)
            {
                if (item.Tag != null && item.Tag.GetType() == typeof(CrmViewType))
                {
                    var checkedProp = item.GetType().GetProperty("Checked", typeof(bool));
                    if (checkedProp != null)
                    {
                        if ((bool)checkedProp.GetValue(item, null))
                        {
                            currentCheckedItem = item;
                        }
                        else if ((CrmViewType)item.Tag == view)
                        {
                            newCheckedItem = item;
                        }
                    }
                }

                if (currentCheckedItem != null && newCheckedItem != null)
                {
                    break;
                }
            }

            // Create the new view
            Enabled = false;
            try
            {
                switch (view)
                {
                    case CrmViewType.Assembly:
                        m_rootNodeList = null;
                        m_stepParentList = null;
                        m_viewNodeList = null;

                        List<ICrmTreeNode> nodes = new List<ICrmTreeNode>();
                        foreach (CrmPluginAssembly assembly in Organization.Assemblies)
                        {
                            // If the same assembly name used for any other custom plugin assembly then that need to be added
                            if ((CrmServiceEndpoint.ServiceBusPluginAssemblyName != assembly.Name || 0 != assembly.CustomizationLevel) &&
                                !assembly.IsProfilerAssembly)
                            {
                                nodes.Add(assembly);
                            }
                        }

                        nodes.AddRange(Organization.ServiceEndpoints.ToArray());

                        if (null != Organization.ProfilerPlugin)
                        {
                            nodes.Add(Organization.ProfilerPlugin);
                        }

                        trvPlugins.LoadNodes(nodes.ToArray());
                        break;
                    case CrmViewType.Entity:
                    case CrmViewType.Message:
                        {
                            //Create the Root Node List
                            if (null == m_rootNodeList)
                            {
                                m_rootNodeList = new Dictionary<string, CrmTreeNode>();
                            }
                            else
                            {
                                m_rootNodeList.Clear();
                            }

                            //Create the Step Parent Node List
                            if (null == m_stepParentList)
                            {
                                m_stepParentList = new Dictionary<Guid, Guid>();
                            }
                            else
                            {
                                m_stepParentList.Clear();
                            }

                            //Create the View Node List
                            if (null == m_viewNodeList)
                            {
                                m_viewNodeList = new Dictionary<Guid, Guid>();
                            }
                            else
                            {
                                m_viewNodeList.Clear();
                            }

                            //Retrieve the of steps
                            foreach (var step in Organization.Steps)
                            {
                                if (step.MessageId != Guid.Empty)
                                {
                                    CrmTreeNode parentNode = CreateCrmTreeNodes(view, step.MessageId, step.MessageEntityId, false);

                                    parentNode.AddChild(step);
                                    m_stepParentList.Add(step.StepId, parentNode.NodeId);
                                }
                            }

                            var nodeList = new CrmTreeNode[m_rootNodeList.Count];
                            m_rootNodeList.Values.CopyTo(nodeList, 0);

                            trvPlugins.LoadNodes(nodeList);
                        }
                        break;
                    default:
                        throw new NotImplementedException("View = " + view.ToString());
                }

                m_currentView = view;

                if (currentCheckedItem != null)
                {
                    currentCheckedItem.GetType().GetProperty("Checked",
                        typeof(bool)).SetValue(currentCheckedItem, false, null);
                }

                if (newCheckedItem != null)
                {
                    newCheckedItem.GetType().GetProperty("Checked",
                        typeof(bool)).SetValue(newCheckedItem, true, null);
                }
            }
            catch (Exception ex)
            {
                ErrorMessageForm.ShowErrorMessageBox(this, "Unable to change the view", "View Error", ex);
            }
            finally
            {
                Enabled = true;
            }
        }

        private CrmTreeNode CreateCrmTreeNodes(CrmViewType view, Guid messageId, Guid messageEntityId, bool addToTree)
        {
            if (Guid.Empty == messageId)
            {
                throw new ArgumentException("Invalid Guid", "messageId");
            }

            CrmTreeNode rootNode, childNode;
            switch (view)
            {
                case CrmViewType.Message:
                    {
                        rootNode = new CrmTreeNode(m_org.Messages[messageId]);
                        if (Guid.Empty == messageEntityId)
                        {
                            childNode = new CrmTreeNode(new CrmMessageEntity(Organization, messageId, Guid.Empty,
                                "none", "none", CrmPluginStepDeployment.Both, null, null));
                        }
                        else
                        {
                            childNode = new CrmTreeNode(m_org.MessageEntities[messageEntityId]);
                        }
                    }
                    break;
                case CrmViewType.Entity:
                    {
                        if (Guid.Empty == messageEntityId)
                        {
                            rootNode = new CrmTreeNode(new CrmMessageEntity(Organization, messageId, Guid.Empty, "none", "none",
                                CrmPluginStepDeployment.Both, null, null));
                        }
                        else
                        {
                            rootNode = new CrmTreeNode(m_org.MessageEntities[messageEntityId]);
                        }
                        childNode = new CrmTreeNode(m_org.Messages[messageId]);
                    }
                    break;
                default:
                    throw new NotImplementedException("View = " + view.ToString());
            }

            Guid rootNodeId = rootNode.NodeId;
            Guid childNodeId = childNode.NodeId;

            if (m_rootNodeList.ContainsKey(rootNode.NodeText))
            {
                rootNode = m_rootNodeList[rootNode.NodeText];
            }
            else
            {
                rootNode.NodeId = Guid.NewGuid();
                m_rootNodeList.Add(rootNode.NodeText, rootNode);

                if (addToTree)
                {
                    trvPlugins.AddNode(Guid.Empty, rootNode);
                }
            }

            if (rootNode.HasChild(childNode.NodeText))
            {
                childNode = (CrmTreeNode)rootNode[childNode.NodeText];
            }
            else
            {
                childNode.NodeId = Guid.NewGuid();
                rootNode.AddChild(childNode);

                if (addToTree)
                {
                    trvPlugins.AddNode(rootNode.NodeId, childNode);
                }
            }

            if (!m_viewNodeList.ContainsKey(rootNodeId))
            {
                m_viewNodeList.Add(rootNodeId, rootNode.NodeId);
            }

            if (!m_viewNodeList.ContainsKey(childNodeId))
            {
                m_viewNodeList.Add(childNodeId, childNode.NodeId);
            }

            return childNode;
        }

        private void UpdateEnableButton(bool currentlyEnabled)
        {
            string imageKey;
            if (currentlyEnabled)
            {
                toolEnable.Text = "&Disable";
                imageKey = "disableStep";
            }
            else
            {
                toolEnable.Text = "&Enable";
                imageKey = "enableStep";
            }

            toolEnable.Image = imlEnableImages.Images[imageKey];

            mnuContextNodeEnable.Text = toolEnable.Text;
            mnuContextNodeEnable.Image = toolEnable.Image;
        }

        /// <summary>
        /// Removes the given CrmTreeNode. If its parent does not have any other children, it will also be removed.
        /// </summary>
        /// <param name="nodeId">Node to be removed</param>
        private void RemoveCrmTreeNodesCascadeUp(CrmTreeNode node)
        {
            if (null == node)
            {
                throw new ArgumentNullException("node");
            }

            //Check if we are going to be removing this node
            while (null != node && 0 == node.ChildCount)
            {
                //Retrieve the parent node
                CrmTreeNode parentNode;
                if (Guid.Empty == node.ParentNodeId)
                {
                    parentNode = null;

                    //This is a root element, remove it from the list
                    if (m_rootNodeList.ContainsKey(node.NodeText))
                    {
                        m_rootNodeList.Remove(node.NodeText);
                    }
                }
                else
                {
                    parentNode = (CrmTreeNode)trvPlugins[node.ParentNodeId];
                    parentNode.RemoveChild(node.NodeText);
                }

                //Remove the node from the tree
                trvPlugins.RemoveNode(node.NodeId);

                //Set the current node to be the parent node
                node = parentNode;
            }
        }

        private void RefreshProfilerGeneralMenu()
        {
            if (false/*OrganizationHelper.IsProfilerSupported*/)
            {
                mnuContextGeneralProfilerInstall.Visible = true;
                mnuContextGeneralSepProfiler.Visible = true;

                toolProfilerInstall.Visible = true;
                toolProfilerSep.Visible = true;
                toolProfilerDebug.Visible = true;

                if (null != m_org.ProfilerPlugin)
                {
                    mnuContextGeneralProfilerInstall.Text = "Stop All P&rofiling && Uninstall Profiler";
                    mnuContextGeneralProfilerInstall.Image = imlEnableImages.Images["UninstallProfiler"];

                    toolProfilerInstall.Image = imlEnableImages.Images["UninstallProfiler"];
                    toolProfilerInstall.Text = "Uninstall Pr&ofiler";
                }
                else
                {
                    mnuContextGeneralProfilerInstall.Text = "Install P&rofiler";
                    mnuContextGeneralProfilerInstall.Image = imlEnableImages.Images["InstallProfiler"];

                    toolProfilerInstall.Image = imlEnableImages.Images["InstallProfiler"];
                    toolProfilerInstall.Text = "Install Pr&ofiler";
                }
            }
            else
            {
                mnuContextGeneralProfilerInstall.Visible = false;
                mnuContextGeneralSepProfiler.Visible = false;

                toolProfilerInstall.Visible = false;
                toolProfilerDebug.Visible = false;
                toolProfilerSep.Visible = false;
            }
        }

        private void RefreshProfilerNodeMenu(CrmPluginStep step)
        {
            mnuContextNodeProfilerEnable.Visible = false;
            mnuContextNodeSepProfiler.Visible = false;

            toolProfilerEnable.Visible = false;

            if (false/*OrganizationHelper.IsProfilerSupported*/ && null != step &&
                null != step.Organization && null != step.Organization.ProfilerPlugin)
            {
                if (!step.Organization.Plugins[step.PluginId].IsProfilerPlugin &&
                    (step.IsProfiled || step.Enabled))
                {
                    mnuContextNodeProfilerEnable.Visible = true;
                    mnuContextNodeSepProfiler.Visible = true;

                    toolProfilerEnable.Visible = true;

                    if (step.IsProfiled)
                    {
                        mnuContextNodeProfilerEnable.Text = "Stop P&rofiling";
                        mnuContextNodeProfilerEnable.Image = imlEnableImages.Images["DisableProfiler"];

                        toolProfilerEnable.Text = "Stop Pro&filing";
                        toolProfilerEnable.Image = imlEnableImages.Images["DisableProfiler"];
                    }
                    else
                    {
                        mnuContextNodeProfilerEnable.Text = "Start P&rofiling";
                        mnuContextNodeProfilerEnable.Image = imlEnableImages.Images["EnableProfiler"];

                        toolProfilerEnable.Text = "Pro&file";
                        toolProfilerEnable.Image = imlEnableImages.Images["EnableProfiler"];
                    }
                }
            }
        }
        #endregion

        #region Private Classes & Enums
        private sealed class CrmEntitySorter : IComparer<ICrmTreeNode>
        {
            public int Compare(ICrmTreeNode node1, ICrmTreeNode node2)
            {
                if (node1 == null)
                {
                    return -1;
                }
                else if (node2 == null)
                {
                    return 1;
                }
                else if (node1.NodeType != node2.NodeType)
                {
                    return ((int)node1.NodeType - (int)node2.NodeType);
                }
                else if (node1.NodeType == CrmTreeNodeType.Step)
                {
                    CrmPluginStep step1 = (CrmPluginStep)node1;
                    CrmPluginStep step2 = (CrmPluginStep)node2;

                    if (step1.Rank == step2.Rank)
                    {
                        if (step1.CreatedOn == null && step2.CreatedOn == null)
                        {
                            return 0;
                        }
                        else if (step1.CreatedOn == null)
                        {
                            return -1;
                        }
                        else if (step2.CreatedOn == null)
                        {
                            return 1;
                        }
                        else
                        {
                            return DateTime.Compare((DateTime)step1.CreatedOn, (DateTime)step2.CreatedOn);
                        }
                    }
                    else if (step1.Rank < step2.Rank)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else
                {
                    return string.Compare(GetNodeText(node1), GetNodeText(node2), false);
                }
            }

            #region Private Helper Methods
            private string GetNodeText(ICrmTreeNode node)
            {
                if (node == null || node.NodeText == null)
                {
                    return null;
                }
                else
                {
                    return node.NodeText;
                }
            }
            #endregion
        }

        private enum CrmViewType
        {
            Assembly,
            Message,
            Entity
        }

        private sealed class CrmTreeNode : ICrmTreeNode
        {
            private ICrmEntity m_entity;
            private Guid m_parentNodeId = Guid.Empty;
            private Guid m_origNodeId;
            private Guid m_nodeId;
            private string m_nodeText;
            private CrmTreeNodeType m_type;
            private string m_typeLabel;
            private CrmTreeNodeImageType m_imageType;
            private CrmTreeNodeImageType m_selectedImageType;

            private CrmTreeNodeType m_childType = CrmTreeNodeType.None;
            private Dictionary<string, CrmTreeNode> m_childList = null;
            private Dictionary<Guid, CrmPluginStep> m_stepList = null;

            public CrmTreeNode(CrmMessage message)
            {
                if (message == null)
                {
                    throw new ArgumentNullException("message");
                }

                m_entity = message;
                m_nodeId = message.MessageId;
                m_nodeText = message.Name;
                m_type = CrmTreeNodeType.Message;
                m_typeLabel = "Message";
                m_imageType = CrmTreeNodeImageType.Message;
                m_selectedImageType = CrmTreeNodeImageType.MessageSelected;

                m_origNodeId = m_nodeId;

                UpdateNodeText();
            }

            public CrmTreeNode(CrmMessageEntity msgEntity)
            {
                if (msgEntity == null)
                {
                    throw new ArgumentNullException("entity");
                }

                m_entity = msgEntity;
                m_nodeId = msgEntity.MessageEntityId;
                if (string.IsNullOrEmpty(msgEntity.PrimaryEntity))
                {
                    m_nodeText = "No Entity";
                }
                else if (string.IsNullOrEmpty(msgEntity.SecondaryEntity) ||
                    msgEntity.SecondaryEntity.Equals("none", StringComparison.CurrentCultureIgnoreCase))
                {
                    m_nodeText = msgEntity.PrimaryEntity;
                }
                else
                {
                    m_nodeText = string.Format("{0} / {1}", msgEntity.PrimaryEntity, msgEntity.SecondaryEntity);
                }
                m_type = CrmTreeNodeType.MessageEntity;
                m_typeLabel = "Entity";
                m_imageType = CrmTreeNodeImageType.MessageEntity;
                m_selectedImageType = CrmTreeNodeImageType.MessageEntitySelected;

                m_origNodeId = m_nodeId;

                UpdateNodeText();
            }

            public CrmTreeNode(Guid nodeId, string text, CrmTreeNodeType type,
                CrmTreeNodeImageType imageType, CrmTreeNodeImageType selectedImageType)
            {
                if (text == null)
                {
                    throw new ArgumentNullException("text");
                }

                m_entity = null;
                m_nodeId = nodeId;
                m_nodeText = text;
                m_type = type;
                m_imageType = imageType;
                m_selectedImageType = selectedImageType;

                m_origNodeId = m_nodeId;

                UpdateNodeText();
            }

            public void AddChild(CrmPluginStep node)
            {
                if (node == null)
                {
                    throw new ArgumentNullException("node");
                }
                else if (m_childType != CrmTreeNodeType.None && m_childType != node.NodeType)
                {
                    throw new ArgumentException("Child Node Type has already been determined");
                }
                else if (m_childType == CrmTreeNodeType.None)
                {
                    m_childType = node.NodeType;
                    m_stepList = new Dictionary<Guid, CrmPluginStep>();
                }

                m_stepList.Add(node.NodeId, node);
            }

            public void AddChild(CrmTreeNode node)
            {
                if (node == null)
                {
                    throw new ArgumentNullException("node");
                }
                else if (m_childType != CrmTreeNodeType.None && m_childType != node.NodeType)
                {
                    throw new ArgumentException("Child Node Type has already been determined");
                }
                else if (m_childType == CrmTreeNodeType.None)
                {
                    m_childType = node.NodeType;
                    m_childList = new Dictionary<string, CrmTreeNode>();
                }

                node.m_parentNodeId = m_nodeId;
                m_childList.Add(node.NodeText, node);
            }

            public void RemoveChild(Guid key)
            {
                if (m_stepList == null)
                {
                    throw new ArgumentException("Id is not in the list");
                }
                else
                {
                    m_stepList.Remove(key);
                }
            }

            public void RemoveChild(string key)
            {
                if (m_childList == null)
                {
                    throw new ArgumentException("Id is not in the list");
                }
                else
                {
                    m_childList.Remove(key);
                }
            }

            public bool HasChild(Guid id)
            {
                if (m_stepList != null)
                {
                    return m_stepList.ContainsKey(id);
                }
                else
                {
                    return false;
                }
            }

            public bool HasChild(string nodeText)
            {
                if (m_childList != null)
                {
                    return m_childList.ContainsKey(nodeText);
                }
                else
                {
                    return false;
                }
            }

            public CrmPluginStep this[Guid id]
            {
                get
                {
                    return m_stepList[id];
                }
            }

            public CrmTreeNode this[string text]
            {
                get
                {
                    return m_childList[text];
                }
            }

            public CrmTreeNodeType ChildNodeType
            {
                get
                {
                    return m_childType;
                }
            }

            public ICrmEntity[] ToEntityArray(CrmTreeNodeType type)
            {
                if (m_childType == CrmTreeNodeType.None)
                {
                    m_childType = type;
                }
                else if (m_childType != type)
                {
                    throw new ArgumentNullException("Child type already determined");
                }

                ICrmEntity[] childList;
                int childIndex = 0;
                switch (type)
                {
                    case CrmTreeNodeType.Message:
                        childList = new CrmMessage[m_childList.Count];
                        foreach (CrmTreeNode childNode in m_childList.Values)
                        {
                            childList[childIndex++] = (CrmMessage)childNode.Entity;
                        }
                        break;
                    case CrmTreeNodeType.MessageEntity:
                        childList = new CrmMessageEntity[m_childList.Count];
                        foreach (CrmTreeNode childNode in m_childList.Values)
                        {
                            childList[childIndex++] = (CrmMessageEntity)childNode.Entity;
                        }
                        break;
                    case CrmTreeNodeType.Step:
                        childList = new CrmPluginStep[m_stepList.Count];
                        m_stepList.Values.CopyTo((CrmPluginStep[])childList, 0);
                        break;
                    default:
                        throw new NotImplementedException("Type = " + type.ToString());
                }

                return childList;
            }

            public Guid ParentNodeId
            {
                get
                {
                    return m_parentNodeId;
                }
            }

            public Guid OriginalNodeId
            {
                get
                {
                    return m_origNodeId;
                }
            }

            public int ChildCount
            {
                get
                {
                    if (null != m_childList)
                    {
                        return m_childList.Count;
                    }
                    else if (null != m_stepList)
                    {
                        return m_stepList.Count;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }

            #region ICrmTreeNode Members
            public ICrmEntity Entity
            {
                get
                {
                    return m_entity;
                }
            }

            public Guid NodeId
            {
                get
                {
                    return m_nodeId;
                }
                set
                {
                    m_nodeId = value;

                    if (null != m_childList)
                    {
                        foreach (CrmTreeNode childNode in m_childList.Values)
                        {
                            childNode.m_parentNodeId = value;
                        }
                    }
                }
            }

            public string NodeText
            {
                get
                {
                    return m_nodeText;
                }
            }

            public CrmTreeNodeType NodeType
            {
                get
                {
                    return m_type;
                }
            }

            public string NodeTypeLabel
            {
                get
                {
                    return m_typeLabel;
                }
            }

            public ICrmTreeNode[] NodeChildren
            {
                get
                {
                    if (m_stepList != null)
                    {
                        CrmPluginStep[] nodeList = new CrmPluginStep[m_stepList.Count];
                        m_stepList.Values.CopyTo(nodeList, 0);

                        return nodeList;
                    }
                    else if (m_childList != null)
                    {
                        CrmTreeNode[] nodeList = new CrmTreeNode[m_childList.Count];
                        m_childList.Values.CopyTo(nodeList, 0);

                        return nodeList;
                    }
                    else
                    {
                        return new ICrmTreeNode[0];
                    }
                }
            }

            public CrmTreeNodeImageType NodeImageType
            {
                get
                {
                    return m_imageType;
                }
            }

            public CrmTreeNodeImageType NodeSelectedImageType
            {
                get
                {
                    return m_selectedImageType;
                }
            }
            #endregion

            #region Private Helpers
            private void UpdateNodeText()
            {
                string prefix;
                switch (m_type)
                {
                    case CrmTreeNodeType.Message:
                        prefix = "Message";
                        break;
                    case CrmTreeNodeType.MessageEntity:
                        prefix = "Entity";
                        break;
                    default:
                        throw new NotImplementedException("NodeType = " + m_type.ToString());
                }

                m_nodeText = string.Format("({0}) {1}", prefix, m_nodeText);
            }
            #endregion
        }
        #endregion

        private void toolClose_Click(object sender, EventArgs e)
        {
            CloseTool();
        }
    }
}
