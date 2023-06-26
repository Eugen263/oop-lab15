using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace FTPClient
{
    public partial class MainForm : Form
    {
        private TcpClient ftpClient;
        private NetworkStream networkStream;
        private StreamReader reader;
        private StreamWriter writer;
        private bool loggedIn = false;
        private string currentDir = "/";

        public MainForm()
        {
            InitializeComponent();
        }

        private void connectBtn_Click(object sender, EventArgs e)
        {
            try
            {
                ftpClient = new TcpClient(hostTextBox.Text, int.Parse(portTextBox.Text));
                networkStream = ftpClient.GetStream();
                reader = new StreamReader(networkStream);
                writer = new StreamWriter(networkStream);

                string response = reader.ReadLine();
                if (!response.StartsWith("220"))
                {
                    MessageBox.Show("Connection error: " + response);
                    ftpClient.Close();
                    return;
                }

                // Login
                sendCommand("USER " + userTextBox.Text);
                response = readResponse();
                if (!response.StartsWith("331"))
                {
                    MessageBox.Show("Login error: " + response);
                    ftpClient.Close();
                    return;
                }

                sendCommand("PASS " + passTextBox.Text);
                response = readResponse();
                if (!response.StartsWith("230"))
                {
                    MessageBox.Show("Login error: " + response);
                    ftpClient.Close();
                    return;
                }

                loggedIn = true;
                statusLabel.Text = "Connected to " + ftpClient.Client.RemoteEndPoint.ToString();

                // Get initial directory listing
                updateDirectoryListing();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection error: " + ex.Message);
            }
        }

        private void disconnectBtn_Click(object sender, EventArgs e)
        {
            if (ftpClient != null && ftpClient.Connected)
            {
                sendCommand("QUIT");
                ftpClient.Close();
                statusLabel.Text = "Disconnected";
                loggedIn = false;
                listBox.Items.Clear();
            }
        }

        private void listBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            string selected = listBox.SelectedItem.ToString();
            if (selected.EndsWith("/") || selected.EndsWith("\\"))
            {
                // Change to directory
                sendCommand("CWD " + selected);
                string response = readResponse();
                if (!response.StartsWith("250"))
                {
                    MessageBox.Show("Error changing directory: " + response);
                    return;
                }
                currentDir += selected;
                updateDirectoryListing();
            }
            else
            {
                // Download file
                if (!loggedIn)
                {
                    MessageBox.Show("Please log in first.");
                    return;
                }

                string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), selected);
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = selected;
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    savePath = saveFileDialog.FileName;
                }
                else
                {
                    return;
                }

                FileStream fileStream = File.Create(savePath);
                sendCommand("TYPE I");
                string response = readResponse();
                if (!response.StartsWith("200"))
                {
                    MessageBox.Show("Error setting transfer mode: " + response);
                    fileStream.Close();
                    return;
                }

                // Метод для завантаження файлу на сервер
                private void UploadFile(string localFilePath, string remoteFilePath)
                {
                    using (var client = new WebClient())
                    {
                        client.Credentials = new NetworkCredential(_username, _password);
                        client.UploadFile(_serverUri + remoteFilePath, "STOR", localFilePath);
                    }
                    RefreshFileList();
                }

                // Метод для створення каталогу на сервері
                private void CreateDirectory(string remoteDirectoryPath)
                {
                    var request = (FtpWebRequest)WebRequest.Create(_serverUri + remoteDirectoryPath);
                    request.Credentials = new NetworkCredential(_username, _password);
                    request.Method = WebRequestMethods.Ftp.MakeDirectory;
                    using (var response = (FtpWebResponse)request.GetResponse())
                    {
                        Console.WriteLine($"Directory created: {response.StatusDescription}");
                    }
                    RefreshFileList();
                }

                // Метод для виконання команди FTP на сервері
                private string ExecuteFtpCommand(string command, string path)
                {
                    var request = (FtpWebRequest)WebRequest.Create(_serverUri + path);
                    request.Credentials = new NetworkCredential(_username, _password);
                    request.Method = command;
                    var response = (FtpWebResponse)request.GetResponse();
                    var responseStream = response.GetResponseStream();
                    var reader = new StreamReader(responseStream);
                    var result = reader.ReadToEnd();
                    reader.Close();
                    response.Close();
                    return result;
                }

                // Метод для виконання команди FTP з параметрами на сервері
                private string ExecuteFtpCommandWithParameter(string command, string path, string parameter)
                {
                    var request = (FtpWebRequest)WebRequest.Create(_serverUri + path);
                    request.Credentials = new NetworkCredential(_username, _password);
                    request.Method = command + " " + parameter;
                    var response = (FtpWebResponse)request.GetResponse();
                    var responseStream = response.GetResponseStream();
                    var reader = new StreamReader(responseStream);
                    var result = reader.ReadToEnd();
                    reader.Close();
                    response.Close();
                    return result;
                }

                // Метод для отримання розміру файлу на сервері
                private long GetFileSize(string remoteFilePath)
                {
                    var request = (FtpWebRequest)WebRequest.Create(_serverUri + remoteFilePath);
                    request.Credentials = new NetworkCredential(_username, _password);
                    request.Method = WebRequestMethods.Ftp.GetFileSize;
                    using (var response = (FtpWebResponse)request.GetResponse())
                    {
                        return response.ContentLength;
                    }
                }

                // Метод для оновлення списку файлів і каталогів на сервері
                private void RefreshFileList()
                {
                    var request = (FtpWebRequest)WebRequest.Create(_serverUri);
                    request.Credentials = new NetworkCredential(_username, _password);
                    request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                    using (var response = (FtpWebResponse)request.GetResponse())
                    {
                        var responseStream = response.GetResponseStream();
                        var reader = new StreamReader(responseStream);
                        var result = reader.ReadToEnd();
                        reader.Close();
                        response.Close();
                        _fileList = result.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Substring(s.LastIndexOf(' ') + 1)).ToList();
                    }
                    DisplayFileList();
                }

                // FTP server host name
private string ftpHost = "ftp.example.com";
        // FTP server username
        private string ftpUser = "username";
        // FTP server password
        private string ftpPassword = "password";
        // FTP server port
        private int ftpPort = 21;
        // FTP server URI
        private string ftpUri;

        // FTP client object
        private FtpClient ftpClient;

        // Connect to FTP server
        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                // Construct the FTP server URI
                ftpUri = $"ftp://{ftpHost}:{ftpPort}";

                // Create the FTP client object
                ftpClient = new FtpClient(ftpUri, ftpUser, ftpPassword);

                // Connect to the FTP server
                ftpClient.Connect();

                // Get the list of files and directories from the FTP server
                UpdateFileList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect to FTP server. {ex.Message}");
            }
        }

        // Get the list of files and directories from the FTP server
        private void UpdateFileList()
        {
            // Clear the list box
            lstFiles.Items.Clear();

            // Get the list of files and directories from the FTP server
            var list = ftpClient.GetListing();

            // Add the files and directories to the list box
            foreach (var item in list)
            {
                lstFiles.Items.Add(item.Name);
            }
        }

        // Upload a file to the FTP server
        private void btnUpload_Click(object sender, EventArgs e)
        {
            try
            {
                // Get the file name and path to upload
                var openFileDialog = new OpenFileDialog();
                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                // Upload the file to the FTP server
                ftpClient.Upload(openFileDialog.FileName);

                // Get the list of files and directories from the FTP server
                UpdateFileList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to upload file. {ex.Message}");
            }
        }

        // Create a directory on the FTP server
        private void btnCreateDir_Click(object sender, EventArgs e)
        {
            try
            {
                // Get the name of the directory to create
                var dirName = txtNewDirName.Text.Trim();

                // Create the directory on the FTP server
                ftpClient.CreateDirectory(dirName);

                // Get the list of files and directories from the FTP server
                UpdateFileList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create directory. {ex.Message}");
            }
        }

        // Delete a file or directory from the FTP server
        private void btnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                // Get the name of the file or directory to delete
                var fileName = lstFiles.SelectedItem.ToString();

                // Determine if the selected item is a directory or file
                var isDirectory = ftpClient.DirectoryExists(fileName);

                // Delete the directory or file from the FTP server
                if (isDirectory)
                    ftpClient.DeleteDirectory(fileName);
                else
                    ftpClient.DeleteFile(fileName);

                // Get the list of files and directories from the FTP server
                UpdateFileList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete file or directory. {ex.Message}");
            }
        }

        // Оголошуємо змінні для зберігання інформації про FTP-з'єднання
        private string ftpServerIP;
        private string ftpUserID;
        private string ftpPassword;

        // Метод для відображення файлів та каталогів FTP-сервера у ListBox
        private void GetFTPDirectory(string path)
        {
            // Створюємо з'єднання з FTP-сервером
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"ftp://{ftpServerIP}/{path}");
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            request.Credentials = new NetworkCredential(ftpUserID, ftpPassword);

            try
            {
                // Отримуємо список файлів та каталогів
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(responseStream))
                        {
                            // Очищаємо ListBox перед відображенням нових файлів та каталогів
                            listBox1.Items.Clear();

                            // Відображаємо список файлів та каталогів у ListBox
                            string line = reader.ReadLine();
                            while (line != null)
                            {
                                listBox1.Items.Add(line);
                                line = reader.ReadLine();
                            }
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                // Обробляємо помилки, які виникають під час отримання списку файлів та каталогів
                FtpWebResponse response = (FtpWebResponse)ex.Response;
                MessageBox.Show($"Error: {response.StatusCode} {response.StatusDescription}");
                response.Close();
            }
        }

        // Метод для завантаження файлу на FTP-сервер
        private void UploadFile(string localFilePath, string remoteDirectory)
        {
            // Створюємо з'єднання з FTP-сервером
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"ftp://{ftpServerIP}/{remoteDirectory}/{Path.GetFileName(localFilePath)}");
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential(ftpUserID, ftpPassword);

            try
            {
                // Завантажуємо файл на FTP-сервер
                using (FileStream fs = File.OpenRead(localFilePath))
                {
                    using (Stream ftpStream = request.GetRequestStream())
                    {
                        byte[] buffer = new byte[10240];
                        int read;
                        while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ftpStream.Write(buffer, 0, read);
                        }
                    }
                }

                // Оновлюємо список файлів та каталогів у ListBox
                GetFTPDirectory(remoteDirectory);
            }
         // Завантаження файлу на сервер
private void btnUpload_Click(object sender, EventArgs e)
            {
                if (lstFiles.SelectedItem == null)
                {
                    MessageBox.Show("Please select a file to upload.");
                    return;
                }

                // Відкриваємо вікно вибору місця збереження файлу на сервері
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "All Files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Отримуємо ім'я файлу з ListBox та створюємо об'єкт FileInfo
                    string fileName = lstFiles.SelectedItem.ToString();
                    FileInfo fileInfo = new FileInfo(fileName);

                    // Відкриваємо файл для читання та передаємо його до методу UploadFile сервера
                    using (FileStream fileStream = fileInfo.OpenRead())
                    {
                        ftpClient.UploadFile(fileStream, saveFileDialog.FileName, FtpRemoteExists.Overwrite, true);
                        RefreshFileList();
                        MessageBox.Show("File uploaded successfully.");
                    }
                }
            }

            // Створення каталогу
            private void btnMakeDir_Click(object sender, EventArgs e)
            {
                string dirName = Microsoft.VisualBasic.Interaction.InputBox("Enter the name of the new directory:", "Create Directory", "");
                if (!string.IsNullOrEmpty(dirName))
                {
                    try
                    {
                        ftpClient.CreateDirectory(dirName);
                        RefreshFileList();
                        MessageBox.Show("Directory created successfully.");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error creating directory: " + ex.Message);
                    }
                }
            }
