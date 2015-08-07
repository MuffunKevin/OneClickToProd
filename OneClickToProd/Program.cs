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

        static void Main(string[] args)
        {
            long svnVersion = createTag();

            updateDistantServer();
            updateVersionInSQL(svnVersion);
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
                    command.CommandText = "UPDATE configuration SET version = @version";
                    command.Parameters.AddWithValue("version", svnVersion);

                    command.ExecuteNonQuery();
                }
                catch (MySql.Data.MySqlClient.MySqlException ex)
                {
                    Console.WriteLine("Une erreur s'est produite lors de l'update SQL:" + ex.Message);
                }                
            }
        }

        private static void updateProject(SshClient client)
        {
            var userName = ConfigurationManager.AppSettings[AppSettingKeys.SVNUserName];

            if (userName.isNullOrEmpty())
            {
                Console.WriteLine("Quel est le nom d'usager SVN?");
                userName = Console.ReadLine();
            }

            Console.WriteLine("Quel est le mot de passe SVN?");
            var password = Console.ReadLine();

            client.RunCommand(string.Format("svn update --username '{0}' --password '{1}'", userName, password));
        }

        private static void connectSSH(SshClient client)
        {
            client.Connect();
        }

        private static long createTag()
        {
            long svnVersion = 0;
            using (SvnClient client = new SvnClient())
            {
                var uriSource = ConfigurationManager.AppSettings[AppSettingKeys.SVNSource];

                if (uriSource.isNullOrEmpty())
                {
                    Console.WriteLine("Quel est l'adresse de source du SVN?");
                    uriSource = Console.ReadLine();
                }
                SvnUriTarget source;
                SvnUriTarget.TryParse(uriSource, out source);

                Console.WriteLine("Quel est l'adresse de destination du SVN?");
                var uriDestination = Console.ReadLine();
                
                var destination = new Uri(uriDestination);
                client.RemoteCopy(source, destination);

                SvnInfoEventArgs infos;
                client.GetInfo(source, out infos);
            }
            return svnVersion;
        }
    }
}
