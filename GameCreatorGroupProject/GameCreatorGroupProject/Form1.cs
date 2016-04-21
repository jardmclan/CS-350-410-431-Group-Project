﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using System.IO;
using OpenTK;
using System.Text.RegularExpressions;

namespace GameCreatorGroupProject
{
    public partial class MainWindow : Form
    {
        // Create an empty Project instance
        Project project = new Project();

        // Variable to track whether a project is actively being edited
        // Necessary to ensure the user doesn't load something nonexistent
        // or exit without saving a brand new project.
        bool projectOpen = false;

        // Debug flag; used to bypass some checks or show extra information
        #if DEBUG
            bool debug = true;
#else
            bool debug = false;
#endif

        //MainClient to be created when form is loaded
        MainClient online;
        private TCPClient chat;

        //private string sprloc = null;
        private Image spr = null;
        private string sprp = null;
        private Vector2[] cOffsets = null;
        private Vector2[] bOffsets = null;

        private List<string> objects = new List<string>();

        private uint chatServerID;

        // Declare a ResourceImporter to make it easier to load and save resources
        ResourceImporter resImporter = new ResourceImporter();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void itemStartServer_Click(object sender, EventArgs e)
        {
            // If no project is open, throw error and abandon function
            if (!projectOpen && !debug)
            {
                MessageBox.Show("Error: No currently open projects.");
                return;
            }

            //Connect to mainServer using ServerInfo
            connect();


            // Create a server
            //ChatServer prjServer = new ChatServer();

            // Start it up
            //prjServer.startServer(project);

            return;
        }

        // This function is called whenever the user clicks File>New on the tool bar.
        private void itemNew_Click(object sender, EventArgs e)
        {
            // Use VisualBasic's input box to get the project name from the user
            string newName = Interaction.InputBox("Enter the project name:", "Enter Project Name", "NewProject", -1, -1);

            // Get the folder from the user
            folderPrjDir.ShowDialog();

            string targetPath = folderPrjDir.SelectedPath + "/" + newName;

            // Create the project directory if necessary.
            if (!System.IO.Directory.Exists(targetPath))
            {
                System.IO.Directory.CreateDirectory(targetPath);
            }

            // Generate the path to the resources folder
            string resPath = targetPath + "/Resources";

            // Create Resources directory if it doesn't exist
            if (!System.IO.Directory.Exists(resPath))
            {
                System.IO.Directory.CreateDirectory(resPath);
            }

            // Create a new project instance
            project.setName(newName);
            project.setDirectory(targetPath);
            project.setResourceDir(resPath);

            // Save the project
            int errNum = project.SaveProject();

            if (errNum == 55)
            {
                // File was open in another application, tell user we failed.
                MessageBox.Show("Error: File still open in another process. Could not save.");
            }

            // Set variable to say there is an open project.
            projectOpen = true;
        
            return;
        }

        // This function is called when the user clicks the Add button in the Resource Manager.
        private void btnAddResource_Click(object sender, EventArgs e)
        {
            // If no project is open, throw error and abandon function
            if (!projectOpen)
            {
                MessageBox.Show("Error: No currently open projects.");
                return;
            }

            openResourceDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.exif;*.tif;*.tiff|JPG|*.jpg;*.jpeg|PNG|*.png|BMP|*.bmp|GIF|*.gif|EXIF|*.exif|TIFF|*.tiff;*.tif";
            // Get the path to the resource from the user
            openResourceDialog.ShowDialog();

            // Use an input box to get the resource name from the user
            string newName = Interaction.InputBox("Enter the resource's name:", "Enter Resource Name", "Resource1", -1, -1);

            // Get extension of the file
            string fileExt = Path.GetExtension(openResourceDialog.FileName);

            // Save the resource in the project and copy file to resource folder
            resImporter.SaveResource(project, newName, fileExt, openResourceDialog.FileName);

            // Show resource in list view
            listResources.Items.Add(newName);
            cmbSprite.Items.Add(project.Resources[newName]);
        }

        // Show the preview of the image when selected, and its file properties
        private void listResources_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If no project is open, throw error and abandon function
            if (!projectOpen)
            {
                MessageBox.Show("Error: No currently open projects.");
                return;
            }

            // Look up item in the resource list to get path and display in the preview pane.
            picPreview.ImageLocation = project.Resources[listResources.SelectedItem.ToString()];

            // Set the file property display to the right of the preview
            // Clear previous lists for new display
            listFPVals.Items.Clear();
            listFProperties.Items.Clear();

            // Add File Name property
            listFProperties.Items.Add("File name:");
            listFPVals.Items.Add(Path.GetFileName(project.Resources[listResources.SelectedItem.ToString()]).ToString());

            // Add Creation Date property
            listFProperties.Items.Add("Date created:");
            listFPVals.Items.Add(File.GetCreationTime((project.Resources[listResources.SelectedItem.ToString()]).ToString()));

            // Add File Size property
            listFProperties.Items.Add("File size:");
            listFPVals.Items.Add((new FileInfo((project.Resources[listResources.SelectedItem.ToString()])).Length.ToString() + " bytes"));
        }

        private void itemSave_Click(object sender, EventArgs e)
        {
            // If no project is open, throw error and abandon function
            if (!projectOpen)
            {
                MessageBox.Show("Error: No currently open projects.");
                return;
            }

            // Save the project
            int errNum = project.SaveProject();

            if (errNum == 55)
            {
                // File was open in another application, tell user we failed.
                MessageBox.Show("Error: File still open in another process. Could not save.");
            }
        }

        private void itemExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void itemOpen_Click(object sender, EventArgs e)
        {
            // Set the file open dialog to only show .prj files
            openResourceDialog.Filter = "Project Files (*.prj)|*.prj|All Files (*.*)|*.*";
            // Prompt user for project file
            openResourceDialog.ShowDialog();
            // Restore filter to unfiltered state
            openResourceDialog.Filter = "All Files (*.*)|*.*";
             
            // Grab user-selected path
            string projPath = openResourceDialog.FileName;

            // Load data into current project
            project.LoadProject(projPath);

            // Clear out the list of resources the user sees
            listResources.Items.Clear();

            // Update the list of resources viewed by the user
            foreach (string resName in project.Resources.Keys)
            {
                listResources.Items.Add(resName);
            }

            foreach (string resPath in project.Resources.Values)
            {
                // Fill up the sprite selection box in the Object Designer window
                cmbSprite.Items.Add(resPath);
            }

            bool invalid;
            listObjects.DataSource = objects;
            foreach (string s in Directory.GetFiles(project.getResourceDir()))
            {
                invalid = false;
                Regex ob = new Regex(@"(.*)\.gob$");
                Regex c = new Regex(@"(.*)\.goc$");
                Match obm;
                Match cm;
                if ((obm = ob.Match(s)).Success)
                {
                    //parses file for validity
                    using (BinaryReader reader = new BinaryReader(File.Open(s, FileMode.Open)))
                    {
                        try
                        {
                            int elem;
                            reader.ReadString();
                            reader.ReadString();
                            elem = reader.ReadInt32();
                            for (int i = 0; i < elem; i++)
                            {
                                reader.ReadInt32();
                                reader.ReadInt32();
                            }
                            //I think this will work? Makes sure this is the end of the file.
                            if (reader.BaseStream.Position != reader.BaseStream.Length)
                            {
                                throw new Exception();
                            }
                        }
                        catch (Exception)
                        {
                            invalid = true;
                        }

                    }
                    if (!invalid)
                    {
                        //is index 1 right? all examples suggest it is, whats at 0?
                        objects.Add(obm.Groups[1].Value);
                        listObjects.DataSource = null;
                        listObjects.DataSource = objects;
                    }

                }
                else if ((cm = c.Match(s)).Success)
                {
                    //pars file for validity

                    objects.Add(cm.Groups[1].Value);
                    listObjects.DataSource = null;
                    listObjects.DataSource = objects;
                }
            }

            projectOpen = true;
        }

        private void itemConnect_Click(object sender, EventArgs e)
        {
            chatServerID = online.requestChatServer();
            MessageBox.Show("Connected to chat server: " + chatServerID.ToString());

            chat = online.getAvailable();

            ChatWindow cw = new ChatWindow(chat);
            cw.Show();

            //online.connectClient(1, chatServerID, 1);
            //chat = MainClient.clients.ElementAt(0);
            //chat.connectClient(ServerInfo.getServerIP());
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            online = new MainClient();
        }

        public void connect()
        {
            Thread t = new Thread(connectMain);
            t.Start();
        }

        //connects the main client to the server
        private void connectMain()
        {
            online.connectClient(ServerInfo.getServerIP());
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            online.disconnect();
        }

        private void sendMessageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string msg = "Hello World!";
            object temp = msg;
            if (chat == null)
                chat = online.getAvailable();
            chat.send(ref temp);
        }

        private void addUserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void addUserDebugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            online.connectClient(1, chatServerID, 0);
        }

        private void addUserReleaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            online.connectClient(1, chatServerID, 1);
        }

        private void radioSprite_MouseDown_1(object sender, MouseEventArgs e)
        {
            
        }

        private void btnSetSprite_Click(object sender, EventArgs e)
        {
            try
            {
                sprp = cmbSprite.Text;
                spr = Image.FromFile(cmbSprite.Text);
                CollisionDesigner.spr = spr;
                picSpriteView.Image = spr;
                radioBox.Enabled = true;
                radioSprite.Enabled = true;
            }
            catch (Exception)
            {
                MessageBox.Show("Invalid Sprite.", "Invalid sprite selection.", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "All Graphics Types|*.jpg;*.jpeg;*.png;*.bmp;*.exif;*.tif;*.tiff|JPG|*.jpg;*.jpeg|PNG|*.png|BMP|*.bmp|GIF|*.gif|EXIF|*.exif|TIFF|*.tiff;*.tif";
            if (d.ShowDialog() == DialogResult.OK)
            {
                cmbSprite.Text = d.FileName;
                //sprloc = d.FileName;
            }
        }

        private void radioSprite_Click(object sender, EventArgs e)
        {
            CollisionDesigner d = new CollisionDesigner();
            d.ShowDialog(this);
            cOffsets = CollisionDesigner.offsets;
            //have something ask for width and height and scale off that
        }

//have to add object to box
        private void btnAddObject_Click(object sender, EventArgs e)
        {
            // If no project is open, throw error and abandon function
            if (!projectOpen)
            {
                MessageBox.Show("Error: No currently open projects.", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show("Error: No currently open projects.");
                return;
            }

            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "Game Object Files|*.gob;*.goc|Game Object Data Files (*.gob)|*.gob|Game Object Code Files (*.goc)|*.goc";
            if (d.ShowDialog() == DialogResult.OK)
            {
                //file must end in .gob, can change if want
                Regex ob = new Regex(@"(.*)\.gob$");
                Regex c = new Regex(@"(.*)\.goc$");
                Match obm;
                Match cm;
                if ((obm = ob.Match(d.FileName)).Success)
                {
                    //parses file for validity
                    using(BinaryReader reader = new BinaryReader(File.Open(d.FileName, FileMode.Open)))
                    {
                        try
                        {
                            int elem;
                            reader.ReadString();
                            reader.ReadString();
                            elem = reader.ReadInt32();
                            for (int i = 0; i < elem; i++)
                            {
                                reader.ReadInt32();
                                reader.ReadInt32();
                            }
                            //I think this will work? Makes sure this is the end of the file.
                            if (reader.BaseStream.Position != reader.BaseStream.Length)
                            {
                                throw new Exception();
                            }
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("Invalid game object file.", "Invalid file.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        
                    }
                    
                    try
                    {
                        File.Copy(d.FileName, project.getResourceDir());
                    }
                    catch (IOException)
                    {
                        MessageBox.Show("Object of the same name already exists.", "Object exists.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    objects.Add(obm.Groups[1].Value);
                    listObjects.DataSource = null;
                    listObjects.DataSource = objects;
                }

                else if ((cm = c.Match(d.FileName)).Success)
                {
                    //parse file for validity, print error if incorrect
                    //also check if already exists
                    objects.Add(cm.Groups[1].Value);
                    File.Copy(d.FileName, project.getResourceDir());
                    listObjects.DataSource = null;
                    listObjects.DataSource = objects;
                }
                else
                {
                    MessageBox.Show("Invalid game object file.", "Invalid file.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void radioBox_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioBox_Click(object sender, EventArgs e)
        {
            //have things to ask for width and height and make box from that
        }

        private void listObjects_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If no project is open, throw error and abandon function
            if (!projectOpen)
            {
                MessageBox.Show("Error: No currently open projects.");
                return;
            }
        }

        private void btnSaveObj_Click(object sender, EventArgs e)
        {
            // If no project is open, throw error and abandon function
            if (!projectOpen)
            {
                MessageBox.Show("Error: No currently open projects.", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show("Error: No currently open projects.");
                return;
            }

            if (spr == null || (radioBox.Checked && bOffsets == null) || (radioSprite.Checked && cOffsets == null) || (!radioSprite.Checked && !radioBox.Checked) || txtObjectName.Text.Equals(""))
            {
                MessageBox.Show("Game objects must have a valid sprite, collision box, and name to be saved.", "Unable to generate game object.", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                string file = project.getResourceDir() + @"\" + txtObjectName.Text + ".gob";
                if (File.Exists(file))
                {
                    DialogResult d = MessageBox.Show("Object of given name already exists.\nWould you like to overwrite the object?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (d == DialogResult.No)
                    {
                        return;
                    }
                    /*
                    int i = 0;
                    while (File.Exists(file + i.ToString()))
                    {
                        i++;
                    }
                    file = file + i.ToString();
                    */
                }
                using (BinaryWriter write = new BinaryWriter(File.Open(file, FileMode.Create)))
                {
                    write.Write(txtObjectName.Text);
                    write.Write(sprp);
                    if (radioBox.Checked)
                    {
                        write.Write(bOffsets.Length);
                        foreach (Vector2 v in bOffsets)
                        {
                            write.Write(v.X);
                            write.Write(v.Y);
                        }
                    }
                    
                    else if(radioSprite.Checked)
                    {
                        write.Write(cOffsets.Length);
                        foreach (Vector2 v in cOffsets)
                        {
                            write.Write(v.X);
                            write.Write(v.Y);
                        }
                    }
                }
            }
            
        }

        private void btnRemoveObject_Click(object sender, EventArgs e)
        {
            //something like this
            //File.Delete(listObjects.SelectedItem.)
        }
    }
}
