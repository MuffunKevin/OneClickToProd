using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using SharpSvn;
using Renci.SshNet;

namespace OneClickToProd
{
    class Program
    {
        private const string MySqlConnectionString = "Server={0};Database={1};Uid={2};Pwd={3};";
        private const string MySqlUpdate = "UPDATE configuration SET version = @version";
        private const string MySqlVersion = "version";
        private const string SVNCommand = "svn update --username '{0}' --password '{1}'";

        static void Main(string[] args)
        {
            var svnVersion = doSVNOperation();
            updateDistantServer();
            updateVersionInSQL(svnVersion);
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

                    if (bool.Parse(createSVNTagConfig))
                    {
                        createSVNTag(client, source);
                    }
                    getSVNVersion(client, source);
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
            Console.WriteLine(Resources.Questions.SVNDestination);
            var uriDestination = Console.ReadLine();

            var destination = new Uri(uriDestination);
            client.RemoteCopy(source, destination);

        }

        private static long getSVNVersion(SvnClient client, SvnUriTarget source)
        {
            SvnInfoEventArgs infos;
            client.GetInfo(source, out infos);

            return 0;
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

            if (host.isNullOrEmpty())
            {
                Console.WriteLine(Resources.Questions.SSHUser);
                userName = Console.ReadLine();
            }

            Console.WriteLine(Resources.Questions.SSHPassword);
            var password = Console.ReadLine();

            using (SshClient client = new SshClient(host, userName, password))
            {
                connectSSH(client);
                updateProject(client);

                client.Disconnect();
            }
        }

        private static void updateVersionInSQL(long svnVersion)
        {
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

            var mysqlPassword = Console.ReadLine();

            var connexionString = string.Format(Program.MySqlConnectionString, mysqlHost, mysqlDatabase, mysqlUser, mysqlPassword);

            using (var connexion = new MySql.Data.MySqlClient.MySqlConnection(connexionString))
            {
                try
                {
                    var command = connexion.CreateCommand();
                    command.CommandText = Program.MySqlUpdate;
                    command.Parameters.AddWithValue(Program.MySqlVersion, svnVersion);

                    command.ExecuteNonQuery();
                }
                catch (MySql.Data.MySqlClient.MySqlException ex)
                {
                    Console.WriteLine(string.Format(Resources.Errors.MySqlUpdate, ex.Message));
                }
            }
        }

        private static void updateProject(SshClient client)
        {
            var userName = ConfigurationManager.AppSettings[AppSettingKeys.SVNUserName];

            if (userName.isNullOrEmpty())
            {
                Console.WriteLine(Resources.Questions.SVNUser);
                userName = Console.ReadLine();
            }

            Console.WriteLine(Resources.Questions.SVNPassword);
            var password = Console.ReadLine();

            client.RunCommand(string.Format(Program.SVNCommand, userName, password));
        }

        private static void connectSSH(SshClient client)
        {
            client.Connect();
        }
    }
}
