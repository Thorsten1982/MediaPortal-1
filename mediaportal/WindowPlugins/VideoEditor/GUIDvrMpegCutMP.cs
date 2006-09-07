/* 
 *	Copyright (C) 2005-2006 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using MediaPortal.Util;
using DirectShowLib.SBE;

namespace WindowPlugins.DvrMpegCut
{
	public class GUIDvrMpegCutMP : GUIWindow
	{
		readonly int windowID = 170601;

		#region GUIControls
		[SkinControlAttribute(23)]
		protected GUILabelControl titelLbl = null;
		[SkinControlAttribute(24)]
		protected GUIButtonControl backBtn = null;
		//[SkinControlAttribute(25)]
		//protected GUIButtonControl cancelBtn = null;
		[SkinControlAttribute(32)]
		protected GUILabelControl startTime = null;
		[SkinControlAttribute(34)]
		protected GUILabelControl oldDurationLbl = null;
		[SkinControlAttribute(101)]
		protected GUIListControl videoListLct = null;
    [SkinControlAttribute(102)]
    protected GUISpinControl joinCutSpinCtrl = null;
		[SkinControlAttribute(99)]
		protected GUIVideoControl videoWindow = null;
    [SkinControlAttribute(103)]
    protected GUIListControl joinListCtrl = null;
    [SkinControlAttribute(104)]
    protected GUIButtonControl startJoinBtn = null;
		#endregion

		#region Own Variables
		const int maxDrives = 50;
		int cntDrives = 0;
		string[] drives = new string[maxDrives];
		string currentFolder = "";
    string lastUsedFolder = "";
    VirtualDirectory directory = new VirtualDirectory();
		ArrayList extensions;
		DvrMpegCutPreview cutScr;
		List<System.IO.FileInfo> joiningList;
		#endregion

		public GUIDvrMpegCutMP()
		{ }

		#region Overrides
		public override int GetID
		{
			get
			{
				return windowID;
			}
			set
			{
			}
		}
		public override bool Init()
		{
			try
			{

				bool init = Load(GUIGraphicsContext.Skin + @"\DvrCutMP.xml");
				if (init)
				{
					GetDrives();
				}
				return init;
			}
			catch
			{
				//MessageBox.Show("Fehler","Fehler",MessageBoxButtons.OKCancel);
				return false;
			}
		}
		protected override void OnPageLoad()
		{
			base.OnPageLoad();
			try 
			{
				extensions = new ArrayList();
				extensions.Add(".dvr-ms");
				extensions.Add(".mpeg");
				extensions.Add(".mpg");
				videoListLct.Clear();
				videoListLct.UpdateLayout();
        startJoinBtn.IsEnabled = false;
        joinListCtrl.IsVisible = false;
				if (joinCutSpinCtrl.GetLabel() == "Zusammenfügen")
				{
					joinListCtrl.IsVisible = true;
					startJoinBtn.IsEnabled = true;
					titelLbl.Label = GUILocalizeStrings.Get(2074);
				}
				joiningList = new List<System.IO.FileInfo>();
        LoadShares();
				LoadDrives();
			}
			catch (Exception e)
			{
				Log.Error("DvrMpegCut: (OnPageLoad) " + e.StackTrace);
			}

		}
		protected override void OnPageDestroy(int new_windowId)
		{
			g_Player.Release();
			base.OnPageDestroy(new_windowId);
		}
		protected override void OnClicked(int controlId, GUIControl control, MediaPortal.GUI.Library.Action.ActionType actionType)
		{
			if (control == backBtn)
			{
				//Abbrechen();
				GUIWindowManager.ShowPreviousWindow();
			}
			/*if (control == abbrechenBtn)
			{
				Abbrechen();
			}*/
			if (control == videoListLct)
			{
				GUIListItem item = videoListLct.SelectedListItem;
				//System.Windows.Forms.MessageBox.Show(item.Path);
				if (!item.IsFolder)
				{
					if (joinCutSpinCtrl.GetLabel() == GUILocalizeStrings.Get(2077))	//Cut
					  ToCutScreen(item.Path);
					if (joinCutSpinCtrl.GetLabel() == GUILocalizeStrings.Get(2078))	//join
          {
						joiningList.Add(new System.IO.FileInfo(item.Path));
             // joinListCtrl.Add(new GUIListItem(item.Path));
						LoadJoinList();		
          }
				}

				else if (item.Label.Substring(1, 1) == ":")  // is a drive
				{
					currentFolder = item.Label;
          if (currentFolder != String.Empty)
            LoadListControl(currentFolder, extensions);
          else
            LoadShares();
						LoadDrives();
				}
				else
					LoadListControl(item.Path, extensions);
				if (item.Path == "")
				{
          LoadShares();
					LoadDrives();
				}
			}

      if (control == joinCutSpinCtrl)
      {
        if (joinCutSpinCtrl.GetLabel() == GUILocalizeStrings.Get(2078))		//join
        {
          joinListCtrl.IsVisible = true;
          startJoinBtn.IsEnabled = true;
					titelLbl.Label = GUILocalizeStrings.Get(2074);
        }
        if(joinCutSpinCtrl.GetLabel() == GUILocalizeStrings.Get(2077))		//cut
        {
          joinListCtrl.IsVisible = false;
          startJoinBtn.IsEnabled = false;
					titelLbl.Label = GUILocalizeStrings.Get(2092);
        }
      }

      if (control == startJoinBtn)
      {
        DvrMsModifier mod = new DvrMsModifier();
				if (joiningList[0] != null && joiningList[1] != null)
					mod.JoinDvr(joiningList);
				//else
					//System.Windows.Forms.MessageBox.Show("keineDatei");
      }


      //System.Windows.Forms.MessageBox.Show(controlId.ToString() + "::" + control.Name + "::" + actionType.ToString());
			base.OnClicked(controlId, control, actionType);
		}

		private void LoadJoinList()
		{
			joinListCtrl.Clear();
			foreach (System.IO.FileInfo file in joiningList)
			{
				joinListCtrl.Add(new GUIListItem(file.FullName));
			}
		}

		#endregion

	
		enum DriveType
		{
			Removable = 2,
			Fixed = 3,
			RemoteDisk = 4,
			CD = 5,
			DVD = 5,
			RamDisk = 6
		}

		/// <summary>
		/// get the number of drives
		/// </summary>
		private void GetDrives()
		{
			cntDrives = 0;
			foreach (string drive in Environment.GetLogicalDrives())
			{
				switch ((DriveType)MediaPortal.Util.Utils.getDriveType(drive))
				{
					case DriveType.Removable:
					case DriveType.CD:
					//case DriveType.DVD:
					case DriveType.Fixed:
					case DriveType.RemoteDisk:
					case DriveType.RamDisk:
						drives[cntDrives] = drive;
						cntDrives++;
						break;
				}
			}
		}

		/// <summary>
		/// Add the drives to the listcontrol with the matching icons
		/// </summary>
		private void LoadDrives()
		{
			try
			{
				currentFolder = "";
				for (int i = 0; i < cntDrives; i++)
				{
					GUIListItem item = new GUIListItem(drives[i]);
					item.IsFolder = true;
					item.Path = drives[i];
					MediaPortal.Util.Utils.SetDefaultIcons(item);
					videoListLct.Add(item);
				}
			}
			catch (Exception ex)
			{
				Log.Error("DvrMpegCut: (LoadDrives) " + ex.StackTrace);
			}
		}

		#region Eventhandler

		/// <summary>
		/// Load the list control with the items of the specified directory
		/// </summary>
		/// <param name="folder">Path of the director to load</param>
		/// <param name="exts">the extensions to show</param>
		private void LoadListControl(string folder, ArrayList exts)
		{
			try
			{
				if (folder != null && folder != "")
					folder = MediaPortal.Util.Utils.RemoveTrailingSlash(folder);
				
				//directory;
				ArrayList itemlist;
				//directory = new VirtualDirectory();
				directory.SetExtensions(exts);
				itemlist = directory.GetDirectory(folder);
				videoListLct.Clear();
				foreach (GUIListItem item in itemlist)
				{
					if (!item.IsFolder) // if item a folder
					{
						GUIListItem pItem = new GUIListItem(item.FileInfo.Name);
						pItem.FileInfo = item.FileInfo;
						pItem.IsFolder = false;
						pItem.Path = String.Format(@"{0}\{1}", folder, item.FileInfo.Name);
						videoListLct.Add(pItem);
					}
					else
					{
						GUIListItem pItem = new GUIListItem(item.Label);
						pItem.IsFolder = true;
						pItem.Path = String.Format(@"{0}\{1}", folder, item.Label);
						if (item.Label == "..")
						{
							string prevFolder = "";
							int pos = folder.LastIndexOf(@"\");
							if (pos >= 0) prevFolder = folder.Substring(0, pos);
							pItem.Path = prevFolder;
						}
						Utils.SetDefaultIcons(pItem);
						videoListLct.Add(pItem);
					}
				}
				currentFolder = folder;
			}
			catch (Exception ex)
			{
				Log.Error("DvrMpegCut: (LoadListControl) "+ ex.Message);
			}
		}

    protected override void OnShowContextMenu()
    {
      GUIListItem selected =  joinListCtrl.SelectedListItem;
      if (selected == null) return;
      else
      {
       /* GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
        if (dlg == null) return;
        dlg.Reset();
        dlg.SetHeading(924); // menu
        dlg.Add("Löschen");
        dlg.DoModal(GetID);
        if (dlg.SelectedId == -1) return;*/
				joiningList.RemoveAt(joinListCtrl.SelectedListItemIndex);
				LoadJoinList();
        //joinListCtrl.RemoveSubItem(joinListCtrl.SelectedListItemIndex);//joinListCtrl.SelectedListItem.);//SelectedLabelText);
				//System.Windows.Forms.MessageBox.Show(selected.Label + "::" + joinListCtrl.SelectedItem.ToString() + "::" + joinListCtrl.SelectedListItemIndex.ToString());
      }
    }

    private void LoadShares()
    {
      using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.Get(Config.Dir.Config) + "MediaPortal.xml"))
      {
        //ShowTrailerButton = xmlreader.GetValueAsBool("plugins", "My Trailers", true);
       // fileMenuEnabled = xmlreader.GetValueAsBool("filemenu", "enabled", true);
        //fileMenuPinCode = Utils.DecryptPin(xmlreader.GetValueAsString("filemenu", "pincode", String.Empty));
        directory.Clear();
        videoListLct.Clear();
        string strDefault = xmlreader.GetValueAsString("movies", "default", String.Empty);
        for (int i = 0; i < 20; i++)
        {
          string strShareName = String.Format("sharename{0}", i);
          string strSharePath = String.Format("sharepath{0}", i);
          string strPincode = String.Format("pincode{0}", i);

          string shareType = String.Format("sharetype{0}", i);
          string shareServer = String.Format("shareserver{0}", i);
          string shareLogin = String.Format("sharelogin{0}", i);
          string sharePwd = String.Format("sharepassword{0}", i);
          string sharePort = String.Format("shareport{0}", i);
          string remoteFolder = String.Format("shareremotepath{0}", i);
          string shareViewPath = String.Format("shareview{0}", i);

          Share share = new Share();
          share.Name = xmlreader.GetValueAsString("movies", strShareName, String.Empty);
          share.Path = xmlreader.GetValueAsString("movies", strSharePath, String.Empty);
          string pinCode = Utils.DecryptPin(xmlreader.GetValueAsString("movies", strPincode, string.Empty));
          if (pinCode != string.Empty)
            share.Pincode = Convert.ToInt32(pinCode);
          else
            share.Pincode = -1;

          share.IsFtpShare = xmlreader.GetValueAsBool("movies", shareType, false);
          share.FtpServer = xmlreader.GetValueAsString("movies", shareServer, String.Empty);
          share.FtpLoginName = xmlreader.GetValueAsString("movies", shareLogin, String.Empty);
          share.FtpPassword = xmlreader.GetValueAsString("movies", sharePwd, String.Empty);
          share.FtpPort = xmlreader.GetValueAsInt("movies", sharePort, 21);
          share.FtpFolder = xmlreader.GetValueAsString("movies", remoteFolder, "/");
          share.DefaultView = (Share.Views)xmlreader.GetValueAsInt("movies", shareViewPath, (int)Share.Views.List);

          if (share.Name.Length > 0)
          {
            if (strDefault == share.Name)
            {
              share.Default = true;
              if (currentFolder.Length == 0)
              {
                currentFolder = share.Path;
              //  m_strDirectoryStart = share.Path;
              }
            }
            directory.Add(share);
          }
          else break;
        }
        //m_askBeforePlayingDVDImage = xmlreader.GetValueAsBool("daemon", "askbeforeplaying", false);
      }
      
      ArrayList itemlist = new ArrayList();
      itemlist = directory.GetRoot();
      foreach (GUIListItem item in itemlist)
      {
       // GUIListItem pItem = new GUIListItem(item.FileInfo.Name);
       // pItem.FileInfo = item.FileInfo;
      //  pItem.IsFolder = false;
       // pItem.Path = String.Format(@"{0}\{1}", folder, item.FileInfo.Name);
        videoListLct.Add(item);
      }
    }

		protected void ToCutScreen(string filepath)
		{
			try
			{
				if (filepath == null)
					System.Windows.Forms.MessageBox.Show("No path");
				if (cutScr == null)
				{
					cutScr = new DvrMpegCutPreview(filepath);
					cutScr.Init();
					if (GUIWindowManager.GetWindow(cutScr.GetID) == null)
					{
						GUIWindow win = (GUIWindow)cutScr;
						GUIWindowManager.Add(ref win);
					}
				}
				else
					cutScr.CutFileName = filepath;

				GUIWindowManager.ActivateWindow(cutScr.GetID);
			}
			catch (Exception ex)
			{
				Log.Error("DvrMpegCut: (ToCutScreen) " + ex.Message);
			}
		}
		#endregion
	}
}
