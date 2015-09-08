# OneClickToProd
One stop app to publish a new version of a web site managed by SVN and hosted on a Linux server

## How it works

1. Connects with SSH on the remote hosting server
2. Does an SVN update at the root of the server
3. Closes the SSH connection
4. (Optional) Creates a tag in the SVN server
5. Updates `configuration` table in the MySQL database to set the `version` field to the SVN revision number

## Configuration

All requested addresses and credentials are customizable in the OneClickToProd.exe.config file.

* `SVNSource` : Address of the SVN server where the project is hosted
* `SVNDestination`: Tag creation destination folder (optional if the `SVNCreateSVNTag` option is not set to `true`)
* `SVNUserName`: The SVN username used to connect
* `SVNCreateSVNTag`: true/false or empty if a tag must be created
* `MySqlHost`: The database host address
* `MySqlUser`: The database username
* `MySqlDatabase`: The database name
* `SSHHost`: The address of the SSH server
* `SSHUser`: The SSH user.

## Credits
This project is made using these 3rd party tools:

* [SSH.Net](https://sshnet.codeplex.com/) using the [NuGet version 2013.4.7](https://www.nuget.org/packages/SSH.NET/)
* [sharpsvn](https://sharpsvn.open.collab.net/servlets/ProjectProcess?documentContainer=c4__Samples)
* [MySql.Data](https://www.nuget.org/packages/MySql.Data/)
