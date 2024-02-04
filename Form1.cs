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
    using static System.Windows.Forms.LinkLabel;
    using System.Net;
    using System.Globalization;

    public partial class Form1 : Form
    {
        private Timer? memTimer;
        private Timer? saveTimer;
        private Timer? getplayerTimer;
        private Timer? getversionTimer;

        private configForm? configForm;

        private Settings Settings = new Settings();

        private string errorLogname = $"error_{DateTime.Now.ToString("yyyyMMddHHmmss")}.log";
        private string projectUrl = $"https://github.com/KirosHan/Palworld-server-protector-DotNet";
        private int playersTimercounter = 0;
        private int playersTimerthreshold = 600;//ÿ��Сʱ����600��
        private int getversionErrorCounter = 0;
        private string versionChcekUrl = $"http://127.0.0.1/version?v=";
        private const string ConfigFilePath = "config.ini";
        private const string JsonConfigFilePath = "config.json";
        private Dictionary<string, DateTime> playerNotificationTimes = new Dictionary<string, DateTime>();//��¼��Ҵ���֪ͨʱ��


        private List<PalUserInfo> lastPlayerlist = new List<PalUserInfo>();

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public Form1()
        {
            InitializeComponent();
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

            getversionTimer = new Timer();
            getversionTimer.Interval = 10000; // ���ö�ʱ�����Ϊs��
            getversionTimer.Tick += getversionTimer_Tick;
        }



        private async void Timer_Tick(object sender, EventArgs e)
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
                if (memoryUsage >= Settings.MemTarget)
                {
                    try
                    {
                        var isProcessRunning = IsProcessRunning(Settings.CmdPath);
                        if (isProcessRunning)
                        {
                            OutputMessageAsync($"�ڴ�ﵽ������ֵ������");
                            // ʹ��rcon�����˷���ָ��

                            var info = await Rcon.SendCommand(Settings.RconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text, "save");

                            OutputMessageAsync($"{info}");

                            var result = await Rcon.SendCommand(Settings.RconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text, $"Shutdown {Settings.RebootSeconds} The_server_will_restart_in_{Settings.RebootSeconds}_seconds.");

                            OutputMessageAsync($"{result}");
                            OutputMessageAsync($"�����浵��...");
                            CopyGameDataToBackupPath();
                            if (checkbox_web_reboot.Checked == true) { SendWebhookAsync("�ڴ�ﵽ������ֵ", $"�ڴ�ʹ���ʣ�{memoryUsage}%,�ѳ��Թرշ�������"); }

                            ShowNotification($"�ڴ�ʹ���ʣ�{memoryUsage}%,�ѳ��Թرշ�������");
                        }


                    }
                    catch (Exception ex)
                    {
                        OutputMessageAsync($"����ָ��ʧ�ܣ��������á�");

                        Task.Run(() => Logger.AppendToErrorLog($"ErrorCode:0x69>>>ָ��ʹ���>>>������Ϣ��{ex.Message}"));



                        if (checkbox_web_reboot.Checked == true) { SendWebhookAsync("Rconʧ��", $"���͹ط�ָ��ʧ�ܣ��뼰ʱ��顣"); }

                        ShowNotification($"���͹ط�ָ��ʧ�ܣ��뼰ʱ��顣");

                    }





                }
            }

            if (checkBox_startprocess.Checked)
            { //���&����
              // �������Ƿ�������
                var isProcessRunning = IsProcessRunning(Settings.CmdPath);
                labelForprogram.Text = $"{(isProcessRunning ? "������" : "δ����")}";
                OutputMessageAsync($"��������״̬��{(isProcessRunning ? "������" : "δ����")}");
                if (!isProcessRunning)
                {

                    try
                    {

                        Process process;
                        int processId;
                        if (checkBox_args.Checked && arguments.Text != "")
                        {
                            OutputMessageAsync($"���ڳ������������({arguments.Text})...");
                            process = Process.Start(Settings.CmdPath, arguments.Text);
                            processId = process.Id;
                        }
                        else
                        {
                            OutputMessageAsync($"���ڳ������������...");

                            process = Process.Start(Settings.CmdPath);
                            processId = process.Id;
                        }
                        if (processId > 0)
                        {
                            labelForPid.Text = processId.ToString();
                            labelForpidText.Visible = true;
                            labelForPid.Visible = true;
                            OutputMessageAsync($"����������ɹ���");
                            if (checkBox_web_startprocess.Checked) { SendWebhookAsync("����������ɹ�", $"����������ɹ���"); }

                            ShowNotification($"����������ɹ���");

                        }
                        else
                        {
                            OutputMessageAsync($"���������ʧ�ܡ�");
                            if (checkBox_web_startprocess.Checked) { SendWebhookAsync("���������ʧ��", $"���������ʧ�ܡ�"); }
                            ShowNotification($"���������ʧ�ܡ�");
                        }
                    }
                    catch (Exception ex)
                    {
                        OutputMessageAsync($"���������ʧ�ܡ�");
                        Task.Run(() => Logger.AppendToErrorLog($"ErrorCode:0x71>>>�������������>>>������Ϣ��{ex.Message}"));

                        if (checkBox_web_startprocess.Checked) { SendWebhookAsync("���������ʧ��", $"���������ʧ�ܣ��뼰ʱ��顣"); }
                        ShowNotification($"���������ʧ�ܣ��뼰ʱ��顣");
                    }


                }


            }

        }

        private async void getplayerTimer_Tick(object sender, EventArgs e) //��ȡ�������
        {
            try
            {
                if (!checkBox_geplayers.Checked)
                {
                    return;
                }

                //var players = RconUtils.ShowPlayers(Settings.RconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text);
                var players = await Rcon.GetPlayers(Settings.RconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text);

                playersCounterLabel.Text = $"��ǰ���ߣ�{players.Count}��";
                // Clear the playersView
                playersView.Items.Clear();

                var playerList = "";
                // Add the players information to the playersView
                foreach (var player in players)
                {
                    var item = new ListViewItem(new[] { player.Name, player.Uid, player.SteamId });
                    playersView.Items.Add(item);
                    playerList = playerList + player.Name + ",";
                }

                DateTime now = DateTime.Now;
                List<string> toNotifyNewPlayers = new List<string>();
                List<string> toNotifyOffPlayers = new List<string>();


                var newPlayerlist = "";


                List<PalUserInfo> newPlayers = players.Except(lastPlayerlist).ToList();
                foreach (var p in newPlayers)
                {
                    if (!playerNotificationTimes.ContainsKey(p.Name) || (now - playerNotificationTimes[p.Name]).TotalSeconds >= 5)
                    {
                        toNotifyNewPlayers.Add(p.Name);
                        playerNotificationTimes[p.Name] = now;
                    }
                }


                var offPlayerlist = "";
                List<PalUserInfo> offPlayers = lastPlayerlist.Except(players).ToList();
                foreach (var p in offPlayers)
                {
                    if (!playerNotificationTimes.ContainsKey(p.Name) || (now - playerNotificationTimes[p.Name]).TotalSeconds >= 5)
                    {
                        toNotifyOffPlayers.Add(p.Name);
                        playerNotificationTimes[p.Name] = now;
                    }
                }
                newPlayerlist = string.Join(",", toNotifyNewPlayers.Select(p => $"[{p}]"));
                offPlayerlist = string.Join(",", toNotifyOffPlayers.Select(p => $"[{p}]"));
                /*f���������ģ�qnmd
                var info = await Rcon.SendCommand(Settings.RconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text, $"Broadcast {newPlayerlist.Replace(" ", "_")}_join_the_game.");
                OutputMessageAsync($"{info}");
                */
                if (checkBox_playerStatus.Checked == true)
                {
                    if (newPlayerlist != "")
                    {
                        OutputMessageAsync($"{newPlayerlist.TrimEnd(',')}��������Ϸ��");
                        SendWebhookAsync("��Ҽ�����Ϸ", $"{newPlayerlist.TrimEnd(',')}��������Ϸ��");
                    }
                    if (offPlayerlist != "")
                    {
                        OutputMessageAsync($"{offPlayerlist.TrimEnd(',')}�뿪����Ϸ��");
                        SendWebhookAsync("����뿪��Ϸ", $"{offPlayerlist.TrimEnd(',')}�뿪����Ϸ��");
                    }

                }


                lastPlayerlist = players;

                playersTimercounter += 1;
                if (playersTimercounter >= playersTimerthreshold)
                {
                    playersTimercounter = 0;
                    playerList = playerList.TrimEnd(',');
                    if (checkBox_web_getplayers.Checked == true) { SendWebhookAsync("�������ͳ��", $"��ǰ������ң�{players.Count}�ˡ�\r\n{playerList}"); }

                }
            }
            catch (Exception ex)
            {
                Task.Run(() => Logger.AppendToErrorLog($"ErrorCode:0x67>>>��ȡ�������ʧ��>>>������Ϣ��{ex.Message}"));

            }
        }

        private async void CopyGameDataToBackupPath()
        {
            await Task.Run(() =>
            {
                try
                {
                    if (Settings.BackupPath == "")
                    {
                        // ע�⣺�Ӻ�̨�̸߳���UIʱ����ʹ��Invoke
                        Invoke(new Action(() => OutputMessageAsync($"δ���ñ��ݴ��Ŀ¼���޷����ݡ�")));
                        return;
                    }
                    string backupFolderName = $"SaveGames-{DateTime.Now.ToString("yyyyMMdd-HHmmss")}.zip";
                    string backupFilePath = Path.Combine(Settings.BackupPath, backupFolderName);

                    if (!Directory.Exists(Settings.GameDataPath))
                    {
                        Invoke(new Action(() => OutputMessageAsync($"��Ϸ�浵·�������ڣ�{Settings.GameDataPath}")));
                        return;
                    }

                    if (!Directory.Exists(Settings.BackupPath))
                    {
                        Invoke(new Action(() => OutputMessageAsync($"�浵����·�������ڣ�{Settings.BackupPath}")));
                        return;
                    }

                    string tempGameDataPath = Path.Combine(Path.GetTempPath(), "TempGameData");
                    Directory.CreateDirectory(tempGameDataPath);
                    string tempGameDataCopyPath = Path.Combine(tempGameDataPath, "GameData");

                    // Copy the game data to the temporary directory
                    DirectoryCopy(Settings.GameDataPath, tempGameDataCopyPath, true);

                    // Create the backup file from the temporary game data directory
                    ZipFile.CreateFromDirectory(tempGameDataCopyPath, backupFilePath);

                    // Delete the temporary game data directory
                    Directory.Delete(tempGameDataPath, true);

                    Invoke(new Action(() => OutputMessageAsync($"��Ϸ�浵�ѳɹ�����")));


                    if (checkBox_web_save.Checked) { SendWebhookAsync("�浵����", $"��Ϸ�浵�ѳɹ����ݡ�"); }
                    ShowNotification($"��Ϸ�浵�ѳɹ����ݡ�");
                }
                catch (Exception ex)
                {
                    Invoke(new Action(() => OutputMessageAsync($"���ݴ浵ʧ��")));


                    Task.Run(() => Logger.AppendToErrorLog($"ErrorCode:0x92>>>���ݴ浵����>>>������Ϣ��{ex.Message}"));

                    if (checkBox_web_save.Checked) { SendWebhookAsync("�浵����ʧ��", $"�浵����ʧ�ܣ��뼰ʱ��顣"); }

                    ShowNotification($"�浵����ʧ�ܣ��뼰ʱ��顣");
                }

            });

        }

        private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the source directory does not exist, throw an exception
            if (!dir.Exists)
            {
                OutputMessageAsync($"��Ϸ�浵·�������ڣ�{sourceDirName}");

                Task.Run(() => Logger.AppendToErrorLog($"ErrorCode:0x91>>>��Ϸ�浵·��������>>>������Ϣ��{sourceDirName}"));

                ShowNotification($"��Ϸ�浵·�������ڣ�{sourceDirName}");
            }

            // If the destination directory does not exist, create it
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
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
                Settings.CmdPath = cmdbox.Text;
                OutputMessageAsync($"��ѡ������·��Ϊ��{Settings.CmdPath}");
                Settings.GameDataPath = Path.Combine(Path.GetDirectoryName(Settings.CmdPath), "Pal", "Saved", "SaveGames");
                gamedataBox.Text = Settings.GameDataPath;
                OutputMessageAsync($"��Ϸ�浵·���޸�Ϊ��{Settings.GameDataPath}");
            }
        }

        private void OutputMessageAsync(string message)
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
        }

        private void getversionTimer_Tick(object sender, EventArgs e) //��ȡ�汾��Ϣ
        {
            string buildVersion = Application.ProductVersion;
            int endIndex = buildVersion.IndexOf('+');
            string version = endIndex > 0 ? buildVersion.Substring(0, endIndex): buildVersion; //ȥ��������ʶ��
            checkVersion(version);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            InitializeTimer();
            string buildVersion = Application.ProductVersion;
            int endIndex = buildVersion.IndexOf('+'); // �ҵ��汾���е�"+"���ŵ�����λ��
            string version = endIndex > 0 ? buildVersion.Substring(0, endIndex) : buildVersion;
            this.Text = $"Palworld Server Protector v{version}";

            LoadConfig();
            memTimer.Start();
            verisionLabel.Text = $"��ǰ�汾��{version}";
            checkVersion(version);
            OutputMessageAsync($"��ǰ�����汾�ţ�{version}");
        }

        private async void checkVersion(string myversion)
        {
            try
            {

                using (WebClient client = new WebClient())
                {
                    string json = await client.DownloadStringTaskAsync(new Uri(versionChcekUrl + myversion));
                    dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                    string latestVersion = data[0].version;
                    string notice = data[0].notice;
                    string news = data[0].news;
                    string updatetime = data[0].date.ToString("yyyy/MM/dd");

                    if (notice != "")
                    {
                        OutputMessageAsync($"{notice}");
                        ShowNotification($"{notice}", true);
                    }
                    if (news != "")
                    {
                        OutputMessageAsync($"{news}");
                    }

                    if (IsVersionNewer(latestVersion, myversion))
                    {

                        this.Invoke(new Action(() =>
                        {
                            linkLabel2.Text = $"����������°汾(v{latestVersion})";
                            projectUrl = data[0].url;
                            linkLabel2.Visible = true;
                        }));

                        OutputMessageAsync($"�����¡��°汾v{latestVersion}��{updatetime}���Ѿ��������������·�����ǰ�����ظ��¡�");
                    }

                }
                getversionTimer.Stop();
            }
            catch
            {
                if (getversionErrorCounter == 0)
                {
                    getversionTimer.Start();
                }
                getversionErrorCounter++;
                if (getversionErrorCounter >= 5)
                {
                    getversionTimer.Stop();
                }

            }

        }

        private bool IsVersionNewer(string latestVersion, string myVersion)
        {
            Version latest = new Version(latestVersion);
            Version current = new Version(myVersion);
            return latest > current;
        }

        private void SyncUIWithSettings(Settings settings)
        {
            // ͬ���ַ�������ֵ����
            cmdbox.Text = settings.CmdPath;
            backupPathbox.Text = settings.BackupPath;
            gamedataBox.Text = settings.GameDataPath;
            memTargetbox.Value = settings.MemTarget;
            //Settings.RconHostbox.Text = settings.RconHost;
            rconPortbox.Text = settings.RconPort.ToString();
            passWordbox.Text = settings.RconPassword;
            rebootSecondbox.Value = settings.RebootSeconds;
            checkSecondbox.Value = settings.CheckSeconds;
            backupSecondsbox.Value = settings.BackupSeconds;
            arguments.Text = settings.Parameters;
            webhookBox.Text = settings.WebhookUrl;

            // ͬ���������ԣ���ѡ��
            checkBox_reboot.Checked = settings.IsReboot;
            checkBox_startprocess.Checked = settings.IsStartProcess;
            checkBox_args.Checked = settings.IsParameters;
            checkBox_Noti.Checked = settings.IsNoti;
            checkBox_save.Checked = settings.IsSave;
            checkBox_geplayers.Checked = settings.IsGetPlayers;
            checkBox_webhook.Checked = settings.IsWebhook;
            checkBox_web_getplayers.Checked = settings.IsWebGetPlayers;
            checkbox_web_reboot.Checked = settings.IsWebReboot;
            checkBox_web_save.Checked = settings.IsWebSave;
            checkBox_web_startprocess.Checked = settings.IsWebStartProcess;
            checkBox_playerStatus.Checked = settings.IsWebPlayerStatus;
        }


        private void LoadConfig()
        {
            try
            {
                if (System.IO.File.Exists(JsonConfigFilePath))
                {
                    Settings = Settings.LoadFromConfigFile(JsonConfigFilePath);
                    OutputMessageAsync($"�� {JsonConfigFilePath} ��ȡ����");
                }
                else
                {
                    if (System.IO.File.Exists(ConfigFilePath))
                    {
                        Settings = Settings.LoadSettingsFromIniFile(ConfigFilePath);
                        OutputMessageAsync($"�� {ConfigFilePath} ��ȡ����");
                    }
                    else
                    {
                        OutputMessageAsync($"δ�ҵ������ļ����Ѽ���Ĭ�����á�");
                        ShowNotification($"δ�ҵ������ļ����Ѽ���Ĭ�����á�");
                    }
                    SaveConfig();
                }
                SyncUIWithSettings(Settings);
            }
            catch (Exception ex)
            {
                OutputMessageAsync($"��ȡ�����ļ�ʧ�ܡ�");
                ShowNotification($"��ȡ�����ļ�ʧ�ܡ�");
                Logger.AppendToErrorLog($"ErrorCode:0xA1>>>��ȡ�����ļ�����>>>������Ϣ��{ex.Message}");
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveConfig();
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; // ȡ���ر��¼�
                this.Hide(); // ���ش���
                ShowNotification($"��������С�������̡�", true);
                notifyIcon1.Visible = true; // ��ʾ����ͼ��
            }
        }


        private void SaveConfig()
        {
            Settings.SaveToConfigFile(JsonConfigFilePath);
        }

        private void checkBox_startprocess_CheckedChanged(object sender, EventArgs e)
        {
            if (Settings.CmdPath == "")
            {
                labelForstart.Text = "[ �ر� ]";
                OutputMessageAsync($"�������÷����·����");
                labelForPid.Visible = false;
                labelForpidText.Visible = false;
                checkBox_startprocess.Checked = false;


            }
            else if (checkBox_startprocess.Checked)
            {
                labelForstart.Text = "[ ���� ]";
                OutputMessageAsync($"�ѿ�ʼ��ط���ˡ�");
            }
            else
            {
                labelForprogram.Text = "δ֪";
                labelForstart.Text = "[ �ر� ]";
                labelForPid.Visible = false;
                labelForpidText.Visible = false;
                OutputMessageAsync($"��ֹͣ��ط���ˡ�");
            }

        }

        private void checkBox_save_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_save.Checked)
            {
                if (Settings.GameDataPath == "")
                {
                    OutputMessageAsync($"����ѡ����Ϸ�浵·����");
                    labelForsave.Text = "[ �ر� ]";
                    checkBox_save.Checked = false;
                }
                else if (Settings.BackupPath == "")
                {
                    OutputMessageAsync($"����ѡ��浵����·����");
                    labelForsave.Text = "[ �ر� ]";
                    checkBox_save.Checked = false;
                }
                else
                {
                    saveTimer.Interval = Convert.ToInt32(backupSecondsbox.Value) * 1000;
                    saveTimer.Start();
                    labelForsave.Text = "[ ���� ]";
                    OutputMessageAsync($"�������Զ����ݴ浵��");
                    OutputMessageAsync($"�Զ��浵��...");
                    CopyGameDataToBackupPath();

                }

            }
            else
            {
                saveTimer.Stop();
                labelForsave.Text = "[ �ر� ]";
                OutputMessageAsync($"��ͣ���Զ����ݴ浵��");
            }

        }

        private void selectBackuppathButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                backupPathbox.Text = folderBrowserDialog.SelectedPath;
                Settings.BackupPath = backupPathbox.Text;
                OutputMessageAsync($"��ѡ��浵����·��Ϊ��{Settings.BackupPath}");
            }
        }





        private void rconPortbox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Char.IsDigit(e.KeyChar) && e.KeyChar != 8)
            {
                e.Handled = true;
            }
        }



        private void rebootSecondbox_ValueChanged(object sender, EventArgs e)
        {


        }

        private void checkBox_reboot_CheckedChanged(object sender, EventArgs e)
        {
            if (Settings.CmdPath == "")
            {
                checkBox_reboot.Checked = false;
                labelForreboot.Text = "[ �ر� ]";
                OutputMessageAsync($"����ѡ������·����");
            }

            else if (checkBox_reboot.Checked)
            {
                labelForreboot.Text = "[ ���� ]";
                OutputMessageAsync($"�������Զ��ط���");
            }
            else
            {
                labelForreboot.Text = "[ �ر� ]";
                OutputMessageAsync($"��ͣ���Զ��ط���");
            }
        }

        private void checkBox_geplayers_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_geplayers.Checked)
            {
                playersTimercounter = playersTimerthreshold;//����������һ��
                getplayerTimer.Start();
                labelForgetplayers.Text = "[ ���� ]";
                OutputMessageAsync($"�������Զ���ȡ������ҡ�");
            }
            else
            {
                getplayerTimer.Stop();
                labelForgetplayers.Text = "[ �ر� ]";
                OutputMessageAsync($"��ͣ���Զ���ȡ������ҡ�");
                playersCounterLabel.Text = $"��ǰ���ߣ�δ֪";
            }
        }

        private void memOutput_Click(object sender, EventArgs e)
        {

        }

        private async void button2_Click(object sender, EventArgs e)
        {

            var info = await Rcon.SendCommand(Settings.RconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text, "save");
            OutputMessageAsync($"{info}");


        }

        private async void button3_Click(object sender, EventArgs e)
        {
            try
            {


                var result = await Rcon.SendCommand(Settings.RconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text, $"Shutdown 10 The_server_will_restart_in_10econds.");

                OutputMessageAsync($"{result}");

            }
            catch (Exception ex)
            {
                OutputMessageAsync($"�ط�ָ���ʧ�ܡ�");
                Task.Run(() => Logger.AppendToErrorLog($"ErrorCode:0x69>>>ָ��ʹ���>>>������Ϣ��{ex.Message}"));

            }

        }

        private async void button4_Click(object sender, EventArgs e)
        {
            var info = "";
            try
            {


                info = await Rcon.SendCommand(Settings.RconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text, "info");

                int startIndex = info.IndexOf("[") + 1;
                int endIndex = info.IndexOf("]");
                string version = info.Substring(startIndex, endIndex - startIndex);
                int lastSpaceIndex = info.LastIndexOf(" ");
                string serverName = info.Substring(lastSpaceIndex + 1);
                labelForservername.Text = $"���������ƣ�{serverName}";
                versionLabel.Text = $"����˰汾��{version}";
                OutputMessageAsync($"��ǰ����˰汾��{version}");

            }
            catch (Exception ex)
            {
                OutputMessageAsync($"��������ʱ��������");

                Task.Run(() => Logger.AppendToErrorLog($"ErrorCode:0x68>>>�������˰汾��Ϣ����>>>����ֵΪ[{info}]>>>������Ϣ��{ex.Message}"));

            }

        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {


                textBox1.Text = textBox1.Text.Replace(" ", "_");
                var info = await Rcon.SendCommand(Settings.RconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text, $"broadcast {textBox1.Text.Trim()}");

                OutputMessageAsync($"{info}");

            }

            catch (Exception ex)
            {
                OutputMessageAsync($"broadcastָ���ʧ�ܡ�");
                Task.Run(() => Logger.AppendToErrorLog($"ErrorCode:0x69>>>ָ��ʹ���>>>������Ϣ��{ex.Message}"));

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
                Settings.GameDataPath = gamedataBox.Text;
                OutputMessageAsync($"��ѡ����Ϸ�浵·��Ϊ��{Settings.GameDataPath}");
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
            Settings.RebootSeconds = Convert.ToInt32(rebootSecondbox.Value);
            isKeyUpEvent_rebootSecond = true;
            OutputMessageAsync($"�����ӳ�������Ϊ��{Settings.RebootSeconds}��");
        }

        private void rebootSecondbox_ValueChanged_1(object sender, EventArgs e)
        {
            if (isKeyUpEvent_rebootSecond)
            {
                isKeyUpEvent_rebootSecond = false;
                return;
            }
            Settings.RebootSeconds = Convert.ToInt32(rebootSecondbox.Value);
            OutputMessageAsync($"�����ӳ�������Ϊ��{Settings.RebootSeconds}��");
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

        private void memTargetbox_ValueChanged(object sender, EventArgs e)
        {
            Settings.MemTarget = (int)memTargetbox.Value;
            OutputMessageAsync($"�ڴ���ֵ�ѵ���Ϊ��{Settings.MemTarget}%");
        }

        private void playersView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (playersView.SelectedItems.Count > 0)
            {

                string Uid = playersView.SelectedItems[0].SubItems[1].Text;
                UIDBox.Text = Uid;

            }
        }

        private async void kickbutton_Click(object sender, EventArgs e)
        {
            try
            {


                //var info = RconUtils.SendMsg(Settings.RconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text, $"KickPlayer {UIDBox.Text.Trim()}");
                var info = await Rcon.SendCommand(Settings.RconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text, $"KickPlayer {UIDBox.Text.Trim()}");

                OutputMessageAsync($"{info}");

            }

            catch (Exception ex)
            {
                OutputMessageAsync($"Kickplayerָ���ʧ�ܡ�");
                Task.Run(() => Logger.AppendToErrorLog($"ErrorCode:0x69>>>ָ��ʹ���>>>������Ϣ��{ex.Message}"));

            }
        }

        private async void banbutton_Click(object sender, EventArgs e)
        {
            try
            {


                // var info = RconUtils.SendMsg(Settings.RconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text, $"BanPlayer {UIDBox.Text.Trim()}");
                var info = await Rcon.SendCommand(Settings.RconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text, $"BanPlayer {UIDBox.Text.Trim()}");

                OutputMessageAsync($"{info}");

            }

            catch (Exception ex)
            {
                OutputMessageAsync($"BanPlayerָ���ʧ�ܡ�{ex.Message}");
                Task.Run(() => Logger.AppendToErrorLog($"ErrorCode:0x69>>>ָ��ʹ���>>>������Ϣ��{ex.Message}"));

            }
        }


        private void settingButton_Click(object sender, EventArgs e)
        {
            if (configForm == null || configForm.IsDisposed) // Check if the configForm is null or disposed
            {
                configForm = new configForm(); // Create a new instance of ConfigForm
                configForm.Show(); // Show the configForm
            }
            else
            {
                configForm.BringToFront(); // Bring the existing configForm to the front
            }
        }

        private void testWebhookbutton_Click(object sender, EventArgs e)
        {

            SendWebhookAsync("���Ա���", "����һ����������֪ͨ��");

        }
        private async Task SendWebhookAsync(string title, string message)
        {
            if (!checkBox_webhook.Checked)
            {
                return;
            }
            if (webhookBox.Text == "")
            {
                OutputMessageAsync($"Webhook��ַΪ�ա�");
                return;
            }
            if (!webhookBox.Text.Contains("http"))
            {
                OutputMessageAsync($"Webhook��ʽ����ȷ��");
                return;
            }
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string webhookUrl = webhookBox.Text;
                    var webhook = new WebhookJson();
                    string json = webhook.GenerateJson(webhookUrl, title, message);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    await client.PostAsync(webhookUrl, content);
                    OutputMessageAsync($"Webhook���ͳɹ���");
                }
            }
            catch (Exception ex)
            {
                OutputMessageAsync($"Webhook����ʧ�ܡ�");

                Task.Run(() => Logger.AppendToErrorLog($"ErrorCode:0x71>>>Webhook���ʹ���>>>��ز�����title=[{title}],message=[{message}]>>>������Ϣ��{ex.Message}"));

            }

        }

        private void checkBox_webhook_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_webhook.Checked)
            {
                webhookBox.Enabled = true;
                testWebhookbutton.Enabled = true;
                labelForwebhook.Text = "[ ���� ]";
                OutputMessageAsync($"������Webhook���͡�");
            }
            else
            {
                webhookBox.Enabled = false;
                testWebhookbutton.Enabled = false;
                labelForwebhook.Text = "[ �ر� ]";
                OutputMessageAsync($"��ͣ��Webhook���͡�");
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show(); // ��ʾ����
            this.WindowState = FormWindowState.Normal;

        }


        private void ShowNotification(string message, Boolean forced = false)
        {
            if (!forced)
            {
                if (checkBox_Noti.Checked)
                {
                    notifyIcon1.BalloonTipText = message;
                    notifyIcon1.ShowBalloonTip(2000);
                }
            }
            else
            {
                notifyIcon1.BalloonTipText = message;
                notifyIcon1.ShowBalloonTip(2000);
            }


        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(projectUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SaveConfig();

            Application.Exit();
        }

    }
}
