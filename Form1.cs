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
    using System.Runtime.InteropServices;
    using System.Text;

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
        private string errorLogname = $"error_{DateTime.Now.ToString("yyyyMMddHHmmss")}.log";

        private const string ConfigFilePath = "config.ini";

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

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
                    catch(Exception ex)
                    {
                        OutputMessageAsync($"����ָ��ʧ�ܣ��������á�");
                        AppendToErrorLog($"����ָ��ʧ�ܣ��������á�{ex.Message}");

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


                            if (checkBox_args.Checked && arguments.Text != "")
                            {
                                OutputMessageAsync($"���ڳ������������({arguments.Text})...");
                                Process.Start(cmdPath, arguments.Text);
                            }
                            else
                            {
                                OutputMessageAsync($"���ڳ������������...");

                                Process.Start(cmdPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            OutputMessageAsync($"���������ʧ�ܡ�");
                            AppendToErrorLog($"���������ʧ�ܣ�{ex.Message}");
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
            catch(Exception ex)
            {
                AppendToErrorLog($"��ȡ�������ʧ�ܣ�{ex.Message}");
            }
        }
        private void CopyGameDataToBackupPath()
        {
            try
            {
                if (backupPath == "")
                {
                    OutputMessageAsync($"δ���ñ��ݴ��Ŀ¼���޷����ݡ�");
                    return;
                }
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
                OutputMessageAsync($"���ݴ浵ʧ��");
                AppendToErrorLog($"���ݴ浵ʧ�ܣ�{ex.Message}");
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
                gamedataBox.Text = gamedataPath;
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

                    if (outPutbox.Lines.Length > 100)
                    {
                        outPutbox.Text = string.Join(Environment.NewLine, outPutbox.Lines.Skip(outPutbox.Lines.Length - 100));
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

   

        private void Form1_Load(object sender, EventArgs e)
        {

            playersView.View = View.Details;
            playersView.Columns.Add(new ColumnHeader() { Text = "Name", Width = playersView.Width / 3 });
            playersView.Columns.Add(new ColumnHeader() { Text = "UID", Width = playersView.Width / 3 });
            playersView.Columns.Add(new ColumnHeader() { Text = "Steam ID", Width = playersView.Width / 3 });

            playersView.FullRowSelect = true;
            playersView.MultiSelect = false;
            playersView.HideSelection = false;

            LoadConfig();
            memTimer.Start();
            string buildVersion = Application.ProductVersion;
            int endIndex = buildVersion.IndexOf('+'); // �ҵ��汾���е�"+"���ŵ�����λ��
            string version = buildVersion.Substring(0, endIndex); // ʹ��Substring������ȡ��0��endIndex֮������ַ���
            verisionLabel.Text = $"��ǰ�汾��{version}";

            OutputMessageAsync($"��ǰ�����汾�ţ�{version}");
            OutputMessageAsync($"����Ŀ��Դ��ַ��https://github.com/KirosHan/Palworld-server-protector-DotNet");

            OutputMessageAsync($"�������ú���Ϣ���ٹ�ѡ��������");

   
        }
        private void LoadConfig()
        {
            try
            {
                if (System.IO.File.Exists(ConfigFilePath))
                {
                    using (StreamReader reader = new StreamReader(ConfigFilePath))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.StartsWith("CmdPath="))
                            {
                                cmdPath = line.Substring("CmdPath=".Length);
                                cmdbox.Text = cmdPath;
                            }
                            else if (line.StartsWith("BackupPath="))
                            {
                                backupPath = line.Substring("BackupPath=".Length);
                                backupPathbox.Text = backupPath;
                            }
                            else if (line.StartsWith("GameDataPath="))
                            {
                                gamedataPath = line.Substring("GameDataPath=".Length);
                                gamedataBox.Text = gamedataPath;
                            }
                            else if (line.StartsWith("MemTarget="))
                            {
                                memTarget = Convert.ToInt32(line.Substring("MemTarget=".Length));
                                memTargetbox.Value = memTarget;
                            }
                            else if (line.StartsWith("RconHost="))
                            {
                                rconHost = line.Substring("RconHost=".Length);
                            }
                            else if (line.StartsWith("RconPort="))
                            {
                                rconPort = Convert.ToInt32(line.Substring("RconPort=".Length));
                                rconPortbox.Text = rconPort.ToString();
                            }
                            else if (line.StartsWith("RconPassword="))
                            {
                                rconPassword = line.Substring("RconPassword=".Length);
                                passWordbox.Text = rconPassword;
                            }
                            else if (line.StartsWith("RebootSeconds="))
                            {
                                rebootSeconds = Convert.ToInt32(line.Substring("RebootSeconds=".Length));
                                rebootSecondbox.Value = rebootSeconds;
                            }
                            else if (line.StartsWith("CheckSeconds="))
                            {
                                memTimer.Interval = Convert.ToInt32(line.Substring("CheckSeconds=".Length)) * 1000;
                                checkSecondbox.Value = memTimer.Interval / 1000;
                            }
                            else if (line.StartsWith("BackupSeconds="))
                            {
                                saveTimer.Interval = Convert.ToInt32(line.Substring("BackupSeconds=".Length)) * 1000;
                                backupSecondsbox.Value = saveTimer.Interval / 1000;
                            }
                            else if (line.StartsWith("Parameters="))
                            {
                                arguments.Text = line.Substring("Parameters=".Length);
                            }
                        }
                    }
                }
                else
                {
                    OutputMessageAsync($"δ�ҵ������ļ����Ѽ���Ĭ�����á�");
                    dataInit();
                }
            }
            catch (Exception ex)
            {
                OutputMessageAsync($"��ȡ�����ļ�ʧ�ܡ�");
                AppendToErrorLog($"��ȡ�����ļ�ʧ�ܣ�{ex.Message}");
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveConfig();
        }




        private void SaveConfig()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(ConfigFilePath))
                {
                    writer.WriteLine("[General]");
                    writer.WriteLine("CmdPath=" + cmdbox.Text);
                    writer.WriteLine("Parameters=" + arguments.Text);
                    writer.WriteLine("BackupPath=" + backupPathbox.Text);
                    writer.WriteLine("GameDataPath=" + gamedataBox.Text);
                    writer.WriteLine("MemTarget=" + memTarget);
                    writer.WriteLine("RconHost=" + rconHost);
                    writer.WriteLine("RconPort=" + rconPortbox.Text);
                    writer.WriteLine("RconPassword=" + passWordbox.Text);
                    writer.WriteLine("RebootSeconds=" + rebootSeconds);
                    writer.WriteLine("CheckSeconds=" + memTimer.Interval / 1000);
                    writer.WriteLine("BackupSeconds=" + saveTimer.Interval / 1000);
                }
            }
            catch (Exception ex)
            {
                OutputMessageAsync($"���������ļ�ʧ�ܡ�");
                AppendToErrorLog($"���������ļ�ʧ�ܣ�{ex.Message}");
            }
        }




        private void dataInit()
        {
            memTimer.Interval = Convert.ToInt32(checkSecondbox.Value) * 1000;

            memTarget = Convert.ToInt32(memTargetbox.Value);
            rconHost = "127.0.0.1";
            rconPort = 25575;
            rconPassword = "admin";
            rebootSeconds = 10;
            cmdPath = "";
            gamedataPath = "";
            backupPath = "";


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
                if (gamedataPath == "")
                {
                    OutputMessageAsync($"����ѡ����Ϸ�浵·����");
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





        private void rconPortbox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Char.IsDigit(e.KeyChar) && e.KeyChar != 8)
            {
                e.Handled = true;
            }
        }



        private void passWordbox_KeyPress(object sender, KeyPressEventArgs e)
        {
            rconPassword = passWordbox.Text;
            //OutputMessageAsync($"����������Ϊ��{rconPassword}");
        }

        private void rebootSecondbox_ValueChanged(object sender, EventArgs e)
        {


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
                OutputMessageAsync($"�ط�ָ���ʧ�ܡ�");
                AppendToErrorLog($"�ط�ָ���ʧ�ܣ�{ex.Message}");
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
                OutputMessageAsync($"infoָ���ʧ�ܡ�");
                AppendToErrorLog($"infoָ���ʧ�ܣ�{ex.Message}");
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                RconUtils.TestConnection(rconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text);

                textBox1.Text = textBox1.Text.Replace(" ", "_");
                var info = RconUtils.SendMsg($"broadcast {textBox1.Text.Trim()}");

                OutputMessageAsync($"�ѷ��ͣ�{info}");

            }

            catch (Exception ex)
            {
                OutputMessageAsync($"broadcastָ���ʧ�ܡ�");
                AppendToErrorLog($"broadcastָ���ʧ�ܣ�{ex.Message}");
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


        private void checkBox_args_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_args.Checked)
            {
                arguments.Enabled = true;
                OutputMessageAsync($"����д���������������");
            }
            else
            {
                arguments.Enabled = false;
            }
        }

        private void selectCustombutton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                gamedataBox.Text = folderBrowserDialog.SelectedPath;
                gamedataPath = gamedataBox.Text;
                OutputMessageAsync($"��ѡ����Ϸ�浵·��Ϊ��{gamedataPath}");
            }
        }
        private bool isKeyUpEvent_backupSecond = false;
        private void backupSecondsbox_KeyUp(object sender, KeyEventArgs e)
        {
            var newBackupSecond = Convert.ToInt32(backupSecondsbox.Value) * 1000;
            saveTimer.Interval = newBackupSecond;
            isKeyUpEvent_backupSecond = true;
            OutputMessageAsync($"�浵�����ѵ���Ϊ��{newBackupSecond / 1000}��");
        }
        private void backupSecondsbox_ValueChanged(object sender, EventArgs e)
        {
            if (isKeyUpEvent_backupSecond)
            {
                isKeyUpEvent_backupSecond = false;
                return;
            }
            var newBackupSecond = Convert.ToInt32(backupSecondsbox.Value) * 1000;
            saveTimer.Interval = newBackupSecond;
            OutputMessageAsync($"�浵�����ѵ���Ϊ��{newBackupSecond / 1000}��");
        }

        private bool isKeyUpEvent_rebootSecond = false;
        private void rebootSecondbox_KeyUp(object sender, KeyEventArgs e)
        {
            rebootSeconds = Convert.ToInt32(rebootSecondbox.Value);
            isKeyUpEvent_rebootSecond = true;
            OutputMessageAsync($"�����ӳ�������Ϊ��{rebootSeconds}��");
        }

        private void rebootSecondbox_ValueChanged_1(object sender, EventArgs e)
        {
            if (isKeyUpEvent_rebootSecond)
            {
                isKeyUpEvent_rebootSecond = false;
                return;
            }
            rebootSeconds = Convert.ToInt32(rebootSecondbox.Value);
            OutputMessageAsync($"�����ӳ�������Ϊ��{rebootSeconds}��");
        }

        private bool isKeyUpEvent_checkSecond = false;

        private void checkSecondbox_ValueChanged(object sender, EventArgs e)
        {
            if (isKeyUpEvent_checkSecond)
            {
                isKeyUpEvent_checkSecond = false;
                return;
            }
            var newSecond = Convert.ToInt32(checkSecondbox.Value);
            memTimer.Interval = newSecond * 1000;
            OutputMessageAsync($"��������ѵ���Ϊ��{newSecond}��");
        }

        private void checkSecondbox_KeyUp(object sender, KeyEventArgs e)
        {
            var newSecond = Convert.ToInt32(checkSecondbox.Value);
            memTimer.Interval = newSecond * 1000;
            isKeyUpEvent_checkSecond = true;
            OutputMessageAsync($"��������ѵ���Ϊ��{newSecond}��");
        }

        private bool isKeyUpEvent_memTarget = false;

        private void memTargetbox_KeyUp(object sender, KeyEventArgs e)
        {
            memTarget = Convert.ToInt32(memTargetbox.Value);
            isKeyUpEvent_memTarget = true;
            OutputMessageAsync($"�ڴ���ֵ�ѵ���Ϊ��{memTarget}%");
        }
        private void memTargetbox_ValueChanged(object sender, EventArgs e)
        {
            if (isKeyUpEvent_memTarget)
            {
                isKeyUpEvent_memTarget = false;
                return;
            }
            memTarget = Convert.ToInt32(memTargetbox.Value);
            OutputMessageAsync($"�ڴ���ֵ�ѵ���Ϊ��{memTarget}%");

        }

        private void playersView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (playersView.SelectedItems.Count > 0)
            {

                string Uid = playersView.SelectedItems[0].SubItems[1].Text;
                UIDBox.Text = Uid;

            }
        }

        private void kickbutton_Click(object sender, EventArgs e)
        {
            try
            {
                RconUtils.TestConnection(rconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text);

                var info = RconUtils.SendMsg($"KickPlayer {UIDBox.Text.Trim()}");

                OutputMessageAsync($"{info}");

            }

            catch (Exception ex)
            {
                OutputMessageAsync($"Kickplayerָ���ʧ�ܡ�");
                AppendToErrorLog($"Kickplayerָ���ʧ�ܣ�{ex.Message}");
            }
        }

        private void banbutton_Click(object sender, EventArgs e)
        {
            try
            {
                RconUtils.TestConnection(rconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text);

                var info = RconUtils.SendMsg($"BanPlayer {UIDBox.Text.Trim()}");

                OutputMessageAsync($"{info}");

            }

            catch (Exception ex)
            {
                OutputMessageAsync($"BanPlayerָ���ʧ�ܡ�{ex.Message}");
                AppendToErrorLog($"BanPlayerָ���ʧ�ܡ�");
            }
        }
        private void AppendToErrorLog(string content)
        {
            string logFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "log");
            string errorLogPath = Path.Combine(logFolderPath, errorLogname);

            if (!Directory.Exists(logFolderPath))
            {
                Directory.CreateDirectory(logFolderPath);
            }

            using (StreamWriter writer = System.IO.File.AppendText(errorLogPath))
            {
                writer.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] {content}");
            }
        }
    }
}
