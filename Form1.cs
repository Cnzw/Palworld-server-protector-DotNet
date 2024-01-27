namespace Palworld_server_protector_DotNet
{
    using Microsoft.VisualBasic.Devices;
    using System.Diagnostics;
    using System.Windows.Forms;
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Drawing;
    using static System.Net.WebRequestMethods;

    public partial class Form1 : Form
    {
        private Timer memTimer;
        private Timer saveTimer;
        private Timer getplayerTimer;
        private string cmdPath;
        private string backupPath;
        private string gamedataPath;
        private Int32 memTarget;
        private string rconHost;
        private Int32 rconPort;
        private string rconPassword;
        private Int32 rebootSeconds;


        public Form1()
        {
            InitializeComponent();
            InitializeTimer();
            string buildVersion = Application.ProductVersion;
            int endIndex = buildVersion.IndexOf('+'); // �ҵ��汾���е�"+"���ŵ�����λ��
            string version = buildVersion.Substring(0, endIndex); // ʹ��Substring������ȡ��0��endIndex֮������ַ���
            this.Text = $"Palworld Server Protector v{version}";
        }

        private void InitializeTimer()
        {
            memTimer = new Timer();
            memTimer.Interval = 35000; // ���ö�ʱ�����Ϊ5��
            memTimer.Tick += Timer_Tick;

            saveTimer = new Timer();
            saveTimer.Interval = 35000; // ���ö�ʱ�����Ϊ5��
            saveTimer.Tick += saveTimer_Tick;

            getplayerTimer = new Timer();
            getplayerTimer.Interval = 3000; // ���ö�ʱ�����Ϊs��
            getplayerTimer.Tick += getplayerTimer_Tick;
        }



        private void Timer_Tick(object sender, EventArgs e)
        {
            // ��ȡϵͳ�ڴ�ʹ�ðٷֱ�
            var memoryUsage = Math.Round(GetSystemMemoryUsagePercentage(), 2);
            memProcessbar.Value = (int)memoryUsage;
            memOutput.Text = $"{memoryUsage}%";
            if (checkBox_mem.Checked)
            {
                //OutputMessageAsync($"��ǰʱ�䣺{DateTime.Now}");
                OutputMessageAsync($"�ڴ�ʹ�ðٷֱȣ�{memoryUsage}%");
            }



            if (checkBox_reboot.Checked)//�Զ��ط�
            {
                if (memoryUsage >= memTarget)
                {
                    try
                    {
                        var isProcessRunning = IsProcessRunning(cmdPath);
                        if (isProcessRunning)
                        {
                            OutputMessageAsync($"�ڴ�ﵽ������ֵ������");
                            // ʹ��rcon�����˷���ָ��
                            RconUtils.TestConnection(rconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text);
                            var info = RconUtils.SendMsg("save");
                            OutputMessageAsync($"{info}");
                            var result = RconUtils.SendMsg($"Shutdown {rebootSeconds} The_server_will_restart_in_{rebootSeconds}_seconds.");

                            OutputMessageAsync($"{result}");
                        }


                    }
                    catch
                    {
                        OutputMessageAsync($"����ָ��ʧ�ܣ��������á�");

                    }





                }
            }

            if (checkBox_startprocess.Checked)
            { //���&����
              // �������Ƿ�������
                var isProcessRunning = IsProcessRunning(cmdPath);
                OutputMessageAsync($"��������״̬��{(isProcessRunning ? "������" : "δ����")}");
                if (!isProcessRunning)
                {
                    if (!isProcessRunning)
                    {
                        try
                        {
                            OutputMessageAsync($"���ڳ������������...");

                            Process.Start(cmdPath);
                        }
                        catch (Exception ex)
                        {
                            OutputMessageAsync($"���������ʧ�ܣ�����·��");
                        }
                    }

                }
            }

        }

        private void getplayerTimer_Tick(object sender, EventArgs e) //��ȡ�������
        {
            try
            {
                if (!checkBox_geplayers.Checked)
                {
                    return;
                }

                RconUtils.TestConnection(rconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text);
                var players = RconUtils.ShowPlayers();

                playersCounterLabel.Text = $"��ǰ���ߣ�{players.Count}��";
                // Clear the playersView
                playersView.Items.Clear();

                // Add the players information to the playersView
                foreach (var player in players)
                {
                    var item = new ListViewItem(new[] { player.name, player.uid, player.steam_id });
                    playersView.Items.Add(item);
                }
            }
            catch
            {

            }
        }
        private void CopyGameDataToBackupPath()
        {
            try
            {
                string backupFolderName = $"SaveGames-{DateTime.Now.ToString("yyyyMMdd-HHmmss")}.zip";
                string backupFilePath = Path.Combine(backupPath, backupFolderName);

                if (!Directory.Exists(gamedataPath))
                {
                    OutputMessageAsync($"��Ϸ�浵·�������ڣ�{gamedataPath}");
                    return;
                }

                if (!Directory.Exists(backupPath))
                {
                    OutputMessageAsync($"�浵����·�������ڣ�{backupPath}");
                    return;
                }

                ZipFile.CreateFromDirectory(gamedataPath, backupFilePath);

                OutputMessageAsync($"��Ϸ�浵�ѳɹ�����");
            }
            catch (Exception ex)
            {
                OutputMessageAsync($"���ݴ浵ʧ�ܣ�{ex.Message}");
            }
        }

        private void saveTimer_Tick(object sender, EventArgs e) //�浵�߼�
        {
            OutputMessageAsync($"�Զ��浵��...");
            CopyGameDataToBackupPath();

        }

        private float GetSystemMemoryUsagePercentage()
        {
            var info = new ComputerInfo();
            var totalMemory = (float)info.TotalPhysicalMemory;
            var availableMemory = (float)info.AvailablePhysicalMemory;

            var memoryUsage = (totalMemory - availableMemory) / totalMemory;

            return memoryUsage * 100;
        }



        private bool IsProcessRunning(string processPath)
        {
            var processName = Path.GetFileNameWithoutExtension(processPath);
            var processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }


        private void selectCmdbutton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Executable Files|*.exe";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                cmdbox.Text = openFileDialog.FileName;
                cmdPath = cmdbox.Text;
                OutputMessageAsync($"��ѡ������·��Ϊ��{cmdPath}");
                gamedataPath = Path.Combine(Path.GetDirectoryName(cmdPath), "Pal", "Saved", "SaveGames");
                OutputMessageAsync($"��Ϸ�浵·���޸�Ϊ��{gamedataPath}");
            }
        }

        private async Task OutputMessageAsync(string message)
        {
            await Task.Run(() =>
            {
                outPutbox.Invoke(new Action(() =>
                {
                    outPutbox.AppendText($"[{DateTime.Now.ToString("HH:mm:ss")}] {message}" + Environment.NewLine);

                    if (outPutbox.Lines.Length > 50)
                    {
                        outPutbox.Text = string.Join(Environment.NewLine, outPutbox.Lines.Skip(outPutbox.Lines.Length - 50));
                        outPutbox.SelectionStart = outPutbox.Text.Length;
                        outPutbox.ScrollToCaret();
                    }
                    else
                    {
                        outPutbox.SelectionStart = outPutbox.Text.Length;
                        outPutbox.ScrollToCaret();
                    }
                }));
            });
        }

        private async void startAndstop_Click(object sender, EventArgs e)
        {
            try
            {
                RconUtils.TestConnection(rconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text);

                await Task.CompletedTask;
                var info = RconUtils.SendMsg("info");
                MessageBox.Show(info);
            }
            catch
            {
                MessageBox.Show("����ʧ�ܣ����");

            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            memTimer.Interval = Convert.ToInt32(checkSecondbox.Value) * 1000;
            memTimer.Start();
            memTarget = Convert.ToInt32(memTargetbox.Value);
            rconHost = "127.0.0.1";
            rconPort = 25575;
            rconPassword = "admin";
            rebootSeconds = 10;
            cmdPath = "";
            backupPath = "";
            playersView.View = View.Details;

            playersView.Columns.Add(new ColumnHeader() { Text = "Name", Width = playersView.Width / 3 });
            playersView.Columns.Add(new ColumnHeader() { Text = "UID", Width = playersView.Width / 3 });
            playersView.Columns.Add(new ColumnHeader() { Text = "Steam ID", Width = playersView.Width / 3 });

            playersView.FullRowSelect = true;
            playersView.MultiSelect = false;
            playersView.HideSelection = false;


            string buildVersion = Application.ProductVersion;
            int endIndex = buildVersion.IndexOf('+'); // �ҵ��汾���е�"+"���ŵ�����λ��
            string version = buildVersion.Substring(0, endIndex); // ʹ��Substring������ȡ��0��endIndex֮������ַ���
            verisionLabel.Text = $"��ǰ�汾��{version}";

            OutputMessageAsync($"��ǰ�����汾�ţ�{version}");
            OutputMessageAsync($"����Ŀ��Դ��ַ��https://github.com/KirosHan/Palworld-server-protector-DotNet");


            OutputMessageAsync($"�������ú���Ϣ���ٹ�ѡ��������");


        }

        private void checkSecondbox_ValueChanged(object sender, EventArgs e)
        {
            var newSecond = Convert.ToInt32(checkSecondbox.Value);
            memTimer.Interval = newSecond * 1000;
            OutputMessageAsync($"��������ѵ���Ϊ��{newSecond}��");
        }

        private void cmdbox_TextChanged(object sender, EventArgs e)
        {
            cmdPath = cmdbox.Text;
            OutputMessageAsync($"�����·���޸�Ϊ��{cmdPath}");
            gamedataPath = Path.Combine(Path.GetDirectoryName(cmdPath), "Pal", "Saved", "SaveGames");
            OutputMessageAsync($"��Ϸ�浵·���޸�Ϊ��{gamedataPath}");


        }

        private void checkBox_startprocess_CheckedChanged(object sender, EventArgs e)
        {
            if (cmdPath == "")
            {
                OutputMessageAsync($"�������÷����·����");
                checkBox_startprocess.Checked = false;

            }
            else if (checkBox_startprocess.Checked)
            {
                OutputMessageAsync($"�ѿ�ʼ��ط���ˡ�");
            }
            else
            {
                OutputMessageAsync($"��ֹͣ��ط���ˡ�");
            }

        }

        private void checkBox_save_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_save.Checked)
            {
                if (cmdPath == "")
                {
                    OutputMessageAsync($"����ѡ������·����");
                    checkBox_save.Checked = false;
                }
                else if (backupPath == "")
                {
                    OutputMessageAsync($"����ѡ��浵����·����");
                    checkBox_save.Checked = false;
                }
                else
                {
                    saveTimer.Interval = Convert.ToInt32(backupSecondsbox.Value) * 1000;
                    saveTimer.Start();
                    OutputMessageAsync($"�������Զ����ݴ浵��");
                }

            }
            else
            {
                saveTimer.Stop();
                OutputMessageAsync($"��ͣ���Զ����ݴ浵��");
            }

        }

        private void backupSecondsbox_ValueChanged(object sender, EventArgs e)
        {

            var newBackupSecond = Convert.ToInt32(backupSecondsbox.Value) * 1000;
            saveTimer.Interval = newBackupSecond;
            OutputMessageAsync($"��������ѵ���Ϊ��{newBackupSecond}��");


        }

        private void selectBackuppathButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                backupPathbox.Text = folderBrowserDialog.SelectedPath;
                backupPath = backupPathbox.Text;
                OutputMessageAsync($"��ѡ��浵����·��Ϊ��{backupPath}");
            }
        }

        private void backupPathbox_TextChanged(object sender, EventArgs e)
        {
            backupPath = backupPathbox.Text;
            OutputMessageAsync($"�����ô浵����·��Ϊ��{backupPath}");
        }

        private void memTargetbox_ValueChanged(object sender, EventArgs e)
        {
            memTarget = Convert.ToInt32(memTargetbox.Value);
            OutputMessageAsync($"�ڴ���ֵ�ѵ���Ϊ��{memTarget}%");

        }

        private void rconPortbox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Char.IsDigit(e.KeyChar) && e.KeyChar != 8)
            {
                e.Handled = true;
            }
        }

        private void rconPortbox_TextChanged(object sender, EventArgs e)
        {
            rconPort = Convert.ToInt32(rconPortbox.Text);
            OutputMessageAsync($"Rcon�˿��ѵ���Ϊ��{rconPort}");

        }

        private void passWordbox_KeyPress(object sender, KeyPressEventArgs e)
        {
            rconPassword = passWordbox.Text;
            //OutputMessageAsync($"����������Ϊ��{rconPassword}");
        }

        private void rebootSecondbox_ValueChanged(object sender, EventArgs e)
        {
            rebootSeconds = Convert.ToInt32(rebootSecondbox.Value);
            OutputMessageAsync($"�����ӳ�������Ϊ��{rebootSeconds}��");

        }

        private void checkBox_reboot_CheckedChanged(object sender, EventArgs e)
        {
            if (cmdPath == "")
            {
                checkBox_reboot.Checked = false;
                OutputMessageAsync($"����ѡ������·����");
            }

            else if (checkBox_reboot.Checked)
            {
                OutputMessageAsync($"�������Զ��ط���");
            }
            else
            {
                OutputMessageAsync($"��ͣ���Զ��ط���");
            }
        }

        private void checkBox_geplayers_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_geplayers.Checked)
            {
                getplayerTimer.Start();
                OutputMessageAsync($"�������Զ���ȡ������ҡ�");
            }
            else
            {
                getplayerTimer.Stop();
                OutputMessageAsync($"��ͣ���Զ���ȡ������ҡ�");
                playersCounterLabel.Text = $"��ǰ���ߣ�δ֪";
            }
        }

        private void memOutput_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {

            RconUtils.TestConnection(rconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text);
            
            
            var info = RconUtils.SendMsg("save");
            OutputMessageAsync($"{info}");


        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                RconUtils.TestConnection(rconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text);

                var result = RconUtils.SendMsg($"Shutdown 10 The_server_will_restart_in_10econds.");

                OutputMessageAsync($"{result}");

            }
            catch (Exception ex)
            {
                OutputMessageAsync($"�ط�ָ���ʧ�ܣ�{ex.Message}");
            }

        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                RconUtils.TestConnection(rconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text);

                var info = RconUtils.SendMsg("info");

                int startIndex = info.IndexOf("[") + 1;
                int endIndex = info.IndexOf("]");
                string version = info.Substring(startIndex, endIndex - startIndex);
                versionLabel.Text = $"����˰汾��{version}";
                OutputMessageAsync($"��ǰ����˰汾��{version}");

            }
            catch (Exception ex)
            {
                OutputMessageAsync($"infoָ���ʧ�ܣ�{ex.Message}");
            }
         
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                RconUtils.TestConnection(rconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text);

                textBox1.Text = textBox1.Text.Replace(" ", "_");
                var info = RconUtils.SendMsg($"broadcast {textBox1.Text.Trim()}");

                OutputMessageAsync($"��ǰ����˰汾��{info}");

            }

            catch (Exception ex)
            {
                OutputMessageAsync($"broadcastָ���ʧ�ܡ�{ex.Message}");
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                string url = "https://github.com/KirosHan/Palworld-server-protector-DotNet";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
            }
        }

        private void passWordbox_TextChanged(object sender, EventArgs e)
        {
            
        }
    }
}
