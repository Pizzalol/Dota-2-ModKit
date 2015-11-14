﻿using FastColoredTextBoxNS;
using LibGit2Sharp;
using MetroFramework;
using MetroFramework.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Dota2ModKit.Forms {
	public partial class SpellLibraryForm : MetroForm {
		private MainForm mainForm;
		string spellLibPath = Path.Combine(Environment.CurrentDirectory, "SpellLibrary");
		string npcPath = "";
		private string currKVPath = "";
		private string luaHeroesPath;
		private string luaItemsPath;
		private string currLuaPath = "";

		public SpellLibraryForm(MainForm mainForm) {
			this.mainForm = mainForm;
			npcPath = Path.Combine(spellLibPath, "game", "scripts", "npc");
			luaHeroesPath = Path.Combine(spellLibPath, "game", "scripts", "vscripts", "heroes");
			luaItemsPath = Path.Combine(spellLibPath, "game", "scripts", "vscripts", "items");

			InitializeComponent();
			notificationLabel.Text = "";

			textBox1.KeyDown += (s, e) => {
				if (e.Control && (e.KeyCode == Keys.A)) {
					textBox1.SelectAll();
				}
			};

			if (!Directory.Exists(spellLibPath)) {
				DialogResult dr = MetroMessageBox.Show(mainForm, strings.SpellLibWillNowBeClonedMsg + " " + spellLibPath,
					strings.SpellLibNotFoundCaption,
					MessageBoxButtons.OKCancel,
					MessageBoxIcon.Information);

				if (dr != DialogResult.OK) {
					return;
				}
			}
			// user wants to continue, clone if necessary, and pull
			mainForm.spellLibraryBtn.Enabled = false;
			mainForm.progressSpinner1.Value = 60;
			mainForm.progressSpinner1.Visible = true;

			if (!Directory.Exists(spellLibPath)) {
				mainForm.text_notification("Cloning SpellLibrary...", MetroColorStyle.Blue, 999999);
			} else {
				mainForm.text_notification("Pulling SpellLibrary...", MetroColorStyle.Blue, 999999);
			}

			var gitWorker = new BackgroundWorker();
			gitWorker.RunWorkerCompleted += (s, e) => {
				mainForm.text_notification("", MetroColorStyle.Blue, 500);
				mainForm.progressSpinner1.Visible = false;
				mainForm.spellLibraryBtn.Enabled = true;

				initTreeView();
			};
			gitWorker.DoWork += (s, e) => {
				if (!Directory.Exists(spellLibPath)) {
					try {
						string gitPath = Repository.Clone("https://github.com/Pizzalol/SpellLibrary", spellLibPath);
						Console.WriteLine("repo path:" + gitPath);
					} catch (Exception ex) {

					}
					return;
				}

				// pull from the repo
				using (var repo = new Repository(spellLibPath)) {
					try {
						//var remote = repo.Network.Remotes["origin"];
						MergeResult mr = repo.Network.Pull(new Signature("myname", "myname@email.com",
							new DateTimeOffset()),
							new PullOptions());
						MergeStatus ms = mr.Status;
					} catch (Exception ex) {}
				}
			};
			gitWorker.RunWorkerAsync();
		}

		private void initTreeView() {
			populateTreeView();
			treeView1.Nodes[0].ExpandAll(); // abilities
			treeView1.Nodes[1].ExpandAll(); // items
			treeView1.SelectedNode = treeView1.Nodes[0].Nodes[0].Nodes[0]; // abilities/hero_name/first_ability

			treeView1.AfterSelect += (s, e) => {
				TreeNode node = e.Node; // first selected node is the first ability of the first hero

				if (node.Parent != null && node.Parent.Parent != null && node.Parent.Parent.Text == "Abilities") {
					string abilName = node.Name;
					string heroName = node.Parent.Name;
					string p = Path.Combine(npcPath, "abilities", heroName, abilName);
					if (File.Exists(p)) {
						changeToKV();
						textBox1.Text = File.ReadAllText(p);
						currKVPath = p;

                        string lua;
                        if(abilName.EndsWith("_lua.txt")) // Check if it is a Lua ability
                        {
                            lua = Path.Combine(luaHeroesPath, "hero_" + heroName, abilName.Replace(".txt", ".lua"));
                        }
                        else
                        {
                            lua = Path.Combine(luaHeroesPath, "hero_" + heroName, abilName.Replace("_datadriven.txt", ".lua"));
                        }
						if (!File.Exists(lua)) {
							luaKVBtn.Enabled = false;

						} else {
							luaKVBtn.Enabled = true;
							luaKVBtn.Text = "Lua Script";
							currLuaPath = lua;
						}
					} else {
						//Console.WriteLine(abilName + " path wasn't found!");
					}

				} else if (node.Parent != null && node.Parent.Text == "Items") {
					string itemName = node.Name;
					string p = Path.Combine(npcPath, "items", itemName);
					if (File.Exists(p)) {
						changeToKV();
						textBox1.Text = File.ReadAllText(p);
						currKVPath = p;

						string lua = Path.Combine(luaItemsPath, itemName.Replace("_datadriven.txt", ".lua"));
						if (!File.Exists(lua)) {
							luaKVBtn.Enabled = false;

						} else {
							luaKVBtn.Enabled = true;
							luaKVBtn.Text = "Lua Script";
							currLuaPath = lua;
						}
					} else {
						//Console.WriteLine(itemName + " path wasn't found!");
					}
				}
			};


			treeView1.KeyDown += (s, e) => {
				if (e.KeyCode == Keys.Enter && treeView1.SelectedNode != null) {
					treeView1.SelectedNode.Expand();
				}
			};

			//treeView1.ExpandAll();
			this.Show();
		}

		private void populateTreeView() {
			TreeNode abilities = treeView1.Nodes.Add("Abilities");
			TreeNode items = treeView1.Nodes.Add("Items");

			//string[] abilPaths = Directory.GetFiles(Path.Combine(npcPath, "abilities"), "*.txt");
			string[] itemPaths = Directory.GetFiles(Path.Combine(npcPath, "items"), "*.txt");
            Dictionary<string, TreeNode> heroNames = new Dictionary<string, TreeNode>();
			//string currHeroName = "";
			List<string> abilArr = new List<string>();
            //StringBuilder allAbils = new StringBuilder();

            string abilityFolder = Path.Combine(npcPath, "abilities");
            string[] heroPaths = Directory.GetDirectories(abilityFolder);
            string[] abilPaths;
            string heroName;
            TreeNode heroNode;
            TreeNode abilNode;

            foreach (string heroPath in heroPaths)
            {
                // Reads hero names from the folder names
                heroName = heroPath.Remove(0, abilityFolder.Length + 1);

                abilPaths = Directory.GetFiles(heroPath);

                heroNode = abilities.Nodes.Add(Util.MakeUnderscoreStringNice(heroName));
                heroNode.Name = heroName;
                heroNames.Add(heroName, heroNode);

                // Reads all the ability names inside the folder and assigns them to the hero
                foreach (string abilPath in abilPaths)
                {
                    string abilName = abilPath.Substring(abilPath.LastIndexOf('\\') + 1);
                    string abilNameClean = abilName.Replace(".txt", "").Replace("_datadriven", "");               

                    abilNode = heroNames[heroName].Nodes.Add(Util.MakeUnderscoreStringNice(abilNameClean));
                    abilNode.Name = abilName;                    
                }
            }           

			// done with abils. now onto items.
			foreach (string itemPath in itemPaths) {
				string itemName = itemPath.Substring(itemPath.LastIndexOf('\\') + 1);
				string niceName = Util.MakeUnderscoreStringNice(itemName.Replace(".txt", "").Replace("_datadriven", "").Replace("item_", ""));

				string txt = File.ReadAllText(itemPath);
				// ensure this item was actually completed by devs of spell library.
				if (txt.StartsWith("//") || Util.ContainsKVKey(txt)) {
					TreeNode itemNode = items.Nodes.Add(niceName);
					itemNode.Name = itemName;
					//itemNode.Expand();
				}
			}
		}

		private void copySpellBtn_Click(object sender, EventArgs e) {
			metroRadioButton1.Select();

			Clipboard.SetText(textBox1.Text);
			text_notification(strings.Copied, MetroColorStyle.Blue, 1000);
		}

		private void luaKVBtn_Click(object sender, EventArgs e) {
			metroRadioButton1.Select();

			if (luaKVBtn.Text == strings.LuaScript) {
				// open lua
				changeToLua();
				textBox1.Text = File.ReadAllText(currLuaPath);
			} else {
				// open kv
				changeToKV();
				textBox1.Text = File.ReadAllText(currKVPath);
			}

		}

		void changeToKV() {
			textBox1.Language = Language.JS;
			luaKVBtn.Text = strings.LuaScript;
			metroToolTip1.SetToolTip(luaKVBtn, strings.OpensTheLuaScript);
		}

		void changeToLua() {
			textBox1.Language = Language.Lua;
			luaKVBtn.Text = strings.KeyValues;
			metroToolTip1.SetToolTip(luaKVBtn, strings.OpensTheKVEntry);
		}

		private void metroScrollBar1_Scroll(object sender, ScrollEventArgs e) {
			Console.WriteLine(e.NewValue);
			Console.WriteLine(textBox1.AutoScrollOffset.X + ", " + textBox1.AutoScrollOffset.Y);
		}

		public void text_notification(string text, MetroColorStyle color, int duration) {
			System.Timers.Timer notificationLabelTimer = new System.Timers.Timer(duration);
			notificationLabelTimer.SynchronizingObject = this;
			notificationLabelTimer.AutoReset = false;
			notificationLabelTimer.Start();
			notificationLabelTimer.Elapsed += notificationLabelTimer_Elapsed;
			notificationLabel.Style = color;
			notificationLabel.Text = text;
		}

		private void notificationLabelTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
			notificationLabel.Text = "";
		}

		private void openFileBtn_Click(object sender, EventArgs e) {
			metroRadioButton1.Select();

			if (luaKVBtn.Text == strings.LuaScript) {
				Process.Start(currKVPath);
			} else {
				// open kv
				Process.Start(currLuaPath);
			}
		}
	}
}
