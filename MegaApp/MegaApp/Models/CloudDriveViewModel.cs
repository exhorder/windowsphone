﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Windows.Security.Authentication.OnlineId;
using mega;
using MegaApp.Classes;
using MegaApp.MegaApi;
using MegaApp.Pages;
using MegaApp.Resources;
using MegaApp.Services;
using Microsoft.Phone.Shell;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.InputPrompt;

namespace MegaApp.Models
{
    public class CloudDriveViewModel : BaseViewModel
    {
        private readonly MegaSDK _megaSdk;

        public CloudDriveViewModel(MegaSDK megaSdk)
        {
            this._megaSdk = megaSdk;
            this.MoveItemMode = false;
            this.CurrentRootNode = null;
            this.ChildNodes = new ObservableCollection<NodeViewModel>();
            this.BreadCrumbs = new ObservableCollection<NodeViewModel>();

            this.RemoveItemCommand = new DelegateCommand(this.RemoveItem);
            this.RenameItemCommand = new DelegateCommand(this.RenameItem);
            this.GetPreviewLinkItemCommand = new DelegateCommand(this.GetPreviewLink);
            this.PreviewItemCommand = new DelegateCommand(this.PreviewItem);
        }

        #region Commands

        public ICommand RemoveItemCommand { get; set; }
        public ICommand GetPreviewLinkItemCommand { get; set; }
        public ICommand DownloadItemCommand { get; set; }
        public ICommand RenameItemCommand { get; set; }

        public ICommand PreviewItemCommand { get; set; }

        #endregion

        #region Public Methods

        public void GoFolderUp()
        {
            MNode parentNode = this._megaSdk.getParentNode(this.CurrentRootNode.GetBaseNode());

            if (parentNode == null || parentNode.getType() == MNodeType.TYPE_UNKNOWN )
                parentNode = this._megaSdk.getRootNode();
            
            this.CurrentRootNode = new NodeViewModel(App.MegaSdk, parentNode);
            CalculateBreadCrumbs(this.CurrentRootNode);
        }

        public void GoToFolder(NodeViewModel folder)
        {
            this.CurrentRootNode = folder;
            CalculateBreadCrumbs(this.CurrentRootNode);
            NavigateService.NavigateTo(typeof(MainPage), NavigationParameter.Browsing, new Dictionary<string, string> { { "Id", Guid.NewGuid().ToString("N") } });
        }

        public void MoveItem(NodeViewModel selectedRootNode)
        {
            if (!IsUserOnline()) return;

            if(this._megaSdk.checkMove(FocusedNode.GetBaseNode(), selectedRootNode.GetBaseNode()).getErrorCode() == MErrorType.API_OK)
                this._megaSdk.moveNode(FocusedNode.GetBaseNode(), selectedRootNode.GetBaseNode(), new MoveNodeRequestListener(this));
        }

        public async void OpenLink()
        {
            if (!IsUserOnline()) return;

            var inputPromptClosedEventArgs = await RadInputPrompt.ShowAsync(new string[] { UiResources.OpenButton, UiResources.CancelButton }, UiResources.OpenLink, vibrate: false);

            if (inputPromptClosedEventArgs.Result != DialogResult.OK) return;

            this._megaSdk.getPublicNode(inputPromptClosedEventArgs.Text, new GetPublicNodeRequestListener(this));
        }

        public void ImportLink(string link)
        {
            this._megaSdk.importFileLink(link, CurrentRootNode.GetBaseNode(), new ImportFileRequestListener(this));;
        }

        public void LoadNodes()
        {
            this.ChildNodes.Clear();

            MNodeList nodeList = this._megaSdk.getChildren(this.CurrentRootNode.GetBaseNode());

            for (int i = 0; i < nodeList.size(); i++)
            {
                ChildNodes.Add(new NodeViewModel(this._megaSdk, nodeList.get(i)));
            }
        }

        public void OnNodeTap(NodeViewModel node)
        {
            switch (node.Type)
            {
                case MNodeType.TYPE_FOLDER:
                    {
                        SelectFolder(node);
                        break;
                    }
            }
        }

        public async void AddFolder(NodeViewModel parentNode)
        {
            if (!IsUserOnline()) return;

            var inputPromptClosedEventArgs = await RadInputPrompt.ShowAsync(new string[] {UiResources.AddButton, UiResources.CancelButton}, UiResources.CreateFolder, vibrate: false);

            if (inputPromptClosedEventArgs.Result != DialogResult.OK) return;

            this._megaSdk.createFolder(inputPromptClosedEventArgs.Text, parentNode.GetBaseNode(), new CreateFolderRequestListener(this));
        }

        public void FetchNodes(NodeViewModel rootRefreshNode = null)
        {
            this.ChildNodes.Clear();

            var fetchNodesRequestListener = new FetchNodesRequestListener(this, rootRefreshNode);
            this._megaSdk.fetchNodes(fetchNodesRequestListener);
        }

        public void SelectFolder(NodeViewModel selectedNode)
        {
            this.CurrentRootNode = selectedNode;
            CalculateBreadCrumbs(this.CurrentRootNode);
            // Create unique uri string to navigate
            NavigateService.NavigateTo(typeof(MainPage), NavigationParameter.Browsing, new Dictionary<string, string> {{"Id", Guid.NewGuid().ToString("N")}});
        }

        #endregion

        #region Private Methods

        private void CalculateBreadCrumbs(NodeViewModel currentRootNode)
        {
            this.BreadCrumbs.Clear();

            if (currentRootNode.Type == MNodeType.TYPE_ROOT) return;

            this.BreadCrumbs.Add(currentRootNode);

            MNode parentNode = currentRootNode.GetBaseNode();
            while ((parentNode = this._megaSdk.getParentNode(parentNode)).getType() !=
                   MNodeType.TYPE_ROOT)
            {
                this.BreadCrumbs.Insert(0, new NodeViewModel(this._megaSdk, parentNode));
            }

        }

        private void GetPreviewLink(object obj)
        {
            if (!IsUserOnline()) return;
            
            this._megaSdk.exportNode(FocusedNode.GetBaseNode(), new ExportNodeRequestListener());

        }

        private void PreviewItem(object obj)
        {
            NavigateService.NavigateTo(typeof(PreviewImagePage), NavigationParameter.Normal);
        }

        private void RemoveItem(object obj)
        {
            if (!IsUserOnline()) return;

            if (MessageBox.Show(String.Format(AppMessages.RemoveItemQuestion, FocusedNode.Name), AppMessages.RemoveItemQuestion_Title, MessageBoxButton.OKCancel) ==
                MessageBoxResult.Cancel) return;

            this._megaSdk.moveNode(FocusedNode.GetBaseNode(), this._megaSdk.getRubbishNode(), new RemoveNodeRequestListener(this));
        }

        private async void RenameItem(object obj)
        {
            if (!IsUserOnline()) return;
            
            var textboxStyle = new Style(typeof(RadTextBox));
            textboxStyle.Setters.Add(new Setter(TextBox.TextProperty, FocusedNode.Name));

            var inputPromptClosedEventArgs = await RadInputPrompt.ShowAsync(new string[] { UiResources.RenameButton, UiResources.CancelButton }, UiResources.RenameItem,
                vibrate: false, inputStyle: textboxStyle);

            if (inputPromptClosedEventArgs.Result != DialogResult.OK) return;

            this._megaSdk.renameNode(FocusedNode.GetBaseNode(), inputPromptClosedEventArgs.Text, new RenameNodeRequestListener(this));
        }

        private bool IsUserOnline()
        {
            bool isOnline = Convert.ToBoolean(this._megaSdk.isLoggedIn());

            if (!isOnline)
                MessageBox.Show(AppMessages.UserNotOnline, AppMessages.UserNotOnline_Title, MessageBoxButton.OK);

            return isOnline;
        }

        #endregion

        #region Properties

        public ObservableCollection<NodeViewModel> ChildNodes { get; set; }
        public ObservableCollection<NodeViewModel> BreadCrumbs { get; set; }

        public NodeViewModel CurrentRootNode { get; set; }

        public NodeViewModel FocusedNode { get; set; }

        public NodeViewModel BreadCrumbNode { get; set; }

        private bool _moveItemMode;
        public bool MoveItemMode
        {
            get { return _moveItemMode; }
            set
            {
                _moveItemMode = value;
                OnPropertyChanged("MoveItemMode");
            }
        }
        #endregion
      
    }
}
