using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpSvn;
using Renci.SshNet;

namespace OneClickToProd
{
    class Program
    {
        static void Main(string[] args)
        {
            createTag();
            Console.WriteLine("Quel est l'adresse du SSH?");
            var host = Console.ReadLine();

            Console.WriteLine("Quel est le nom d'usagé SSH?");
            var userName = Console.ReadLine();

            Console.WriteLine("Quel est le mot de passe SSH?");
            var password = Console.ReadLine();

            using (SshClient client = new SshClient(host, userName, password))
            {
                connectSSH(client);
                updateProject(client);

                client.Disconnect();
            }
            updateVersionInSQL();
        }

        private static void updateVersionInSQL()
        {
            throw new NotImplementedException();
        }

        private static void updateProject(SshClient client)
        {
            Console.WriteLine("Quel est le nom d'usager SVN?");
            var userName = Console.ReadLine();

            Console.WriteLine("Quel est le mot de passe SVN?");
            var password = Console.ReadLine();

            client.RunCommand(string.Format("svn update --username '{0}' --password '{1}'", userName, password));
        }

        private static void connectSSH(SshClient client)
        {
            client.Connect();
        }

        private static void createTag()
        {
            using (SvnClient client = new SvnClient())
            {
                Console.WriteLine("Quel est l'adresse de source du SVN?");
                var uriSource = Console.ReadLine();
                var source = new SvnUriTarget(uriSource);

                Console.WriteLine("Quel est l'adresse de destination du SVN?");
                var uriDestination = Console.ReadLine();
                var destination = new Uri(uriDestination);
                client.RemoteCopy(source, destination);
            }
        }
    }
}
