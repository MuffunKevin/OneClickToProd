using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using SharpSvn;
using Renci.SshNet;
using System.Security;
using System.Runtime.InteropServices;

namespace OneClickToProd
{
    class Program
    {
        private const string MySqlConnectionString = "Server={0};Database={1};Uid={2};Pwd={3};";
        private const string MySqlUpdate = "UPDATE configuration SET svnVersion = @version";
        private const string MySqlVersion = "version";
        private const string SVNCommand = "svn update --username '{0}' --password '{1}'";

        static void Main(string[] args)
        {
            try
            {
                loadConfigFile(args);

                var svnVersion = doSVNOperation();
                updateDistantServer();
                updateVersionInSQL(svnVersion);
                Console.WriteLine(Resources.UILogging.Completed);
            }
            catch (Exception ex)
            {
                var baseColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(Resources.UILogging.AnErrorOccured);
                Console.ForegroundColor = baseColor;
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.WriteLine(Resources.UILogging.PressAKey);
                Console.ReadLine();
            }
        }

        private static void loadConfigFile(string[] args)
        {
            if (args.Any(a => a.Contains("config")))
            {
                var splitter = new[] { "=" };
                var runtimeconfigfile = args.Where(a => a.Contains("config")).First().Split(splitter, StringSplitOptions.RemoveEmptyEntries).Last();

                if (!runtimeconfigfile.Contains(".config"))
                {
                    runtimeconfigfile = runtimeconfigfile + ".config";
                }

                if(System.IO.Directory.Exists("Configs")){
                    runtimeconfigfile = Environment.CurrentDirectory + "\\Configs\\" + runtimeconfigfile;
                } else if(System.IO.Directory.Exists("configs")) {
                    runtimeconfigfile = Environment.CurrentDirectory + "\\configs\\" + runtimeconfigfile;
                }

                if (System.IO.File.Exists(runtimeconfigfile))
                {
                    System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    config.AppSettings.File = runtimeconfigfile;
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("appSettings");
                }
                else
                {
                    throw new Exception(Resources.Errors.ConfigFileNotFound);
                }
            }
            else
            {
                System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (System.IO.Directory.Exists("Configs"))
                {
                    config.AppSettings.File = Environment.CurrentDirectory + "\\Configs\\base.config";
                }
                else if (System.IO.Directory.Exists("configs"))
                {
                    config.AppSettings.File = Environment.CurrentDirectory + "\\configs\\base.config";
                }
                //config.AppSettings.File = "base.config";
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");

    //                <add key="SVNSource" value="http://svn.keybookserver.com:8080/svn/photosm-dev.keybook.com/" />
    //<add key="SVNDestination" value="" />
    //<add key="SVNUserName" value="keybook" />
    //<add key="SVNCreateSVNTag" value="false" />
    //<add key="MySqlHost" value="209.171.112.120" />
    //<add key="MySqlUser" value="kbergeron" />
    //<add key="MySqlDatabase" value="photosm" />
    //<add key="SSHHost" value="209.171.112.107" />
    //<add key="SSHUser" value="photosm.keybook.com" />
            }
        }

        private static long doSVNOperation()
        {
            long svnVersion = 0;
            using (SvnClient client = new SvnClient())
            {
                var uriSource = ConfigurationManager.AppSettings[AppSettingKeys.SVNSource];

                if (uriSource.isNullOrEmpty())
                {
                    Console.WriteLine(Resources.Questions.SVNSource);
                    uriSource = Console.ReadLine();
                }

                SvnUriTarget source;
                if (SvnUriTarget.TryParse(uriSource, out source))
                {
                    var createSVNTagConfig = ConfigurationManager.AppSettings[AppSettingKeys.CreateSVNTag];

                    bool mustcreateSVNTag;
                    if (bool.TryParse(createSVNTagConfig, out mustcreateSVNTag))
                    {
                        if (mustcreateSVNTag)
                        {
                            createSVNTag(client, source);
                        }
                    }
                    svnVersion = getSVNVersion(client, source);
                }
                else
                {
                    throw new Exception(Resources.Errors.UnableToParseSVNSource);
                }
            }

            return svnVersion;
        }

        private static void createSVNTag(SvnClient client, SvnUriTarget source)
        {
            ConsoleLogStartAction(Resources.UILogging.CreateSvnTag);

            Console.WriteLine(Resources.Questions.SVNDestination);
            var uriDestination = Console.ReadLine();

            var destination = new Uri(uriDestination);
            client.RemoteCopy(source, destination);

            ConsoleEndAction();

        }

        private static long getSVNVersion(SvnClient client, SvnUriTarget source)
        {
            ConsoleLogStartAction(Resources.UILogging.GetSvnVersion);

            SvnInfoEventArgs infos;
            client.GetInfo(source, out infos);

            Console.WriteLine(string.Format(Resources.UILogging.FoundSVNVersion, infos.Revision));

            ConsoleEndAction();

            return infos.Revision;
        }

        private static void updateDistantServer()
        {
            var host = ConfigurationManager.AppSettings[AppSettingKeys.SSHHost];

            if (host.isNullOrEmpty())
            {
                Console.WriteLine(Resources.Questions.SSHAdresse);
                host = Console.ReadLine();
            }

            var userName = ConfigurationManager.AppSettings[AppSettingKeys.SSHUser];

            if (userName.isNullOrEmpty())
            {
                Console.WriteLine(Resources.Questions.SSHUser);
                userName = Console.ReadLine();
            }

            Console.WriteLine(Resources.Questions.SSHPassword);
            using (SecureString password = GetPassword())
            {
                using (SshClient client = new SshClient(host, userName, password.ConvertToUnsecureString()))
                {
                    connectSSH(client);
                    updateProject(client);
                    disconnectSSH(client);

                }
            }
        }

        private static void disconnectSSH(SshClient client)
        {
            ConsoleLogStartAction(Resources.UILogging.DisconnectSSH);

            client.Disconnect();

            ConsoleEndAction();
        }

        private static void updateProject(SshClient client)
        {
            ConsoleLogStartAction(Resources.UILogging.UpdateSVN);

            var userName = ConfigurationManager.AppSettings[AppSettingKeys.SVNUserName];

            if (userName.isNullOrEmpty())
            {
                Console.WriteLine(Resources.Questions.SVNUser);
                userName = Console.ReadLine();
            }

            Console.WriteLine(Resources.Questions.SVNPassword);
            using (SecureString password = GetPassword())
            {
                client.RunCommand(string.Format(Program.SVNCommand, userName, password.ConvertToUnsecureString()));
            }

            ConsoleEndAction();
        }

        private static void connectSSH(SshClient client)
        {
            ConsoleLogStartAction(Resources.UILogging.ConnectSSH);

            client.Connect();

            ConsoleEndAction();
        }

        private static void updateVersionInSQL(long svnVersion)
        {
            ConsoleLogStartAction(Resources.UILogging.UpdateSQL);

            var mysqlHost = ConfigurationManager.AppSettings[AppSettingKeys.MySqlHost];
            var mysqlUser = ConfigurationManager.AppSettings[AppSettingKeys.MySqlUser];
            var mysqlDatabase = ConfigurationManager.AppSettings[AppSettingKeys.MySqlDatabase];

            if (mysqlHost.isNullOrEmpty())
            {
                Console.WriteLine(Resources.Questions.MySQLHost);
                mysqlHost = Console.ReadLine();
            }

            if (mysqlUser.isNullOrEmpty())
            {
                Console.WriteLine(Resources.Questions.MySQLUser);
                mysqlUser = Console.ReadLine();
            }

            if (mysqlDatabase.isNullOrEmpty())
            {
                Console.WriteLine(Resources.Questions.MySQLDatabase);
                mysqlDatabase = Console.ReadLine();
            }

            Console.WriteLine(Resources.Questions.MySQLPassword);

            var connexionString = string.Empty;
            using (SecureString mysqlPassword = GetPassword())
            {
                connexionString = string.Format(Program.MySqlConnectionString, mysqlHost, mysqlDatabase, mysqlUser, mysqlPassword.ConvertToUnsecureString());

                using (var connexion = new MySql.Data.MySqlClient.MySqlConnection(connexionString))
                {
                    try
                    {
                        connexion.Open();
                        var command = connexion.CreateCommand();
                        command.CommandText = Program.MySqlUpdate;
                        command.Parameters.AddWithValue(Program.MySqlVersion, svnVersion);

                        command.ExecuteNonQuery();

                        connexion.Close();
                    }
                    catch (MySql.Data.MySqlClient.MySqlException ex)
                    {
                        Console.WriteLine(string.Format(Resources.Errors.MySqlUpdate, ex.Message));
                    }
                }
            }
            ConsoleEndAction();
        }

        private static void ConsoleEndAction()
        {
            Console.Write(Resources.UILogging.Done + Environment.NewLine);
        }

        private static void ConsoleLogStartAction(string p)
        {
            Console.Write(p + Resources.UILogging.Spacer + Environment.NewLine);
        }

        public static SecureString GetPassword()
        {
            SecureString pwd = new SecureString();
            while (true)
            {
                ConsoleKeyInfo i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (i.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd.RemoveAt(pwd.Length - 1);
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    pwd.AppendChar(i.KeyChar);
                    Console.Write("*");
                }
            }

            pwd.MakeReadOnly();
            Console.WriteLine();
            return pwd;
        }
    }     
}
