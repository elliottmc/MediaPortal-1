/* 
 *	Copyright (C) 2005 Media Portal
 *	http://mediaportal.sourceforge.net
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
using System.Xml;
using System.Net;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using MediaPortal.GUI.Library;

namespace WebEPG_conf
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class fChannels : System.Windows.Forms.Form
	{
		private string startDirectory;
		private Form selection;
		//private TreeNode tChannels;
		//private TreeNode tGrabbers;
		private SortedList ChannelList;
		private SortedList CountryList;
		private Hashtable hChannelInfo;
		private Hashtable hGrabberInfo;
		private EventHandler handler;
		private EventHandler selectHandler;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label l_cID;
		private System.Windows.Forms.Button bAdd;
		private System.Windows.Forms.ListBox lbChannels;
		private System.Windows.Forms.GroupBox gbChannelDetails;
		private System.Windows.Forms.GroupBox gbGrabber;
		private System.Windows.Forms.Button bSave;
		private System.Windows.Forms.TextBox tbCount;
		private System.Windows.Forms.Label lCount;
		private System.Windows.Forms.Button bRemove;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button bUpdate;
		private System.Windows.Forms.TextBox tbChannelName;
		private System.Windows.Forms.OpenFileDialog importFile;
		private System.Windows.Forms.TextBox tbChannelID;
		private System.Windows.Forms.TextBox tbURL;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox tbValid;
		private System.Windows.Forms.ListBox lbGrabbers;
		private System.Windows.Forms.Button bLoad;
		private System.ComponentModel.IContainer components;

		public fChannels()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			selectHandler = new EventHandler(DoSelect);
			handler = new EventHandler(DoEvent);
			bUpdate.Click += handler;
			bSave.Click += handler;
			bAdd.Click += handler;
			bRemove.Click += handler;
			//bImport.Click += handler;
			bLoad.Click += handler;
			lbChannels.SelectedValueChanged += handler;

			startDirectory = Environment.CurrentDirectory;

			//Log.WriteFile(Log.LogType.Log, false, "WebEPG Config: Loading Channels");
			//hChannelInfo = new Hashtable();$

			Load();
			UpdateList("", -1);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.bAdd = new System.Windows.Forms.Button();
			this.lbChannels = new System.Windows.Forms.ListBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.lCount = new System.Windows.Forms.Label();
			this.tbCount = new System.Windows.Forms.TextBox();
			this.bSave = new System.Windows.Forms.Button();
			this.gbChannelDetails = new System.Windows.Forms.GroupBox();
			this.tbValid = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.tbURL = new System.Windows.Forms.TextBox();
			this.bUpdate = new System.Windows.Forms.Button();
			this.tbChannelID = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.l_cID = new System.Windows.Forms.Label();
			this.tbChannelName = new System.Windows.Forms.TextBox();
			this.bRemove = new System.Windows.Forms.Button();
			this.gbGrabber = new System.Windows.Forms.GroupBox();
			this.lbGrabbers = new System.Windows.Forms.ListBox();
			this.importFile = new System.Windows.Forms.OpenFileDialog();
			this.bLoad = new System.Windows.Forms.Button();
			this.groupBox2.SuspendLayout();
			this.gbChannelDetails.SuspendLayout();
			this.gbGrabber.SuspendLayout();
			this.SuspendLayout();
			// 
			// bAdd
			// 
			this.bAdd.Location = new System.Drawing.Point(8, 368);
			this.bAdd.Name = "bAdd";
			this.bAdd.Size = new System.Drawing.Size(72, 24);
			this.bAdd.TabIndex = 12;
			this.bAdd.Text = "Add";
			// 
			// lbChannels
			// 
			this.lbChannels.Location = new System.Drawing.Point(32, 32);
			this.lbChannels.Name = "lbChannels";
			this.lbChannels.Size = new System.Drawing.Size(168, 277);
			this.lbChannels.TabIndex = 10;
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.bLoad);
			this.groupBox2.Controls.Add(this.lCount);
			this.groupBox2.Controls.Add(this.tbCount);
			this.groupBox2.Controls.Add(this.bSave);
			this.groupBox2.Location = new System.Drawing.Point(16, 8);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(200, 400);
			this.groupBox2.TabIndex = 13;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Channels";
			this.groupBox2.Enter += new System.EventHandler(this.groupBox2_Enter);
			// 
			// lCount
			// 
			this.lCount.Location = new System.Drawing.Point(104, 312);
			this.lCount.Name = "lCount";
			this.lCount.Size = new System.Drawing.Size(80, 16);
			this.lCount.TabIndex = 1;
			this.lCount.Text = "Channel Count";
			// 
			// tbCount
			// 
			this.tbCount.Location = new System.Drawing.Point(16, 304);
			this.tbCount.Name = "tbCount";
			this.tbCount.Size = new System.Drawing.Size(72, 20);
			this.tbCount.TabIndex = 0;
			this.tbCount.Text = "";
			// 
			// bSave
			// 
			this.bSave.Location = new System.Drawing.Point(112, 368);
			this.bSave.Name = "bSave";
			this.bSave.Size = new System.Drawing.Size(72, 24);
			this.bSave.TabIndex = 16;
			this.bSave.Text = "Save";
			// 
			// gbChannelDetails
			// 
			this.gbChannelDetails.Controls.Add(this.tbValid);
			this.gbChannelDetails.Controls.Add(this.label1);
			this.gbChannelDetails.Controls.Add(this.tbURL);
			this.gbChannelDetails.Controls.Add(this.bUpdate);
			this.gbChannelDetails.Controls.Add(this.tbChannelID);
			this.gbChannelDetails.Controls.Add(this.label4);
			this.gbChannelDetails.Controls.Add(this.l_cID);
			this.gbChannelDetails.Controls.Add(this.tbChannelName);
			this.gbChannelDetails.Controls.Add(this.bAdd);
			this.gbChannelDetails.Controls.Add(this.bRemove);
			this.gbChannelDetails.Controls.Add(this.gbGrabber);
			this.gbChannelDetails.Location = new System.Drawing.Point(224, 8);
			this.gbChannelDetails.Name = "gbChannelDetails";
			this.gbChannelDetails.Size = new System.Drawing.Size(312, 400);
			this.gbChannelDetails.TabIndex = 14;
			this.gbChannelDetails.TabStop = false;
			this.gbChannelDetails.Text = "Channel Details";
			// 
			// tbValid
			// 
			this.tbValid.Location = new System.Drawing.Point(256, 72);
			this.tbValid.Name = "tbValid";
			this.tbValid.ReadOnly = true;
			this.tbValid.Size = new System.Drawing.Size(24, 20);
			this.tbValid.TabIndex = 21;
			this.tbValid.Text = "";
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(16, 72);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(64, 23);
			this.label1.TabIndex = 20;
			this.label1.Text = "URL";
			// 
			// tbURL
			// 
			this.tbURL.Location = new System.Drawing.Point(88, 72);
			this.tbURL.Name = "tbURL";
			this.tbURL.ReadOnly = true;
			this.tbURL.Size = new System.Drawing.Size(160, 20);
			this.tbURL.TabIndex = 19;
			this.tbURL.Text = "";
			// 
			// bUpdate
			// 
			this.bUpdate.Location = new System.Drawing.Point(120, 368);
			this.bUpdate.Name = "bUpdate";
			this.bUpdate.Size = new System.Drawing.Size(72, 24);
			this.bUpdate.TabIndex = 18;
			this.bUpdate.Text = "Update";
			// 
			// tbChannelID
			// 
			this.tbChannelID.Location = new System.Drawing.Point(88, 24);
			this.tbChannelID.Name = "tbChannelID";
			this.tbChannelID.Size = new System.Drawing.Size(192, 20);
			this.tbChannelID.TabIndex = 13;
			this.tbChannelID.Text = "";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(16, 24);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(80, 16);
			this.label4.TabIndex = 10;
			this.label4.Text = "ID";
			// 
			// l_cID
			// 
			this.l_cID.Location = new System.Drawing.Point(16, 48);
			this.l_cID.Name = "l_cID";
			this.l_cID.Size = new System.Drawing.Size(64, 23);
			this.l_cID.TabIndex = 8;
			this.l_cID.Text = "Name";
			// 
			// tbChannelName
			// 
			this.tbChannelName.Location = new System.Drawing.Point(88, 48);
			this.tbChannelName.Name = "tbChannelName";
			this.tbChannelName.Size = new System.Drawing.Size(192, 20);
			this.tbChannelName.TabIndex = 7;
			this.tbChannelName.Text = "";
			// 
			// bRemove
			// 
			this.bRemove.Location = new System.Drawing.Point(232, 368);
			this.bRemove.Name = "bRemove";
			this.bRemove.Size = new System.Drawing.Size(72, 24);
			this.bRemove.TabIndex = 17;
			this.bRemove.Text = "Remove";
			// 
			// gbGrabber
			// 
			this.gbGrabber.Controls.Add(this.lbGrabbers);
			this.gbGrabber.Location = new System.Drawing.Point(8, 104);
			this.gbGrabber.Name = "gbGrabber";
			this.gbGrabber.Size = new System.Drawing.Size(296, 256);
			this.gbGrabber.TabIndex = 15;
			this.gbGrabber.TabStop = false;
			this.gbGrabber.Text = "Grabber List";
			// 
			// lbGrabbers
			// 
			this.lbGrabbers.Location = new System.Drawing.Point(16, 24);
			this.lbGrabbers.Name = "lbGrabbers";
			this.lbGrabbers.Size = new System.Drawing.Size(264, 212);
			this.lbGrabbers.TabIndex = 0;
			// 
			// importFile
			// 
			this.importFile.FileName = "ChannelList.xml";
			this.importFile.Filter = "Xml Files (*.xml)|*.xml";
			this.importFile.Title = "Import MP Channel File";
			// 
			// bLoad
			// 
			this.bLoad.Location = new System.Drawing.Point(16, 368);
			this.bLoad.Name = "bLoad";
			this.bLoad.Size = new System.Drawing.Size(72, 24);
			this.bLoad.TabIndex = 18;
			this.bLoad.Text = "Reload all";
			// 
			// fChannels
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(552, 421);
			this.Controls.Add(this.gbChannelDetails);
			this.Controls.Add(this.lbChannels);
			this.Controls.Add(this.groupBox2);
			this.MaximizeBox = false;
			this.Name = "fChannels";
			this.Text = "WebEPG Channel Config";
			this.groupBox2.ResumeLayout(false);
			this.gbChannelDetails.ResumeLayout(false);
			this.gbGrabber.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Application.Run(new fChannels());
			//Application.Run(new fGrabber());
		}

		private void label2_Click(object sender, System.EventArgs e)
		{
		
		}

		private void groupBox2_Enter(object sender, System.EventArgs e)
		{
		
		}

		private void DoSelect(Object source, EventArgs e)
		{
			if(source==selection)
			{
				if(selection.Text == "Selection ")
				{
					this.Activate();
					string[] id = (string[]) selection.Tag;
					selection.Text = "Selection";

					tbChannelName.Tag = id[0];
					ChannelInfo info = (ChannelInfo) hChannelInfo[id[0]];
					if(info != null)
					{
						tbChannelName.Text = info.FullName;
						//Log.WriteFile(Log.LogType.Log, false, "WebEPG Config: Selection: {0}", info.FullName);

						UpdateCurrent();
					}
				}
			}
		}

		private void DoEvent(Object source, EventArgs e)
		{
			if(source==bSave)
			{
				Log.WriteFile(Log.LogType.Log, false, "WebEPG Config: Button: Save");
				string confFile = startDirectory + "\\channels\\channels.xml";
				if(System.IO.File.Exists(confFile))
				{
					System.IO.File.Delete(confFile.Replace(".xml",".bak"));
					System.IO.File.Move(confFile,confFile.Replace(".xml",".bak"));
				}
				MediaPortal.Profile.Xml xmlwriter = new MediaPortal.Profile.Xml(confFile);

				ChannelInfo[] infoList = new ChannelInfo[ChannelList.Count];

				int index=0;
				IDictionaryEnumerator Enumerator = ChannelList.GetEnumerator();
				while (Enumerator.MoveNext())
				{
					infoList[index] = (ChannelInfo) Enumerator.Value;
					if(infoList[index].ChannelID != null && infoList[index].FullName != null)
					{
						if(infoList[index].GrabberList != null)
						{
							IDictionaryEnumerator grabEnum = infoList[index].GrabberList.GetEnumerator();
							while (grabEnum.MoveNext())
							{
								GrabberInfo gInfo = (GrabberInfo) grabEnum.Value;
								SortedList chList = (SortedList) CountryList[gInfo.Country];
								if(chList[index] == null)
								{
									chList.Add(index, "");
									//CountryList.Remove(gInfo.Country);
									//CountryList.Add(gInfo.Country, chList);
								}
							}
						}
						index++;
					}
				}

				xmlwriter.SetValue("ChannelInfo", "TotalChannels", index.ToString());

				for(int i=0; i < index; i++)
				{
					xmlwriter.SetValue(i.ToString(), "ChannelID", infoList[i].ChannelID);
					xmlwriter.SetValue(i.ToString(), "FullName", infoList[i].FullName);
				}


				IDictionaryEnumerator countryEnum = CountryList.GetEnumerator();
				while (countryEnum.MoveNext())
				{
					SortedList chList = (SortedList) countryEnum.Value;
					xmlwriter.SetValue((string) countryEnum.Key, "TotalChannels", chList.Count);

					index=0;
					IDictionaryEnumerator chEnum = chList.GetEnumerator();
					while (chEnum.MoveNext())
					{
						xmlwriter.SetValue((string) countryEnum.Key, index.ToString(), chEnum.Key);
						index++;
					}	
				}
				xmlwriter.Save();
			}

			if(source==bUpdate)
			{
				Log.WriteFile(Log.LogType.Log, false, "WebEPG Config: Button: Update");
				ReplaceCurrent();
			}

			if(source==bLoad)
			{
				string channel ="";
				if(lbChannels.SelectedIndex > -1)
				{
					ChannelInfo info = (ChannelInfo) ChannelList.GetByIndex(lbChannels.SelectedIndex);
					channel = info.ChannelID;
				}
				Load();
				UpdateList(channel, -1);
			}

			if(source==bRemove)
			{
				Log.WriteFile(Log.LogType.Log, false, "WebEPG Config: Button: Remove");
				if(lbChannels.SelectedIndex != -1)
				{
					ChannelList.RemoveAt(lbChannels.SelectedIndex);
					UpdateList("", lbChannels.SelectedIndex);
				}
			}

			if(source==bAdd)
			{
				Log.WriteFile(Log.LogType.Log, false, "WebEPG Config: Button: Add");
				ChannelInfo info = new ChannelInfo();

				while(ChannelList[info.DisplayName] != null)
					info.DisplayName+="*";

				info.FullName = tbChannelName.Text;
				info.ChannelID = tbChannelID.Text;
				
				ChannelList.Add(info.DisplayName, info);
				UpdateList(info.DisplayName, -1);
			}

			if(source==lbChannels)
			{
				if(lbChannels.SelectedIndex > -1)
				{
					ChannelInfo info = (ChannelInfo) ChannelList.GetByIndex(lbChannels.SelectedIndex);
					tbChannelID.Text = info.ChannelID;
					tbChannelName.Text = info.FullName;
					int index;
					if((index = info.ChannelID.IndexOf("@")) != -1)
						tbURL.Text = info.ChannelID.Substring(index+1);
					else
						tbURL.Text = info.ChannelID;

					//					tbValid.Text = "";
					//					IPHostEntry ipEntry;
					//					try
					//					{
					//						ipEntry = Dns.GetHostByName (tbURL.Text);
					//					}
					//					catch(System.Net.Sockets.SocketException ex)
					//					{
					//						tbValid.Text = "*";
					//					}

					string[] list;
					if(info.GrabberList != null)
					{
						IDictionaryEnumerator Enumerator = info.GrabberList.GetEnumerator();

						list = new string[info.GrabberList.Count];
						int i=0;

						while (Enumerator.MoveNext())
						{
							GrabberInfo grabber = (GrabberInfo) Enumerator.Value;
							list[i++] = grabber.GrabberID;
						}
						//tbCount.Text = ChannelList.Count.ToString();
					
					}
					else
					{
						list = new string[0];
					}
					lbGrabbers.DataSource = list;
				}
			}

//			if(source==bImport)
//			{
//				//tChannels = new TreeNode("Channels");
//				if(System.IO.Directory.Exists(startDirectory + "\\Channels"))
//					GetTreeChannels(startDirectory + "\\Channels");
//				UpdateList("", -1);
//			}

			if(source==selection)
				selection=null;

		}

		private void UpdateCurrent()
		{
			if(lbChannels.SelectedIndex != -1)
			{
				ChannelInfo info = (ChannelInfo) ChannelList.GetByIndex(lbChannels.SelectedIndex);

				info.FullName = tbChannelName.Text;
				info.ChannelID = tbChannelID.Text;

				ChannelList.SetByIndex(lbChannels.SelectedIndex, info);
			}
		}

		private void ReplaceCurrent()
		{
			if(lbChannels.SelectedIndex != -1)
			{
				ChannelList.RemoveAt(lbChannels.SelectedIndex);

				ChannelInfo info = new ChannelInfo();

				info.FullName = tbChannelName.Text;
				info.ChannelID = tbChannelID.Text;

				ChannelList.Add(info.ChannelID, info);

				UpdateList(info.ChannelID, -1);
			}
		}

		private void GetTreeGrabbers(string Location)
		{
			System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(Location); 
			System.IO.DirectoryInfo[] dirList = dir.GetDirectories();
			if(dirList.Length > 0)
			{
				for(int i=0; i < dirList.Length; i++)
				{     
					//LOAD FOLDERS
					System.IO.DirectoryInfo g = dirList[i];
					//TreeNode MainNext = new TreeNode(g.Name); //
					GetTreeGrabbers(g.FullName);
					//Main.Nodes.Add(MainNext);
					//MainNext.Tag = (g.FullName); 
				}
			}
			else
			{
				GetGrabbers(Location);
			}

		}

		private void GetGrabbers(string Location)
		{
			System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(Location); 
			Log.WriteFile(Log.LogType.Log, false, "WebEPG Config: Directory: {0}", Location);
			GrabberInfo gInfo;
			foreach (System.IO.FileInfo file in dir.GetFiles("*.xml"))
			{
				gInfo = new GrabberInfo();
				XmlDocument xml=new XmlDocument();
				XmlNodeList channelList;
				try 
				{
					Log.WriteFile(Log.LogType.Log, false, "WebEPG Config: File: {0}", file.Name);
					xml.Load(file.FullName);
					channelList = xml.DocumentElement.SelectNodes("/profile/section/entry");
  				
					XmlNode entryNode = xml.DocumentElement.SelectSingleNode("section[@name=\"Info\"]/entry[@name=\"GuideDays\"]");
					if (entryNode!=null)
						gInfo.GrabDays = int.Parse(entryNode.InnerText);
					entryNode = xml.DocumentElement.SelectSingleNode("section[@name=\"Info\"]/entry[@name=\"SiteDescription\"]");
					if (entryNode!=null)
						gInfo.SiteDesc = entryNode.InnerText;
					entryNode = xml.DocumentElement.SelectSingleNode("section[@name=\"Listing\"]/entry[@name=\"SubListingLink\"]");
					gInfo.Linked = false;
					if (entryNode!=null)
						gInfo.Linked = true;
				} 
				catch(System.Xml.XmlException ex) 
				{
					Log.WriteFile(Log.LogType.Log, false, "WebEPG Config: File open failed - XML error");
					return;
				}
				
				string GrabberSite = file.Name.Replace(".xml", "");
				GrabberSite = GrabberSite.Replace("_", ".");

				gInfo.GrabberID=file.Directory.Name + "\\" + file.Name;
				gInfo.GrabberName = GrabberSite;
				gInfo.Country = file.Directory.Name;
				hGrabberInfo.Add(gInfo.GrabberID, gInfo);

				if(CountryList[file.Directory.Name] == null)
					CountryList.Add(file.Directory.Name, new SortedList());

				//TreeNode gNode = new TreeNode(GrabberSite);
				//Main.Nodes.Add(gNode);
				//XmlNode cl=sectionList.Attributes.GetNamedItem("ChannelList");

				foreach (XmlNode nodeChannel in channelList)
				{
					if (nodeChannel.Attributes!=null)
					{
						XmlNode id = nodeChannel.ParentNode.Attributes.Item(0);
						if(id.InnerXml == "ChannelList")
						{
							id = nodeChannel.Attributes.Item(0);
							//idList.Add(id.InnerXml);

							ChannelInfo info = (ChannelInfo) ChannelList[id.InnerXml];
							if(info != null) // && info.GrabberList[gInfo.GrabberID] != null)
							{
//								TreeNode tNode = new TreeNode(info.FullName);
//								string [] tag = new string[2];
//								tag[0] = info.ChannelID;
//								tag[1] = GrabberSite;
//								tNode.Tag = tag;
//								gNode.Nodes.Add(tNode);
								if(info.GrabberList == null)
									info.GrabberList = new SortedList();
								if(info.GrabberList[gInfo.GrabberID] == null)
									info.GrabberList.Add(gInfo.GrabberID, gInfo);
							}
							else
							{
								info = new ChannelInfo();
								info.ChannelID = id.InnerXml;
								info.FullName = info.ChannelID;
								info.GrabberList = new SortedList();
								info.GrabberList.Add(gInfo.GrabberID, gInfo);
								ChannelList.Add(info.ChannelID, info);
							}
						}
					}
				}
			}
		}

		private void GetTreeChannels(string Location) //ref TreeNode Main, string Location)
		{
			System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(Location);
			if(dir.Exists) 
			{
				System.IO.DirectoryInfo[] dirList = dir.GetDirectories();
				if(dirList.Length > 0)
				{
					for(int i=0; i < dirList.Length; i++)
					{     
						//LOAD FOLDERS
						System.IO.DirectoryInfo g = dirList[i];
						//TreeNode MainNext = new TreeNode(g.Name); //
						GetTreeChannels(g.FullName); //ref MainNext, g.FullName);
						//Main.Nodes.Add(MainNext);
						//MainNext.Tag = (g.FullName); 
					}
				}
				else
				{
					GetChannels(Location); //ref Main, Location);
				}
			}
		}


		private void GetChannels(string Location) //ref TreeNode Main, string Location)
		{
			System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(Location); 
			Log.WriteFile(Log.LogType.Log, false, "WebEPG Config: Directory: {0}", Location);
			foreach (System.IO.FileInfo file in dir.GetFiles("*.xml")) 
			{
				Log.WriteFile(Log.LogType.Log, false, "WebEPG Config: File: {0}", file.Name);
				ChannelInfo info = GetChannelInfo(file.FullName);
				if(info != null)
				{
					if(ChannelList[info.ChannelID] == null)
						ChannelList.Add(info.ChannelID, info);
					else
					{
						ChannelList.Remove(info.ChannelID);
						ChannelList.Add(info.ChannelID, info);
					}

				}
			}
		}

		private ChannelInfo GetChannelInfo(string filename)
		{
			MediaPortal.Profile.Xml xmlreader = new MediaPortal.Profile.Xml(filename);
			ChannelInfo info = new ChannelInfo();

			info.FullName = xmlreader.GetValueAsString("ChannelInfo", "FullName", "");
			if(info.FullName == "")
			{
				Log.WriteFile(Log.LogType.Log, false, "WebEPG Config: File error: FullName not found");
				return null;
			}
			info.ChannelID = xmlreader.GetValueAsString("ChannelInfo", "ChannelID", "");
			if(info.ChannelID == "")
			{
				Log.WriteFile(Log.LogType.Log, false, "WebEPG Config: File error: ChannelID not found");
				return null;
			}
			int GrabberCount = xmlreader.GetValueAsInt("ChannelInfo", "Grabbers", 0);
			if(GrabberCount == 0)
			{
				Log.WriteFile(Log.LogType.Log, false, "WebEPG Config: File error: Grabbers not found");
				return null;
			}

			info.GrabberList = new SortedList();
//			for(int i=0; i < GrabberCount; i++)
//			{
//				string GrabberNumb = "Grabber" + (i+1).ToString();
//				string GrabberID = xmlreader.GetValueAsString("ChannelInfo", GrabberNumb, "");
//				if(GrabberID == "")
//				{
//					Log.WriteFile(Log.LogType.Log, false, "WebEPG Config: File error: {0} not found", GrabberNumb);
//					return null;
//				}
//				
//				int start = GrabberID.IndexOf("\\") + 1;
//				int end =  GrabberID.LastIndexOf(".");
//							
//				string GrabberSite = GrabberID.Substring(start, end-start);
//				GrabberSite = GrabberSite.Replace("_", ".");
//				info.GrabberList.Add(GrabberSite, GrabberID); 
//			}

			return info;
		}

		private void UpdateList(string select, int index)
		{
			IDictionaryEnumerator Enumerator = ChannelList.GetEnumerator();

			string[] list = new string[ChannelList.Count];
			int i=0;
			int selectedIndex=-1;

			while (Enumerator.MoveNext())
			{
				ChannelInfo channel = (ChannelInfo) Enumerator.Value;
				if(channel.ChannelID == select)
					selectedIndex=i;
				list[i++] = channel.ChannelID;
			}
			tbCount.Text = ChannelList.Count.ToString();
			lbChannels.DataSource = list;
			if(selectedIndex > 0)
				lbChannels.SelectedIndex = selectedIndex;
			if(index > 0)
			{
				if(index >= ChannelList.Count)
					index = ChannelList.Count-1;
				lbChannels.SelectedIndex = index;
			}
		}

		private void Load()
		{
			ChannelList = new SortedList();

			if(System.IO.File.Exists(startDirectory + "\\channels\\channels.xml"))
			{
				Log.WriteFile(Log.LogType.Log, false, "WebEPG Config: Loading Existing channels.xml");
				MediaPortal.Profile.Xml xmlreader = new MediaPortal.Profile.Xml(startDirectory + "\\channels\\channels.xml");
				int channelCount = xmlreader.GetValueAsInt("ChannelInfo", "TotalChannels", 0);	

				for (int ich = 0; ich < channelCount; ich++)
				{
					ChannelInfo channel = new ChannelInfo();
					channel.ChannelID = xmlreader.GetValueAsString(ich.ToString(), "ChannelID", "");
					channel.FullName = xmlreader.GetValueAsString(ich.ToString(), "FullName", "");
					if(channel.FullName == "")
						channel.FullName = channel.ChannelID;
					if(channel.ChannelID != "")
						ChannelList.Add(channel.ChannelID, channel);
				}
			}

			
			Log.WriteFile(Log.LogType.Log, false, "WebEPG Config: Loading Grabbers");
			hGrabberInfo = new Hashtable();
			CountryList = new SortedList();
			//tGrabbers = new TreeNode("Web Sites");
			if(System.IO.Directory.Exists(startDirectory + "\\Grabbers"))
				GetTreeGrabbers(startDirectory + "\\Grabbers");
			else
				Log.WriteFile(Log.LogType.Log, true, "WebEPG Config: Cannot find grabbers directory");

			//
			// TODO: Add any constructor code after InitializeComponent call
			//

			//ChannelList = new SortedList();

			//hChannelInfo.
		}
	}
}